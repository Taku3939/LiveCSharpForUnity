using System;
using MessagePack;
using UnityEngine;

namespace LiveCoreLibrary.Commands
{
    [MessagePackObject]
    public class PenLightPacket : ITcpCommand
    {
        [Key(0)] public ulong UserId { get; set; }
        [Key(1)] public float R { get; set; }
        [Key(2)] public float G { get; set; }
        [Key(3)] public float B { get; set; }

        [Key(4)] public int Mode { get;set; }
        [IgnoreMember] public Color Color => new Color(R, G, B);
        public PenLightPacket(ulong userId, float r, float g, float b, int mode)
        {
            UserId = userId;
            R = r;
            G = g;
            B = b;
            Mode = mode;
        }
        
        public PenLightPacket(ulong userId, Color color, int mode)
        {
            UserId = userId;
            R = color.r;
            G = color.g;
            B = color.b;
            Mode = mode;
        }
    }

}