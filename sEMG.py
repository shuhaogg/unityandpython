import time
import serial
import zmq
import json
from threading import Thread
import sys


class sEMGHandler:
    def __init__(self, port, zmq_address="tcp://*:5558"):
        # 串口配置
        self.port = port
        self.baudrate = 115200
        self.arduino = None
        self.running = False
        self.connect = False

        # ZMQ配置
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.PUB)
        self.socket.setsockopt(zmq.SNDHWM, 20)
        self.socket.bind(zmq_address)
        print(f"▶ ZMQ服务器已启动 | 地址: {zmq_address}")

    def sEMG_init(self):
        """增强型串口初始化"""
        try:
            print(f"\n=== 正在初始化sEMG @ {self.port} ===")
            print(f"波特率: {self.baudrate}")
            print(f"超时设置: 1秒")

            self.arduino = serial.Serial(
                port=self.port,
                baudrate=self.baudrate,
                timeout=1,
                parity=serial.PARITY_NONE,
                stopbits=serial.STOPBITS_ONE
            )

            if not self.arduino.is_open:
                raise Exception("物理连接失败")

            time.sleep(2)
            self.connect = True
            print(f"✔ 串口已打开 | 状态: {self.arduino.is_open}")
            print(f"✔ 流控: RTS={self.arduino.rtscts} DTR={self.arduino.dtr}\n")

        except Exception as e:
            print(f"\n✖ 初始化失败: {str(e)}")
            if 'access denied' in str(e).lower():
                print("可能原因: 串口被其他程序占用")
            self.connect = False

    def start_data_reading(self):
        """启动数据采集线程"""
        if not self.connect:
            print("错误：未连接到传感器")
            return

        self.running = True
        Thread(target=self._reading_loop, daemon=True).start()
        print("▶ 开始采集数据...")

    def _reading_loop(self):
        """数据采集主循环（带详细诊断）"""
        self.arduino.reset_input_buffer()
        print("清空串口缓冲区完成")

        while self.running:
            try:
                # 读取原始数据
                raw_data = self.arduino.readline()
                if not raw_data:
                    print("收到空数据包，可能超时")
                    continue

                # 诊断输出
                # print(f"\n原始字节: {raw_data}")
                # print(f"HEX格式: {raw_data.hex()}")

                # 解析数据
                parsed = self._parse_data(raw_data)
                if parsed:
                    self._process_and_send(parsed)
                else:
                    print("✖ 解析失败，数据格式不符")

            except Exception as e:
                print(f"⚠ 循环异常: {str(e)}")
                self.running = False

    def _parse_data(self, raw_data):
        """增强型数据解析"""
        try:
            # 尝试多种编码方式
            decoded = raw_data.decode('ascii', errors='replace').strip()
            # print(f"ASCII解码: {decoded}")

            # 尝试GBK编码
            if '\\x' in decoded:
                decoded = raw_data.decode('gbk', errors='replace').strip()
                print(f"GBK解码: {decoded}")

            # 支持两种数据格式
            if decoded.startswith("sEMG"):
                parts = decoded.split(',')[1:5]  # sEMG,ad1,ad2,ad3,ad4...
            else:
                parts = decoded.split(',')[0:4]  # ad1,ad2,ad3,ad4...

            # 验证数据完整性
            if len(parts) < 4:
                print(f"数据字段不足: 需要4个，实际收到{len(parts)}个")
                return None

            return [int(float(p)) for p in parts[:4]]  # 处理小数情况

        except UnicodeDecodeError:
            print("UTF-8解码失败，尝试Latin-1编码")
            decoded = raw_data.decode('latin-1').strip()
            print(f"Latin-1解码: {decoded}")
            return None

        except Exception as e:
            print(f"解析异常: {str(e)}")
            return None

    def _process_and_send(self, channel_values):
        """数据处理与发送"""
        try:
            ad1, ad2, ad3, ad4 = channel_values
            data_packet = {
                "timestamp": time.time(),
                "ad1": ad1,
                "ad2": ad2,
                "ad3": ad3,
                "ad4": ad4
            }

            # 打印发送数据
            # print("\n--------------------------------")
            # print("成功解析数据包:")
            # print(json.dumps(data_packet, indent=2))
            print("--------------------------------\n")

            # ZMQ发送
            self.socket.send_string("sEMG", zmq.SNDMORE)
            self.socket.send_json(data_packet)

        except zmq.ZMQError as e:
            print(f"ZMQ发送失败: {str(e)}")
        except Exception as e:
            print(f"数据处理异常: {str(e)}")

    def stop_data_reading(self):
        """安全停止采集"""
        self.running = False
        if self.arduino and self.arduino.is_open:
            self.arduino.close()
        self.socket.close()
        self.context.term()
        print("▼ 所有连接已安全关闭")


# 使用示例
if __name__ == "__main__":
    collector = sEMGHandler(port='COM6')
    collector.sEMG_init()

    if collector.connect:
        collector.start_data_reading()

        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("\n用户中断...")
            collector.stop_data_reading()














"""原来"""
# import time
# import serial
# import datetime
# import csv
# import pandas as pd
#
#
# class sEMGHandler:
#     def __init__(self, port):
#         self.seleting = False
#         self.port = port
#         self.connect = False
#         self.arduino = None
#         self.running = False
#         self.on_data_received = None  # 回调函数
#         self.start_time = None
#         self.sEMG_data = []  # 存储ad1, ad2, ad3, ad4的数据
#         self.x_data1 = []  # 存储时间戳
#         self.df = pd.DataFrame(columns=['Time', 'ad1', 'ad2', 'ad3', 'ad4'])
#
#     def sEMG_init(self):
#         """初始化sEMG串口"""
#         self.arduino = serial.Serial(self.port, 115200, timeout=1)
#         print('sEMG connected!')
#         self.connect = True
#         time.sleep(2)  # 稳定串口
#
#
#     def start_data_reading(self):
#         """启动数据采集"""
#         self.running = True
#         self.start_time = time.time()
#         self.update_sEMG_data()
#
#     def stop_data_reading(self):
#         """断开连接"""
#         self.running = False
#         if self.arduino and self.arduino.is_open:
#             self.arduino.close()
#         print("sEMG串口已关闭")
#
#     def update_sEMG_data(self):
#
#         self.arduino.reset_input_buffer()  # 清空串口输入缓冲区
#         print('肌电传感器缓冲区清理完毕，开始读取肌电数据')
#         """实时更新sEMG数据"""
#         while self.running:
#             try:
#                 wd = self.arduino.readline()
#                 ad = self.parse_sEMG_data(wd)
#                 if ad:
#                     ad1, ad2, ad3, ad4 = ad
#                     current_time = time.time()
#                     sEMG_dict = {'Time': current_time, 'ad1': ad1, 'ad2': ad2, 'ad3': ad3, 'ad4': ad4}
#                     self.sEMG_data.append(sEMG_dict)
#                     # print(sEMG_dict)
#                     # # print(current_time,ad)
#             except serial.SerialException:
#                 pass
#
#     def parse_sEMG_data(self, sEmg_data):
#         """解析sEMG数据"""
#         ad = sEmg_data.decode('ascii', errors='ignore')  # 得到字符串
#         fd = ad.split(',')
#         if len(fd) == 7:
#             try:
#                 ad1 = int(fd[1])
#                 ad2 = int(fd[2])
#                 ad3 = int(fd[3])
#                 ad4 = int(fd[4])
#                 return ad1, ad2, ad3, ad4
#             except (IndexError, ValueError) as e:
#                 print(f"Error parsing data: {e}, Data: {fd}")
#                 return None
#         return None
#
#
#     def save_dict_to_csv(self, data_dict, csv_file):
#         """保存数据至CSV"""
#         try:
#             with open(csv_file, mode='x', newline='') as file:
#                 writer = csv.writer(file)
#                 # 写入表头（根据字典的键）
#                 writer.writerow(data_dict.keys())
#         except FileExistsError:
#             pass  # 文件已存在，则不需重新写入表头
#
#         # 追加写入数据行
#         with open(csv_file, mode='a', newline='') as file:
#             writer = csv.writer(file)
#             writer.writerow(data_dict.values())  # 写入数据行
#
