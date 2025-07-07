import time
import serial
import datetime
import csv
import zmq  # 新增ZMQ库
import msgpack  # 新增高效序列化库
from collections import deque


class My2901Handler:
    def __init__(self, port, zmq_address="tcp://*:5559"):  # 降低发送频率
        self.port = port
        self.command = b'\xff\x82\x00\x00\x0A\x00\x00\x00\x00\x00\x00\x74\n'
        self.ser = None
        self.running = False
        self.connect = False
        self.my2901_data = []
        self.start_time = time.time()

        # ZMQ配置
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.PUB)
        self.socket.setsockopt(zmq.SNDHWM, 20)
        self.socket.setsockopt(zmq.LINGER, 0)
        self.socket.bind(zmq_address)
        self.last_send_time = 0

    def my2901_init(self):
        """初始化串口，准备数据采集"""
        try:
            self.ser = serial.Serial(self.port, 115200, timeout=1)
            print('MY2901 Ready')
            self.ser.write(self.command)
            self.connect = True
            time.sleep(2)
            print('My2901 connected!')
        except serial.SerialException as e:
            print(f"串口打开失败: {e}")

    def start_data_reading(self):
        """启动数据采集"""
        self.running = True
        self.start_time = time.time()
        self.update_my2901_data()

    def stop_data_reading(self):
        """停止数据采集"""
        self.running = False
        if self.ser and self.ser.is_open:
            self.ser.close()
        # ========== 新增ZMQ清理 ==========
        self.socket.close()
        self.context.term()
        print("sEMG串口和ZMQ资源已释放")

    def update_my2901_data(self):
        """实时更新并发送数据"""
        self.ser.reset_input_buffer()
        print('开始读取压力传感器数据...')

        while self.running:
            try:
                if self.ser.is_open:
                    data = self.ser.read(20)
                    if data:
                        sensors = self.parse_my2901_data(data)
                        if sensors:
                            self.send_to_unity(*sensors)
            except serial.SerialException:
                print("串口异常!")
                self.running = False

    def send_to_unity(self, s2, s3, s6, s7):
        """发送四通道数据"""
        data = {
            "sensor2": s2,
            "sensor3": s3,
            "sensor6": s6,
            "sensor7": s7,
            "time": time.time()
        }
        try:
            self.socket.send_string("sensors", zmq.SNDMORE)  # 主题标识
            self.socket.send_json(data)
        except zmq.ZMQError as e:
            print(f"发送失败: {e}")



    # parse_my2901_data和save_dict_to_csv方法保持不变
    def parse_my2901_data(self, data):
        """解析 传感器 数据"""
        if len(data) < 20:
            return None
        try:
            return (
                int(str(data[7]), 16) * 100 + int(str(data[8]), 16),  # sensor2
                int(str(data[9]), 16) * 100 + int(str(data[10]), 16),  # sensor3
                int(str(data[15]), 16) * 100 + int(str(data[16]), 16),  # sensor6
                int(str(data[17]), 16) * 100 + int(str(data[18]), 16)  # sensor7
            )
        except (IndexError, ValueError):
            return None

        # ========== 发送数据到Unity ==========
        #                 self.send_to_unity(sensor2, current_time)
        #                 # 保存到CSV（可选）
        #                 my2901_dict = {
        #                     'Time': current_time,
        #                     'sensor2': sensor2,
        #                     'sensor3': sensors[1],
        #                     'sensor6': sensors[2],
        #                     'sensor7': sensors[3]
        #                 }
        #                 self.my2901_data.append(my2901_dict)
        #                 # self.save_dict_to_csv(my2901_dict, f'{self.today_date}_my2901_data.csv')
        # except serial.SerialException:
        #     print("Serial exception occurred.")
        #     self.running = False









"""原来"""
        # try:
        #     data_7 = int(str(data[7]), 16)
        #     data_8 = int(str(data[8]), 16)
        #     data_9 = int(str(data[9]), 16)
        #     data_10 = int(str(data[10]), 16)
        #     data_15 = int(str(data[15]), 16)
        #     data_16 = int(str(data[16]), 16)
        #     data_17 = int(str(data[17]), 16)
        #     data_18 = int(str(data[18]), 16)
        #
        #     # 获得AD值
        #     sensor2 = data_7 * 100 + data_8
        #     sensor3 = data_9 * 100 + data_10
        #     sensor6 = data_15 * 100 + data_16
        #     sensor7 = data_17 * 100 + data_18
        #
        #     return sensor2, sensor3, sensor6, sensor7
        # except (IndexError, ValueError):
        #     return None

# if __name__ == "__main__":
#     handler = My2901Handler(port="COM5")  # 修改为实际串口号
#     handler.my2901_init()
#     try:
#         handler.start_data_reading()
#     except KeyboardInterrupt:
#         handler.stop_data_reading()








# import time
# import serial
# import datetime
# import csv
#
# class My2901Handler:
#     def __init__(self, port):
#         self.port = port
#         self.command = b'\xff\x82\x00\x00\x0A\x00\x00\x00\x00\x00\x00\x74\n'
#         self.ser = None
#         self.running = False  # 控制数据采集的标志
#         self.connect = False
#         self.my2901_data = []
#         self.start_time = time.time()
#
#     def my2901_init(self):
#         """初始化串口，准备数据采集"""
#         try:
#             self.ser = serial.Serial(self.port, 115200, timeout=1)
#             print('MY2901 Ready')
#             self.ser.write(self.command)
#             self.connect = True
#             time.sleep(2)
#             print('My2901 connected!')
#
#         except serial.SerialException as e:
#             print(f"Error opening serial port: {e}")
#
#     def start_data_reading(self):
#         """启动数据采集"""
#         self.running = True
#         self.start_time = time.time()
#         self.update_my2901_data()
#
#     def stop_data_reading(self):
#         """停止数据采集"""
#         self.running = False
#         if self.ser and self.ser.is_open:
#             self.ser.close()
#         print("sEMG串口已关闭")
#
#     def update_my2901_data(self):
#         """实时更新 MY2901 数据并保存到 CSV 文件"""
#         self.ser.reset_input_buffer()  # 清空串口输入缓冲区
#         print('压力传感器缓冲区清理完毕，开始读取压力数据')
#
#         while self.running:
#             try:
#                 if self.ser.is_open:  # 确保串口是开放的
#                     data = self.ser.read(20)
#                     if data:
#                         sensors = self.parse_my2901_data(data)
#                         if sensors:
#                             sensor2, sensor3, sensor6, sensor7 = sensors
#                             current_time = time.time()
#
#                             my2901_dict = {
#                                 'Time': current_time,
#                                 'sensor2': sensor2,
#                                 'sensor3': sensor3,
#                                 'sensor6': sensor6,
#                                 'sensor7': sensor7
#                             }
#                             self.my2901_data.append(my2901_dict)
#                             # print(my2901_dict)
#                             # self.save_dict_to_csv(my2901_dict, f'{self.today_date}_my2901_data.csv')
#             except serial.SerialException:
#                 print("Serial exception occurred.")
#                 self.running = False  # 出现异常时停止数据采集
#
#     def parse_my2901_data(self, data):
#         """解析 MY2901 数据"""
#         if len(data) < 20:
#             return None
#         try:
#             data_7 = int(str(data[7]), 16)
#             data_8 = int(str(data[8]), 16)
#             data_9 = int(str(data[9]), 16)
#             data_10 = int(str(data[10]), 16)
#             data_15 = int(str(data[15]), 16)
#             data_16 = int(str(data[16]), 16)
#             data_17 = int(str(data[17]), 16)
#             data_18 = int(str(data[18]), 16)
#
#             # 获得AD值
#             sensor2 = data_7 * 100 + data_8
#             sensor3 = data_9 * 100 + data_10
#             sensor6 = data_15 * 100 + data_16
#             sensor7 = data_17 * 100 + data_18
#
#             return sensor2, sensor3, sensor6, sensor7
#         except (IndexError, ValueError):
#             return None
#
#     @staticmethod
#     def save_dict_to_csv(data_dict, csv_file):
#         """保存数据到 CSV 文件"""
#         try:
#             # 如果文件不存在，创建文件并写入表头
#             with open(csv_file, mode='x', newline='') as file:
#                 writer = csv.writer(file)
#                 writer.writerow(data_dict.keys())  # 写入表头
#         except FileExistsError:
#             pass  # 文件已存在，则不需重新写入表头
#
#         # 追加写入数据行
#         with open(csv_file, mode='a', newline='') as file:
#             writer = csv.writer(file)
#             writer.writerow(data_dict.values())  # 写入数据行