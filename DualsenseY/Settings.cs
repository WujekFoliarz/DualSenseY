using Newtonsoft.Json;
using System.IO;
using System.Windows;
using Wujek_Dualsense_API;

namespace DualSenseY
{
    public class Settings
    {
        public readonly static string Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\DualSenseY\\Configs";

        public void SaveProfileToFile(string fileName, Profile profile)
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            File.WriteAllText(Path + "\\" + fileName + ".dyp", JsonConvert.SerializeObject(profile));
        }

        public Profile ReadProfileFromFile(string path)
        {
            if (File.Exists(path))
            {
                string file = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<Profile>(file);
            }
            else
            {
                MessageBox.Show("This file doesn't exist");
                return null;
            }
        }

        public class Profile
        {
            public int R { get; set; }
            public int G { get; set; }
            public int B { get; set; }
            public bool IgnoreDS4Lightbar { get; set; }
            public int ControllerEmulation { get; set; } // 0 = off, 1 = x360, 2 = ds4
            public bool UseTouchpadAsMouse { get; set; }
            public int TouchpadSensitivity { get; set; }
            public int MicrophoneVolume { get; set; }
            public float SpeakerVolume { get; set; }
            public float LeftActuatorVolume { get; set; }
            public float RightActuatorVolume { get; set; }
            public bool UseHeadset { get; set; }
            public LED.PlayerLED playerLED { get; set; }
            public LED.MicrophoneLED microphoneLED { get; set; }
            public TriggerType.TriggerModes leftTriggerMode { get; set; }
            public int[] leftTriggerForces { get; set; }
            public TriggerType.TriggerModes rightTriggerMode { get; set; }
            public int[] rightTriggerForces { get; set; }
        }
    }
}
