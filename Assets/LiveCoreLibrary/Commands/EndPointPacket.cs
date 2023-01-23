using System;
using System.Collections.Generic;
using MessagePack;

namespace LiveCoreLibrary.Commands
{
    [MessagePackObject]
    public class HolePunchingPacket : IUdpCommand
    {
        [Key(0)] public readonly ulong UserId;
        [Key(1)] public readonly string[] NatAddress;

        public HolePunchingPacket(ulong userId, string[] natAddress)
        {
            this.UserId = userId;
            this.NatAddress = natAddress;
        }
    }

    [MessagePackObject]
    public readonly struct EndPointPacket : IEquatable<EndPointPacket>
    {
        [Key(0)] public readonly ulong Id;
        [Key(1)] public readonly string Address;
        [Key(2)] public readonly string[] NatAddresses;
        [Key(3)] public readonly int Port;

        public EndPointPacket(ulong id, string address, string[] natAddresses, int port)
        {
            Id = id;
            Address = address;
            NatAddresses = natAddresses;
            Port = port;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Id.GetHashCode();
            hash = hash * 23 + Address.GetHashCode();
            hash = hash * 23 + NatAddresses.GetHashCode();
            hash = hash * 23 + Port.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is EndPointPacket)
                return Equals((EndPointPacket)other);
            return false;
        }

        public bool Equals(EndPointPacket other)
        {
            return Id == other.Id &&
                   Address == other.Address &&
                   NatAddresses == other.NatAddresses &&
                   Port == other.Port;
        }

        public static bool operator ==(EndPointPacket lhs, EndPointPacket rhs)
        {
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(EndPointPacket lhs, EndPointPacket rhs) => !(lhs == rhs);
    }

    [MessagePackObject]
    public class EndPointPacketHolder : ITcpCommand
    {
        [Key(0)] public readonly EndPointPacket[] EndPointPackets;

        public EndPointPacketHolder(EndPointPacket[] endPointPackets)
        {
            this.EndPointPackets = endPointPackets;
        }


        public bool GetPacketById(ulong id, out EndPointPacket endPointPacket)
        {
            foreach (var x in EndPointPackets)
            {
                if (x.Id == id)
                {
                    endPointPacket = x;
                    return true;
                }
            }

            endPointPacket = default;
            return false;
        }
    }
}