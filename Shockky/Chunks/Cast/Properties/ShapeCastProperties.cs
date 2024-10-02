﻿using System.Drawing;

using Shockky.IO;

namespace Shockky.Chunks.Cast
{
    public class ShapeCastProperties : ShockwaveItem, ICastProperties
    {
        public ShapeType Type { get; set; }
        public Rectangle Rectangle { get; set; }
        public short Pattern { get; set; }
        public byte ForegroundColor { get; set; }
        public byte BackgroundColor { get; set; }
        public bool IsFilled { get; set; }
        public InkType Ink { get; set; }
        public byte LineSize { get; set; }
        public byte LineDirection { get; set; }

        public ShapeCastProperties()
        { }
        public ShapeCastProperties(ref ShockwaveReader input)
        {
            Type = (ShapeType)input.ReadInt16();
            Rectangle = input.ReadRect();

            Pattern = input.ReadInt16();
            ForegroundColor = input.ReadByte();
            BackgroundColor = input.ReadByte();

            byte flags = input.ReadByte(); //TODO:
            IsFilled = (flags << 1) == 1;
            Ink = (InkType)(flags & 0x3F);

            LineSize = (byte)(input.ReadByte() - 1);
            LineDirection = (byte)(input.ReadByte() - 5);
        }

        public override int GetBodySize()
        {
            int size = 0;
            size += sizeof(short);
            size += sizeof(short) * 4;
            size += sizeof(short);
            size += sizeof(byte);
            size += sizeof(byte);
            size += sizeof(bool);
            size += sizeof(byte);
            size += sizeof(byte);
            return size;
        }

        public override void WriteTo(ShockwaveWriter output)
        {
            output.Write((short)Type);
            output.Write(Rectangle);

            output.Write(Pattern);
            output.Write(ForegroundColor);
            output.Write(BackgroundColor);

            output.Write((byte)(IsFilled ? 1 : 0)); //TODO:
            output.Write((byte)(LineSize + 1));
            output.Write((byte)(LineDirection + 5));
        }
    }
}
