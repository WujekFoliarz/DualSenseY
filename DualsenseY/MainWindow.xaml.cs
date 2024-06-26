﻿using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wujek_Dualsense_API;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Windows.Media.Media3D;

namespace DualSenseY
{
    public partial class MainWindow : Window
    {
        private UDP udp;
        public Dualsense[] dualsense = new Dualsense[4];
        private string controllerInstanceID = string.Empty;
        public int currentControllerNumber = 0;
        private int[] leftTriggerForces = new int[7];
        private int[] rightTriggerForces = new int[7];
        private bool leftTrigger = true;
        private TriggerType.TriggerModes currentLeftTrigger = TriggerType.TriggerModes.Off;
        private TriggerType.TriggerModes currentRightTrigger = TriggerType.TriggerModes.Off;
        private int leftTriggerModeIndex = 0;
        private int rightTriggerModeIndex = 0;
        private Stopwatch UDPtime = new Stopwatch();
        private ControllerEmulation controllerEmulation;

        private bool firstTimeCmbSelect = true;

        public MainWindow()
        {
            InitializeComponent();
            controlPanel.Visibility = Visibility.Collapsed;
            controllerEmulationBox.Visibility = Visibility.Hidden;
            cmbControllerSelect.SelectedIndex = 0;

            leftTriggerForces[0] = 0;
            leftTriggerForces[1] = 0;
            leftTriggerForces[2] = 0;
            leftTriggerForces[3] = 0;
            leftTriggerForces[4] = 0;
            leftTriggerForces[5] = 0;
            leftTriggerForces[6] = 0;

            rightTriggerForces[0] = 0;
            rightTriggerForces[1] = 0;
            rightTriggerForces[2] = 0;
            rightTriggerForces[3] = 0;
            rightTriggerForces[4] = 0;
            rightTriggerForces[5] = 0;
            rightTriggerForces[6] = 0;

            udp = new UDP();
            new Thread(() => { Thread.CurrentThread.Priority = ThreadPriority.Lowest; Thread.CurrentThread.IsBackground = true; WatchUDPUpdates(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; WatchMicrophoneLevel(); }).Start();
        }

        private MMDeviceEnumerator MDE = new MMDeviceEnumerator();
        private MMDevice[] MD = new MMDevice[4];
        private WaveInEvent waveInStream = new WaveInEvent();
        private void WatchMicrophoneLevel()
        {
            while (true)
            {
                if (MD[currentControllerNumber] != null && dualsense[currentControllerNumber] != null && dualsense[currentControllerNumber].ConnectionType == ConnectionType.USB && dualsense[currentControllerNumber].Working)
                {
                        var AMI = MD[currentControllerNumber].AudioMeterInformation;
                        this.Dispatcher.Invoke(new Action(() => { micProgressBar.Value = AMI.PeakValues[0] * 100; }));
                }
                else if (MD[currentControllerNumber] == null)
                {
                    waveInStream.Dispose();
                    waveInStream = new WaveInEvent();

                    foreach (MMDevice mmdevice in MDE.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
                    {
                        if (currentControllerNumber > 0)
                        {
                            if (mmdevice.FriendlyName.Contains("Wireless Controller") && mmdevice.FriendlyName.Contains(Convert.ToString(currentControllerNumber + 1)))
                            {
                                MD[currentControllerNumber] = mmdevice;
                                break;
                            }
                        }
                        else
                        {
                            if (mmdevice.FriendlyName.Contains("Wireless Controller") && !mmdevice.FriendlyName.Contains("2") && !mmdevice.FriendlyName.Contains("3") && !mmdevice.FriendlyName.Contains("4"))
                            {
                                MD[0] = mmdevice;
                                break;
                            }
                        }
                    }

                    waveInStream.StartRecording();
                }

                Thread.Sleep(100);
            }
        }

        private async void WatchUDPUpdates()
        {
            UDPtime.Start();
            while (udp.serverOn)
            {

                    this.Dispatcher.Invoke(() =>
                    {
                        if (UDPtime.ElapsedMilliseconds >= 1000 || dualsense[currentControllerNumber] == null || dualsense[currentControllerNumber].Working == false)
                        {
                            if (dualsense[currentControllerNumber] != null && dualsense[currentControllerNumber].Working)
                                controlPanel.Visibility = Visibility.Visible;

                            udpStatus.Text = "UDP: Inactive";
                            udpStatusDot.Fill = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            controlPanel.Visibility = Visibility.Collapsed;
                            udpStatus.Text = "UDP: Active";
                            udpStatusDot.Fill = new SolidColorBrush(Colors.Green);
                        }
                    });


                if (dualsense[currentControllerNumber] != null && dualsense[currentControllerNumber].Working && udp.currentPacket != null && udp.serverOn)
                {
                    if (udp.newPacket)
                        UDPtime.Restart();

                    foreach (UDP.Instruction instruction in udp.currentPacket.instructions)
                    {
                        switch (instruction.type)
                        {
                            case UDP.InstructionType.MicLED:
                                switch ((UDP.MicLEDMode)Convert.ToInt32(instruction.parameters[1]))
                                {
                                    case UDP.MicLEDMode.Pulse:
                                        dualsense[currentControllerNumber].SetMicrophoneLED(LED.MicrophoneLED.ON);
                                        break;
                                    case UDP.MicLEDMode.On:
                                        dualsense[currentControllerNumber].SetMicrophoneLED(LED.MicrophoneLED.ON);
                                        break;
                                    case UDP.MicLEDMode.Off:
                                        dualsense[currentControllerNumber].SetMicrophoneLED(LED.MicrophoneLED.OFF);
                                        break;
                                }
                                break;
                            case UDP.InstructionType.TriggerUpdate:

                                int[] triggerForces = { 0, 0, 0, 0, 0, 0, 0 };
                                TriggerType.TriggerModes triggerType = TriggerType.TriggerModes.Off;

                                switch ((UDP.TriggerMode)Convert.ToInt32(instruction.parameters[2]))
                                {
                                    case UDP.TriggerMode.Normal:
                                        triggerType = TriggerType.TriggerModes.Rigid_B;
                                        triggerForces[0] = 0;
                                        triggerForces[1] = 0;
                                        triggerForces[2] = 0;
                                        triggerForces[3] = 0;
                                        triggerForces[4] = 0;
                                        triggerForces[5] = 0;
                                        triggerForces[6] = 0;
                                        break;
                                    case UDP.TriggerMode.GameCube:
                                        triggerType = TriggerType.TriggerModes.Pulse;
                                        triggerForces[0] = 144;
                                        triggerForces[1] = 160;
                                        triggerForces[2] = byte.MaxValue;
                                        triggerForces[3] = 0;
                                        triggerForces[4] = 0;
                                        triggerForces[5] = 0;
                                        triggerForces[6] = 0;
                                        break;
                                    case UDP.TriggerMode.VerySoft:
                                        long VerySoftTrigger = -6258686;
                                        triggerType = TriggerType.TriggerModes.Rigid_A;
                                        triggerForces[0] = BitConverter.GetBytes(VerySoftTrigger)[1];
                                        triggerForces[1] = BitConverter.GetBytes(VerySoftTrigger)[2];
                                        triggerForces[2] = BitConverter.GetBytes(VerySoftTrigger)[3];
                                        triggerForces[3] = BitConverter.GetBytes(VerySoftTrigger)[4];
                                        triggerForces[4] = BitConverter.GetBytes(VerySoftTrigger)[5];
                                        triggerForces[5] = BitConverter.GetBytes(VerySoftTrigger)[6];
                                        triggerForces[6] = BitConverter.GetBytes(VerySoftTrigger)[7];
                                        break;
                                    case UDP.TriggerMode.Soft:
                                        long SoftTrigger = -6273790;
                                        triggerType = TriggerType.TriggerModes.Rigid_A;
                                        triggerForces[0] = BitConverter.GetBytes(SoftTrigger)[1];
                                        triggerForces[1] = BitConverter.GetBytes(SoftTrigger)[2];
                                        triggerForces[2] = BitConverter.GetBytes(SoftTrigger)[3];
                                        triggerForces[3] = BitConverter.GetBytes(SoftTrigger)[4];
                                        triggerForces[4] = BitConverter.GetBytes(SoftTrigger)[5];
                                        triggerForces[5] = BitConverter.GetBytes(SoftTrigger)[6];
                                        triggerForces[6] = BitConverter.GetBytes(SoftTrigger)[7];
                                        break;
                                    case UDP.TriggerMode.Hard:
                                        long HardTrigger = -6283262;
                                        triggerType = TriggerType.TriggerModes.Rigid_A;
                                        triggerForces[0] = BitConverter.GetBytes(HardTrigger)[1];
                                        triggerForces[1] = BitConverter.GetBytes(HardTrigger)[2];
                                        triggerForces[2] = BitConverter.GetBytes(HardTrigger)[3];
                                        triggerForces[3] = BitConverter.GetBytes(HardTrigger)[4];
                                        triggerForces[4] = BitConverter.GetBytes(HardTrigger)[5];
                                        triggerForces[5] = BitConverter.GetBytes(HardTrigger)[6];
                                        triggerForces[6] = BitConverter.GetBytes(HardTrigger)[7];
                                        break;
                                    case UDP.TriggerMode.VeryHard:
                                        long VeryHardTrigger = -6287358;
                                        triggerType = TriggerType.TriggerModes.Rigid_A;
                                        triggerForces[0] = BitConverter.GetBytes(VeryHardTrigger)[1];
                                        triggerForces[1] = BitConverter.GetBytes(VeryHardTrigger)[2];
                                        triggerForces[2] = BitConverter.GetBytes(VeryHardTrigger)[3];
                                        triggerForces[3] = BitConverter.GetBytes(VeryHardTrigger)[4];
                                        triggerForces[4] = BitConverter.GetBytes(VeryHardTrigger)[5];
                                        triggerForces[5] = BitConverter.GetBytes(VeryHardTrigger)[6];
                                        triggerForces[6] = BitConverter.GetBytes(VeryHardTrigger)[7];
                                        break;
                                    case UDP.TriggerMode.Hardest:
                                        long HardestTrigger = -65534;
                                        triggerType = TriggerType.TriggerModes.Rigid_A;
                                        triggerForces[0] = BitConverter.GetBytes(HardestTrigger)[1];
                                        triggerForces[1] = BitConverter.GetBytes(HardestTrigger)[2];
                                        triggerForces[2] = BitConverter.GetBytes(HardestTrigger)[3];
                                        triggerForces[3] = BitConverter.GetBytes(HardestTrigger)[4];
                                        triggerForces[4] = BitConverter.GetBytes(HardestTrigger)[5];
                                        triggerForces[5] = BitConverter.GetBytes(HardestTrigger)[6];
                                        triggerForces[6] = BitConverter.GetBytes(HardestTrigger)[7];
                                        break;
                                    case UDP.TriggerMode.Rigid:
                                        long RigidTrigger = 16711682L;
                                        triggerType = TriggerType.TriggerModes.Rigid;
                                        triggerForces[0] = BitConverter.GetBytes(RigidTrigger)[1];
                                        triggerForces[1] = BitConverter.GetBytes(RigidTrigger)[2];
                                        triggerForces[2] = BitConverter.GetBytes(RigidTrigger)[3];
                                        triggerForces[3] = BitConverter.GetBytes(RigidTrigger)[4];
                                        triggerForces[4] = BitConverter.GetBytes(RigidTrigger)[5];
                                        triggerForces[5] = BitConverter.GetBytes(RigidTrigger)[6];
                                        triggerForces[6] = BitConverter.GetBytes(RigidTrigger)[7];
                                        break;
                                    case UDP.TriggerMode.VibrateTrigger:
                                        triggerType = TriggerType.TriggerModes.Pulse_AB;
                                        triggerForces[0] = 37;
                                        triggerForces[1] = 35;
                                        triggerForces[2] = 6;
                                        triggerForces[3] = 39;
                                        triggerForces[4] = 33;
                                        triggerForces[5] = 35;
                                        triggerForces[6] = 34;
                                        break;
                                    case UDP.TriggerMode.Choppy:
                                        triggerType = TriggerType.TriggerModes.Rigid_A;
                                        triggerForces[0] = 2;
                                        triggerForces[1] = 39;
                                        triggerForces[2] = 33;
                                        triggerForces[3] = 39;
                                        triggerForces[4] = 38;
                                        triggerForces[5] = 2;
                                        triggerForces[6] = 0;
                                        break;
                                    case UDP.TriggerMode.Medium:
                                        triggerType = TriggerType.TriggerModes.Pulse_A;
                                        triggerForces[0] = 2;
                                        triggerForces[1] = 35;
                                        triggerForces[2] = 1;
                                        triggerForces[3] = 6;
                                        triggerForces[4] = 6;
                                        triggerForces[5] = 1;
                                        triggerForces[6] = 33;
                                        break;
                                    case UDP.TriggerMode.VibrateTriggerPulse:
                                        triggerType = TriggerType.TriggerModes.Pulse_AB;
                                        triggerForces[0] = 37;
                                        triggerForces[1] = 35;
                                        triggerForces[2] = 6;
                                        triggerForces[3] = 39;
                                        triggerForces[4] = 33;
                                        triggerForces[5] = 35;
                                        triggerForces[6] = 34;
                                        break;
                                    case UDP.TriggerMode.CustomTriggerValue:
                                        switch ((UDP.CustomTriggerValueMode)Convert.ToInt32(instruction.parameters[3]))
                                        {
                                            case UDP.CustomTriggerValueMode.OFF:
                                                triggerType = TriggerType.TriggerModes.Off;
                                                break;
                                            case UDP.CustomTriggerValueMode.Pulse:
                                                triggerType = TriggerType.TriggerModes.Pulse;
                                                break;
                                            case UDP.CustomTriggerValueMode.PulseA:
                                                triggerType = TriggerType.TriggerModes.Pulse_A;
                                                break;
                                            case UDP.CustomTriggerValueMode.PulseB:
                                                triggerType = TriggerType.TriggerModes.Pulse_B;
                                                break;
                                            case UDP.CustomTriggerValueMode.PulseAB:
                                                triggerType = TriggerType.TriggerModes.Pulse_AB;
                                                break;
                                            case UDP.CustomTriggerValueMode.Rigid:
                                                triggerType = TriggerType.TriggerModes.Rigid;
                                                break;
                                            case UDP.CustomTriggerValueMode.RigidA:
                                                triggerType = TriggerType.TriggerModes.Rigid_A;
                                                break;
                                            case UDP.CustomTriggerValueMode.RigidB:
                                                triggerType = TriggerType.TriggerModes.Rigid_B;
                                                break;
                                            case UDP.CustomTriggerValueMode.RigidAB:
                                                triggerType = TriggerType.TriggerModes.Rigid_AB;
                                                break;
                                        }

                                        if (instruction.parameters.Length >= 5)
                                        {
                                            int j = 0;
                                            for (int i = 4; i < instruction.parameters.Length; i++)
                                            {
                                                triggerForces[j] = Convert.ToByte(Convert.ToInt32(instruction.parameters[i]));
                                                j++;
                                            }
                                        }
                                        break;
                                    case UDP.TriggerMode.Resistance:
                                        byte start = Convert.ToByte(Convert.ToInt32(instruction.parameters[3]));
                                        byte force = Convert.ToByte(Convert.ToInt32(instruction.parameters[4]));

                                        if (start > 9)
                                        {
                                            break;
                                        }
                                        if (force > 8)
                                        {
                                            break;
                                        }

                                        if (force > 0)
                                        {
                                            byte b = (byte)(force - 1 & 7);
                                            uint num = 0U;
                                            ushort num2 = 0;
                                            for (int i = (int)start; i < 10; i++)
                                            {
                                                num |= (uint)((uint)b << 3 * i);
                                                num2 |= (ushort)(1 << i);
                                            }

                                            triggerType = TriggerType.TriggerModes.Rigid_A;
                                            triggerForces[0] = (byte)(num2 & 255);
                                            triggerForces[1] = (byte)(num2 >> 8 & 255);
                                            triggerForces[2] = (byte)(num & 255U);
                                            triggerForces[3] = (byte)(num >> 8 & 255U);
                                            triggerForces[4] = (byte)(num >> 16 & 255U);
                                            triggerForces[5] = (byte)(num >> 24 & 255U);
                                            triggerForces[6] = 0;
                                        }
                                        break;
                                    case UDP.TriggerMode.Bow:
                                        start = Convert.ToByte(Convert.ToInt32(instruction.parameters[3]));
                                        byte end = Convert.ToByte(Convert.ToInt32(instruction.parameters[4]));
                                        force = Convert.ToByte(Convert.ToInt32(instruction.parameters[5]));
                                        byte snapForce = Convert.ToByte(Convert.ToInt32(instruction.parameters[6]));

                                        if (start > 8)
                                        {
                                            break;
                                        }
                                        if (end > 8)
                                        {
                                            break;
                                        }
                                        if (start >= end)
                                        {
                                            break;
                                        }
                                        if (force > 8)
                                        {
                                            break;
                                        }
                                        if (snapForce > 8)
                                        {
                                            break;
                                        }
                                        if (end > 0 && force > 0 && snapForce > 0)
                                        {
                                            ushort num = (ushort)(1 << (int)start | 1 << (int)end);
                                            uint num2 = (uint)((int)(force - 1 & 7) | (int)(snapForce - 1 & 7) << 3);

                                            triggerType = TriggerType.TriggerModes.Pulse_A;
                                            triggerForces[0] = (byte)(num & 255);
                                            triggerForces[1] = (byte)(num >> 8 & 255);
                                            triggerForces[2] = (byte)(num2 & 255U);
                                            triggerForces[3] = (byte)(num2 >> 8 & 255U);
                                            triggerForces[4] = 0;
                                            triggerForces[5] = 0;
                                            triggerForces[6] = 0;
                                        }
                                        break;
                                    case UDP.TriggerMode.Galloping:
                                        start = Convert.ToByte(Convert.ToInt32(instruction.parameters[3]));
                                        end = Convert.ToByte(Convert.ToInt32(instruction.parameters[4]));
                                        byte firstFoot = Convert.ToByte(Convert.ToInt32(instruction.parameters[5]));
                                        byte secondFoot = Convert.ToByte(Convert.ToInt32(instruction.parameters[6]));
                                        byte frequency = Convert.ToByte(Convert.ToInt32(instruction.parameters[7]));

                                        if (start > 8)
                                        {
                                            break;
                                        }
                                        if (end > 9)
                                        {
                                            break;
                                        }
                                        if (start >= end)
                                        {
                                            break;
                                        }
                                        if (secondFoot > 7)
                                        {
                                            break;
                                        }
                                        if (firstFoot > 6)
                                        {
                                            break;
                                        }
                                        if (firstFoot >= secondFoot)
                                        {
                                            break;
                                        }
                                        if (frequency > 0)
                                        {
                                            ushort num = (ushort)(1 << (int)start | 1 << (int)end);
                                            uint num2 = (uint)((int)(secondFoot & 7) | (int)(firstFoot & 7) << 3);
                                            triggerType = TriggerType.TriggerModes.Pulse_B;
                                            triggerForces[0] = frequency;
                                            triggerForces[1] = (byte)(num & 255);
                                            triggerForces[2] = (byte)(num >> 8 & 255);
                                            triggerForces[3] = (byte)(num2 & 255U);
                                            triggerForces[4] = 0;
                                            triggerForces[5] = 0;
                                            triggerForces[6] = 0;
                                        }
                                        break;
                                    case UDP.TriggerMode.SemiAutomaticGun:
                                        start = Convert.ToByte(Convert.ToInt32(instruction.parameters[3]));
                                        end = Convert.ToByte(Convert.ToInt32(instruction.parameters[4]));
                                        force = Convert.ToByte(Convert.ToInt32(instruction.parameters[5]));

                                        if (start > 7 || start < 2)
                                        {
                                            break;
                                        }
                                        if (end > 8)
                                        {
                                            break;
                                        }
                                        if (end <= start)
                                        {
                                            break;
                                        }
                                        if (force > 8)
                                        {
                                            break;
                                        }
                                        if (force > 0)
                                        {
                                            ushort num = (ushort)(1 << (int)start | 1 << (int)end);
                                            triggerType = TriggerType.TriggerModes.Rigid_AB;

                                            triggerForces[0] = (byte)(num & 255);
                                            triggerForces[1] = (byte)(num >> 8 & 255);
                                            triggerForces[2] = force - 1;
                                            triggerForces[3] = 0;
                                            triggerForces[4] = 0;
                                            triggerForces[5] = 0;
                                            triggerForces[6] = 0;
                                        }
                                        break;
                                    case UDP.TriggerMode.AutomaticGun:
                                        start = Convert.ToByte(Convert.ToInt32(instruction.parameters[3]));
                                        byte strength = Convert.ToByte(Convert.ToInt32(instruction.parameters[4]));
                                        frequency = Convert.ToByte(Convert.ToInt32(instruction.parameters[4]));

                                        if (start > 9)
                                        {
                                            break;
                                        }
                                        if (strength > 8)
                                        {
                                            break;
                                        }
                                        if (strength > 0 && frequency > 0)
                                        {
                                            byte b = (byte)(strength - 1 & 7);
                                            uint num = 0U;
                                            ushort num2 = 0;
                                            for (int i = (int)start; i < 10; i++)
                                            {
                                                num |= (uint)((uint)b << 3 * i);
                                                num2 |= (ushort)(1 << i);
                                            }

                                            triggerType = TriggerType.TriggerModes.Pulse_B;
                                            triggerForces[0] = frequency;
                                            triggerForces[1] = (byte)(num2 & 255);
                                            triggerForces[2] = (byte)(num2 >> 8 & 255);
                                            triggerForces[3] = (byte)(num & 255U);
                                            triggerForces[4] = (byte)(num >> 8 & 255U);
                                            triggerForces[5] = (byte)(num >> 16 & 255U);
                                            triggerForces[6] = (byte)(num >> 24 & 255U);
                                        }
                                        break;
                                    case UDP.TriggerMode.Machine:
                                        start = Convert.ToByte(Convert.ToInt32(instruction.parameters[3]));
                                        end = Convert.ToByte(Convert.ToInt32(instruction.parameters[4]));
                                        byte strengthA = Convert.ToByte(Convert.ToInt32(instruction.parameters[5]));
                                        byte strengthB = Convert.ToByte(Convert.ToInt32(instruction.parameters[6]));
                                        frequency = Convert.ToByte(Convert.ToInt32(instruction.parameters[7]));
                                        byte period = Convert.ToByte(Convert.ToInt32(instruction.parameters[8]));

                                        if (start > 8)
                                        {
                                            break;
                                        }
                                        if (end > 9)
                                        {
                                            break;
                                        }
                                        if (end <= start)
                                        {
                                            break;
                                        }
                                        if (strengthA > 7)
                                        {
                                            break;
                                        }
                                        if (strengthB > 7)
                                        {
                                            break;
                                        }
                                        if (frequency > 0)
                                        {
                                            ushort num = (ushort)(1 << (int)start | 1 << (int)end);
                                            uint num2 = (uint)((int)(strengthA & 7) | (int)(strengthB & 7) << 3);
                                            triggerType = TriggerType.TriggerModes.Pulse_B;
                                            triggerForces[0] = frequency;
                                            triggerForces[2] = period;
                                            triggerForces[1] = (byte)(num & 255);
                                            triggerForces[3] = (byte)(num >> 8 & 255);
                                            triggerForces[4] = (byte)(num2 & 255U);
                                            triggerForces[5] = 0;
                                            triggerForces[6] = 0;

                                        }
                                        break;
                                    default:
                                        triggerForces[0] = 0;
                                        triggerForces[1] = 0;
                                        triggerForces[2] = 0;
                                        triggerForces[3] = 0;
                                        triggerForces[4] = 0;
                                        triggerForces[5] = 0;
                                        triggerForces[6] = 0;
                                        triggerType = TriggerType.TriggerModes.Off;
                                        break;
                                }

                                switch ((UDP.Trigger)Convert.ToInt32(instruction.parameters[1]))
                                {
                                    case UDP.Trigger.Left:
                                        dualsense[currentControllerNumber].SetLeftTrigger(triggerType, triggerForces[0], triggerForces[1], triggerForces[2], triggerForces[3], triggerForces[4], triggerForces[5], triggerForces[6]);
                                        break;
                                    case UDP.Trigger.Right:
                                        dualsense[currentControllerNumber].SetRightTrigger(triggerType, triggerForces[0], triggerForces[1], triggerForces[2], triggerForces[3], triggerForces[4], triggerForces[5], triggerForces[6]);
                                        break;

                                }

                                break;
                            case UDP.InstructionType.RGBUpdate:
                                if(Convert.ToInt32(instruction.parameters[1]) >= 0 && Convert.ToInt32(instruction.parameters[2]) >= 0 && Convert.ToInt32(instruction.parameters[3]) >= 0)
                                    dualsense[currentControllerNumber].SetLightbar(Convert.ToInt32(instruction.parameters[1]), Convert.ToInt32(instruction.parameters[2]), Convert.ToInt32(instruction.parameters[3]));
                                break;
                            case UDP.InstructionType.PlayerLEDNewRevision:
                                switch ((UDP.PlayerLEDNewRevision)Convert.ToInt32(instruction.parameters[1]))
                                {
                                    case UDP.PlayerLEDNewRevision.AllOff:
                                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.OFF);
                                        break;
                                    case UDP.PlayerLEDNewRevision.One:
                                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_1);
                                        break;
                                    case UDP.PlayerLEDNewRevision.Two:
                                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_2);
                                        break;
                                    case UDP.PlayerLEDNewRevision.Three:
                                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_3);
                                        break;
                                    case UDP.PlayerLEDNewRevision.Four:
                                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_4);
                                        break;
                                    case UDP.PlayerLEDNewRevision.Five:
                                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_4);
                                        break;
                                }

                                break;
                        }
                    }

                }

                Thread.Sleep(1);
            }
        }

        private static int ScaleTo255(int number)
        {
            if (number < 0 || number > 8)
            {
                throw new ArgumentOutOfRangeException("number", "The input number must be between 0 and 8.");
            }

            // Scale from 0-8 to 0-255
            return (int)((number / 8.0) * 255);
        }


        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement actual connection logic

            if (btnConnect.Content == "Disconnect Controller")
            {
                dualsense[currentControllerNumber].Dispose();
                UpdateConnectionStatus();
            }
            else
            {
                try
                {
                    switch (cmbControllerSelect.SelectedIndex)
                    {
                        case 0:
                            if (dualsense[0] == null || !dualsense[0].Working)
                            {
                                dualsense[0] = new Dualsense(0);
                                dualsense[0].Start();
                                currentControllerNumber = 0;
                            }
                            break;
                        case 1:
                            if (dualsense[1] == null || !dualsense[1].Working)
                            {
                                dualsense[1] = new Dualsense(1);
                                dualsense[1].Start();
                                currentControllerNumber = 1;
                            }
                            break;
                        case 2:
                            if (dualsense[2] == null || !dualsense[2].Working)
                            {
                                dualsense[2] = new Dualsense(2);
                                dualsense[2].Start();
                                currentControllerNumber = 2;
                            }
                            break;
                        case 3:
                            if (dualsense[3] == null || !dualsense[3].Working)
                            {
                                dualsense[3] = new Dualsense(3);
                                dualsense[3].Start();
                                currentControllerNumber = 3;
                            }
                            break;
                    }

                    UpdateConnectionStatus();
                    dualsense[currentControllerNumber].SetLightbarTransition((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value, 50, 10);
                    switch (LEDbox.SelectedIndex)
                    {
                        case 0:
                            dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.OFF);
                            break;
                        case 1:
                            dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_1);
                            break;
                        case 2:
                            dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_2);
                            break;
                        case 3:
                            dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_3);
                            break;
                        case 4:
                            dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_4);
                            break;
                        case 5:
                            dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.ALL);
                            break;
                    }
                    if (micLEDcheckbox.IsChecked == true)
                        dualsense[currentControllerNumber].SetMicrophoneLED(LED.MicrophoneLED.ON);
                    else
                        dualsense[currentControllerNumber].SetMicrophoneLED(LED.MicrophoneLED.OFF);
                    dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, leftTriggerForces[0], leftTriggerForces[1], leftTriggerForces[2], leftTriggerForces[3], leftTriggerForces[4], leftTriggerForces[5], leftTriggerForces[6]);
                    dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, rightTriggerForces[0], rightTriggerForces[1], rightTriggerForces[2], rightTriggerForces[3], rightTriggerForces[4], rightTriggerForces[5], rightTriggerForces[6]);
                }
                catch (System.Exception)
                {
                    MessageBox.Show($"Controller {currentControllerNumber + 1} is not plugged in");
                }
            }
        }

        private void cmbControllerSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (firstTimeCmbSelect)
            {
                currentControllerNumber = cmbControllerSelect.SelectedIndex;
                firstTimeCmbSelect = false;
            }
            else
            {
                currentControllerNumber = cmbControllerSelect.SelectedIndex;
                UpdateConnectionStatus();
            }
        }

        private void UpdateConnectionStatus()
        {

            if (dualsense[currentControllerNumber] != null && dualsense[currentControllerNumber].Working)
            {
                txtStatus.Text = "Status: Connected";
                btnConnect.Content = "Disconnect Controller";
                controlPanel.Visibility = Visibility.Visible;
                cmbControllerSelect.Visibility = Visibility.Hidden;
                controllerEmulationBox.Visibility = Visibility.Visible;

                if(controllerEmulationBox.SelectedIndex == 1 && controllerEmulation == null)
                    controllerEmulation = new ControllerEmulation(dualsense[currentControllerNumber], true);
                else if(controllerEmulationBox.SelectedIndex == 2 && controllerEmulation == null)
                    controllerEmulation = new ControllerEmulation(dualsense[currentControllerNumber], false);

                if (dualsense[currentControllerNumber].ConnectionType == ConnectionType.BT)
                    micTab.IsEnabled = false;
                else
                    micTab.IsEnabled = true;

            }
            else if (dualsense[currentControllerNumber] == null || !dualsense[currentControllerNumber].Working)
            {
                txtStatus.Text = "Status: Disconnected";
                btnConnect.Content = "Connect Controller";
                controlPanel.Visibility = Visibility.Collapsed;
                cmbControllerSelect.Visibility = Visibility.Visible;
                controllerEmulationBox.Visibility = Visibility.Hidden;
                controllerEmulationBox.SelectedIndex = 0;
            }

        }

        private void sliderLeftMotor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void sliderRightMotor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void btnTestVibration_Click(object sender, RoutedEventArgs e)
        {
            dualsense[currentControllerNumber].SetStandardRumble((byte)sliderLeftMotor.Value, (byte)sliderRightMotor.Value);
        }

        private void sliderLED_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dualsense[currentControllerNumber].SetLightbar((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value);
            LEDlabel.Text = $"LED Color: ({(byte)sliderRed.Value},{(byte)sliderGreen.Value},{(byte)sliderBlue.Value})";
        }

        private void sliderLeftTrigger_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // TODO: Implement left trigger resistance control
            Console.WriteLine($"Left Trigger Resistance: {e.NewValue}");
        }

        private void sliderRightTrigger_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // TODO: Implement right trigger resistance control
            Console.WriteLine($"Right Trigger Resistance: {e.NewValue}");
        }

        private void sliderForce1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce1.Text = Convert.ToByte(sliderForce1.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[0] = Convert.ToByte(sliderForce1.Value);
                dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[0] = Convert.ToByte(sliderForce1.Value);
                dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce2.Text = Convert.ToByte(sliderForce2.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[1] = Convert.ToByte(sliderForce2.Value);
                dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[1] = Convert.ToByte(sliderForce2.Value);
                dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce3.Text = Convert.ToByte(sliderForce3.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[2] = Convert.ToByte(sliderForce3.Value);
                dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[2] = Convert.ToByte(sliderForce3.Value);
                dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce4_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce4.Text = Convert.ToByte(sliderForce4.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[3] = Convert.ToByte(sliderForce4.Value);
                dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[3] = Convert.ToByte(sliderForce4.Value);
                dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce5_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce5.Text = Convert.ToByte(sliderForce5.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[4] = Convert.ToByte(sliderForce5.Value);
                dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[4] = Convert.ToByte(sliderForce5.Value);
                dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce6_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce6.Text = Convert.ToByte(sliderForce6.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[5] = Convert.ToByte(sliderForce6.Value);
                dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[5] = Convert.ToByte(sliderForce6.Value);
                dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce7_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce7.Text = Convert.ToByte(sliderForce7.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[6] = Convert.ToByte(sliderForce7.Value);
                dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[6] = Convert.ToByte(sliderForce7.Value);
                dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }

        }

        private void ComboBoxTrigger_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                if (triggerLeftOrRightBox.SelectedIndex == 0)
                {
                    leftTrigger = true;
                    triggerModeCmb.SelectedIndex = leftTriggerModeIndex;
                    sliderForce1.Value = leftTriggerForces[0];
                    sliderForce2.Value = leftTriggerForces[1];
                    sliderForce3.Value = leftTriggerForces[2];
                    sliderForce4.Value = leftTriggerForces[3];
                    sliderForce5.Value = leftTriggerForces[4];
                    sliderForce6.Value = leftTriggerForces[5];
                    sliderForce7.Value = leftTriggerForces[6];

                    textForce1.Text = leftTriggerForces[0].ToString();
                    textForce2.Text = leftTriggerForces[1].ToString();
                    textForce3.Text = leftTriggerForces[2].ToString();
                    textForce4.Text = leftTriggerForces[3].ToString();
                    textForce5.Text = leftTriggerForces[4].ToString();
                    textForce6.Text = leftTriggerForces[5].ToString();
                    textForce7.Text = leftTriggerForces[6].ToString();
                }
                else
                {
                    leftTrigger = false;
                    triggerModeCmb.SelectedIndex = rightTriggerModeIndex;
                    sliderForce1.Value = rightTriggerForces[0];
                    sliderForce2.Value = rightTriggerForces[1];
                    sliderForce3.Value = rightTriggerForces[2];
                    sliderForce4.Value = rightTriggerForces[3];
                    sliderForce5.Value = rightTriggerForces[4];
                    sliderForce6.Value = rightTriggerForces[5];
                    sliderForce7.Value = rightTriggerForces[6];

                    textForce1.Text = rightTriggerForces[0].ToString();
                    textForce2.Text = rightTriggerForces[1].ToString();
                    textForce3.Text = rightTriggerForces[2].ToString();
                    textForce4.Text = rightTriggerForces[3].ToString();
                    textForce5.Text = rightTriggerForces[4].ToString();
                    textForce6.Text = rightTriggerForces[5].ToString();
                    textForce7.Text = rightTriggerForces[6].ToString();
                }
            }
        }

        private void triggerModeCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                if (leftTrigger)
                {
                    leftTriggerModeIndex = triggerModeCmb.SelectedIndex;

                    switch (triggerModeCmb.SelectedIndex)
                    {
                        case 0:
                            currentLeftTrigger = TriggerType.TriggerModes.Off;
                            break;
                        case 1:
                            currentLeftTrigger = TriggerType.TriggerModes.Rigid;
                            break;
                        case 2:
                            currentLeftTrigger = TriggerType.TriggerModes.Pulse;
                            break;
                        case 3:
                            currentLeftTrigger = TriggerType.TriggerModes.Rigid_A;
                            break;
                        case 4:
                            currentLeftTrigger = TriggerType.TriggerModes.Rigid_B;
                            break;
                        case 5:
                            currentLeftTrigger = TriggerType.TriggerModes.Pulse_AB;
                            break;
                        case 6:
                            currentLeftTrigger = TriggerType.TriggerModes.Pulse_A;
                            break;
                        case 7:
                            currentLeftTrigger = TriggerType.TriggerModes.Pulse_B;
                            break;
                        case 8:
                            currentLeftTrigger = TriggerType.TriggerModes.Pulse_AB;
                            break;
                        case 9:
                            currentLeftTrigger = TriggerType.TriggerModes.Calibration;
                            break;
                    }

                    dualsense[currentControllerNumber].SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
                }
                else
                {
                    rightTriggerModeIndex = triggerModeCmb.SelectedIndex;

                    switch (triggerModeCmb.SelectedIndex)
                    {
                        case 0:
                            currentRightTrigger = TriggerType.TriggerModes.Off;
                            break;
                        case 1:
                            currentRightTrigger = TriggerType.TriggerModes.Rigid;
                            break;
                        case 2:
                            currentRightTrigger = TriggerType.TriggerModes.Pulse;
                            break;
                        case 3:
                            currentRightTrigger = TriggerType.TriggerModes.Rigid_A;
                            break;
                        case 4:
                            currentRightTrigger = TriggerType.TriggerModes.Rigid_B;
                            break;
                        case 5:
                            currentRightTrigger = TriggerType.TriggerModes.Pulse_AB;
                            break;
                        case 6:
                            currentRightTrigger = TriggerType.TriggerModes.Pulse_A;
                            break;
                        case 7:
                            currentRightTrigger = TriggerType.TriggerModes.Pulse_B;
                            break;
                        case 8:
                            currentRightTrigger = TriggerType.TriggerModes.Pulse_AB;
                            break;
                        case 9:
                            currentRightTrigger = TriggerType.TriggerModes.Calibration;
                            break;
                    }

                    dualsense[currentControllerNumber].SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
                }
            }


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            udp.Dispose();

            foreach (Dualsense ds in dualsense)
            {
                if (ds != null)
                    ds.Dispose();
            }

            if (controllerEmulation != null)
                controllerEmulation.Dispose();
        }

        private void controllerEmulationBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (controllerEmulationBox.SelectedIndex)
            {
                case 0:
                    if (controllerEmulation != null)
                        controllerEmulation.Dispose();
                    break;
                case 1:
                    if (controllerEmulation != null)
                        controllerEmulation.Dispose();
                    controllerEmulation = new ControllerEmulation(dualsense[currentControllerNumber], true);
                    break;
                case 2:
                    if (controllerEmulation != null)
                        controllerEmulation.Dispose();
                    controllerEmulation = new ControllerEmulation(dualsense[currentControllerNumber], false);
                    break;
            }
        }

        private void LEDbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                switch (LEDbox.SelectedIndex)
                {
                    case 0:
                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.OFF);
                        break;
                    case 1:
                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_1);
                        break;
                    case 2:
                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_2);
                        break;
                    case 3:
                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_3);
                        break;
                    case 4:
                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.PLAYER_4);
                        break;
                    case 5:
                        dualsense[currentControllerNumber].SetPlayerLED(LED.PlayerLED.ALL);
                        break;
                }
            }   
        }

        private void micLEDcheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                dualsense[currentControllerNumber].SetMicrophoneLED(LED.MicrophoneLED.ON);
            }
        }

        private void micLEDcheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                dualsense[currentControllerNumber].SetMicrophoneLED(LED.MicrophoneLED.OFF);
            }
        }

        private void sliderMicVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsInitialized)
            {
                dualsense[currentControllerNumber].SetMicrophoneVolume((int)sliderMicVolume.Value);
                micVolumeText.Text = $"Microphone Volume: {(int)sliderMicVolume.Value}";
            }
        }
    }
}