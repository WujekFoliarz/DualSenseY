using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Nefarius.Drivers.HidHide;
using Nefarius.Utilities.DeviceManagement.PnP;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Wujek_Dualsense_API;

namespace DualSenseY
{
    public partial class MainWindow : Window
    {
        private UDP udp;
        private Version version = new Version();
        public Dualsense dualsense;
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
        private Stopwatch AudioTestCooldown = new Stopwatch();
        private ControllerEmulation controllerEmulation;
        private Settings settings = new Settings();
        private bool connected = false;
        private bool useTouchpadAsMouse = false;
        private bool audioToLED = false;
        private int audioR = 0;
        private int audioG = 0;
        private int audioB = 0;
        private bool VigemAndHidHidePresent = true;
        private string[] customHotkey = new string[5];
        private int[] customHotkeyIndex = new int[20];
        private string currentControllerID = string.Empty;
        private string currentControllerPath = string.Empty;

        private string[] ports = new string[4];
        private string[] configs = new string[4];

        private bool firstTimeCmbSelect = true;
        private HidHideControlService hidHide = new HidHideControlService();
        private System.Windows.Forms.NotifyIcon MyNotifyIcon;

        public MainWindow()
        {
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            if(Process.GetProcessesByName("DSX").Count() == 0)
            {
                UDP.StartFakeDSXProcess();
            }

            try
            {
                InitializeComponent();
            }
            catch
            {
                MessageBox.Show("error");
            }

            EnumerateControllers();
            txtStatus.Text = "Right click the dropdown box to refresh controller list, avoid connecting one controller multiple times.";

            changelogText.Text = Constants.Changelog;

            MyNotifyIcon = new System.Windows.Forms.NotifyIcon();
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/dualsenseyicon.ico")).Stream;
            MyNotifyIcon.Icon = new System.Drawing.Icon(iconStream);
            MyNotifyIcon.Click += MyNotifyIcon_Click;
            iconStream.Dispose();
            this.ShowInTaskbar = true;

            connectedTo.Visibility = Visibility.Hidden;
            discoSpeedSlider.Visibility = Visibility.Hidden;
            discoSpeedText.Visibility = Visibility.Hidden;
            connectionTypeBTicon.Visibility = Visibility.Hidden;
            connectionTypeUSBicon.Visibility = Visibility.Hidden;
            batteryStatusText.Visibility = Visibility.Hidden;
            edgeIcon.Visibility = Visibility.Hidden;

            minimizeToTrayBox.IsChecked = DualSenseY.Properties.Settings.Default.minimizeToTray;
            launchMinimizedBox.IsChecked = DualSenseY.Properties.Settings.Default.launchMinimized;
            //launchWithWindowsBox.IsChecked = DualSenseY.Properties.Settings.Default.launchWithWindows;
            connectOnStartupBox.IsChecked = DualSenseY.Properties.Settings.Default.connectOnStartup;

            if (DualSenseY.Properties.Settings.Default.launchMinimized)
            {
                this.ShowInTaskbar = false;
                this.Hide();
                MyNotifyIcon.Visible = true;
            }

            if (DualSenseY.Properties.Settings.Default.connectOnStartup && DualsenseUtils.GetControllerIDs().Count != 0 && Process.GetProcessesByName("DualSenseY").Count() == 1)
            {
                ConnectToController();
            }

            controlPanelText.Text = $"DualSense Control Panel -- Version {version.CurrentVersion}";
            version.RemoveOldFiles();
            if (version.IsOutdated())
                updateBtn.Visibility = Visibility.Visible;
            else
                updateBtn.Visibility = Visibility.Hidden;

            controlPanel.Visibility = Visibility.Hidden;
            loadConfigBtn.Visibility = Visibility.Hidden;
            saveConfigBtn.Visibility = Visibility.Hidden;
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

            screenshotCooldown.Start();
            udp = new UDP();
            udp.Events.NewPacket += Events_NewPacket;
            UDPtime.Start();
            new Thread(() => { Thread.CurrentThread.Priority = ThreadPriority.Lowest; Thread.CurrentThread.IsBackground = true; WatchConfigAssigment(); }).Start();
            new Thread(() => { Thread.CurrentThread.Priority = ThreadPriority.Lowest; Thread.CurrentThread.IsBackground = true; WatchUDPUpdates(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; ReadTouchpad(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; WatchSystemAudioLevel(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; WatchHotkeys(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; WatchBatteryStatus(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; StartDisco(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; WatchMotion(); }).Start();
            new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; WatchAudioDefaultDevice(); }).Start();
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled exception occurred, contact developer: \n" + e.Exception.Message + "\n\nStacktrace: \n" + e.Exception.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void WatchConfigAssigment()
        {
            while (true)
            {
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Properties.Settings.Default.Reload();

                        port1.Content = Properties.Settings.Default.port1 != string.Empty ? DualSenseY.Properties.Settings.Default.port1 : "Click to assign port";
                        configport1.Content = Properties.Settings.Default.config1 != string.Empty ? Path.GetFileNameWithoutExtension(Properties.Settings.Default.config1) : "None";

                        port2.Content = Properties.Settings.Default.port2 != string.Empty ? Properties.Settings.Default.port2 : "Click to assign port";
                        configport2.Content = Properties.Settings.Default.config2 != string.Empty ? Path.GetFileNameWithoutExtension(Properties.Settings.Default.config2) : "None";

                        port3.Content = Properties.Settings.Default.port3 != string.Empty ? DualSenseY.Properties.Settings.Default.port3 : "Click to assign port";
                        configport3.Content = Properties.Settings.Default.config3 != string.Empty ? Path.GetFileNameWithoutExtension(Properties.Settings.Default.config3) : "None";

                        port4.Content = Properties.Settings.Default.port4 != string.Empty ? Properties.Settings.Default.port4 : "Click to assign port";
                        configport4.Content = Properties.Settings.Default.config4 != string.Empty ? Path.GetFileNameWithoutExtension(Properties.Settings.Default.config4) : "None";
                    });
                }
                catch (TaskCanceledException) { }

                Thread.Sleep(1000);
            }
        }

        private void WatchMotion()
        {
            while (true)
            {
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (dualsense != null && dualsense.Working)
                        {
                            AcceXtext.Text = $"Accelerometer X      {dualsense.ButtonState.accelerometer.X}";
                            AcceX.Value = dualsense.ButtonState.accelerometer.X;

                            AcceYtext.Text = $"Accelerometer Y      {dualsense.ButtonState.accelerometer.Y}";
                            AcceY.Value = dualsense.ButtonState.accelerometer.Y;

                            AcceZtext.Text = $"Accelerometer Z      {dualsense.ButtonState.accelerometer.Z}";
                            AcceZ.Value = dualsense.ButtonState.accelerometer.Z;

                            GyroXtext.Text = $"Gyroscope X      {dualsense.ButtonState.gyro.X}";
                            GyroX.Value = dualsense.ButtonState.gyro.X;

                            GyroYtext.Text = $"Gyroscope Y      {dualsense.ButtonState.gyro.Y}";
                            GyroY.Value = dualsense.ButtonState.gyro.Y;

                            GyroZtext.Text = $"Gyroscope Z        {dualsense.ButtonState.gyro.Z}";
                            GyroZ.Value = dualsense.ButtonState.gyro.Z;
                        }
                    });
                }
                catch (TaskCanceledException) { }

                Thread.Sleep(50);
            }
        }

        private void WatchBatteryStatus()
        {
            while (true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (dualsense != null && dualsense.Working)
                    {
                        dualsense.Battery.Level = 75;
                        if(lightbarBattery.IsChecked == true)
                        {
                            if(dualsense.Battery.Level >= 75)
                            {
                                dualsense.SetLightbarTransition(0, 255, 0, 10, 100);
                            }
                            else if(dualsense.Battery.Level <= 50 && dualsense.Battery.Level >= 25)
                            {
                                dualsense.SetLightbarTransition(255, 255, 0, 10, 100);
                            }
                            else if (dualsense.Battery.Level <= 25 && dualsense.Battery.Level >= 10)
                            {
                                dualsense.SetLightbarTransition(255, 0, 0, 10, 100);
                            }
                            else if (dualsense.Battery.Level <= 10)
                            {
                                dualsense.SetLightbarTransition(10, 0, 0, 10, 100);
                            }
                        }

                        if (ledBattery.IsChecked == true)
                        {
                            if (dualsense.Battery.Level >= 75)
                            {
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_4);
                            }
                            else if (dualsense.Battery.Level <= 50 && dualsense.Battery.Level >= 25)
                            {
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_3);
                            }
                            else if (dualsense.Battery.Level <= 25 && dualsense.Battery.Level >= 10)
                            {
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_2);
                            }
                            else if (dualsense.Battery.Level <= 10)
                            {
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_1);
                            }
                        }

                        if (udp != null && udp.serverOn)
                        {
                            udp.Battery = dualsense.Battery.Level;
                        }

                        switch (dualsense.Battery.State)
                        {
                            case BatteryState.State.POWER_SUPPLY_STATUS_DISCHARGING:
                                batteryStatusText.Text = $"Battery Status: DISCHARGING | {dualsense.Battery.Level}%";
                                break;

                            case BatteryState.State.POWER_SUPPLY_STATUS_CHARGING:
                                batteryStatusText.Text = $"Battery Status: CHARGING | {dualsense.Battery.Level}%";
                                break;

                            default:
                                batteryStatusText.Text = $"Battery Status: UNKNOWN | {dualsense.Battery.Level}%";
                                break;
                        }
                    }
                });

                Thread.Sleep(5000);
            }
        }

        private void WatchAudioDefaultDevice()
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            while (true)
            {
                MMDevice newDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (newDevice.ID != device.ID)
                {
                    if (!newDevice.FriendlyName.Contains("Wireless Controller"))
                    {
                        device = newDevice;
                        watchSystemAudio = false;
                        Thread.Sleep(1000);
                        watchSystemAudio = true;
                        new Thread(() => { Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Priority = ThreadPriority.Lowest; WatchSystemAudioLevel(); }).Start();
                        if (dualsense != null && dualsense.Working && newDevice.State == DeviceState.Active)
                        {
                            dualsense.ReinitializeHapticFeedback();
                        }
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            if (audioToHapticsBtn.IsChecked == true)
                            {
                                audioToHapticsBtn.IsChecked = false;
                                System.Windows.MessageBox.Show("You can't use audio passthrough if the DualSense Wireless Controller is set as the default output device.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    }
                }

                Thread.Sleep(100);
            }
        }

        private void MyNotifyIcon_Click(object? sender, EventArgs e)
        {
            this.Show();
            this.Activate();
            this.Focus();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            MyNotifyIcon.Visible = false;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized && minimizeToTrayBox.IsChecked == true)
            {
                this.ShowInTaskbar = false;
                MyNotifyIcon.Visible = true;
            }
        }

        private bool micOff = false;
        private Stopwatch screenshotCooldown = new Stopwatch();

        private void HandleHotkey(int selectedIndex, int boxIndex)
        {
            switch (selectedIndex)
            {
                case 0: // Screenshot
                    {
                        if (screenshotCooldown.ElapsedMilliseconds > 500)
                        {
                            Utils.ScreenshotToClipboard();
                            screenshotCooldown.Restart();
                            dualsense.PlayHaptics("screenshot.wav", (float)speakerSlider.Value, (float)leftActuatorSlider.Value, (float)rightActuatorSlider.Value, true);
                        }
                        break;
                    }
                case 1: // X360 Controller emu
                    {
                        if (VigemAndHidHidePresent && emuStatusForConfig == 0 || emuStatusForConfig == 2)
                        {
                            x360Emu();
                        }
                        else if (VigemAndHidHidePresent && emuStatusForConfig == 1)
                        {
                            stopEmu();
                        }
                        break;
                    }
                case 2: // DS4 emu
                    {
                        if (VigemAndHidHidePresent && emuStatusForConfig == 0 || emuStatusForConfig == 1)
                        {
                            ds4Emu();
                        }
                        else if (VigemAndHidHidePresent && emuStatusForConfig == 2)
                        {
                            stopEmu();
                        }
                        break;
                    }
                case 3: // Audio passthrough
                    {
                        if (dualsense.ConnectionType == ConnectionType.USB)
                        {
                            if (audioToHapticsBtn.IsChecked == false)
                            {
                                audioToHapticsBtn.IsChecked = true;
                            }
                            else
                            {
                                audioToHapticsBtn.IsChecked = false;
                            }
                        }
                        break;
                    }
                case 4:
                    {
                        try
                        {
                            if (customHotkey[boxIndex] != null && customHotkey[boxIndex] != string.Empty)
                            {
                                System.Windows.Forms.SendKeys.SendWait(customHotkey[boxIndex]);
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "There was an error with your hotkey", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;
                    }
            }
        }

        private void WatchHotkeys()
        {
            bool micOff = false;
            bool lastMicButton = false;
            bool lastUp = false;
            bool lastLeft = false;
            bool lastRight = false;
            bool lastDown = false;
            while (true)
            {
                Thread.Sleep(100);
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (dualsense != null && dualsense.Working)
                        {
                            if (!dualsense.ButtonState.micBtn && lastMicButton && !lastUp && !lastLeft && !lastRight && !lastDown)
                            {
                                HandleHotkey(hotkeyBoxMic.SelectedIndex, 0); // MIC BUTTON
                            }
                            else if (!dualsense.ButtonState.micBtn && lastMicButton
                            && !dualsense.ButtonState.DpadUp && lastUp)
                            {
                                HandleHotkey(hotkeyBoxMicPlusUp.SelectedIndex, 1);
                            }
                            else if (!dualsense.ButtonState.micBtn && lastMicButton
                            && !dualsense.ButtonState.DpadLeft && lastLeft)
                            {
                                HandleHotkey(hotkeyBoxMicPlusLeft.SelectedIndex, 3);
                            }
                            else if (!dualsense.ButtonState.micBtn && lastMicButton
                            && !dualsense.ButtonState.DpadRight && lastRight)
                            {
                                HandleHotkey(hotkeyBoxMicPlusRight.SelectedIndex, 2);
                            }
                            else if (!dualsense.ButtonState.micBtn && lastMicButton
                            && !dualsense.ButtonState.DpadDown && lastDown)
                            {
                                HandleHotkey(hotkeyBoxMicPlusDown.SelectedIndex, 4);
                            }

                            lastMicButton = dualsense.ButtonState.micBtn;
                            lastUp = dualsense.ButtonState.DpadUp;
                            lastLeft = dualsense.ButtonState.DpadLeft;
                            lastRight = dualsense.ButtonState.DpadRight;
                            lastDown = dualsense.ButtonState.DpadDown;
                        }
                    });
                }
                catch (TaskCanceledException) { Environment.Exit(0); }
            }
        }

        private void Events_NewPacket(object? sender, Events.PacketEvent e)
        {
            UDPtime.Restart();
            if (dualsense != null && dualsense.Working && connected)
            {
                foreach (UDP.Instruction instruction in e.packet.instructions)
                {
                    switch (instruction.type)
                    {
                        case UDP.InstructionType.MicLED:
                            switch ((UDP.MicLEDMode)Convert.ToInt32(instruction.parameters[1]))
                            {
                                case UDP.MicLEDMode.Pulse:
                                    dualsense.SetMicrophoneLED(LED.MicrophoneLED.ON);
                                    break;

                                case UDP.MicLEDMode.On:
                                    dualsense.SetMicrophoneLED(LED.MicrophoneLED.ON);
                                    break;

                                case UDP.MicLEDMode.Off:
                                    dualsense.SetMicrophoneLED(LED.MicrophoneLED.OFF);
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
                                    byte start = instruction.parameters[3] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[3])) : (byte)0;
                                    byte force = instruction.parameters[4] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[4])) : (byte)0;

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
                                    start = instruction.parameters[3] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[3])) : (byte)0;
                                    byte end = instruction.parameters[4] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[4])) : (byte)0;
                                    force = instruction.parameters[5] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[5])) : (byte)0;
                                    byte snapForce = instruction.parameters[6] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[6])) : (byte)0;

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
                                    start = instruction.parameters[3] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[3])) : (byte)0;
                                    end = instruction.parameters[4] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[4])) : (byte)0;
                                    byte firstFoot = instruction.parameters[5] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[5])) : (byte)0;
                                    byte secondFoot = instruction.parameters[6] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[6])) : (byte)0;
                                    byte frequency = instruction.parameters[7] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[7])) : (byte)0;

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
                                    start = instruction.parameters[3] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[3])) : (byte)0;
                                    end = instruction.parameters[4] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[4])) : (byte)0;
                                    force = instruction.parameters[5] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[5])) : (byte)0;

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
                                    start = instruction.parameters[3] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[3])) : (byte)0;
                                    byte strength = instruction.parameters[4] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[4])) : (byte)0;
                                    frequency = instruction.parameters[4] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[4])) : (byte)0;

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

                                    start = instruction.parameters[3] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[3])) : (byte)0;
                                    end = instruction.parameters[4] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[4])) : (byte)0;
                                    byte strengthA = instruction.parameters[5] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[5])) : (byte)0;
                                    byte strengthB = instruction.parameters[6] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[6])) : (byte)0;
                                    frequency = instruction.parameters[7] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[7])) : (byte)0;
                                    byte period = instruction.parameters[8] != null ? Convert.ToByte(Convert.ToInt32(instruction.parameters[8])) : (byte)0;

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
                                    dualsense.SetLeftTrigger(triggerType, triggerForces[0], triggerForces[1], triggerForces[2], triggerForces[3], triggerForces[4], triggerForces[5], triggerForces[6]);
                                    break;

                                case UDP.Trigger.Right:
                                    dualsense.SetRightTrigger(triggerType, triggerForces[0], triggerForces[1], triggerForces[2], triggerForces[3], triggerForces[4], triggerForces[5], triggerForces[6]);
                                    break;
                            }

                            break;

                        case UDP.InstructionType.RGBUpdate:
                            if (Convert.ToInt32(instruction.parameters[1]) >= 0 && Convert.ToInt32(instruction.parameters[2]) >= 0 && Convert.ToInt32(instruction.parameters[3]) >= 0)
                                dualsense.SetLightbar(Convert.ToInt32(instruction.parameters[1]), Convert.ToInt32(instruction.parameters[2]), Convert.ToInt32(instruction.parameters[3]));
                            break;

                        case UDP.InstructionType.TriggerThreshold:
                            if (controllerEmulation != null)
                            {
                                controllerEmulation.leftTriggerThreshold = Convert.ToInt32(instruction.parameters[1]);
                                controllerEmulation.rightTriggerThreshold = Convert.ToInt32(instruction.parameters[2]);
                            }
                            break;

                        case UDP.InstructionType.PlayerLEDNewRevision:
                            switch ((UDP.PlayerLEDNewRevision)Convert.ToInt32(instruction.parameters[1]))
                            {
                                case UDP.PlayerLEDNewRevision.AllOff:
                                    dualsense.SetPlayerLED(LED.PlayerLED.OFF);
                                    break;

                                case UDP.PlayerLEDNewRevision.One:
                                    dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_1);
                                    break;

                                case UDP.PlayerLEDNewRevision.Two:
                                    dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_2);
                                    break;

                                case UDP.PlayerLEDNewRevision.Three:
                                    dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_3);
                                    break;

                                case UDP.PlayerLEDNewRevision.Four:
                                    dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_4);
                                    break;

                                case UDP.PlayerLEDNewRevision.Five:
                                    dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_4);
                                    break;
                            }
                            break;

                        case UDP.InstructionType.HapticFeedback:
                            dualsense.SetVibrationType(Vibrations.VibrationType.Haptic_Feedback);
                            this.Dispatcher.Invoke(() => { audioToHapticsBtn.IsChecked = false; });
                            dualsense.PlayHaptics((string)instruction.parameters[1], (float)Convert.ToSingle(instruction.parameters[2]), (float)Convert.ToSingle(instruction.parameters[3]), (float)Convert.ToSingle(instruction.parameters[4]), (bool)Convert.ToBoolean(instruction.parameters[5]));
                            break;

                        case UDP.InstructionType.RGBTransitionUpdate:
                            dualsense.SetLightbarTransition(Convert.ToInt32(instruction.parameters[1]), Convert.ToInt32(instruction.parameters[2]), Convert.ToInt32(instruction.parameters[3]), Convert.ToInt32(instruction.parameters[4]), Convert.ToInt32(instruction.parameters[5]));
                            break;
                    }
                }
            }
        }

        private MMDeviceEnumerator MDE = new MMDeviceEnumerator();
        private MMDevice[] MD = new MMDevice[4];
        private WaveInEvent waveInStream = new WaveInEvent();
        private bool watchSystemAudio = true;

        private void WatchSystemAudioLevel()
        {
            MMDevice defaultAudio = MDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            while (watchSystemAudio)
            {
                if (UDPtime.ElapsedMilliseconds >= 8000 && dualsense != null && dualsense.Working)
                {
                    var AMI = defaultAudio.AudioMeterInformation;
                    float value = AMI.PeakValues[0] * 300;
                    float value2 = AMI.PeakValues[0] * 500;

                    if (audioToLED)
                    {
                        if (value <= 255)
                        {
                            dualsense.SetLightbar((int)value, (int)value, (int)value);
                        }

                        if (value2 < 25)
                        {
                            dualsense.SetPlayerLED(LED.PlayerLED.OFF);
                        }
                        else if (value2 > 25 && value2 < 50)
                        {
                            dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_1);
                        }
                        else if (value2 > 50 && value2 < 75)
                        {
                            dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_2);
                        }
                        else if (value2 > 75 && value < 95)
                        {
                            dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_3);
                        }
                        else if (value2 > 95)
                        {
                            dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_4);
                        }
                    }
                }
                Thread.Sleep(15);
            }
        }

        private const int maxX = 1919;
        private const int maxY = 1079;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy,
                      int dwData, int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        private POINT lastTouchPad = new POINT();
        private POINT lastCursorPos = new POINT();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public struct POINT
        {
            public int X;
            public int Y;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private int sensitivity = 3;

        private void ReadTouchpad()
        {
            GetCursorPos(out POINT posfirst);
            lastCursorPos = posfirst;
            double accelerationFactor = 0.01; // This can be adjusted
            bool wasHeld = false;
            bool wasClicked = false;
            int swipeX = 0;
            int swipeY = 0;

            while (true)
            {
                GetCursorPos(out POINT pos);
                if (dualsense != null && dualsense.Working)
                {
                    try
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            if (!dualsense.ButtonState.touchBtn && wasClicked && useTouchpadAsMouse)
                            {
                                mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
                            }

                            if (dualsense.ButtonState.trackPadTouch0.IsActive)
                            {
                                if (wasHeld && useTouchpadAsMouse)
                                {
                                    swipeX = dualsense.ButtonState.trackPadTouch0.X - lastTouchPad.X;
                                    swipeY = dualsense.ButtonState.trackPadTouch0.Y - lastTouchPad.Y;
                                    SetCursorPos(pos.X + swipeX / 5 * sensitivity, pos.Y + swipeY / 5 * sensitivity);
                                }
                                else
                                {
                                    swipeX = 0;
                                    swipeY = 0;
                                }

                                touchLeftDot.Visibility = Visibility.Visible;
                                touchPadText.Text = $"Track Touch 1: X={dualsense.ButtonState.trackPadTouch0.X}, Y={dualsense.ButtonState.trackPadTouch0.Y}";

                                touchLeftDot.Margin = new Thickness(ScaleToMax(dualsense.ButtonState.trackPadTouch0.X, maxX, 285), ScaleToMax(dualsense.ButtonState.trackPadTouch0.Y, maxY, 135), 0, 0);

                                if (dualsense.ButtonState.touchBtn)
                                {
                                    if (useTouchpadAsMouse)
                                    {
                                        if (dualsense.ButtonState.trackPadTouch0.X > 1100)
                                        {
                                            if (!wasClicked)
                                            {
                                                mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
                                            }
                                        }
                                        else
                                        {
                                            mouse_event((int)(MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);
                                            mouse_event((int)(MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
                                        }
                                    }

                                    wasClicked = true;
                                    touchPadBorder.BorderBrush = new SolidColorBrush(Colors.Red);
                                }
                                else
                                {
                                    wasClicked = false;
                                    touchPadBorder.BorderBrush = new SolidColorBrush(Colors.Black);
                                }

                                if (dualsense.ButtonState.trackPadTouch1.IsActive)
                                {
                                    touchPadText.Text = $"Track Touch 1: X={dualsense.ButtonState.trackPadTouch0.X}, Y={dualsense.ButtonState.trackPadTouch0.Y}\nTrack Touch 2: X={dualsense.ButtonState.trackPadTouch1.X}, Y={dualsense.ButtonState.trackPadTouch1.Y}";
                                    touchRightDot.Visibility = Visibility.Visible;
                                    touchRightDot.Margin = new Thickness(ScaleToMax(dualsense.ButtonState.trackPadTouch1.X, maxX, 285), ScaleToMax(dualsense.ButtonState.trackPadTouch1.Y, maxY, 135), 0, 0);
                                }
                                else
                                    touchRightDot.Visibility = Visibility.Hidden;

                                wasHeld = true;
                            }
                            else
                            {
                                wasHeld = false;
                                swipeX = 0;
                                swipeY = 0;
                                touchPadText.Text = string.Empty;
                                touchLeftDot.Visibility = Visibility.Hidden;
                                touchRightDot.Visibility = Visibility.Hidden;
                            }
                        });

                        lastTouchPad.X = dualsense.ButtonState.trackPadTouch0.X;
                        lastTouchPad.Y = dualsense.ButtonState.trackPadTouch0.Y;
                    }
                    catch
                    {
                        continue;
                    }
                }

                lastCursorPos = pos;
                Thread.Sleep(10);
            }
        }

        private static double ScaleToMax(double value, double maxOriginal, double maxTarget)
        {
            // Calculate the scaling factor
            double scaleFactor = maxTarget / maxOriginal;
            // Scale the value
            return value * scaleFactor;
        }

        private bool connectedToController = false;
        private bool wereSettingsReset = false;

        private async void WatchUDPUpdates()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (dualsense != null && dualsense.Working && connectedToController)
                {
                    controlPanel.Visibility = Visibility.Visible;
                    ledTab.IsEnabled = true;
                    triggersTab.IsEnabled = true;
                    loadConfigBtn.Visibility = Visibility.Visible;
                    saveConfigBtn.Visibility = Visibility.Visible;
                }
            });
            Thread.Sleep(8500); // wait a sec before starting

            while (udp.serverOn)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (UDPtime.ElapsedMilliseconds >= 8000)
                    {
                        if (dualsense != null && dualsense.Working && connectedToController)
                        {
                            if (!wereSettingsReset)
                            {
                                ReadCurrentValues();
                                wereSettingsReset = true;
                            }

                            controlPanel.Visibility = Visibility.Visible;
                            ledTab.IsEnabled = true;
                            triggersTab.IsEnabled = true;
                            loadConfigBtn.Visibility = Visibility.Visible;
                            saveConfigBtn.Visibility = Visibility.Visible;
                        }

                        if (udp == null || udp.unavailable)
                        {
                            udpStatus.Text = "UDP: Unavailable";
                            udpStatusDot.Fill = new SolidColorBrush(Colors.Gray);
                        }
                        else
                        {
                            udpStatus.Text = "UDP: Inactive";
                            udpStatusDot.Fill = new SolidColorBrush(Colors.Red);
                        }
                    }
                    else
                    {
                        if (btnConnect.Content != "Disconnect Controller")
                            controlPanel.Visibility = Visibility.Hidden;
                        ledTab.IsEnabled = false;
                        triggersTab.IsEnabled = false;
                        discoBox.IsChecked = false;
                        lightbarBattery.IsChecked = false;
                        ledBattery.IsChecked = false;
                        loadConfigBtn.Visibility = Visibility.Hidden;
                        saveConfigBtn.Visibility = Visibility.Hidden;
                        wereSettingsReset = false;

                        if (ledTab.IsSelected || triggersTab.IsSelected)
                            controlPanel.SelectedIndex = 0;

                        udpStatus.Text = "UDP: Active";
                        udpStatusDot.Fill = new SolidColorBrush(Colors.Green);
                    }
                });

                Thread.Sleep(125);
            }

            this.Dispatcher.Invoke(() =>
            {
                if (udp == null || udp.unavailable)
                {
                    udpStatus.Text = "UDP: Unavailable";
                    udpStatusDot.Fill = new SolidColorBrush(Colors.Gray);
                }
            });
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
            if (btnConnect.Content == "Disconnect Controller")
            {
                isDiscoOn = false;
                stopEmu();
                RestoreController(true, dualsense, true);
                isHiding = false;
                dualsense.Dispose();
                UpdateConnectionStatus();
                connected = false;
            }
            else
            {
                ConnectToController();
            }
        }

        private void EnumerateControllers()
        {
            cmbControllerSelect.Items.Clear();

            foreach (string controller in DualsenseUtils.GetControllerIDs())
            {
                cmbControllerSelect.Items.Add(controller.Split("&")[3]);
            }
        }

        private void ConnectToController()
        {
            try
            {
                batteryStatusText.Text = "Battery Status: UNKNOWN | ?%";
                if (dualsense != null && dualsense.Working)
                {
                    dualsense.Dispose();
                }

                string devicePath = string.Empty;

                try
                {
                    devicePath = DualsenseUtils.GetControllerIDs().Where(x => x.Contains(cmbControllerSelect.SelectedValue.ToString())).FirstOrDefault();
                }
                catch
                {
                    MessageBox.Show("Something went wrong, try refreshing the controller list by right clicking the dropdown box", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                dualsense = new Dualsense(devicePath);
                dualsense.Start();
                dualsense.Connection.ControllerDisconnected += Connection_ControllerDisconnected;
                currentControllerNumber = 0;
                currentControllerID = cmbControllerSelect.SelectedValue.ToString();
                currentControllerPath = dualsense.DeviceID;
                controllerEmulation = new ControllerEmulation();
                controllerEmulation.dualsense = dualsense;

                connected = true;
                ReadCurrentValues();
                UpdateConnectionStatus();
                audioToHapticsBtn.IsChecked = false;

                if(Properties.Settings.Default.port1 == currentControllerID)
                {
                    ApplySettingsFromProfile(settings.ReadProfileFromFile(DualSenseY.Properties.Settings.Default.config1));
                }
                else if (Properties.Settings.Default.port2 == currentControllerID)
                {
                    ApplySettingsFromProfile(settings.ReadProfileFromFile(DualSenseY.Properties.Settings.Default.config2));
                }
                else if (Properties.Settings.Default.port3 == currentControllerID)
                {
                    ApplySettingsFromProfile(settings.ReadProfileFromFile(DualSenseY.Properties.Settings.Default.config3));
                }
                else if (Properties.Settings.Default.port4 == currentControllerID)
                {
                    ApplySettingsFromProfile(settings.ReadProfileFromFile(DualSenseY.Properties.Settings.Default.config4));
                }

            }
            catch (Exception error)
            {
                if (error.Message.Contains("Couldn't"))
                {
                    MessageBox.Show($"Controller is not plugged in");
                }
                else
                {
                    MessageBox.Show("ERROR, PLEASE CONTACT THE DEVELOPER" + "\n\n" + error.Message + "\n" + error.StackTrace, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Connection_ControllerDisconnected(object? sender, ConnectionStatus.Controller e)
        {
            if (isHiding)
            {
                dualsense.Dispose();
                this.Dispatcher.Invoke(() =>
                {
                    if (dualsense == null || !dualsense.Working)
                    {
                        dualsense = new Dualsense(currentControllerPath);
                        dualsense.Start();
                        dualsense.Connection.ControllerDisconnected += Connection_ControllerDisconnected;
                        currentControllerNumber = 0;
                        controllerEmulation.dualsense = dualsense;
                    }

                    if (emuStatusForConfig == 2 && dualsense != null && audioToHapticsBtn.IsChecked == false)
                    {
                        dualsense.PlayHaptics("blip.wav", 1, 0, 0, true);
                    }
                    ReadCurrentValues();
                });

                isHiding = false;
            }
            else
            {
                RestoreController(false, dualsense, false);
                UpdateConnectionStatus();
                dualsense.Dispose();
                controllerEmulation.Dispose();
                MessageBox.Show($"Controller {currentControllerID} has been disconnected!", "Controller update", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void cmbControllerSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void UpdateConnectionStatus()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (dualsense != null && dualsense.Working)
                {
                    connectedToController = true;

                    if (dualsense.DeviceType == DeviceType.DualSense_Edge)
                    {
                        edgeIcon.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        edgeIcon.Visibility = Visibility.Hidden;
                    }

                    txtStatus.Text = "Status: Connected";
                    btnConnect.Content = "Disconnect Controller";
                    controlPanel.Visibility = Visibility.Visible;
                    cmbControllerSelect.Visibility = Visibility.Hidden;
                    loadConfigBtn.Visibility = Visibility.Visible;
                    saveConfigBtn.Visibility = Visibility.Visible;
                    batteryStatusText.Visibility = Visibility.Visible;
                    connectedTo.Visibility = Visibility.Visible;
                    connectedTo.Text = "Connected to " + currentControllerID;

                    ds4EmuButton.IsEnabled = true;
                    x360EmuButton.IsEnabled = true;
                    stopEmuBtn.Visibility = Visibility.Hidden;
                    textUnderControllerEmuButtons.Visibility = Visibility.Visible;
                    crnEmulatingText.Text = string.Empty;

                    if (controllerEmulation.isViGEMBusInstalled)
                    {
                        ViGEMBusStatusText.Text = "ViGEMBus Status: Detected";
                        ViGEMBusDownloadBtn.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        VigemAndHidHidePresent = false;
                        ViGEMBusStatusText.Text = "ViGEMBus Status: Not found";
                        ViGEMBusDownloadBtn.Visibility = Visibility.Visible;
                        x360EmuButton.IsEnabled = false;
                        ds4EmuButton.IsEnabled = false;
                        textUnderControllerEmuButtons.Text = "Required software was not found, please install.";
                    }

                    try
                    {
                        if (hidHide.IsInstalled)
                        {
                            hidhideVersionText.Text = "HidHide version: " + hidHide.LocalDriverVersion;
                            HidHideDownloadBtn.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            VigemAndHidHidePresent = false;
                            hidhideVersionText.Text = "HidHide: Not found";
                            HidHideDownloadBtn.Visibility = Visibility.Visible;
                            x360EmuButton.IsEnabled = false;
                            ds4EmuButton.IsEnabled = false;
                            textUnderControllerEmuButtons.Text = "Required software was not found, please install.";
                        }
                    }
                    catch (Nefarius.Drivers.HidHide.Exceptions.HidHideDriverNotFoundException)
                    {
                        VigemAndHidHidePresent = false;
                        hidhideVersionText.Text = "HidHide: Not found";
                        HidHideDownloadBtn.Visibility = Visibility.Visible;
                        x360EmuButton.IsEnabled = false;
                        ds4EmuButton.IsEnabled = false;
                        textUnderControllerEmuButtons.Text = "Required software was not found, please install.";
                    }

                    if (dualsense.ConnectionType == ConnectionType.BT)
                    {
                        micTab.IsEnabled = false;
                        speakerTab.IsEnabled = false;
                        audioToHapticsBtn.IsEnabled = false;
                        connectionTypeBTicon.Visibility = Visibility.Visible;
                        connectionTypeUSBicon.Visibility = Visibility.Hidden;
                        if (controllerEmulation != null)
                        {
                            controllerEmulation.ForceStopRumble = false;
                        }
                    }
                    else
                    {
                        if (controllerEmulation != null && audioToHapticsBtn.IsChecked == true)
                        {
                            controllerEmulation.ForceStopRumble = true;
                        }
                        else if (controllerEmulation != null && audioToHapticsBtn.IsChecked == false)
                        {
                            controllerEmulation.ForceStopRumble = false;
                        }

                        micTab.IsEnabled = true;
                        speakerTab.IsEnabled = true;
                        soundLEDcheckbox.IsEnabled = true;
                        audioToHapticsBtn.IsEnabled = true;
                        touchpadTab.IsEnabled = true;
                        connectionTypeBTicon.Visibility = Visibility.Hidden;
                        connectionTypeUSBicon.Visibility = Visibility.Visible;
                    }
                }
                else if (dualsense == null || !dualsense.Working)
                {
                    connectedToController = false;
                    edgeIcon.Visibility = Visibility.Hidden;
                    txtStatus.Text = "Status: Disconnected";
                    btnConnect.Content = "Connect Controller";
                    controlPanel.Visibility = Visibility.Hidden;
                    cmbControllerSelect.Visibility = Visibility.Visible;
                    loadConfigBtn.Visibility = Visibility.Hidden;
                    saveConfigBtn.Visibility = Visibility.Hidden;
                    connectionTypeBTicon.Visibility = Visibility.Hidden;
                    connectionTypeUSBicon.Visibility = Visibility.Hidden;
                    controlPanel.SelectedIndex = 0;
                    batteryStatusText.Visibility = Visibility.Hidden;
                    connectedTo.Visibility = Visibility.Hidden;
                    connectedTo.Text = "Connected to ???";
                }
            });
        }

        private void ReadCurrentValues()
        {
            if (this.IsInitialized && dualsense != null && dualsense.Working)
            {
                this.Dispatcher.Invoke(() =>
                {
                    dualsense.SetSpeakerVolumeInSoftware((float)speakerSlider.Value, (float)leftActuatorSlider.Value, (float)rightActuatorSlider.Value);
                    if (!audioToLED && connected)
                    {
                        dualsense.SetLightbarTransition((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value, 50, 5);
                        switch (LEDbox.SelectedIndex)
                        {
                            case 0:
                                dualsense.SetPlayerLED(LED.PlayerLED.OFF);
                                break;

                            case 1:
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_1);
                                break;

                            case 2:
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_2);
                                break;

                            case 3:
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_3);
                                break;

                            case 4:
                                dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_4);
                                break;

                            case 5:
                                dualsense.SetPlayerLED(LED.PlayerLED.ALL);
                                break;
                        }
                    }
                    if (micLEDcheckbox.IsChecked == true && connected)
                        dualsense.SetMicrophoneLED(LED.MicrophoneLED.ON);
                    else
                        dualsense.SetMicrophoneLED(LED.MicrophoneLED.OFF);

                    if (outputHeadsetBox.IsChecked == true && connected)
                        dualsense.SetAudioOutput(AudioOutput.HEADSET);
                    else
                        dualsense.SetAudioOutput(AudioOutput.SPEAKER);

                    if (discoBox.IsChecked == true && connected)
                    {
                        isDiscoOn = true;
                    }
                    else
                    {
                        isDiscoOn = false;
                    }

                    dualsense.SetLeftTrigger(currentLeftTrigger, leftTriggerForces[0], leftTriggerForces[1], leftTriggerForces[2], leftTriggerForces[3], leftTriggerForces[4], leftTriggerForces[5], leftTriggerForces[6]);
                    dualsense.SetRightTrigger(currentRightTrigger, rightTriggerForces[0], rightTriggerForces[1], rightTriggerForces[2], rightTriggerForces[3], rightTriggerForces[4], rightTriggerForces[5], rightTriggerForces[6]);
                });
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
            if ((byte)sliderLeftMotor.Value == 0 && (byte)sliderRightMotor.Value == 0)
            {
                dualsense.SetVibrationType(Vibrations.VibrationType.Haptic_Feedback);
                dualsense.SetStandardRumble((byte)sliderLeftMotor.Value, (byte)sliderRightMotor.Value);
            }
            else
            {
                dualsense.SetVibrationType(Vibrations.VibrationType.Standard_Rumble);
                dualsense.SetStandardRumble((byte)sliderLeftMotor.Value, (byte)sliderRightMotor.Value);
            }
        }

        private void sliderLED_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dualsense.SetLightbar((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value);
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
                dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[0] = Convert.ToByte(sliderForce1.Value);
                dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce2.Text = Convert.ToByte(sliderForce2.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[1] = Convert.ToByte(sliderForce2.Value);
                dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[1] = Convert.ToByte(sliderForce2.Value);
                dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce3.Text = Convert.ToByte(sliderForce3.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[2] = Convert.ToByte(sliderForce3.Value);
                dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[2] = Convert.ToByte(sliderForce3.Value);
                dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce4_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce4.Text = Convert.ToByte(sliderForce4.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[3] = Convert.ToByte(sliderForce4.Value);
                dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[3] = Convert.ToByte(sliderForce4.Value);
                dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce5_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce5.Text = Convert.ToByte(sliderForce5.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[4] = Convert.ToByte(sliderForce5.Value);
                dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[4] = Convert.ToByte(sliderForce5.Value);
                dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce6_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce6.Text = Convert.ToByte(sliderForce6.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[5] = Convert.ToByte(sliderForce6.Value);
                dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[5] = Convert.ToByte(sliderForce6.Value);
                dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
        }

        private void sliderForce7_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textForce7.Text = Convert.ToByte(sliderForce7.Value).ToString();
            if (leftTrigger)
            {
                leftTriggerForces[6] = Convert.ToByte(sliderForce7.Value);
                dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
            }
            else
            {
                rightTriggerForces[6] = Convert.ToByte(sliderForce7.Value);
                dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
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

                    dualsense.SetLeftTrigger(currentLeftTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
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

                    dualsense.SetRightTrigger(currentRightTrigger, Convert.ToInt32(sliderForce1.Value), Convert.ToInt32(sliderForce2.Value), Convert.ToInt32(sliderForce3.Value), Convert.ToInt32(sliderForce4.Value), Convert.ToInt32(sliderForce5.Value), Convert.ToInt32(sliderForce6.Value), Convert.ToInt32(sliderForce7.Value));
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            audioToLED = false;
            if (controllerEmulation != null)
                controllerEmulation.Dispose();

            if (udp != null)
            {
                udp.Dispose();
            }

            RestoreController(true, dualsense, true);
        }

        private void LEDbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                switch (LEDbox.SelectedIndex)
                {
                    case 0:
                        dualsense.SetPlayerLED(LED.PlayerLED.OFF);
                        break;

                    case 1:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_1);
                        break;

                    case 2:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_2);
                        break;

                    case 3:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_3);
                        break;

                    case 4:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_4);
                        break;

                    case 5:
                        dualsense.SetPlayerLED(LED.PlayerLED.ALL);
                        break;
                }
            }
        }

        private void micLEDcheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                dualsense.SetMicrophoneLED(LED.MicrophoneLED.ON);
            }
        }

        private void micLEDcheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                dualsense.SetMicrophoneLED(LED.MicrophoneLED.OFF);
            }
        }

        private void sliderMicVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsInitialized)
            {
                dualsense.SetMicrophoneVolume((int)sliderMicVolume.Value);
                micVolumeText.Text = $"Microphone Volume: {(int)sliderMicVolume.Value}";
            }
        }

        private void testSpeakerButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                try
                {
                    dualsense.SetVibrationType(Vibrations.VibrationType.Haptic_Feedback);
                    AudioTestCooldown.Restart();
                    dualsense.PlayHaptics("audiotest.wav", (float)speakerSlider.Value, (float)leftActuatorSlider.Value, (float)rightActuatorSlider.Value, true);
                }
                catch (Exception ex) { MessageBox.Show("Unhandled exception occurred, contact developer: \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            version.Update();
        }

        private void saveConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                Settings.Profile profile = new Settings.Profile();
                profile.R = (int)sliderRed.Value;
                profile.G = (int)sliderGreen.Value;
                profile.B = (int)sliderBlue.Value;
                if ((bool)micLEDcheckbox.IsChecked)
                    profile.microphoneLED = LED.MicrophoneLED.ON;
                else
                    profile.microphoneLED = LED.MicrophoneLED.OFF;
                switch (LEDbox.SelectedIndex)
                {
                    case 0:
                        profile.playerLED = LED.PlayerLED.OFF;
                        break;

                    case 1:
                        profile.playerLED = LED.PlayerLED.PLAYER_1;
                        break;

                    case 2:
                        profile.playerLED = LED.PlayerLED.PLAYER_2;
                        break;

                    case 3:
                        profile.playerLED = LED.PlayerLED.PLAYER_3;
                        break;

                    case 4:
                        profile.playerLED = LED.PlayerLED.PLAYER_4;
                        break;

                    case 5:
                        profile.playerLED = LED.PlayerLED.ALL;
                        break;
                }
                profile.leftTriggerMode = currentLeftTrigger;
                profile.rightTriggerMode = currentRightTrigger;
                profile.leftTriggerForces = leftTriggerForces;
                profile.rightTriggerForces = rightTriggerForces;

                profile.HotKey1 = hotkeyBoxMic.SelectedIndex;
                profile.HotKey2 = hotkeyBoxMicPlusUp.SelectedIndex;
                profile.HotKey3 = hotkeyBoxMicPlusRight.SelectedIndex;
                profile.HotKey4 = hotkeyBoxMicPlusLeft.SelectedIndex;
                profile.HotKey5 = hotkeyBoxMicPlusDown.SelectedIndex;
                profile.customHotkey = this.customHotkey;
                profile.customHotkeyIndex = this.customHotkeyIndex;

                if (controllerEmulation != null)
                {
                    profile.IgnoreDS4Lightbar = (bool)ds4LightbarIgnoreBox.IsChecked;
                }

                profile.UseTouchpadAsMouse = (bool)useAsMouseBox.IsChecked;
                profile.TouchpadSensitivity = (int)sensitivitySlider.Value;
                profile.ControllerEmulation = emuStatusForConfig;
                profile.SpeakerVolume = (float)speakerSlider.Value;
                profile.MicrophoneVolume = (int)sliderMicVolume.Value;
                profile.LeftActuatorVolume = (float)leftActuatorSlider.Value;
                profile.RightActuatorVolume = (float)rightActuatorSlider.Value;
                profile.UseHeadset = (bool)outputHeadsetBox.IsChecked;

                profile.DiscoMode = (bool)discoBox.IsChecked;
                profile.DiscoSpeed = discoSpeed;

                profile.LightbarBattery = (bool)lightbarBattery.IsChecked;
                profile.LEDBattery = (bool)ledBattery.IsChecked;

                var dialog = new Microsoft.Win32.SaveFileDialog();

                if (!Directory.Exists(Settings.Path))
                    Directory.CreateDirectory(Settings.Path);

                dialog.InitialDirectory = Settings.Path;
                dialog.DefaultExt = ".dyp"; // Default file extension
                dialog.Filter = "DualSenseY Profile (.dyp)|*.dyp"; // Filter files by extension

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    try
                    {
                        settings.SaveProfileToFile(dialog.FileName, profile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Config creation failed!\n" + ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void configBtn_Click(object sender, RoutedEventArgs e)
        {
            var control = (Button)sender;
            string path = Settings.Path + "\\" + control.Content + ".dyp";
        }

        private void controlPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controlPanel.SelectedIndex == 2)
            {
            }
        }

        private void loadConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            if (!Directory.Exists(Settings.Path))
                Directory.CreateDirectory(Settings.Path);

            dialog.InitialDirectory = Settings.Path;
            dialog.FileName = "DualSense Profile"; // Default file name
            dialog.DefaultExt = ".dyp"; // Default file extension
            dialog.Filter = "DualSenseY Profile (.dyp)|*.dyp"; // Filter files by extension

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string path = dialog.FileName;

                if (File.Exists(path))
                {
                    Settings.Profile profile = settings.ReadProfileFromFile(path);

                    if (profile != null)
                    {
                        ApplySettingsFromProfile(profile);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("File is corrupted", "File read error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("File doesn't exist", "File read error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApplySettingsFromProfile(Settings.Profile profile)
        {
            dualsense.SetLightbarTransition(profile.R, profile.G, profile.B, 10, 10);
            dualsense.SetLeftTrigger(profile.leftTriggerMode, profile.leftTriggerForces[0], profile.leftTriggerForces[1], profile.leftTriggerForces[2], profile.leftTriggerForces[3], profile.leftTriggerForces[4], profile.leftTriggerForces[5], profile.leftTriggerForces[6]);
            dualsense.SetRightTrigger(profile.rightTriggerMode, profile.rightTriggerForces[0], profile.rightTriggerForces[1], profile.rightTriggerForces[2], profile.rightTriggerForces[3], profile.rightTriggerForces[4], profile.rightTriggerForces[5], profile.rightTriggerForces[6]);
            dualsense.SetMicrophoneLED(profile.microphoneLED);
            dualsense.SetPlayerLED(profile.playerLED);

            controlPanel.SelectedIndex = 0;
            triggerLeftOrRightBox.SelectedIndex = 0;
            sliderRed.Value = profile.R;
            sliderGreen.Value = profile.G;
            sliderBlue.Value = profile.B;
            currentLeftTrigger = profile.leftTriggerMode;
            currentRightTrigger = profile.rightTriggerMode;
            ds4LightbarIgnoreBox.IsChecked = profile.IgnoreDS4Lightbar;
            useAsMouseBox.IsChecked = profile.UseTouchpadAsMouse;
            sensitivitySlider.Value = profile.TouchpadSensitivity;
            speakerSlider.Value = profile.SpeakerVolume;
            leftActuatorSlider.Value = profile.LeftActuatorVolume;
            rightActuatorSlider.Value = profile.RightActuatorVolume;
            outputHeadsetBox.IsChecked = profile.UseHeadset;
            sliderMicVolume.Value = profile.MicrophoneVolume;
            discoBox.IsChecked = profile.DiscoMode;
            discoSpeedSlider.Value = profile.DiscoSpeed;
            discoSpeed = profile.DiscoSpeed;
            lightbarBattery.IsChecked = profile.LightbarBattery;
            ledBattery.IsChecked = profile.LEDBattery;

            hotkeyBoxMic.SelectedIndex = profile.HotKey1;
            hotkeyBoxMicPlusUp.SelectedIndex = profile.HotKey2;
            hotkeyBoxMicPlusRight.SelectedIndex = profile.HotKey3;
            hotkeyBoxMicPlusLeft.SelectedIndex = profile.HotKey4;
            hotkeyBoxMicPlusDown.SelectedIndex = profile.HotKey5;
            customHotkey = profile.customHotkey;
            customHotkeyIndex = profile.customHotkeyIndex;

            try
            {
                if (controllerEmulation != null && controllerEmulation.isViGEMBusInstalled && hidHide.IsInstalled)
                {
                    switch (profile.ControllerEmulation)
                    {
                        case 0:
                            stopEmu();
                            break;

                        case 1:
                            x360Emu();
                            break;

                        case 2:
                            ds4Emu();
                            break;
                    }
                }
            }
            catch { }

            switch (profile.playerLED)
            {
                case LED.PlayerLED.OFF:
                    LEDbox.SelectedIndex = 0;
                    break;

                case LED.PlayerLED.PLAYER_1:
                    LEDbox.SelectedIndex = 1;
                    break;

                case LED.PlayerLED.PLAYER_2:
                    LEDbox.SelectedIndex = 2;
                    break;

                case LED.PlayerLED.PLAYER_3:
                    LEDbox.SelectedIndex = 3;
                    break;

                case LED.PlayerLED.PLAYER_4:
                    LEDbox.SelectedIndex = 4;
                    break;

                case LED.PlayerLED.ALL:
                    LEDbox.SelectedIndex = 5;
                    break;
            }

            if (profile.microphoneLED == LED.MicrophoneLED.ON)
                micLEDcheckbox.IsChecked = true;
            else
                micLEDcheckbox.IsChecked = false;

            switch (currentLeftTrigger)
            {
                case TriggerType.TriggerModes.Off:
                    leftTriggerModeIndex = 0;
                    break;

                case TriggerType.TriggerModes.Rigid:
                    leftTriggerModeIndex = 1;
                    break;

                case TriggerType.TriggerModes.Pulse:
                    leftTriggerModeIndex = 2;
                    break;

                case TriggerType.TriggerModes.Rigid_A:
                    leftTriggerModeIndex = 3;
                    break;

                case TriggerType.TriggerModes.Rigid_B:
                    leftTriggerModeIndex = 4;
                    break;

                case TriggerType.TriggerModes.Rigid_AB:
                    leftTriggerModeIndex = 5;
                    break;

                case TriggerType.TriggerModes.Pulse_A:
                    leftTriggerModeIndex = 6;
                    break;

                case TriggerType.TriggerModes.Pulse_B:
                    leftTriggerModeIndex = 7;
                    break;

                case TriggerType.TriggerModes.Pulse_AB:
                    leftTriggerModeIndex = 8;
                    break;

                case TriggerType.TriggerModes.Calibration:
                    leftTriggerModeIndex = 9;
                    break;
            }

            switch (currentRightTrigger)
            {
                case TriggerType.TriggerModes.Off:
                    rightTriggerModeIndex = 0;
                    break;

                case TriggerType.TriggerModes.Rigid:
                    rightTriggerModeIndex = 1;
                    break;

                case TriggerType.TriggerModes.Pulse:
                    rightTriggerModeIndex = 2;
                    break;

                case TriggerType.TriggerModes.Rigid_A:
                    rightTriggerModeIndex = 3;
                    break;

                case TriggerType.TriggerModes.Rigid_B:
                    rightTriggerModeIndex = 4;
                    break;

                case TriggerType.TriggerModes.Rigid_AB:
                    rightTriggerModeIndex = 5;
                    break;

                case TriggerType.TriggerModes.Pulse_A:
                    rightTriggerModeIndex = 6;
                    break;

                case TriggerType.TriggerModes.Pulse_B:
                    rightTriggerModeIndex = 7;
                    break;

                case TriggerType.TriggerModes.Pulse_AB:
                    rightTriggerModeIndex = 8;
                    break;

                case TriggerType.TriggerModes.Calibration:
                    rightTriggerModeIndex = 9;
                    break;
            }

            leftTriggerForces = profile.leftTriggerForces;
            triggerModeCmb.SelectedIndex = leftTriggerModeIndex;
            sliderForce1.Value = leftTriggerForces[0];
            sliderForce2.Value = leftTriggerForces[1];
            sliderForce3.Value = leftTriggerForces[2];
            sliderForce4.Value = leftTriggerForces[3];
            sliderForce5.Value = leftTriggerForces[4];
            sliderForce6.Value = leftTriggerForces[5];
            sliderForce7.Value = leftTriggerForces[6];
            rightTriggerForces = profile.rightTriggerForces;
        }

        private void useAsMouseBox_Checked(object sender, RoutedEventArgs e)
        {
            useTouchpadAsMouse = true;
        }

        private void useAsMouseBox_Unchecked(object sender, RoutedEventArgs e)
        {
            useTouchpadAsMouse = false;
        }

        private void audioToHapticsBtn_Checked(object sender, RoutedEventArgs e)
        {
            MMDevice defaultAudio = MDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (!defaultAudio.FriendlyName.Contains("Wireless Controller"))
            {
                if (controllerEmulation != null)
                    controllerEmulation.ForceStopRumble = true;
                if (dualsense != null && dualsense.Working)
                {
                    dualsense.SetVibrationType(Vibrations.VibrationType.Haptic_Feedback);
                    dualsense.StartSystemAudioToHaptics();
                }
            }
            else
            {
                audioToHapticsBtn.IsChecked = false;
                System.Windows.MessageBox.Show("You can't use audio passthrough if the DualSense Wireless Controller is set as the default output device.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void audioToHapticsBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controllerEmulation != null)
                controllerEmulation.ForceStopRumble = false;
            if (dualsense != null && dualsense.Working)
            {
                dualsense.SetVibrationType(Vibrations.VibrationType.Haptic_Feedback);
                dualsense.StopSystemAudioToHaptics();
            }
        }

        private void soundLEDcheckbox_Checked(object sender, RoutedEventArgs e)
        {
            audioToLED = true;
            sliderRed.IsEnabled = false;
            sliderGreen.IsEnabled = false;
            sliderBlue.IsEnabled = false;
            LEDbox.IsEnabled = false;
            discoBox.IsChecked = false;
            discoBox.IsEnabled = false;
            lightbarBattery.IsEnabled = false;
            ledBattery.IsEnabled = false;
            discoSpeedSlider.Visibility = Visibility.Hidden;
        }

        private void soundLEDcheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if(lightbarBattery.IsChecked == false)
            {
                audioToLED = false;
                sliderRed.IsEnabled = true;
                sliderGreen.IsEnabled = true;
                sliderBlue.IsEnabled = true;
            }

            lightbarBattery.IsEnabled = true;
            ledBattery.IsEnabled = true;

            LEDbox.IsEnabled = true;

            if(lightbarBattery.IsChecked == false && ledBattery.IsChecked == false)
            {
                discoBox.IsEnabled = true;
                discoSpeedSlider.Visibility = Visibility.Visible;
            }

            if (dualsense != null && dualsense.Working)
            {
                dualsense.SetLightbar((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value);
                switch (LEDbox.SelectedIndex)
                {
                    case 0:
                        dualsense.SetPlayerLED(LED.PlayerLED.OFF);
                        break;

                    case 1:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_1);
                        break;

                    case 2:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_2);
                        break;

                    case 3:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_3);
                        break;

                    case 4:
                        dualsense.SetPlayerLED(LED.PlayerLED.PLAYER_4);
                        break;

                    case 5:
                        dualsense.SetPlayerLED(LED.PlayerLED.ALL);
                        break;
                }
            }

        }

        private void speakerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dualsense != null && dualsense.Working)
            {
                dualsense.SetSpeakerVolumeInSoftware((float)speakerSlider.Value, (float)leftActuatorSlider.Value, (float)rightActuatorSlider.Value);
            }
        }

        private void ViGEMBusDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", "https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0");
        }

        private void HidHideDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", "https://github.com/nefarius/HidHide/releases");
        }

        private int emuStatusForConfig = 0;

        private void ds4Emu()
        {
            try
            {
                emuStatusForConfig = 2;
                if (dualsense != null && dualsense.Working)
                {
                    controllerEmulation.StopEmulation();
                    HideController();
                    ds4EmuButton.IsEnabled = false;
                    x360EmuButton.IsEnabled = true;
                    stopEmuBtn.Visibility = Visibility.Visible;
                    controllerEmulation.StartDS4Emulation();

                    if (controllerEmulation.Emulating && !controllerEmulation.isEmulating360)
                    {
                        textUnderControllerEmuButtons.Visibility = Visibility.Hidden;
                        crnEmulatingText.Text = "Currently emulating: DualShock 4";
                    }
                    else
                    {
                        crnEmulatingText.Text = string.Empty;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Unhandled exception occurred, contact developer: \n" + ex.Message + "\n\nStackTrace:\n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void x360Emu()
        {
            try
            {
                emuStatusForConfig = 1;
                if (dualsense != null && dualsense.Working)
                {
                    controllerEmulation.StopEmulation();
                    HideController();
                    x360EmuButton.IsEnabled = false;
                    ds4EmuButton.IsEnabled = true;
                    stopEmuBtn.Visibility = Visibility.Visible;
                    controllerEmulation.StartX360Emulation();

                    if (controllerEmulation.Emulating && controllerEmulation.isEmulating360)
                    {
                        textUnderControllerEmuButtons.Visibility = Visibility.Hidden;
                        crnEmulatingText.Text = "Currently emulating: Xbox 360 controller";
                    }
                    else
                    {
                        crnEmulatingText.Text = string.Empty;
                    }

                    ReadCurrentValues();
                }
            }
            catch (Exception ex) { MessageBox.Show("Unhandled exception occurred, contact developer: \n" + ex.Message + "\n\nStackTrace:\n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void stopEmu()
        {
            emuStatusForConfig = 0;
            if (dualsense != null && dualsense.Working)
            {
                stopEmuBtn.Visibility = Visibility.Hidden;
                controllerEmulation.StopEmulation();
                ds4EmuButton.IsEnabled = true;
                x360EmuButton.IsEnabled = true;
                textUnderControllerEmuButtons.Visibility = Visibility.Visible;
                crnEmulatingText.Text = "";
                RestoreController(true, dualsense, false);
            }
        }

        private void x360EmuButton_Click(object sender, RoutedEventArgs e)
        {
            x360Emu();
        }

        private void ds4EmuButton_Click(object sender, RoutedEventArgs e)
        {
            ds4Emu();
        }

        private void stopEmuBtn_Click(object sender, RoutedEventArgs e)
        {
            stopEmu();
        }

        private bool isHiding = false;

        private void HideController()
        {
            string dirFullName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string instanceID = PnPDevice.GetInstanceIdFromInterfaceId(dualsense.DeviceID).ToString();
            hidHide.AddApplicationPath(dirFullName.Replace(".dll", ".exe"));
            hidHide.AddBlockedInstanceId(instanceID);
            hidHide.IsAppListInverted = false;
            hidHide.IsActive = true;
            isHiding = true;
            PnPDevice tempDevice = PnPDevice.GetDeviceByInterfaceId(dualsense.DeviceID);
            try
            {
                tempDevice.Disable();
            }
            catch { } // Do nothing, it's over.
            tempDevice.Enable();
        }

        private void RestoreController(bool restart, Dualsense dualsense, bool dispose)
        {
            try
            {
                if (hidHide.IsInstalled)
                {
                    if (dualsense != null && dualsense.Working)
                    {
                        PnPDevice tempDevice = PnPDevice.GetDeviceByInterfaceId(dualsense.DeviceID);

                        if (restart)
                        {
                            isHiding = true;
                            try
                            {
                                if (dispose)
                                {
                                    dualsense.Dispose();
                                }
                                tempDevice.Disable();
                            }
                            catch { }
                        }
                        else
                        {
                            isHiding = false;
                        }

                        tempDevice.Enable();
                    }

                    hidHide.ClearBlockedInstancesList();
                    hidHide.ClearApplicationsList();
                    hidHide.IsActive = false;
                }
            }
            catch (Nefarius.Drivers.HidHide.Exceptions.HidHideDriverNotFoundException) { }
        }

        private void sensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sensitivityText.Text = "Sensitivity: " + (int)sensitivitySlider.Value;
            sensitivity = (int)sensitivitySlider.Value;
        }

        private void ds4LightbarIgnoreBox_Checked(object sender, RoutedEventArgs e)
        {
            if (controllerEmulation != null)
            {
                controllerEmulation.IgnoreDS4Lightbar = true;
            }

            if (dualsense != null && dualsense.Working)
            {
                dualsense.SetLightbar((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value);
            }
        }

        private void ds4LightbarIgnoreBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controllerEmulation != null)
            {
                controllerEmulation.IgnoreDS4Lightbar = false;
            }

            if (dualsense != null && dualsense.Working)
            {
                dualsense.SetLightbar((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value);
            }
        }

        private void outputHeadsetBox_Checked(object sender, RoutedEventArgs e)
        {
            if (dualsense != null && dualsense.Working)
            {
                speakerLabel.Text = "Headset";
                dualsense.SetAudioOutput(AudioOutput.HEADSET);
            }
        }

        private void outputHeadsetBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (dualsense != null && dualsense.Working)
            {
                speakerLabel.Text = "Speaker";
                dualsense.SetAudioOutput(AudioOutput.SPEAKER);
            }
        }

        private string GetConfig()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            if (!Directory.Exists(Settings.Path))
                Directory.CreateDirectory(Settings.Path);

            dialog.InitialDirectory = Settings.Path;
            dialog.FileName = "DualSense Profile"; // Default file name
            dialog.DefaultExt = ".dyp"; // Default file extension
            dialog.Filter = "DualSenseY Profile (.dyp)|*.dyp"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string path = dialog.FileName;

                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private void minimizeToTrayBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.minimizeToTray = true;
            Properties.Settings.Default.Save();
        }

        private void minimizeToTrayBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.minimizeToTray = false;
            Properties.Settings.Default.Save();
        }

        private void connectOnStartupBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.connectOnStartup = true;
            Properties.Settings.Default.Save();
        }

        private void connectOnStartupBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.connectOnStartup = false;
            Properties.Settings.Default.Save();
        }

        private void launchWithWindowsBox_Checked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rk.SetValue("DualSenseY", System.Reflection.Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe"));
            Properties.Settings.Default.launchWithWindows = true;
            Properties.Settings.Default.Save();
        }

        private void launchWithWindowsBox_Unchecked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rk.DeleteValue("DualSenseY", false);
            Properties.Settings.Default.launchWithWindows = false;
            Properties.Settings.Default.Save();
        }

        private void launchMinimizedBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.launchMinimized = true;
            Properties.Settings.Default.Save();
        }

        private void launchMinimizedBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.launchMinimized = false;
            Properties.Settings.Default.Save();
        }

        private void discordBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", "https://discord.gg/AFYvxf282U");
        }

        private void githubBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", "https://github.com/WujekFoliarz/DualSenseY/issues");
        }

        private void editBindMic_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int i = 0;
            int index1 = 0;
            int index2 = 0;
            int index3 = 0;
            int index4 = 0;

            switch (btn.Name)
            {
                case "editBindMic":
                    {
                        i = 0;
                        index1 = customHotkeyIndex[0];
                        index2 = customHotkeyIndex[1];
                        index3 = customHotkeyIndex[2];
                        index4 = customHotkeyIndex[3];
                        break;
                    }
                case "editBindMic2":
                    {
                        i = 1;
                        index1 = customHotkeyIndex[4];
                        index2 = customHotkeyIndex[5];
                        index3 = customHotkeyIndex[6];
                        index4 = customHotkeyIndex[7];
                        break;
                    }
                case "editBindMic3":
                    {
                        i = 2;
                        index1 = customHotkeyIndex[8];
                        index2 = customHotkeyIndex[9];
                        index3 = customHotkeyIndex[10];
                        index4 = customHotkeyIndex[11];
                        break;
                    }
                case "editBindMic4":
                    {
                        i = 3;
                        index1 = customHotkeyIndex[12];
                        index2 = customHotkeyIndex[13];
                        index3 = customHotkeyIndex[14];
                        index4 = customHotkeyIndex[15];
                        break;
                    }
                case "editBindMic5":
                    {
                        i = 4;
                        index1 = customHotkeyIndex[16];
                        index2 = customHotkeyIndex[17];
                        index3 = customHotkeyIndex[18];
                        index4 = customHotkeyIndex[19];
                        break;
                    }
            }

            var dialog = new HotkeyEdit(index1, index2, index3, index4);

            if (dialog.ShowDialog() == true)
            {
                customHotkey[i] = string.Empty;

                if (dialog.firstKey != "[EMPTY]")
                {
                    switch (dialog.firstKey)
                    {
                        case "Control":
                            customHotkey[i] += "^";
                            break;

                        case "Alt":
                            customHotkey[i] += "!";
                            break;

                        case "Shift":
                            customHotkey[i] += "+";
                            break;

                        default:
                            if (dialog.firstKey.Length < 2)
                                customHotkey[i] += dialog.firstKey.ToUpper();
                            else
                                customHotkey[i] += "{" + dialog.firstKey.ToUpper() + "}";
                            break;
                    }
                }

                if (dialog.secondKey != "[EMPTY]")
                {
                    switch (dialog.secondKey)
                    {
                        case "Control":
                            customHotkey[i] += "^";
                            break;

                        case "Alt":
                            customHotkey[i] += "!";
                            break;

                        case "Shift":
                            customHotkey[i] += "+";
                            break;

                        default:
                            if (dialog.secondKey.Length < 2)
                                customHotkey[i] += dialog.secondKey.ToUpper();
                            else
                                customHotkey[i] += "{" + dialog.secondKey.ToUpper() + "}";
                            break;
                    }
                }

                if (dialog.thirdKey != "[EMPTY]")
                {
                    switch (dialog.thirdKey)
                    {
                        case "Control":
                            customHotkey[i] += "^";
                            break;

                        case "Alt":
                            customHotkey[i] += "!";
                            break;

                        case "Shift":
                            customHotkey[i] += "+";
                            break;

                        default:
                            if (dialog.thirdKey.Length < 2)
                                customHotkey[i] += dialog.thirdKey.ToUpper();
                            else
                                customHotkey[i] += "{" + dialog.thirdKey.ToUpper() + "}";
                            break;
                    }
                }

                if (dialog.fourthKey != "[EMPTY]")
                {
                    switch (dialog.fourthKey)
                    {
                        case "Control":
                            customHotkey[i] += "^";
                            break;

                        case "Alt":
                            customHotkey[i] += "!";
                            break;

                        case "Shift":
                            customHotkey[i] += "+";
                            break;

                        default:
                            if (dialog.fourthKey.Length < 2)
                                customHotkey[i] += dialog.fourthKey.ToUpper();
                            else
                                customHotkey[i] += "{" + dialog.fourthKey.ToUpper() + "}";
                            break;
                    }
                }

                customHotkeyIndex[(i + 1) * 4 - 4] = dialog.firstIndex;
                customHotkeyIndex[(i + 1) * 4 - 3] = dialog.secondIndex;
                customHotkeyIndex[(i + 1) * 4 - 2] = dialog.thirdIndex;
                customHotkeyIndex[(i + 1) * 4 - 1] = dialog.fourthIndex;
            }
        }

        private void hotkeyBoxMic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                ComboBox box = (ComboBox)sender;
                switch (box.Name)
                {
                    case "hotkeyBoxMic":
                        if (box.SelectedItem.ToString().Split(':')[1].Trim() == "Custom hotkey")
                        {
                            editBindMic.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            editBindMic.Visibility = Visibility.Hidden;
                        }
                        break;

                    case "hotkeyBoxMicPlusUp":
                        if (box.SelectedItem.ToString().Split(':')[1].Trim() == "Custom hotkey")
                        {
                            editBindMic2.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            editBindMic2.Visibility = Visibility.Hidden;
                        }
                        break;

                    case "hotkeyBoxMicPlusRight":
                        if (box.SelectedItem.ToString().Split(':')[1].Trim() == "Custom hotkey")
                        {
                            editBindMic3.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            editBindMic3.Visibility = Visibility.Hidden;
                        }
                        break;

                    case "hotkeyBoxMicPlusLeft":
                        if (box.SelectedItem.ToString().Split(':')[1].Trim() == "Custom hotkey")
                        {
                            editBindMic4.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            editBindMic4.Visibility = Visibility.Hidden;
                        }
                        break;

                    case "hotkeyBoxMicPlusDown":
                        if (box.SelectedItem.ToString().Split(':')[1].Trim() == "Custom hotkey")
                        {
                            editBindMic5.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            editBindMic5.Visibility = Visibility.Hidden;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        private bool isDiscoOn = false;
        private int discoSpeed = 1;

        private void StartDisco()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            byte[] led = new byte[3];
            int step = 5; // Color transition step size (higher values = faster transitions)

            led[0] = 255;
            led[1] = 0;
            led[2] = 0;

            int colorState = 0; // 0 = red to yellow, 1 = yellow to green, 2 = green to cyan, etc.

            while (true)
            {
                if (dualsense != null && dualsense.Working && isDiscoOn && stopwatch.ElapsedMilliseconds >= 20 - discoSpeed)
                {
                    dualsense.SetLightbar(led[0], led[1], led[2]);

                    switch (colorState)
                    {
                        case 0:
                            led[1] += (byte)step;
                            if (led[1] >= 255) colorState = 1;
                            break;

                        case 1:
                            led[0] -= (byte)step;
                            if (led[0] <= 0) colorState = 2;
                            break;

                        case 2:
                            led[2] += (byte)step;
                            if (led[2] >= 255) colorState = 3;
                            break;

                        case 3:
                            led[1] -= (byte)step;
                            if (led[1] <= 0) colorState = 4;
                            break;

                        case 4:
                            led[0] += (byte)step;
                            if (led[0] >= 255) colorState = 5;
                            break;

                        case 5:
                            led[2] -= (byte)step;
                            if (led[2] <= 0) colorState = 0;
                            break;
                    }

                    stopwatch.Restart();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void discoBox_Checked(object sender, RoutedEventArgs e)
        {
            discoSpeedSlider.Visibility = Visibility.Visible;
            discoSpeedText.Visibility = Visibility.Visible;
            lightbarBattery.IsChecked = false;
            ledBattery.IsChecked = false;
            lightbarBattery.IsEnabled = false;
            ledBattery.IsEnabled = false;
            sliderRed.IsEnabled = false;
            sliderGreen.IsEnabled = false;
            sliderBlue.IsEnabled = false;
            soundLEDcheckbox.IsEnabled = false;
            isDiscoOn = true;
            discoSpeed = (int)discoSpeedSlider.Value;
        }

        private void discoBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (dualsense != null && dualsense.Working)
            {
                discoSpeedSlider.Visibility = Visibility.Hidden;
                discoSpeedText.Visibility = Visibility.Hidden;

                if(lightbarBattery.IsChecked == false && ledBattery.IsChecked == false)
                {
                    soundLEDcheckbox.IsEnabled = true;
                }

                isDiscoOn = false;
                sliderRed.IsEnabled = true;
                sliderGreen.IsEnabled = true;
                sliderBlue.IsEnabled = true;
                lightbarBattery.IsEnabled = true;
                ledBattery.IsEnabled = true;
                dualsense.SetLightbarTransition((byte)sliderRed.Value, (byte)sliderGreen.Value, (byte)sliderBlue.Value, 10, 50);
            }
        }

        private void discoSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            discoSpeed = (int)discoSpeedSlider.Value;
        }

        private void cmbControllerSelect_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                EnumerateControllers();
            }
        }

        private void port1_Click(object sender, RoutedEventArgs e)
        {          
            port1.Content = currentControllerID;
            ports[0] = currentControllerID;
            Properties.Settings.Default.port1 = currentControllerID;
            Properties.Settings.Default.Save();
        }

        private void configport1_Click(object sender, RoutedEventArgs e)
        {
            string config = GetConfig();
            if(config != string.Empty)
            {
                configport1.Content = Path.GetFileNameWithoutExtension(config);
                Properties.Settings.Default.config1 = config;
                Properties.Settings.Default.Save();
            }
        }

        private void port2_Click(object sender, RoutedEventArgs e)
        {
            port2.Content = currentControllerID;
            ports[1] = currentControllerID;
            Properties.Settings.Default.port2 = currentControllerID;
            Properties.Settings.Default.Save();
        }

        private void configport2_Click(object sender, RoutedEventArgs e)
        {
            string config = GetConfig();
            if (config != string.Empty)
            {
                configport2.Content = Path.GetFileNameWithoutExtension(config);
                Properties.Settings.Default.config2 = config;
                Properties.Settings.Default.Save();
            }
        }

        private void port3_Click(object sender, RoutedEventArgs e)
        {
            port3.Content = currentControllerID;
            ports[2] = currentControllerID;
            Properties.Settings.Default.port3 = currentControllerID;
            Properties.Settings.Default.Save();
        }

        private void configport3_Click(object sender, RoutedEventArgs e)
        {
            string config = GetConfig();
            if (config != string.Empty)
            {
                configport3.Content = Path.GetFileNameWithoutExtension(config);
                Properties.Settings.Default.config3 = config;
                Properties.Settings.Default.Save();
            }
        }

        private void port4_Click(object sender, RoutedEventArgs e)
        {           
            port4.Content = currentControllerID;
            ports[3] = currentControllerID;
            Properties.Settings.Default.port4 = currentControllerID;
            Properties.Settings.Default.Save();
        }

        private void configport4_Click(object sender, RoutedEventArgs e)
        {
            string config = GetConfig();
            if (config != string.Empty)
            {
                configport4.Content = Path.GetFileNameWithoutExtension(config);
                Properties.Settings.Default.config4 = config;
                Properties.Settings.Default.Save();
            }
        }

        private void port1_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            port1.Content = "Click to assign port";
            Properties.Settings.Default.port1 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void port2_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            port2.Content = "Click to assign port";
            Properties.Settings.Default.port2 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void port3_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            port3.Content = "Click to assign port";
            Properties.Settings.Default.port3 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void port4_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            port4.Content = "Click to assign port";
            Properties.Settings.Default.port4 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void configport1_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            configport1.Content = "None";
            Properties.Settings.Default.config1 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void configport2_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            configport2.Content = "None";
            Properties.Settings.Default.config2 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void configport3_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            configport3.Content = "None";
            Properties.Settings.Default.config3 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void configport4_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            configport4.Content = "None";
            Properties.Settings.Default.config4 = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void lightbarBattery_Checked(object sender, RoutedEventArgs e)
        {
            
            sliderRed.IsEnabled = false;
            sliderGreen.IsEnabled = false;
            sliderBlue.IsEnabled = false;
            discoBox.IsEnabled = false;
            discoBox.IsChecked = false;
            soundLEDcheckbox.IsEnabled = false;
            soundLEDcheckbox.IsChecked = false;
        }

        private void lightbarBattery_Unchecked(object sender, RoutedEventArgs e)
        {
            sliderRed.IsEnabled = true;
            sliderGreen.IsEnabled = true;
            sliderBlue.IsEnabled = true;

            if(ledBattery.IsChecked == false)
            {
                discoBox.IsEnabled = true;
                soundLEDcheckbox.IsEnabled = true;
            }

            ReadCurrentValues();
        }

        private void ledBattery_Checked(object sender, RoutedEventArgs e)
        {
            LEDbox.IsEnabled = false;
        }

        private void ledBattery_Unchecked(object sender, RoutedEventArgs e)
        {
            if (lightbarBattery.IsChecked == false)
            {
                discoBox.IsEnabled = true;
                soundLEDcheckbox.IsEnabled = true;
            }
            LEDbox.IsEnabled = true;
            ReadCurrentValues();
        }
    }
}