import threading
from datetime import datetime
from bleak import BleakClient
import asyncio
import csv
import asyncio
import time
import pandas as pd
import zmq
import json
from threading import Thread

class IMUHandler:
    TempBytes = []
    def __init__(self, mac_address, imu_name, zmq_port=5556):
        self.zmq_port = zmq_port
        self.mac_address = mac_address
        self.imu_name = imu_name
        self.ble_device = None
        self.connect = False
        self.csv_file = None
        self.time = 0
        self.lock = threading.Lock()  # 确保线程安全
        self.client = None
        self.is_running = False  # 新增运行状态标志
        self.send_interval = 0.02
        self.last_send_time = 0
        self.notify_uuid = "0000ffe4-0000-1000-8000-00805f9a34fb"
        self.writer_characteristic = None
        self.deviceData = {}
        self.isOpen = False
        self.imu_data = []

        # 新增ZMQ配置
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.PUB)
        self.socket.setsockopt(zmq.LINGER, 0)

        try:
            self.socket.bind(f"tcp://*:{self.zmq_port}")
            print(f"{self.imu_name} 绑定端口 {self.zmq_port} 成功")
        except zmq.ZMQError as e:
            print(f"{self.imu_name} 端口绑定失败: {e}")
            raise

    async def imu_init(self):
        try:
            self.client = BleakClient(self.mac_address)
            await self.client.connect(timeout=10.0)
            self.is_connected = self.client.is_connected

            if self.is_connected:
                print(f"{self.imu_name} 蓝牙连接成功")
                self.is_running = True  # 关键修复：启用运行标志
            else:
                print(f"{self.imu_name} 蓝牙连接失败")

        except Exception as e:
            print(f"{self.imu_name} 初始化错误: {e}")
            raise

    async def updata(self):
        if not self.is_connected:
            return

        await self.client.start_notify(self.notify_uuid, self.onDataReceived)
        print(f"{self.imu_name} 开始数据接收循环")  # 调试日志
        try:
            while self.is_running:
                await asyncio.sleep(self.send_interval)
                self.send_imu_data()
        except Exception as e:
            print(f"{self.imu_name} 数据循环异常: {e}")
        finally:
            await self.client.stop_notify(self.notify_uuid)

    def send_imu_data(self):
        current_time = time.time()
        if current_time - self.last_send_time < self.send_interval:
            return
        try:
            data = {
                "AccX": self.deviceData.get("AccX", 0),
                "AccY": self.deviceData.get("AccY", 0),
                "AccZ": self.deviceData.get("AccZ", 0),
                "AsX": self.deviceData.get("AsX", 0),
                "AsY": self.deviceData.get("AsY", 0),
                "AsZ": self.deviceData.get("AsZ", 0),
                "AngX": self.deviceData.get("AngX", 0),
                "AngY": self.deviceData.get("AngY", 0),
                "AngZ": self.deviceData.get("AngZ", 0),
                "time": current_time
            }
            print(f"[{self.imu_name}] 准备发送数据到端口 {self.zmq_port}: {str(data)[:60]}...")  # 显示前60字符
            start_time = time.perf_counter()
            self.socket.send_multipart([
                b"imu",
                json.dumps(data).encode('utf-8')
            ])
            self.last_send_time = current_time
        except Exception as e:
            print(f"[{self.imu_name}] 发送失败: {str(e)}")

    def onDataReceived(self, sender, data):
        tempdata = bytes.fromhex(data.hex())
        self.time = time.time()

        for var in tempdata:
            self.TempBytes.append(var)

            if len(self.TempBytes) == 1 and self.TempBytes[0] != 0x55:
                del self.TempBytes[0]
                continue
            if len(self.TempBytes) == 2 and (self.TempBytes[1] != 0x61 and self.TempBytes[1] != 0x71):
                del self.TempBytes[0]
                continue
            if len(self.TempBytes) == 20:
                self.processData(self.TempBytes)
                self.TempBytes.clear()

    # 数据解析 data analysis
    def processData(self, Bytes):
        if Bytes[1] == 0x61:
            Ax = self.getSignInt16(Bytes[3] << 8 | Bytes[2]) / 32768 * 16
            Ay = self.getSignInt16(Bytes[5] << 8 | Bytes[4]) / 32768 * 16
            Az = self.getSignInt16(Bytes[7] << 8 | Bytes[6]) / 32768 * 16
            Gx = self.getSignInt16(Bytes[9] << 8 | Bytes[8]) / 32768 * 2000
            Gy = self.getSignInt16(Bytes[11] << 8 | Bytes[10]) / 32768 * 2000
            Gz = self.getSignInt16(Bytes[13] << 8 | Bytes[12]) / 32768 * 2000
            AngX = self.getSignInt16(Bytes[15] << 8 | Bytes[14]) / 32768 * 180
            AngY = self.getSignInt16(Bytes[17] << 8 | Bytes[16]) / 32768 * 180
            AngZ = self.getSignInt16(Bytes[19] << 8 | Bytes[18]) / 32768 * 180

            self.set("AccX", round(Ax, 3))      # round()将数字保留至后3位
            self.set("AccY", round(Ay, 3))
            self.set("AccZ", round(Az, 3))
            self.set("AsX", round(Gx, 3))
            self.set("AsY", round(Gy, 3))
            self.set("AsZ", round(Gz, 3))
            self.set("AngX", round(AngX, 3))
            self.set("AngY", round(AngY, 3))
            self.set("AngZ", round(AngZ, 3))

            self.callback_method()      # 传递当前实例

            def __del__(self):
                self.is_running = False
                if self.socket:
                    self.socket.close()
                if self.context:
                    self.context.term()

    @staticmethod
    def getSignInt16(num):
        if num >= pow(2, 15):
            num -= pow(2, 16)
        return num

    def set(self, key, value):
        # 将设备数据存到键值 Saving device data to key values
        self.deviceData[key] = value

    def callback_method(self):
        updatedDeviceData = {'Time': self.time, **self.deviceData}
        self.imu_data.append(updatedDeviceData)
# if __name__ == '__main__':
#     imu1 = IMUHandler("F9:7C:9A:FC:3A:36", "IMU1")
#
#
#     # 创建一个asyncio任务并运行
#     async def main():
#         await imu1.imu_init()
#
#         await imu1.updata()
#
#
#     asyncio.run(main())













"""原来"""
# import threading
# from datetime import datetime
# from bleak import BleakClient
# import asyncio
# import csv
# import asyncio
# import time
# import pandas as pd
#
# class IMUHandler:
#     TempBytes = []
#     def __init__(self, mac_address, imu_name):
#         self.mac_address = mac_address
#         self.imu_name = imu_name
#         self.ble_device = None
#         self.connect = False
#         self.csv_file = None
#         self.time = 0
#         self.lock = threading.Lock()  # 确保线程安全
#         self.client = None
#         self.notify_characteristic = "0000ffe4-0000-1000-8000-00805f9a34fb"
#         self.writer_characteristic = None
#         self.deviceData = {}
#         self.co = False
#         self.imu_data = []
#     async def imu_init(self):
#         try:
#             self.client = BleakClient(self.mac_address)
#             print(self.client)
#             await asyncio.wait_for(self.client.connect(), timeout=10)
#             if self.client.is_connected:
#                 print("Connected to device")
#                 self.isOpen = True
#                 self.connect = True
#                 print(time.time())
#             else:
#                 print("Failed to connect")
#         except Exception as e:
#             print(f"发生错误：{e}")
#
#     async def updata(self):
#         if self.client and self.client.is_connected:
#             print("IMU reading...")
#             await self.client.start_notify(self.notify_characteristic, self.onDataReceived)
#             try:
#                 while self.isOpen:  # 持续运行直到 isOpen 变为 False
#                     await asyncio.sleep(1)  # 保持事件循环运行
#             except asyncio.CancelledError:
#                 print("Update task cancelled")
#             finally:
#                 # 在退出时停止通知
#                 await self.client.stop_notify(self.notify_characteristic)
#                 await self.EndConnect()  # 确保任务结束时断开设备
#                 print("Stopped notification")
#         else:
#             print("Client is not connected")
#
#     async def EndConnect(self):
#         """断开 BLE 设备连接"""
#         if self.client and self.client.is_connected:
#             print("Disconnecting IMU...")
#             self.isOpen = False  # 停止数据接收循环
#             try:
#                 await self.client.stop_notify(self.notify_characteristic)
#             except Exception as e:
#                 print(f"停止通知时出错: {e}")
#
#             await self.client.EndConnect()
#             self.connect = False
#             print("IMU Disconnected.")
#
#     def onDataReceived(self, sender, data):
#         tempdata = bytes.fromhex(data.hex())
#         self.time = time.time()
#
#         for var in tempdata:
#             self.TempBytes.append(var)
#
#             if len(self.TempBytes) == 1 and self.TempBytes[0] != 0x55:
#                 del self.TempBytes[0]
#                 continue
#             if len(self.TempBytes) == 2 and (self.TempBytes[1] != 0x61 and self.TempBytes[1] != 0x71):
#                 del self.TempBytes[0]
#                 continue
#             if len(self.TempBytes) == 20:
#                 self.processData(self.TempBytes)
#                 self.TempBytes.clear()
#
#     # 数据解析 data analysis
#     def processData(self, Bytes):
#         if Bytes[1] == 0x61:
#             Ax = self.getSignInt16(Bytes[3] << 8 | Bytes[2]) / 32768 * 16
#             Ay = self.getSignInt16(Bytes[5] << 8 | Bytes[4]) / 32768 * 16
#             Az = self.getSignInt16(Bytes[7] << 8 | Bytes[6]) / 32768 * 16
#             Gx = self.getSignInt16(Bytes[9] << 8 | Bytes[8]) / 32768 * 2000
#             Gy = self.getSignInt16(Bytes[11] << 8 | Bytes[10]) / 32768 * 2000
#             Gz = self.getSignInt16(Bytes[13] << 8 | Bytes[12]) / 32768 * 2000
#             AngX = self.getSignInt16(Bytes[15] << 8 | Bytes[14]) / 32768 * 180
#             AngY = self.getSignInt16(Bytes[17] << 8 | Bytes[16]) / 32768 * 180
#             AngZ = self.getSignInt16(Bytes[19] << 8 | Bytes[18]) / 32768 * 180
#
#             self.set("AccX", round(Ax, 3))      # round()将数字保留至后3位
#             self.set("AccY", round(Ay, 3))
#             self.set("AccZ", round(Az, 3))
#             self.set("AsX", round(Gx, 3))
#             self.set("AsY", round(Gy, 3))
#             self.set("AsZ", round(Gz, 3))
#             self.set("AngX", round(AngX, 3))
#             self.set("AngY", round(AngY, 3))
#             self.set("AngZ", round(AngZ, 3))
#
#             self.callback_method()      # 传递当前实例
#
#     @staticmethod
#     def getSignInt16(num):
#         if num >= pow(2, 15):
#             num -= pow(2, 16)
#         return num
#
#     def set(self, key, value):
#         # 将设备数据存到键值 Saving device data to key values
#         self.deviceData[key] = value
#
#     def callback_method(self):
#         updatedDeviceData = {'Time': self.time, **self.deviceData}
#         self.imu_data.append(updatedDeviceData)
#
#
#
# if __name__ == '__main__':
#     imu1 = IMUHandler("F9:7C:9A:FC:3A:36", "IMU1")
#
#
#     # 创建一个asyncio任务并运行
#     async def main():
#         await imu1.imu_init()
#
#         await imu1.updata()
#
#
#     asyncio.run(main())