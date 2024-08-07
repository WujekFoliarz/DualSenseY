using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Runtime.InteropServices;
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
                new Thread(() => { Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    Emulate();
                }).Start();
            }
        }

        public void StopEmulation()
        {
            Emulating = false;

            if(x360Controller != null)
            {
                x360Controller.Disconnect();
            }

            if(dualshock4 != null)
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
                    dualshock4.SetButtonState(DualShock4Button.Cross, dualsense.ButtonState.cross);
                    dualshock4.SetButtonState(DualShock4Button.Circle, dualsense.ButtonState.circle);
                    dualshock4.SetButtonState(DualShock4Button.Triangle, dualsense.ButtonState.triangle);
                    dualshock4.SetButtonState(DualShock4Button.Square, dualsense.ButtonState.square);

                    DualShock4DPadDirection direction = DualShock4DPadDirection.None;
                    if (dualsense.ButtonState.DpadDown) { direction = DualShock4DPadDirection.South; }
                    else if (dualsense.ButtonState.DpadUp) { direction = DualShock4DPadDirection.North; }
                    else if (dualsense.ButtonState.DpadLeft) { direction = DualShock4DPadDirection.West; }
                    else if (dualsense.ButtonState.DpadRight) { direction = DualShock4DPadDirection.East; }
                    dualshock4.SetDPadDirection(direction);

                    dualshock4.SetAxisValue(DualShock4Axis.LeftThumbX, (byte)dualsense.ButtonState.LX);
                    dualshock4.SetAxisValue(DualShock4Axis.LeftThumbY, (byte)dualsense.ButtonState.LY);
                    dualshock4.SetAxisValue(DualShock4Axis.RightThumbX, (byte)dualsense.ButtonState.RX);
                    dualshock4.SetAxisValue(DualShock4Axis.RightThumbY, (byte)dualsense.ButtonState.RY);
                    dualshock4.SetButtonState(DualShock4Button.ThumbLeft, dualsense.ButtonState.L3);
                    dualshock4.SetButtonState(DualShock4Button.ThumbRight, dualsense.ButtonState.R3);

                    if ((byte)dualsense.ButtonState.L2 >= leftTriggerThreshold)
                        dualshock4.LeftTrigger = (byte)dualsense.ButtonState.L2;
                    else
                        dualshock4.LeftTrigger = (byte)0;

                    if ((byte)dualsense.ButtonState.R2 >= rightTriggerThreshold)
                        dualshock4.RightTrigger = (byte)dualsense.ButtonState.R2;
                    else
                        dualshock4.RightTrigger = (byte)0;

                    dualshock4.SetButtonState(DualShock4SpecialButton.Touchpad, dualsense.ButtonState.touchBtn);
                    dualshock4.SetButtonState(DualShock4Button.Share, dualsense.ButtonState.share);
                    dualshock4.SetButtonState(DualShock4Button.Options, dualsense.ButtonState.options);
                    dualshock4.SetButtonState(DualShock4Button.ShoulderLeft, dualsense.ButtonState.L1);
                    dualshock4.SetButtonState(DualShock4Button.ShoulderRight, dualsense.ButtonState.R1);
                    dualshock4.SetButtonState(DualShock4SpecialButton.Ps, dualsense.ButtonState.ps);
                }

                Thread.Sleep(1);
            }
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

