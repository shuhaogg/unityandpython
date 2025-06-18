from eConEXG import iRecorder
import time
import pandas as pd


class EGGHandler():
    def __init__(self, name):
        self.dev_type = name
        self.connect = False
        self.dev = iRecorder(dev_type=self.dev_type)
        self.EGG_data = []
        print(f'[EGG:]{self.dev.get_dev_info()}')  # 查看设备的基本信息

    def EGG_init(self):
        # 设置采集频率
        self.dev.set_frequency(1000)
        print(f'[EGG:]{iRecorder.get_available_frequency(dev_type=self.dev_type)}')
        # 搜索设备
        self.dev.find_devs()
        while True:
            ret = self.dev.get_devs()  # 搜索设备
            time.sleep(0.5)
            if ret:
                break
        print(f"[EGG:]Devs: {ret}")
        print('EGG device connected!')

        # 连接设备
        self.dev.connect_device(ret[0])

        self.connect = True

    def EGG_imp(self):
        self.dev.start_acquisition_impedance()
        time.sleep(5)
        imp_values = self.dev.get_impedance()
        if imp_values is not None:
            self.imp_value = imp_values
            print(f'阻抗值为：{self.imp_value}')
        else:
            print('阻抗值不可用，请重试')


    def updata(self):
        self.dev.start_acquisition_data(with_q=True)

        while True:
            raw_data = self.dev.get_data(timeout=0.01)
            try:

                # 假设 raw_data 是列表，每行代表一组多通道数据
                if raw_data:
                    for row in raw_data:
                        # new_row = {"Time": time.time(), "Channel1": row[0], "Channel2": row[1], "Channel3": row[2],
                        #            "Channel4": row[3], "Channel5": row[4], "Channel6": row[5], "Channel7": row[6],
                        #            "Channel8": row[7]}
                        new_row = {"Time": time.time(), "ad1": row[0], "ad2": row[1], "ad3": row[2],
                                   "ad4": row[3]}
                        self.EGG_data.append(new_row)
                        # print(new_row)

            except Exception as e:
                continue





if __name__ == '__main__':
    EGG_dev = EGGHandler('USB8')
    EGG_dev.EGG_init()
    EGG_dev.updata()