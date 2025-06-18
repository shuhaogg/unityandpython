import sys
import time
import csv
from qasync import QEventLoop, asyncSlot
import asyncio
from PyQt5.QtWidgets import QApplication, QMainWindow
from PyQt5 import uic
from device import sEMG, My2901, IMU, EGG
from concurrent.futures import ThreadPoolExecutor
from PyQt5.QtCore import pyqtSignal
import os


class MyWindow(QMainWindow):
    sEMG_data_signal = pyqtSignal(tuple)  # 定义信号，传递 (ad1, ad2, ad3, ad4)

    def __init__(self):
        super().__init__()
        self.init_ui()

    def init_ui(self):
        self.ui = uic.loadUi('多模信息采集系统.ui')
        print(self.ui.__dict__)

        self.dev_status = [False, False, False, False, False]

        # 设备选择状态
        self.select_sEMG = self.ui.checkBox
        self.select_my2901 = self.ui.checkBox_2
        self.select_imu = self.ui.checkBox_3
        self.select_EGG = self.ui.checkBox_4
        self.select_Vis = self.ui.checkBox_5

        # 点击按钮
        self.SelectFinish = self.ui.pushButton_1
        self.impGet = self.ui.pushButton_3
        self.begain_read = self.ui.pushButton
        self.begain_record = self.ui.pushButton_4
        self.end_record = self.ui.pushButton_5
        self.end_read = self.ui.pushButton_6

        # 显示词条
        self.sEMG_show = self.ui.label_2
        self.my2901_show = self.ui.label_3
        self.imu_show = self.ui.label_4
        self.EGG_show = self.ui.label_5
        self.Vis_show = self.ui.label_6
        self.imp_show = self.ui.lineEdit
        self.name = self.ui.lineEdit_4
        self.times = self.ui.spinBox
        self.sEMG_data = self.ui.lineEdit_5

        # 输入信息
        self.sEMG_COM = self.ui.lineEdit_2
        self.my2901_COM = self.ui.lineEdit_3
        self.EGG_COM = self.ui.lineEdit_6

        # 设备选择完成并连接成功
        self.SelectFinish.clicked.connect(self.dev_select)
        self.impGet.clicked.connect(self.impCheck)

        # 对设备数据进行读取
        self.begain_record.clicked.connect(self.record_data)
        self.end_record.clicked.connect(self.end_recording)

        # 调用更新方法
        self.begain_read.clicked.connect(self.start_updata)

        self.executor = ThreadPoolExecutor(max_workers=5)

        self.end_read.clicked.connect(self.endding)

    @asyncSlot()
    async def start_updata(self):
        """在按下 'begain_read' 按钮时调用 updata 方法"""
        await self.updata()

    @asyncSlot()
    async def dev_select(self):
        """选择设备并连接"""
        self.reset_status()
        sEMG_status = self.select_sEMG.isChecked()
        my2901_status = self.select_my2901.isChecked()
        imu_status = self.select_imu.isChecked()
        EGG_status = self.select_EGG.isChecked()
        Vis_status = self.select_Vis.isChecked()

        self.dev_status = [sEMG_status, my2901_status, imu_status, EGG_status, Vis_status]

        print("设备状态：", self.dev_status)

        if self.dev_status[0]:
            if self.sEMG_COM.text() != "(COM)":
                port = self.sEMG_COM.text()
            else:
                port = 'COM6'
            self.sEMG_dev = sEMG.sEMGHandler(port)
            try:
                self.sEMG_dev.sEMG_init()
                if self.sEMG_dev.connect:
                    self.update_status(self.sEMG_show, '已连接', None, "green")
            except Exception as e:
                self.update_status(self.sEMG_show, '连接失败', e)
                print(e)
            self.sEMG_dev.on_data_received = lambda data: self.sEMG_data_signal.emit(data)

        if self.dev_status[1]:
            if self.my2901_COM.text() != "(COM)":
                port = self.my2901_COM.text()
            else:
                port = 'COM5'
            self.my2901_dev = My2901.My2901Handler(port)
            try:
                self.my2901_dev.my2901_init()
                if self.my2901_dev.connect:
                    self.update_status(self.my2901_show, '已连接', None, "green")
            except Exception as e:
                self.update_status(self.my2901_show, '连接失败', e)
                print(e)

        if self.dev_status[2]:  # IMU设备
            try:
                print('ready')
                self.update_status(self.imu_show, '连接中.', None, "orange")

                # 使用不同端口初始化两个IMU
                self.IMU1_dev = IMU.IMUHandler(
                    mac_address='F9:7C:9A:FC:3A:36',
                    imu_name='imu1',
                    zmq_port=5556  # 通道1
                )
                self.IMU2_dev = IMU.IMUHandler(
                    mac_address='FE:FD:25:A0:06:F8',
                    imu_name='imu2',
                    zmq_port=5557  # 通道2
                )

                await asyncio.gather(
                    self.IMU1_dev.imu_init(),
                    self.IMU2_dev.imu_init()
                )

                # 连接状态判断
                connected_devices = [
                    dev for dev in [self.IMU1_dev, self.IMU2_dev]
                    if dev.connect
                ]

                if len(connected_devices) == 2:
                    self.update_status(self.imu_show, '双设备已连接', None, "green")
                elif len(connected_devices) == 1:
                    self.update_status(self.imu_show, '单设备已连接', None, "orange")
                else:
                    self.update_status(self.imu_show, '连接失败', None, "red")

            except Exception as e:
                self.update_status(self.imu_show, '连接失败: ', e, "red")
                print(f"IMU连接异常: {str(e)}")

        if self.dev_status[3]:
            self.EGG_dev = EGG.EGGHandler(self.EGG_COM.text())

            try:
                self.EGG_dev.EGG_init()
                if self.EGG_dev.connect:
                    self.EGG_show.setText('已连接')
                    self.EGG_show.setStyleSheet("background-color: green;")
            except Exception as e:
                self.update_status(self.EGG_show, '连接失败', e)
                print(e)

    def reset_status(self):
        """重置设备状态"""
        devices = [self.sEMG_show, self.my2901_show, self.imu_show, self.EGG_show]
        for device in devices:
            device.setText('未连接')
            device.setStyleSheet("background-color: red;")

    def update_status(self, label, message, exception=None, color='red'):
        """更新设备连接状态"""
        if exception is None:
            label.setText(message)
            label.setStyleSheet(f"background-color: {color};")
        else:
            label.setText(f'{message}: {exception}')
            label.setStyleSheet(f"background-color: {color};")

    def impCheck(self):
        self.EGG_dev.EGG_imp()
        self.imp_show.setText(f"脑电传感器阻抗值为：{self.EGG_dev.imp_value}")

    def wrap_updata(self, func):
        """包装 IMU 更新为同步方法"""
        asyncio.run(func())

    async def updata(self):
        """使用多线程方式调用 IMU 的 updata"""
        if self.dev_status[0]:
            self.executor.submit(self.sEMG_dev.start_data_reading)
            print("sEMG数据采集已启动")  # 添加状态提示
        if self.dev_status[1]:
            self.executor.submit(self.my2901_dev.start_data_reading)

        if self.dev_status[2]:
            try:
                # 包装 IMU 更新为同步方法并提交
                # self.executor.submit(self.wrap_updata, self.IMU1_dev.updata)
                # self.executor.submit(self.wrap_updata, self.IMU2_dev.updata)
                # 正确启动异步任务
                tasks = [
                    self.IMU1_dev.updata(),
                    self.IMU2_dev.updata()
                ]
                await asyncio.gather(*tasks)
                print('IMU数据读取已启动')
            except Exception as e:
                print(f"IMU读取出错: {e}")

        if self.dev_status[3]:
            self.executor.submit(self.EGG_dev.updata)


    def record_data(self):
        self.time1 = time.time()
        if self.dev_status[0]:
            if self.sEMG_dev.sEMG_data:
                print("recording sEMG data...")
            else:
                print("something wrong on sEMG")


        if self.dev_status[1]:
            if self.my2901_dev.my2901_data:
                print('recording my2901 data...')
            else:
                print("something wrong on my2901")

        if self.dev_status[2]:
            if self.IMU1_dev.imu_data and self.IMU2_dev.imu_data:
                print('recording IMU data...')
            else:
                print("something wrong on IMU")

        if self.dev_status[3]:
            if self.EGG_dev.EGG_data:
                print('recording EGG data...')
            else:
                print("something wrong on EGG")
    def end_recording(self):
        self.time2 = time.time()
        if self.dev_status[0]:
            partial_sEMG_data = self.get_data_by_time_range(self.sEMG_dev.sEMG_data, self.time1, self.time2)

            sEMG_csv_name = f'{self.name.text()}_{self.times.text()}_sEMG.csv'
            self.save_to_csv(sEMG_csv_name, partial_sEMG_data)

        if self.dev_status[1]:
            partial_my2901_data = self.get_data_by_time_range(self.my2901_dev.my2901_data, self.time1, self.time2)

            my2901_csv_name = f'{self.name.text()}_{self.times.text()}_my2901.csv'
            self.save_to_csv(my2901_csv_name, partial_my2901_data)

        if self.dev_status[2]:
            partial_IMU1_data = self.get_data_by_time_range(self.IMU1_dev.imu_data, self.time1, self.time2)
            partial_IMU2_data = self.get_data_by_time_range(self.IMU2_dev.imu_data, self.time1, self.time2)

            IMU1_csv_name = f'{self.name.text()}_{self.times.text()}_IMU1.csv'
            IMU2_csv_name = f'{self.name.text()}_{self.times.text()}_IMU2.csv'

            self.save_to_csv(IMU1_csv_name, partial_IMU1_data)
            self.save_to_csv(IMU2_csv_name, partial_IMU2_data)

        if self.dev_status[3]:
            partial_EGG_data = self.get_data_by_time_range(self.EGG_dev.EGG_data, self.time1, self.time2)
            # print(self.EGG_dev.EGG_data)
            EGG_csv_name = f'{self.name.text()}_{self.times.text()}_EGG.csv'
            # print(EGG_csv_name)

            self.save_to_csv(EGG_csv_name, partial_EGG_data)

    def get_data_by_time_range(self, data_list, start_time, end_time):
        """根据时间范围提取数据"""
        return [item for item in data_list if start_time <= item['Time'] <= end_time]

    def save_to_csv(self, csv_name, data):
        """
        保存数据到 CSV 文件中，文件保存在 record_data 文件夹下以 self.name.text() 命名的子文件夹中。

        :param csv_name: str, 保存的 CSV 文件名
        :param data: list, 包含字典的列表数据
        """
        if not data:
            print("列表为空，无数据保存。")
            return

        # 创建 record_data 文件夹路径
        folder_path = os.path.join(os.getcwd(), 'record_data')
        os.makedirs(folder_path, exist_ok=True)  # 如果文件夹不存在则创建

        # 创建子文件夹路径，以 self.name.text() 作为文件夹名称
        subfolder_path = os.path.join(folder_path, self.name.text())
        os.makedirs(subfolder_path, exist_ok=True)

        # 构造完整 CSV 文件路径
        file_path = os.path.join(subfolder_path, csv_name)

        # 获取所有键作为表头
        fieldnames = data[0].keys()

        try:
            with open(file_path, mode='w', newline='', encoding='utf-8') as file:
                writer = csv.DictWriter(file, fieldnames=fieldnames)
                writer.writeheader()  # 写入表头
                writer.writerows(data)  # 写入数据
            print(f"数据成功保存到 {file_path}")
        except Exception as e:
            print(f"保存 CSV 文件时出错: {e}")



    def endding(self):
        if self.dev_status[0]:
            self.executor.submit(self.sEMG_dev.stop_data_reading())
        if self.dev_status[1]:
            self.executor.submit(self.my2901_dev.stop_data_reading())
        if self.dev_status[2]:
            try:
                # 包装 IMU 更新为同步方法并提交
                self.executor.submit(self.wrap_updata, self.IMU1_dev.updata)
                self.executor.submit(self.wrap_updata, self.IMU2_dev.updata)
            except Exception as e:
                print(f"IMU读取出错: {e}")

        if self.dev_status[3]:
            print('心电输出')
            # self.executor.submit(self.EGG_dev.update_EGG_data)

        self.select_sEMG.setCheckState(False)
        self.select_my2901.setCheckState(False)
        self.select_imu.setCheckState(False)
        self.select_EGG.setCheckState(False)
        self.select_Vis.setCheckState(False)

        self.reset_status()



if __name__ == '__main__':
    app = QApplication(sys.argv)

    loop = QEventLoop(app)
    asyncio.set_event_loop(loop)  # 设置事件循环

    w = MyWindow()
    w.ui.show()

    with loop:
        loop.run_forever()

    app.exec()
