using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DualSenseY
{
    public class UDP : IDisposable
    {
        private UdpClient client;
        private IPEndPoint ipendPoint = new IPEndPoint(IPAddress.Any, 0);
        public bool serverOn = false;
        public bool newPacket = false;
        public Packet currentPacket;

        public UDP()
        {
            if (!File.Exists(@"C:\Temp\DualSenseX\DualSenseX_PortNumber.txt"))
                File.WriteAllText(@"C:\Temp\DualSenseX\DualSenseX_PortNumber.txt", "6969");

            Connect();
        }

        private void Connect()
        {
            serverOn = false;
            var portNumber = File.ReadAllText(@"C:\Temp\DualSenseX\DualSenseX_PortNumber.txt");
            client = new UdpClient(Convert.ToInt32(portNumber));
            serverOn = true;
            new Thread(() => Listen()).Start();
        }

        private void Listen()
        {
            try
            {
                while (serverOn)
                {
                    newPacket = false;
                    byte[] bytes = client.Receive(ref ipendPoint);
                    newPacket = true;
                    UDPResponse response = new UDPResponse();
                    response.Status = "DSX Received UDP Instructions";
                    response.TimeReceived = string.Format("{0}", DateTime.Now);
                    response.isControllerConnected = true;
                    response.BatteryLevel = 100; // doesn't matter
                    string s = JsonConvert.SerializeObject(response);
                    byte[] bytes2 = Encoding.ASCII.GetBytes(s);
                    client.Send(bytes2, bytes2.Length, ipendPoint);
                    string string_json = Encoding.ASCII.GetString(bytes);
                    currentPacket = JsonConvert.DeserializeObject<Packet>(string_json);
                    Thread.Sleep(5);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            serverOn = false;
            client.Dispose();
        }

        public class UDPResponse
        {
            // Token: 0x0400073E RID: 1854
            public string Status;

            // Token: 0x0400073F RID: 1855
            public string TimeReceived;

            // Token: 0x04000740 RID: 1856
            public bool isControllerConnected;

            // Token: 0x04000741 RID: 1857
            public int BatteryLevel;
        }

        public enum TriggerMode
        {
            Normal = 0,
            GameCube = 1,
            VerySoft = 2,
            Soft = 3,
            Hard = 4,
            VeryHard = 5,
            Hardest = 6,
            Rigid = 7,
            VibrateTrigger = 8,
            Choppy = 9,
            Medium = 10,
            VibrateTriggerPulse = 11,
            CustomTriggerValue = 12,
            Resistance = 13,
            Bow = 14,
            Galloping = 15,
            SemiAutomaticGun = 16,
            AutomaticGun = 17,
            Machine = 18
        }

        public enum CustomTriggerValueMode
        {
            OFF = 0,
            Rigid = 1,
            RigidA = 2,
            RigidB = 3,
            RigidAB = 4,
            Pulse = 5,
            PulseA = 6,
            PulseB = 7,
            PulseAB = 8,
            VibrateResistance = 9,
            VibrateResistanceA = 10,
            VibrateResistanceB = 11,
            VibrateResistanceAB = 12,
            VibratePulse = 13,
            VibratePulseA = 14,
            VibratePulsB = 15,
            VibratePulseAB = 16
        }

        public enum PlayerLEDNewRevision
        {
            One = 0,
            Two = 1,
            Three = 2,
            Four = 3,
            Five = 4, // Five is Also All On
            AllOff = 5
        }
        public enum MicLEDMode
        {
            On = 0,
            Pulse = 1,
            Off = 2
        }

        public enum Trigger
        {
            Invalid,
            Left,
            Right
        }

        public enum InstructionType
        {
            Invalid,
            TriggerUpdate,
            RGBUpdate,
            PlayerLED,
            TriggerThreshold,
            MicLED,
            PlayerLEDNewRevision,
            ResetToUserSettings
        }

        public struct Instruction
        {
            public InstructionType type;
            public object[] parameters;
        }

        public class Packet
        {
            public Instruction[] instructions;
        }

        public class ServerResponse
        {
            public string Status;
            public string TimeReceived;
            public bool isControllerConnected;
            public int BatteryLevel;
        }
    }
}
