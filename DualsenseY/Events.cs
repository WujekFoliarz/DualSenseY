using static DualSenseY.UDP;

namespace DualSenseY
{
    public class Events
    {
        public class PacketEvent : EventArgs
        {
            public Packet packet { get; set; }

            public PacketEvent(Packet newPacket)
            {
                packet = newPacket;
            }
        }

        public event EventHandler<PacketEvent> NewPacket;
        public void OnNewPacket(Packet Packet)
        {
            if (this.NewPacket != null)
            {
                this.NewPacket(this, new PacketEvent(Packet));
            }
        }
    }
}
