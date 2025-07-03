import sys
import time
import csv
from qasync import QEventLoop, asyncSlot
import asyncio
from PyQt5.QtWidgets import QApplication, QMainWindow
from PyQt5.QtCore import QThread, pyqtSignal
from PyQt5 import uic
from device import sEMG, My2901, IMU, EGG, Video
from concurrent.futures import ThreadPoolExecutor
import os
import pyads
from datetime import datetime


#新增导入文件
from device import fk
import zmq
import random, numpy as np
from concurrent.futures import ThreadPoolExecutor
import json



class MyWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        # ✅ 先初始化线程池，必须在调用 submit 之前
        self.executor = ThreadPoolExecutor(max_workers=7)
        self.motor_running = False
        # 初始化 UI
        self.init_ui()
        # ✅ 初始化 ZMQ 命令接收 socket
        self.command_context = zmq.Context()
        self.command_socket = self.command_context.socket(zmq.REP)
        self.command_socket.bind("tcp://*:6666")
        # ✅ 启动 Unity 命令监听线程
        self.executor.submit(self.listen_for_unity_command)

    def init_ui(self):
        self.ui = uic.loadUi('update1.ui', self)  # 绑定到self，避免界面元素无法访问
        self.setWindowTitle("Exoskeleton Control System")
        print(self.ui.__dict__)

        # —— 新增：ZeroMQ 发布 socket —— #
        context = zmq.Context()
        self.zmq_socket = context.socket(zmq.PUB)
        self.zmq_socket.bind("tcp://*:5555")

        self.motor_connect = False
        # self.executor = ThreadPoolExecutor(max_workers=7)
        self.dev_status = [False, False, False, False, False]
        self.plc = pyads.Connection('192.168.5.4.1.1', 852)
        self.plc.open()
        time.sleep(0.5)
        print('plc connect!')
        self.motor_data = []

        self.tabWidget = self.ui.tabWidget
        # 主动训练界面
        # 主动界面设备选择状态
        self.select_sEMG = self.ui.checkBox
        self.select_my2901 = self.ui.checkBox_2
        self.select_imu = self.ui.checkBox_3
        self.select_EGG = self.ui.checkBox_4
        self.select_Vis = self.ui.checkBox_5
        self.sEMG_COM = self.ui.lineEdit
        self.my2901_COM = self.ui.lineEdit_2
        self.EGG_COM = self.ui.lineEdit_3
        # 传感器选择显示
        self.sEMG_show      = self.ui.label_29
        self.my2901_show    = self.ui.label_30
        self.imu_show       = self.ui.label_31
        self.EGG_show       = self.ui.label_33
        self.Vis_show       = self.ui.label_32
        # 电机状态显示
        self.motor11 = self.ui.label_21
        self.motor12 = self.ui.label_22
        self.motor13 = self.ui.label_23
        self.motor14 = self.ui.label_24
        # 主动模式交互信息
        self.active_show = self.ui.label_34
        # 视频信息显示
        self.video_label = self.ui.label_35
        # 实验信息
        self.name = self.ui.lineEdit_8
        self.movement = self.ui.comboBox
        self.times = self.ui.spinBox
        # 主动按钮信息
        self.dev_connect = self.ui.pushButton
        self.active_record_begain = self.ui.pushButton_11
        self.movement_select_aa = self.ui.pushButton_12
        self.active_endding = self.ui.pushButton_13

        # 设备连接
        self.dev_connect.clicked.connect(self.dev_select)
        # 试验结束
        self.active_endding.clicked.connect(self.ending)
        # 动作选择
        self.movement_select_aa.clicked.connect(self.MovementSelect)
        # 开始采集
        self.active_record_begain.clicked.connect(self.BegainCollect)



        # 被动训练界面
        # 当前存在动作
        self.movement_show = self.ui.listWidget
        # 当前选择动作
        self.movement_select = self.ui.pushbutton
        self.movement_select_show = self.ui.label
        # 被动训练交互信息
        self.passive_show = self.ui.label_2
        # 动作录入
        self.movement_input = self.ui.pushButton_3
        self.movement_input_end = self.ui.pushButton_10
        # 电状态显示
        self.motor1 = self.ui.label_8
        self.motor2 = self.ui.label_9
        self.motor3 = self.ui.label_7
        self.motor4 = self.ui.label_10
        # 电机位置输入
        self.pos_motor1 = self.ui.lineEdit_4
        self.pos_motor2 = self.ui.lineEdit_5
        self.pos_motor3 = self.ui.lineEdit_6
        self.pos_motor4 = self.ui.lineEdit_7
        # 电机绝对移动
        self.ab_move = self.ui.pushButton_14
        # 被动模式 运动示教
        self.teach = self.ui.pushButton_5
        # 被动模式 回归原点
        self.zero_pos = self.ui.pushButton_15
        # 被动模式 开始
        self.pastrain_begin = self.ui.pushButton_2
        # 被动模式 结束训练
        self.passive_endding = self.ui.pushButton_4

        # 训练动作列表初始化更新
        self.FileCheck()

        # 动作选择
        if self.movement_select:
            self.movement_select.clicked.connect(self.MovementSelect)
        else:
            print("Error: self.movement_select is not valid")
        # 被动训练
        self.pastrain_begin.clicked.connect(self.PasTrain)
        # 动作录入
        self.movement_input.clicked.connect(self.TrainRecord)
        # 录入结束
        self.movement_input_end.clicked.connect(self.TrainRecordEnd)
        # 运动示教
        self.teach.clicked.connect(self.Teaching)
        # 回归原点
        self.zero_pos.clicked.connect(self.ZeroPos)
        # 电机移动
        self.ab_move.clicked.connect(self.PosMove)
        # 结束训练
        self.passive_endding.clicked.connect(self.UnableMotor)

    # 新增：向Unity发送状态信息
    def update_unity_status(self, motor_status=None, current_action=None, feedback=None):
        msg = {
            "motor_status": motor_status,
            "current_action": current_action,
            "feedback": feedback
        }
        # 过滤掉值为None的键
        msg = {k: v for k, v in msg.items() if v is not None}
        try:
            self.zmq_socket.send_string(json.dumps(msg))
            print(f"[ZMQ] 发送状态: {msg}")
        except Exception as e:
            print(f"[ZMQ] 发送状态出错: {e}")

    #新增新方法监听unity
    def listen_for_unity_command(self):
        print("[ZMQ] 命令接收线程已启动")
        while True:
            try:
                message = self.command_socket.recv_string()
                print(f"[ZMQ] 收到Unity命令: {message}")

                # 处理Unity发送的动作选择命令（核心修改）
                if message.startswith("select_action:"):
                    # 从命令中提取动作名称（如"movement2"）
                    action_name = message.split(":")[1].strip()
                    print(f"[Unity] 选择的动作名称: {action_name}")

                    # ① 找到动作名称对应的CSV文件路径
                    # 遍历self.csv_address（在FileCheck中初始化，存储了所有动作的名称和路径）
                    target_path = None
                    for name, path in self.csv_address:
                        if name == action_name:  # 匹配动作名称
                            target_path = path
                            break

                    if target_path is None:
                        # 动作不存在时的错误处理
                        error_msg = f"动作'{action_name}'不存在，请检查名称是否正确"
                        self.command_socket.send_string(error_msg)
                        self.update_unity_status(feedback=error_msg)
                        continue

                    # ② 手动设置当前选中的动作，模拟UI操作
                    # （根据当前Tab页，模拟用户在UI上选择了该动作）
                    if self.tabWidget.currentIndex() == 0:  # 被动训练Tab
                        # 模拟在listWidget中选中该动作
                        # 找到listWidget中对应的项并选中
                        for i in range(self.movement_show.count()):
                            item = self.movement_show.item(i)
                            if item.text() == action_name:
                                self.movement_show.setCurrentItem(item)
                                break
                    else:  # 主动训练Tab
                        # 模拟在comboBox中选中该动作
                        for i in range(self.movement.count()):
                            if self.movement.itemText(i) == action_name:
                                self.movement.setCurrentIndex(i)
                                break

                    # ③ 调用MovementSelect方法，加载动作数据
                    self.MovementSelect()  # 关键：执行动作加载逻辑

                    # 反馈成功信息
                    self.command_socket.send_string(f"动作'{action_name}'加载成功")
                    self.update_unity_status(
                        current_action=action_name,
                        feedback=f"动作'{action_name}'已加载"
                    )

                # 其他命令处理（保持不变）
                elif message == "start_teaching":
                    self.Teaching()
                    self.command_socket.send_string("ok")
                elif message == "record_start":
                    self.TrainRecord()
                    self.command_socket.send_string("ok")
                elif message == "record_end":
                    self.TrainRecordEnd()
                    self.command_socket.send_string("ok")
                elif message == "zero_pos":
                    self.ZeroPos()
                    self.command_socket.send_string("ok")
                elif message == "enable_motor":
                    self.EnableMotor()
                    self.command_socket.send_string("ok")
                elif message == "disable_motor":
                    self.UnableMotor()
                    self.command_socket.send_string("ok")
                elif message == "start_passive_training":  # 新增被动训练命令
                    # 检查是否已选择动作
                    if hasattr(self, 'current_action') and self.current_action:
                        self.PasTrain()
                        self.command_socket.send_string("ok")
                        self.update_unity_status(
                            current_action=self.current_action,
                            feedback="被动训练已启动"
                        )
                    else:
                        error_msg = "请先选择训练动作"
                        self.command_socket.send_string(error_msg)
                        self.update_unity_status(feedback=error_msg)
                else:
                    print("[ZMQ] 未知命令")
                    self.command_socket.send_string("unknown command")
            except Exception as e:
                print(f"[ZMQ] 命令监听异常: {e}")
                try:
                    self.command_socket.send_string("error")
                except:
                    pass  # 防止双重异常

    # 新增修改
    def __del__(self):
        if hasattr(self, 'executor'):
            self.executor.shutdown(wait=False)

    # 电机使能
    def EnableMotor(self):
        self.plc.write_by_name('MAIN_Motion_Control.ax_state', 2, pyads.PLCTYPE_INT)
        self.motor_connect = True
        self.update_status(self.passive_show,'电机完成使能',  'green')
        self.update_status(self.active_show, '电机完成使能', 'green')
        # 新增 发送状态信息
        self.update_unity_status(motor_status="已使能", feedback="电机已完成使能")
        time.sleep(1)
        self.MotorCheck()

    # 电机去使能
    def UnableMotor(self):
        self.plc.write_by_name('MAIN_Motion_Control.ax_state', 3)
        self.motor_connect = False
        self.update_status(self.passive_show, '电机完成去使能', 'red')
        self.update_status(self.active_show, '电机完成去使能', 'red')
        # 新增 发送状态信息
        self.update_unity_status(motor_status="未使能", feedback="电机已完成去使能")
        time.sleep(1)
        self.MotorCheck()

    # 电机使能状态检查
    def MotorCheck(self):
        motor_status = [False, False, False, False]
        try:
            status_list = self.plc.read_list_by_name([
                'GVL.ax_power[0].Status',
                'GVL.ax_power[1].Status',
                'GVL.ax_power[2].Status',
                'GVL.ax_power[3].Status'
            ])
            motor_status = [bool(status) for status in status_list]
        except Exception as e:
            print(f"电机状态读取出错: {e}")
        print('电机状态：', motor_status)

        # 新增 发送状态信息
        motor_status_text = "已使能" if all(motor_status) else "未使能"
        self.update_unity_status(motor_status=motor_status_text)

        if motor_status[0]:
            self.update_status(self.motor1, "已连接", 'green')
            self.update_status(self.motor11, "已连接", 'green')
        else:
            self.update_status(self.motor1, "未连接", 'red')
            self.update_status(self.motor11, "未连接", 'red')
        if motor_status[1]:
            self.update_status(self.motor2, "已连接", 'green')
            self.update_status(self.motor12, "已连接", 'green')
        else:
            self.update_status(self.motor2, "未连接", 'red')
            self.update_status(self.motor12, "未连接", 'red')
        if motor_status[2]:
            self.update_status(self.motor3, "已连接", 'green')
            self.update_status(self.motor13, "已连接", 'green')
        else:
            self.update_status(self.motor3, "未连接", 'red')
            self.update_status(self.motor13, "未连接", 'red')
        if motor_status[3]:
            self.update_status(self.motor4, "已连接", 'green')
            self.update_status(self.motor14, "已连接", 'green')
        else:
            self.update_status(self.motor4, "未连接", 'red')
            self.update_status(self.motor14, "未连接", 'red')

    # 训练动作检查初始化
    def FileCheck(self):
        self.csv_address = []
        self.folder_path = os.path.join(os.getcwd(), 'train')
        self.csv_files = [
            f[:-4] for f in os.listdir(self.folder_path)
            if f.endswith('.csv') and os.path.isfile(os.path.join(self.folder_path, f))
        ]
        for i in self.csv_files:
            add = (i,f'{self.folder_path}\\{i}.csv')
            self.csv_address.append(add)
        # print(self.csv_address)
        self.movement_show.clear()
        self.movement.clear()

        self.movement_show.addItems(self.csv_files)
        for text, data in self.csv_address:
            self.movement.addItem(text, data)

    # 被动训练
    def PasTrain(self):
        self.SetPos()
        # 创建线程读写任务
        self.pastrain_thread = PasTrainThread(self.plc)
        self.pastrain_thread.passive_train.connect(self.PTrainingStatus)
        self.pastrain_thread.start()
        self.update_status(self.passive_show, '正在进入被动训练',  'pink')

        # 新增 发送状态信息
        self.update_unity_status(
            current_action=self.current_action,
            feedback="正在进行被动训练"
        )

    def PTrainingStatus(self, message):
        if message == f"movement训练完成。":
            self.update_status(self.passive_show, message,  'green')

            # 新增发送状态信息
            self.update_unity_status(feedback="被动训练已完成")

        elif message ==f"movement正在训练！":
            self.update_status(self.passive_show, message,  'pink')
        else:
            print(message)
            message = 'something mistake in passive training'
            self.update_status(self.passive_show, message,  'red')

            # 新增发送错误状态
            self.update_unity_status(feedback=message)

    # 设置为位置模式
    def SetPos(self):
        model = self.plc.read_by_name('GVL.ax_opmoderead[0]', pyads.PLCTYPE_SINT)
        if model == 8:
            self.EnableMotor()
            self.update_status(self.passive_show, '已进入位置模式', 'blue')
            time.sleep(1)
        else:
            self.UnableMotor()
            time.sleep(0.5)
            self.plc.write_by_name('MAIN_Motion_Control.ax_state', 32)
            time.sleep(0.5)
            self.EnableMotor()

            # 新增发送状态信息
            self.update_unity_status(feedback="已进入位置模式")

    # 设置为力矩模式
    def SetTor(self):
        model = self.plc.read_by_name('GVL.ax_opmoderead[0]', pyads.PLCTYPE_SINT)
        if model == 10:
            self.EnableMotor()
            self.update_status(self.passive_show, '已进入力矩模式', 'orange')
            time.sleep(1)
        else:
            self.UnableMotor()
            time.sleep(0.5)
            self.plc.write_by_name('MAIN_Motion_Control.ax_state', 31)
            time.sleep(0.5)
            self.EnableMotor()
            self.update_status(self.passive_show, '已进入力矩模式', 'orange')

            # 新增发送状态信息
            self.update_unity_status(feedback="已进入力矩模式")

    # 位置模式下角度绝对移动
    def PosMove(self):
        # 置于位置模式
        self.SetPos()
        pos1 = self.pos_motor1.text()
        pos2 = self.pos_motor2.text()
        pos3 = self.pos_motor3.text()
        pos4 = self.pos_motor4.text()

        self.plc.write_by_name('MAIN_Motion_Control.set_pos[0]', pos1)
        self.plc.write_by_name('MAIN_Motion_Control.set_pos[1]', pos2)
        self.plc.write_by_name('MAIN_Motion_Control.set_pos[2]', pos3)
        self.plc.write_by_name('MAIN_Motion_Control.set_pos[3]', pos4)
        time.sleep(0.5)
        self.plc.write_by_name('MAIN_Motion_Control.ax_state', 11)
        self.update_status(self.passive_show, '正在移动至指定位置', 'pink')

        # 新增发送状态信息
        self.update_unity_status(
            feedback=f"正在移动至指定位置: [{pos1}, {pos2}, {pos3}, {pos4}]"
        )

    # 回归原点
    def ZeroPos(self):
        self.SetPos()
        self.plc.write_by_name('MAIN_Motion_Control.ax_state', 10)
        self.update_status(self.passive_show, '正在回归初始位置', 'pink')
        time.sleep(0.5)

        # 新增发送状态信息
        self.update_unity_status(feedback="正在回归初始位置")
        time.sleep(0.5)
        self.update_status(self.passive_show, '已回归初始位置', 'green')
        # 新增发送状态信息
        self.update_unity_status(feedback="已回归初始位置")

    # 动作录入
    def TrainRecord(self):
        self.SetTor()
        self.plc.write_by_name('MAIN_Motion_Control.ax_state', 40)
        # 摩擦力补偿
        self.plc.write_by_name('MAIN_Motion_Control.Fric_Compen_En', True)
        self.update_status(self.passive_show, '重力摩擦力补偿完成', 'orange')

        # 新增发送状态信息
        self.update_unity_status(feedback="重力摩擦力补偿完成")

        time.sleep(1)
        self.plc.write_by_name('MAIN_Data_Process.record_en', True)
        self.update_status(self.passive_show, '可进行动作录入', 'orange')

        # 新增发送状态信息
        self.update_unity_status(feedback="可进行动作录入")

    # 运动示教
    def Teaching(self):
        self.SetTor()
        self.plc.write_by_name('MAIN_Motion_Control.ax_state', 40)
        self.plc.write_by_name('MAIN_Motion_Control.Fric_Compen_En', True)
        self.update_status(self.passive_show, '重力摩擦力补偿完成', 'orange')

        # 发送状态信息
        self.update_unity_status(feedback="重力摩擦力补偿完成")

        time.sleep(1)
        ## —— 新增：启动后台“电机循环” —— #
        if not self.motor_running:
            self.motor_running = True
            self.executor.submit(self.motor_loop)
            self.update_status(self.passive_show, '开始运动示教 (随机角度)', 'green')
        #self.update_status(self.passive_show, '可进行自由运动', 'orange')

    def motor_loop(self):
        prev_pos = None
        last_sent = None
        threshold = 0.1

        while self.motor_running:
            try:
                # ① 从 PLC 读出四个电机的累计步数（或当前角度计数值）
                actpos_counts = self.plc.read_by_name(
                    'GVL.ax_actpos', pyads.PLCTYPE_ARR_LREAL(4)
                )

                # ② “步数→弧度” 的换算
                #    你需要在 device/fk.py 里实现一个 counts_array_to_angles 函数，
                #    它接收一个 4 元组的“步数”或“脉冲数”，返回 4 个关节角度（弧度）。
                joint_angles = fk.counts_array_to_angles(actpos_counts)

                # ③ 正运动学：计算末端执行器位置
                current_pos = fk.forward_kinematics(joint_angles)

                # ④ (贪吃蛇)判断方向、去抖，并通过 ZeroMQ 发出 “W/A/S/D”
                # if prev_pos is not None:
                #     dx = current_pos[0] - prev_pos[0]
                #     dy = current_pos[1] - prev_pos[1]
                #     direction = None
                #     if abs(dx) > abs(dy) and abs(dx) > threshold:
                #         direction = "D" if dx > 0 else "A"
                #     elif abs(dy) > threshold:
                #         direction = "W" if dy > 0 else "S"
                #     if direction and direction != last_sent:
                #         self.zmq_socket.send_string(direction)
                #         print(f"[真实] send: {direction}")
                #         last_sent = direction
                #
                # prev_pos = current_pos
                # ④（打地鼠） 直接发送坐标
                msg = json.dumps({
                    'x': float(current_pos[0]),
                    'y': float(current_pos[1]),
                    'z': float(current_pos[2])
                })
                self.zmq_socket.send_string(msg)

            except Exception as e:
                print("motor_loop 出错：", e)

            time.sleep(0.1)

    # 示教结束 并写入文件
    def TrainRecordEnd(self):

        self.plc.write_by_name('MAIN_Data_Process.record_end', True)
        print('运动结束')

        # 发送状态信息
        self.update_unity_status(feedback="运动示教结束，正在保存数据")

        time.sleep(1)
        # 构造完整 CSV 文件路径
        os.makedirs(self.folder_path, exist_ok=True)  # 如果文件夹不存在则创建
        folder_path = os.path.join(os.getcwd(), 'train')
        num = len([file for file in os.listdir(folder_path) if file.endswith('.csv')])
        filename = os.path.join(folder_path, f'新建动作{num + 1}.csv')
        print(filename)
        # 写入文件名
        self.plc.write_by_name('data_write.sFileName', filename, pyads.PLCTYPE_STRING)
        self.csvwrite_thread = CsvWrite(self.plc)
        self.csvwrite_thread.csv_write.connect(self.CsvwriteStatus)
        self.csvwrite_thread.start()
        self.update_status(self.passive_show, '示教结束', 'orange')

        # 选择训练动作 读取csv文件词条显示
    def MovementSelect(self):
            if self.tabWidget.currentIndex() == 0:
                n = self.movement_show.currentItem()
                # 新增
                if n is None:
                    print("未选择动作")
                    return
                print(f"{self.folder_path}\\{n.text()}.csv")
                filename = f"{self.folder_path}\\{n.text()}.csv"
                self.plc.write_by_name('data_read.sFileName', filename, pyads.PLCTYPE_STRING)
                self.update_status(self.passive_show, '文件目录写入', 'pink')
                self.update_status(self.movement_select_show, n.text(), 'white')

                # 新增更新当前动作
                self.current_action = n.text()
                # 新增发送状态信息
                self.update_unity_status(
                    current_action=self.current_action,
                    feedback="已选择训练动作"
                )

                time.sleep(0.5)
            if self.tabWidget.currentIndex() == 1:
                filename = self.movement.currentData()

                # 新增
                if filename is None:
                    print("未选择动作")
                    return

                self.plc.write_by_name('data_read.sFileName', filename, pyads.PLCTYPE_STRING)
                self.update_status(self.active_show, '训练文件正在写入', 'pink')

                # 新增更新当前动作
                self.current_action = self.movement.currentText()
                # 新增发送状态信息
                self.update_unity_status(
                    current_action=self.current_action,
                    feedback="已选择训练动作"
                )

                time.sleep(0.5)
            self.csv_reading = CsvRead(self.plc)
            self.csv_reading.csv_read.connect(self.CsvreadStatus)
            self.csv_reading.start()

        # 文件读取状态显示
    def CsvreadStatus(self, message):
            if message == "动作读取完毕。":
                self.update_status(self.passive_show, message, 'green')
                self.update_status(self.active_show, message, 'green')

                # 新增发送状态信息
                self.update_unity_status(feedback="动作数据已加载")

            elif message == "动作正在读取！":
                self.update_status(self.passive_show, message, 'pink')
                self.update_status(self.active_show, message, 'pink')
            else:
                print(message)
                message = 'something mistake in read csv'
                self.update_status(self.passive_show, message, 'red')
                self.update_status(self.active_show, message, 'red')

                # 新增发送错误状态
                self.update_unity_status(feedback=message)

        # 文件写入状态显示
    def CsvwriteStatus(self, message):
            if message == "已写入csv文件中":
                self.update_status(self.passive_show, message, 'green')
                self.FileCheck()

                # 新增 发送状态信息
                self.update_unity_status(feedback="动作数据已保存")

            elif message == "正在写入csv文件中":
                self.update_status(self.passive_show, message, 'yellow')
            else:
                print(message)
                message = 'something mistake in read csv'
                self.update_status(self.passive_show, message, 'red')

                # 新增发送错误状态
                self.update_unity_status(feedback=message)

        # 选设备并连接
    @asyncSlot()
    async def dev_select(self):
            # self.RstDevStatus()
            sEMG_status = self.select_sEMG.isChecked()
            my2901_status = self.select_my2901.isChecked()
            imu_status = self.select_imu.isChecked()
            EGG_status = self.select_EGG.isChecked()
            Vis_status = self.select_Vis.isChecked()
            self.dev_status = [sEMG_status, my2901_status, imu_status, EGG_status, Vis_status]
            connect_sit = [False, False, False, False, False]
            print("设备状态：", self.dev_status)

            # 新增发送状态信息
            self.update_unity_status(feedback="正在连接设备")

            if self.dev_status[4]:
                self.vid_dev = Video.VideoHandler()
                self.vid_dev.frame_signal.connect(self.update_frame)
                try:
                    self.vid_dev.VidInit()
                    if self.vid_dev.connect:
                        connect_sit[4] = True
                        self.update_status(self.Vis_show, '已连接', 'green')
                    else:
                        self.update_status(self.Vis_show, '未连接', 'red')
                except Exception as e:
                    self.update_status(self.Vis_show, f'error:{e}', 'red')
            if self.dev_status[0]:
                port = self.sEMG_COM.text()
                self.sEMG_dev = sEMG.sEMGHandler(port)
                try:
                    self.sEMG_dev.sEMG_init()
                    if self.sEMG_dev.connect:
                        connect_sit[0] = self.sEMG_dev.connect
                        self.update_status(self.sEMG_show, '已连接', "green")
                except Exception as e:
                    self.update_status(self.sEMG_show, f'error:{e}', 'red')
                    print(e)
                # self.sEMG_dev.on_data_received = lambda data: self.sEMG_data_signal.emit(data)
            if self.dev_status[1]:
                port = self.my2901_COM.text()
                self.my2901_dev = My2901.My2901Handler(port)
                try:
                    self.my2901_dev.my2901_init()
                    if self.my2901_dev.connect:
                        connect_sit[1] = self.my2901_dev.connect
                        self.update_status(self.my2901_show, '已连接', "green")
                except Exception as e:
                    self.update_status(self.my2901_show, f'error:{e}', 'red')
                    print(e)
            if self.dev_status[2]:
                try:
                    self.update_status(self.imu_show, '连接中.', "orange")
                    self.IMU1_dev = IMU.IMUHandler('F9:7C:9A:FC:3A:36', 'imu1')
                    self.IMU2_dev = IMU.IMUHandler('FE:FD:25:A0:06:F8', 'imu2')

                    await asyncio.gather(self.IMU1_dev.imu_init(), self.IMU2_dev.imu_init())

                    if self.IMU1_dev.connect and self.IMU2_dev.connect:
                        connect_sit[2] = True
                        self.update_status(self.imu_show, '已连接', "green")
                    else:
                        self.update_status(self.imu_show, '部分连接失败', "red")
                except asyncio.TimeoutError:
                    self.update_status(self.imu_show, '连接超时', "red")
                except Exception as e:
                    self.update_status(self.imu_show, f'error:{e}', "red")
            if self.dev_status[3]:
                self.EGG_dev = EGG.EGGHandler(self.EGG_COM.text())
                try:
                    self.EGG_dev.EGG_init()
                    if self.EGG_dev.connect:
                        connect_sit[3] = True
                        self.update_status(self.EGG_show, '已连接', "green")
                except Exception as e:
                    self.update_status(self.EGG_show, f'error:{e}', 'red')
                    print(e)

            # 设备连接状态检查

            if connect_sit == self.dev_status:
                # await self.start_updata()
                # self.EnableMotor()
                self.update_status(self.active_show, '传感器与电机设备连接成功', 'green')

                # 新增发送状态信息
                self.update_unity_status(feedback="传感器设备连接成功")

            else:
                self.update_status(self.active_show, '传感器设备连接未完成', 'red')

                # 新增发送状态信息
                self.update_unity_status(feedback="传感器设备连接未完成")

    @asyncSlot()
        # async def start_updata(self):
        # await self.updata()

    async def updata(self):
        """使用多线程方式调用 IMU 的 updata"""
        self.SetPos()
        if self.motor_connect:
            self.executor.submit(self.GetMotor)
        if self.dev_status[0]:
            self.executor.submit(self.sEMG_dev.start_data_reading)

        if self.dev_status[1]:
            self.executor.submit(self.my2901_dev.start_data_reading)
        if self.dev_status[2]:
            try:
                # 包装 IMU 更新为同步方法并提交
                self.executor.submit(self.wrap_updata, self.IMU1_dev.updata)
                self.executor.submit(self.wrap_updata, self.IMU2_dev.updata)
            except Exception as e:
                print(f"IMU读取出错: {e}")

        if self.dev_status[3]:
            self.executor.submit(self.EGG_dev.updata)
        if self.dev_status[4]:
            self.executor.submit(self.vid_dev.start())


    def GetMotor(self):
        print('电机链接成功')

        # 新增
        prev_pos = None
        last_sent = None
        threshold = 0.1  # 阈值：0.1 米

        while self.motor_connect:
            try:
                curtime = self.plc.read_by_name('GVL.curtime', pyads.PLCTYPE_STRING)
                actpos = self.plc.read_by_name('GVL.ax_actpos', pyads.PLCTYPE_ARR_LREAL(4))
                actdif = self.plc.read_by_name('GVL.ax_encoderdiff', pyads.PLCTYPE_ARR_DINT(4))
                acttor = self.plc.read_by_name('GVL.ax_ActualTorque', pyads.PLCTYPE_ARR_INT(4))
                dt = datetime.strptime(curtime, "%Y-%m-%d-%H:%M:%S.%f")
                timestamp = dt.timestamp()
                motor_dict = {'Time': timestamp,
                              'actpos0': actpos[0], 'actpos1': actpos[1], 'actpos2': actpos[2],
                              'actpos3': actpos[3],
                              'actdif0': actdif[0], 'actdif1': actdif[1], 'actdif2': actdif[2],
                              'actdif3': actdif[3],
                              'acttor0': acttor[0], 'acttor1': acttor[1], 'acttor2': acttor[2],
                              'acttor3': acttor[3],
                              }
                self.motor_data.append(motor_dict)
            except Exception as e:
                print("GetMotor 出错：", e)
            #     # 新增：步数→弧度 → FK 计算 —— #
            #     joint_angles = fk.counts_array_to_angles(actpos)
            #     current_pos = fk.forward_kinematics(joint_angles)
            #
            #     #  新增：阈值判断 + 去抖 + ZeroMQ 发送 —— #
            #     if prev_pos is not None:
            #         dx = current_pos[0] - prev_pos[0]
            #         dy = current_pos[1] - prev_pos[1]
            #         direction = None
            #         if abs(dx) > abs(dy) and abs(dx) > threshold:
            #             direction = "D" if dx > 0 else "A"
            #         elif abs(dy) > threshold:
            #             direction = "W" if dy > 0 else "S"
            #         if direction and direction != last_sent:
            #             self.zmq_socket.send_string(direction)
            #             print(f"[Python] send: {direction}")
            #             last_sent = direction
            #     prev_pos = current_pos
            #

            #
            # time.sleep(0.1)  # 每 100ms 循环一次

    # 更新视频画面

    def update_frame(self, pixmap):
        self.video_label.setPixmap(pixmap)

        # 开始采集
    def BegainCollect(self):
        # self.SetPos()
        # self.time1 = time.time()
        # print(self.time1)
        # self.active_train = PasTrainThread(self.plc)
        # self.active_train.passive_train.connect(self.ATrainingStatus)
        # self.active_train.start()

        if hasattr(self, 'vid_dev'):
            self.vid_dev.stop()
            time.sleep(0.5)  # 等待资源释放

            # 重新初始化摄像头
        self.vid_dev = Video.VideoHandler()
        self.vid_dev.frame_signal.connect(self.update_frame)
        if not self.vid_dev.VidInit():
            return  # 初始化失败直接返回

        # 启动线程
        self.vid_dev.start()
        self.time1 = time.time()


    def ATrainingStatus(self, message):
        if message == f"movement训练完成。":
            self.update_status(self.active_show, message, 'green')
        elif message == f"movement正在训练！":
            self.update_status(self.active_show, message, 'pink')
        else:
            print(message)
            message = 'something mistake in passive training'
            self.update_status(self.active_show, message, 'red')


        # def record_data(self):
        #     self.time1 = time.time()
        #     if self.dev_status[0]:
        #         if self.sEMG_dev.sEMG_data:
        #             print("recording sEMG data...")
        #         else:
        #             print("something wrong on sEMG")
        #     if self.dev_status[1]:
        #         if self.my2901_dev.my2901_data:
        #             print('recording my2901 data...')
        #         else:
        #             print("something wrong on my2901")
        #     if self.dev_status[2]:
        #         if self.IMU1_dev.imu_data and self.IMU2_dev.imu_data:
        #             print('recording IMU data...')
        #         else:
        #             print("something wrong on IMU")
        #     if self.dev_status[3]:
        #         if self.EGG_dev.EGG_data:
        #             print('recording EGG data...')
        #         else:
        #             print("something wrong on EGG")
        #     if self.dev_status[4]:
        #         if self.vid_dev.video_data:
        #             print('recording Video data...')
        #         else:
        #             print("something wrong on Video")

    def SaveCsv(self):
        print('保存至csv文件中')
        motor_csv_name = f'{self.name.text()}_{self.movement.currentText()}_{self.times.text()}_motor.csv'
        print(motor_csv_name)
        partial_motor_data = self.get_data_by_time_range(self.motor_data, self.time1, self.active_train.time2)
        self.save_to_csv(motor_csv_name, partial_motor_data)
        self.motor_data.clear()
        if self.dev_status[0]:
            partial_sEMG_data = self.get_data_by_time_range(self.sEMG_dev.sEMG_data, self.time1,
                                                            self.active_train.time2)
            sEMG_csv_name = f'{self.name.text()}_{self.movement.currentText()}_{self.times.text()}_sEMG.csv'
            self.save_to_csv(sEMG_csv_name, partial_sEMG_data)
            self.sEMG_dev.sEMG_data.clear()

        if self.dev_status[1]:
            partial_my2901_data = self.get_data_by_time_range(self.my2901_dev.my2901_data, self.time1,
                                                              self.active_train.time2)
            my2901_csv_name = f'{self.name.text()}_{self.movement.currentText()}_{self.times.text()}_my2901.csv'
            self.save_to_csv(my2901_csv_name, partial_my2901_data)
            self.my2901_dev.my2901_data.clear()
        if self.dev_status[2]:
            partial_IMU1_data = self.get_data_by_time_range(self.IMU1_dev.imu_data, self.time1,
                                                            self.active_train.time2)
            partial_IMU2_data = self.get_data_by_time_range(self.IMU2_dev.imu_data, self.time1,
                                                            self.active_train.time2)
            IMU1_csv_name = f'{self.name.text()}_{self.movement.currentText()}_{self.times.text()}_IMU1.csv'
            IMU2_csv_name = f'{self.name.text()}_{self.movement.currentText()}_{self.times.text()}_IMU2.csv'
            self.save_to_csv(IMU1_csv_name, partial_IMU1_data)
            self.save_to_csv(IMU2_csv_name, partial_IMU2_data)
            self.IMU1_dev.imu_data.clear()
            self.IMU2_dev.imu_data.clear()
        if self.dev_status[3]:
            partial_EGG_data = self.get_data_by_time_range(self.EGG_dev.EGG_data, self.time1,
                                                           self.active_train.time2)
            EGG_csv_name = f'{self.name.text()}_{self.movement.currentText()}_{self.times.text()}_EGG.csv'
            self.save_to_csv(EGG_csv_name, partial_EGG_data)
            self.EGG_dev.EGG_data.clear()
        if self.dev_status[4]:
            partial_Vio_data = self.get_data_by_time_range(self.vid_dev.video_data, self.time1,
                                                           self.active_train.time2)
            Vio_csv_name = f'{self.name.text()}_{self.movement.currentText()}_{self.times.text()}_Vio.csv'
            self.save_to_csv(Vio_csv_name, partial_Vio_data)
            self.vid_dev.video_data.clear()
        self.times.setValue(self.times.value() + 1)


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


    def get_data_by_time_range(self, data_list, start_time, end_time):
        """根据时间范围提取数据"""
        return [item for item in data_list if start_time <= item['Time'] <= end_time]

    # 传感器断开连接

    def ending(self):
        self.select_sEMG.setCheckState(False)
        self.select_my2901.setCheckState(False)
        self.select_imu.setCheckState(False)
        self.select_EGG.setCheckState(False)
        self.select_Vis.setCheckState(False)
        self.UnableMotor()
        self.RstDevStatus()

        # 新增发送状态信息
        self.update_unity_status(
            motor_status="未使能",
            feedback="训练已结束"
        )




    def wrap_updata(self, func):
        """包装 IMU 更新为同步方法"""
        asyncio.run(func())

        # 重置传感器状态标签
    def RstDevStatus(self):
        self.UnableMotor()
        if self.dev_status[0]:
            self.executor.submit(self.sEMG_dev.stop_data_reading())
        if self.dev_status[1]:
            self.executor.submit(self.my2901_dev.stop_data_reading())
        if self.dev_status[2]:
            try:
                # 包装 IMU 更新为同步方法并提交
                self.executor.submit(self.wrap_updata, self.IMU1_dev.EndConnect)
                self.executor.submit(self.wrap_updata, self.IMU2_dev.EndConnect)
            except Exception as e:
                print(f"IMU读取出错: {e}")

        if self.dev_status[3]:
            print('心电输出')
            # self.executor.submit(self.EGG_dev.update_EGG_data)

        if self.dev_status[4]:
            self.executor.submit(self.vid_dev.stop())
            self.update_status(self.Vis_show, '摄像头已关闭', 'white')
        devices = [self.sEMG_show, self.my2901_show, self.imu_show, self.EGG_show, self.Vis_show]
        for device in devices:
            self.update_status(device, '未连接', 'red')


    def update_status(self, label, message, color='red'):
        label.setText(message)
        label.setStyleSheet(f"background-color: {color};")

# 多线程类进行动作选择
class CsvRead(QThread):
    csv_read = pyqtSignal(str)  # 用于更新界面的信号

    def __init__(self, plc):
        super().__init__()
        self.plc = plc

    def run(self):
        try:
            self.plc.write_by_name('data_read.bRead', True, pyads.PLCTYPE_BOOL)
            time.sleep(0.5)
            # 等待读取完成
            while True:
                bBusy = self.plc.read_by_name('data_read.bBusy')
                if not bBusy:
                    self.csv_read.emit("动作读取完毕。")
                    break
                else:
                    self.csv_read.emit("动作正在读取！")
                time.sleep(1)
        except Exception as e:
            self.update_signal.emit(f"发生错误: {e}")


    # 多线程类 被动训练
class PasTrainThread(QThread):
    passive_train = pyqtSignal(str)  # 用于通知任务完成的信号

    def __init__(self, plc):
        super().__init__()
        self.plc = plc

    def run(self):
        self.plc.write_by_name('MAIN_Motion_Control.ax_state', 26)
        # 选择滤波加平滑后的轨迹
        self.plc.write_by_name('MAIN_Motion_Control.traj_select', 2)
        # 等待动作完成
        while True:
            try:
                ax_state = self.plc.read_by_name('MAIN_Motion_Control.ax_state')
                if ax_state == -1:
                    self.passive_train.emit(f"movement训练完成。")
                    self.time2 = time.time()
                    break
                else:
                    self.passive_train.emit(f"movement正在训练！")
                time.sleep(1)
            except Exception as e:
                print(e)


class CsvWrite(QThread):
    csv_write = pyqtSignal(str)  # 用于更新界面的信号
    def __init__(self, plc):
        super().__init__()
        self.plc = plc
    def run(self):
        try:
            self.plc.write_by_name('data_write.bWrite', True, pyads.PLCTYPE_BOOL)
            while True:
                bBusy = self.plc.read_by_name('data_write.bBusy', pyads.PLCTYPE_BOOL)
                if bBusy:
                    self.csv_write.emit('正在写入csv文件中')
                else:
                    self.csv_write.emit('已写入csv文件中')
                    break
                time.sleep(1)
        except Exception as e:
            print(f'something wrong in csv write like {e}')

    if __name__ == '__main__':
        app = QApplication(sys.argv)

        loop = QEventLoop(app)
        asyncio.set_event_loop(loop)  # 设置事件循环

        w = MyWindow()
        w.ui.show()

        with loop:
            loop.run_forever()

        app.exec()