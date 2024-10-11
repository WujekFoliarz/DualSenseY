using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Windows;
using Wujek_Dualsense_API;

namespace DualSenseY
{
    public class ControllerEmulation : IDisposable
    {
        private ViGEmClient client;
        public bool isEmulating360 = false;
        public bool Emulating = false;
        public IDualShock4Controller dualshock4;
        public IXbox360Controller x360Controller;
        public Dualsense dualsense;
        public int leftTriggerThreshold = 0;
        public int rightTriggerThreshold = 0;
        public bool ForceStopRumble = true;
        public bool isViGEMBusInstalled = false;
        public bool IgnoreDS4Lightbar = false;

        public ControllerEmulation()
        {
            try
            {
                client = new ViGEmClient();
                isViGEMBusInstalled = true;
            }
            catch (VigemBusNotFoundException)
            {
                isViGEMBusInstalled = false;
            }
        }

        public void StartX360Emulation()
        {
            if (isViGEMBusInstalled)
            {
                Emulating = false;

                if (x360Controller != null)
                {
                    x360Controller.Disconnect();
                }

                if (dualshock4 != null)
                {
                    dualshock4.Disconnect();
                }

                if (x360Controller == null)
                    x360Controller = client.CreateXbox360Controller();

                x360Controller.Connect();
                x360Controller.FeedbackReceived += X360Controller_FeedbackReceived;
                isEmulating360 = true;
                Emulating = true;

                new Thread(() => Emulate()).Start();
            }
        }

        private void X360Controller_FeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
        {
            if (!ForceStopRumble)
            {
                dualsense.SetVibrationType(Vibrations.VibrationType.Standard_Rumble);
                dualsense.SetStandardRumble(e.LargeMotor, e.SmallMotor);
            }
        }

        public void StartDS4Emulation()
        {
            if (isViGEMBusInstalled)
            {
                Emulating = false;

                if (x360Controller != null)
                {
                    x360Controller.Disconnect();
                }

                if (dualshock4 != null)
                {
                    dualshock4.Disconnect();
                }

                if (dualshock4 == null)
                    dualshock4 = client.CreateDualShock4Controller();

                dualshock4.Connect();
                dualshock4.FeedbackReceived += Dualshock4_FeedbackReceived;
                isEmulating360 = false;
                Emulating = true;
                new Thread(() =>
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    Emulate();
                }).Start();
            }
        }

        public void StopEmulation()
        {
            Emulating = false;

            if (x360Controller != null)
            {
                x360Controller.Disconnect();
            }

            if (dualshock4 != null)
            {
                dualshock4.Disconnect();
            }
        }

        private void Dualshock4_FeedbackReceived(object sender, DualShock4FeedbackReceivedEventArgs e)
        {
            if (!ForceStopRumble)
            {
                dualsense.SetVibrationType(Vibrations.VibrationType.Standard_Rumble);
                dualsense.SetStandardRumble(e.LargeMotor, e.SmallMotor);
            }
            
            if (!IgnoreDS4Lightbar)
            {
                if (e.LightbarColor.Red != 0 || e.LightbarColor.Green != 0 || e.LightbarColor.Blue != 0)
                {
                    dualsense.SetLightbar(e.LightbarColor.Red, e.LightbarColor.Green, e.LightbarColor.Blue);
                }
            }
        }

        private void Emulate()
        {
            byte[] rawDS4 = new byte[63];

            while (Emulating)
            {
                if (isEmulating360)
                {
                    x360Controller.SetButtonState(Xbox360Button.A, dualsense.ButtonState.cross);
                    x360Controller.SetButtonState(Xbox360Button.B, dualsense.ButtonState.circle);
                    x360Controller.SetButtonState(Xbox360Button.Y, dualsense.ButtonState.triangle);
                    x360Controller.SetButtonState(Xbox360Button.X, dualsense.ButtonState.square);
                    x360Controller.SetButtonState(Xbox360Button.Up, dualsense.ButtonState.DpadUp);
                    x360Controller.SetButtonState(Xbox360Button.Left, dualsense.ButtonState.DpadLeft);
                    x360Controller.SetButtonState(Xbox360Button.Right, dualsense.ButtonState.DpadRight);
                    x360Controller.SetButtonState(Xbox360Button.Down, dualsense.ButtonState.DpadDown);

                    //-32767 minimum
                    //32766 max
                    x360Controller.SetAxisValue(Xbox360Axis.LeftThumbX, (short)ConvertRange(dualsense.ButtonState.LX, 0, 255, -32767, 32766));
                    x360Controller.SetAxisValue(Xbox360Axis.LeftThumbY, (short)ConvertRange(dualsense.ButtonState.LY, 255, 0, -32767, 32766));
                    x360Controller.SetAxisValue(Xbox360Axis.RightThumbX, (short)ConvertRange(dualsense.ButtonState.RX, 0, 255, -32767, 32766));
                    x360Controller.SetAxisValue(Xbox360Axis.RightThumbY, (short)ConvertRange(dualsense.ButtonState.RY, 255, 0, -32767, 32766));
                    x360Controller.SetButtonState(Xbox360Button.LeftThumb, dualsense.ButtonState.L3);
                    x360Controller.SetButtonState(Xbox360Button.RightThumb, dualsense.ButtonState.R3);

                    if ((byte)dualsense.ButtonState.L2 >= leftTriggerThreshold)
                        x360Controller.LeftTrigger = (byte)dualsense.ButtonState.L2;
                    else
                        x360Controller.LeftTrigger = (byte)0;

                    if ((byte)dualsense.ButtonState.R2 >= rightTriggerThreshold)
                        x360Controller.RightTrigger = (byte)dualsense.ButtonState.R2;
                    else
                        x360Controller.RightTrigger = (byte)0;

                    x360Controller.SetButtonState(Xbox360Button.Start, dualsense.ButtonState.options);
                    x360Controller.SetButtonState(Xbox360Button.Back, dualsense.ButtonState.share);
                    x360Controller.SetButtonState(Xbox360Button.LeftShoulder, dualsense.ButtonState.L1);
                    x360Controller.SetButtonState(Xbox360Button.RightShoulder, dualsense.ButtonState.R1);
                    x360Controller.SetButtonState(Xbox360Button.Guide, dualsense.ButtonState.ps);
                }
                else
                {
                    rawDS4[0] = (byte)dualsense.ButtonState.LX;
                    rawDS4[1] = (byte)dualsense.ButtonState.LY;
                    rawDS4[2] = (byte)dualsense.ButtonState.RX;
                    rawDS4[3] = (byte)dualsense.ButtonState.RY;

                    byte xoState = 0x0;
                    xoState = (byte)(dualsense.ButtonState.triangle ? xoState | (byte)DualShock4Buttons.Tringle : xoState);
                    xoState = (byte)(dualsense.ButtonState.circle ? xoState | (byte)DualShock4Buttons.Circle : xoState);
                    xoState = (byte)(dualsense.ButtonState.cross ? xoState | (byte)DualShock4Buttons.Cross : xoState);
                    xoState = (byte)(dualsense.ButtonState.square ? xoState | (byte)DualShock4Buttons.Square : xoState);

                    if (dualsense.ButtonState.DpadUp && dualsense.ButtonState.DpadLeft)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_NorthWest);

                    if (dualsense.ButtonState.DpadDown && dualsense.ButtonState.DpadLeft)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_SouthWest);
                    else if (dualsense.ButtonState.DpadDown && dualsense.ButtonState.DpadRight)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_SouthEast);
                    else if (dualsense.ButtonState.DpadUp && dualsense.ButtonState.DpadRight)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_NorthEast);
                    else if (dualsense.ButtonState.DpadLeft)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_West);
                    else if (dualsense.ButtonState.DpadDown)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_South);
                    else if (dualsense.ButtonState.DpadRight)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_East);
                    else if (dualsense.ButtonState.DpadUp)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_North);
                    else if (!dualsense.ButtonState.DpadUp && !dualsense.ButtonState.DpadDown && !dualsense.ButtonState.DpadLeft && !dualsense.ButtonState.DpadRight)
                        xoState = (byte)(xoState | (byte)DualShock4Buttons.Dpad_Neutral);
                    rawDS4[4] = xoState;

                    byte lState = 0x0;
                    lState = (byte)(dualsense.ButtonState.R3 ? lState | (byte)DualShock4Buttons.R3 : lState);
                    lState = (byte)(dualsense.ButtonState.L3 ? lState | (byte)DualShock4Buttons.L3 : lState);
                    lState = (byte)(dualsense.ButtonState.options ? lState | (byte)DualShock4Buttons.Options : lState);
                    lState = (byte)(dualsense.ButtonState.share ? lState | (byte)DualShock4Buttons.Share : lState);
                    lState = (byte)(dualsense.ButtonState.R1 ? lState | (byte)DualShock4Buttons.R1 : lState);
                    lState = (byte)(dualsense.ButtonState.L1 ? lState | (byte)DualShock4Buttons.L1 : lState);
                    rawDS4[5] = lState;

                    byte tState = 0x0;
                    tState = (byte)(dualsense.ButtonState.touchBtn ? tState |  (byte)DualShock4Buttons.TPAD_Click : tState);
                    tState = (byte)(dualsense.ButtonState.ps ? tState | (byte)DualShock4Buttons.PS : tState);
                    rawDS4[6] = tState;

                    rawDS4[7] = (byte)dualsense.ButtonState.L2;
                    rawDS4[8] = (byte)dualsense.ButtonState.R2;

                    short timestamp = (short)(dualsense.ButtonState.accelerometer.SensorTimestamp / 16);
                    rawDS4[9] = (byte)(timestamp & 0xFF);
                    rawDS4[10] = (byte)((timestamp >> 8) & 0xFF);

                    rawDS4[18] = (byte)(dualsense.ButtonState.accelerometer.X & 0xFF);
                    rawDS4[19] = (byte)((dualsense.ButtonState.accelerometer.X >> 8) & 0xFF);

                    rawDS4[20] = (byte)(dualsense.ButtonState.accelerometer.Y & 0xFF);
                    rawDS4[21] = (byte)((dualsense.ButtonState.accelerometer.Y >> 8) & 0xFF);

                    rawDS4[22] = (byte)(dualsense.ButtonState.accelerometer.Z & 0xFF);
                    rawDS4[23] = (byte)((dualsense.ButtonState.accelerometer.Z >> 8) & 0xFF);

                    rawDS4[12] = (byte)(dualsense.ButtonState.gyro.X & 0xFF);
                    rawDS4[13] = (byte)((dualsense.ButtonState.gyro.X >> 8) & 0xFF);

                    rawDS4[14] = (byte)(dualsense.ButtonState.gyro.Y & 0xFF);
                    rawDS4[15] = (byte)((dualsense.ButtonState.gyro.Y >> 8) & 0xFF);

                    rawDS4[16] = (byte)(dualsense.ButtonState.gyro.Z & 0xFF);
                    rawDS4[17] = (byte)((dualsense.ButtonState.gyro.Z >> 8) & 0xFF);

                    rawDS4[32] = 1;
                    rawDS4[33] = (byte)dualsense.ButtonState.TouchPacketNum;
                    rawDS4[34] = (byte)dualsense.ButtonState.trackPadTouch0.RawTrackingNum;
                    rawDS4[35] = (byte)(dualsense.ButtonState.trackPadTouch0.X & 0xFF);
                    rawDS4[36] = (byte)((byte)(dualsense.ButtonState.trackPadTouch0.X >> 8) & 0x0F | (dualsense.ButtonState.trackPadTouch0.Y << 4) & 0xF0);
                    rawDS4[37] = (byte)(dualsense.ButtonState.trackPadTouch0.Y >> 4);

                    rawDS4[38] = (byte)dualsense.ButtonState.trackPadTouch1.RawTrackingNum;
                    rawDS4[39] = (byte)(dualsense.ButtonState.trackPadTouch1.X & 0xFF);
                    rawDS4[40] = (byte)((byte)(dualsense.ButtonState.trackPadTouch1.X >> 8) & 0x0F | (dualsense.ButtonState.trackPadTouch1.Y << 4) & 0xF0);
                    rawDS4[41] = (byte)(dualsense.ButtonState.trackPadTouch1.Y >> 4);


                    dualshock4.SubmitRawReport(rawDS4);
                }

                Thread.Sleep(1);
            }
        }

        public byte[] EncodeCoordinates(int x, int y)
        {
            x &= 0xFFF;
            y &= 0xFFF;

            ushort combined = (ushort)((y << 12) | x);

            return new byte[] { (byte)(combined & 0xFF), (byte)((combined >> 8) & 0xFF) };
        }

        private enum DualShock4Buttons
        {
            Tringle = 1 << 7,
            Circle = 1 << 6,
            Cross = 1 << 5,
            Square = 1 << 4,

            R3 = 1 << 7,
            L3 = 1 << 6,
            Options = 1 << 5,
            Share = 1 << 4,
            R1 = 1 << 1,
            L1 = 1 << 0,

            TPAD_Click = 1 << 1,
            PS = 1 << 0,

            Dpad_Neutral = 0b_1000,
            Dpad_NorthWest = 0b_0111,
            Dpad_West = 0b_0110,
            Dpad_SouthWest = 0b_0101,
            Dpad_South = 0b_0100,
            Dpad_SouthEast = 0b_0011,
            Dpad_East = 0b_0010,
            Dpad_NorthEast = 0b_0001,
            Dpad_North = 0b_0000
        }

        private int ConvertRange(int value, int oldMin, int oldMax, int newMin, int newMax)
        {
            if (oldMin == oldMax)
            {
                throw new ArgumentException("Old minimum and maximum cannot be equal.");
            }
            float ratio = (float)(newMax - newMin) / (float)(oldMax - oldMin);
            float scaledValue = (value - oldMin) * ratio + newMin;
            return Math.Clamp((int)scaledValue, newMin, newMax);
        }

        public void Dispose()
        {
            Emulating = false;
            if (dualshock4 != null)
            {
                dualshock4.Disconnect();
                dualshock4.Dispose();
            }
            if (x360Controller != null)
            {
                x360Controller.Disconnect();
            }
        }


    }
}

