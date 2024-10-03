using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DualSenseY
{
    // stupid resources wouldnt work
    public static class Constants
    {
        public static string Changelog = "5.3\nBluetooth connections are now more reliable\nSound to LED now works on bluetooth\nAdded LED battery status display option\n\n" +
            "5.2\nReplaced startup config with per port configuration\n\n" +
            "5.1\nAdded multi controller support\nAdded DualSense Edge indicator\n\n" +
            "5.0\nAdded changelog tab\nApplication now correctly reports battery level to UDP mods\nError messagebox should now display on fatal crashes\n\n" +
            "4.9\nFixed \"Cyberpunk 2077 Enhanced DualSense Support\" mod crashing randomly\r\nDisco Mode will disable automatically on UDP connection\n\n" +
            "4.8\nAdded \"Motion\" tab\n\n" +
            "4.7\nMade it less likely that Windows Defender will incorrectly flag a fake DSX process as a Trojan (please report if this still happens)\r\nBlocked \"Disco Mode\" option while using \"Sound To LED\"\n\n" +
            "4.6\nAdded disco mode for lightbar\n\n" +
            "4.5\nIncreased UDP timeout time\r\nStartup is now faster\n\n" +
            "4.4\nIncreased mod compatibility\n\n" +
            "4.3\nFixed gyro directions for DS4 emulation\n\n" +
            "4.2\nEmulated DS4 now supports touchpad\r\nEmulated DS4 now supports gyro\n\n" +
            "4.1\nAdded keyboard mapping for hotkeys\n\n" +
            "4.0\nAdded battery status for bluetooth connections\n\n" +
            "3.9\nFixed \"Sound to LED\" not working when changing default audio output device\n\n" +
            "3.8\nTouchpad tab should now work on bluetooth connections\r\nAdded \"Help\" tab\n\n" +
            "3.7\nFixed connection type indication icon not appearing when using connect on startup option\r\nChanged \"Save Config\" dialog box to Windows' save file dialog\n\n" +
            "3.6\nFixed adaptive triggers tab\r\nSpeaker test and screenshot sound now work even when Audio Passthrough is enabled\r\nAudio Passthrough output device should change automatically now\r\nFixed Audio Passthrough not working on devices with more than 2 channels\n\n" +
            "3.5\nAdded an option to load your config on startup\r\nAdded minimize to tray option\r\nAdded connect on startup option\r\nAdded minimize on startup option\r\nAdded hotkeys (Screenshot, X360 emu, DS4 emu, audio passthrough)\r\nLeft and right stereo channels now split correctly when using audio passthrough\r\nFixed microphone\r\nAdjusted UI\n\n" +
            "3.4\nClicking \"Disconnect Controller\" will turn off the LEDs, which previously only worked when the application was closed.\r\nAdded external headset support\r\nAdded connection type indication\n\n" +
            "3.3\nDisconnecting from the controller lets go off the lightbar\r\nAdded new UDP instructions\n\n" +
            "3.2\nFixed rumble not working on bluetooth\n\n" +
            "3.1\nDS4 lightbar emulation now works for games that support it\r\nConfigs now save touchpad, microphone, sound and emulation settings\n\n" +
            "3.0\nFixed UDP status falsely reporting \"Active\" on startup\r\nFixed app crashing on PCs with Intel Audio driver on USB connection\r\nFixed \"node removal failed\" error in some situations.\n\n" +
            "2.9\nAdded sensitivity slider to the touchpad tab\r\nDualSense controller will now be hidden durning controller emulation (HidHide required)\n\n" +
            "2.8\nFixed vibration test\r\nFixed app crashing on connect via bluetooth\n\n" +
            "2.7\nImproved UDP support\n\n" +
            "2.6\nFixed controller not being detected on computers with uncommon audio devices\r\nAdded audio to haptics\r\nAdded audio to LED\r\nRemoved microphone monitoring (it didn't work well anyway)\n\n" +
            "2.5\nFixed app crashing on startup in some instances\r\nChanged default microphone volume to 35\n\n" +
            "2.4\nAudio device detection was rewritten (Thanks Nefarius)\n\n" +
            "2.3\nRunning vibrations at 0 will change the UseRumbleNotHaptics flag to native\n\n" +
            "2.2\nAdded touchpad support\n\n" +
            "2.1\nFrom this version, exiting the application will automatically set the controller to native mode (Useful for games like ZZZ)\r\nFixed app crashing on controller disconnect\n\n" +
            "2.0\nFixed rumble not working when emulating x360 controller/ds4\n\n" +
            "1.9\nAdded configs\n\n" +
            "1.8\nAdded auto updater\n\n" +
            "1.7\nAdded an icon\r\nAdded minimize button\r\nAdded haptic feedback and speaker test\n\n" +
            "1.6\nFixed UDP status not updating\n\n" +
            "1.5\nAdded microphone control\r\nMade lightbar transition on controller connection more smooth\n\n" +
            "1.4\nAdded Player LED customization support\r\nAdded an option to toggle Microphone LED\n\n" +
            "1.3\nFixed app not launching on some computers\n\n" +
            "1.2\nDrastically decreased CPU usage\n\n" +
            "1.1\nFixed lightbar flickering\r\nFixed adaptive trigger labels being set to 255 on startup";

    }
}
