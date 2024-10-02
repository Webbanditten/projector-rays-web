﻿using Shockky.IO;

namespace Shockky.Chunks
{
    public class UnknownChunk : ChunkItem
    {
        public byte[] Data { get; set; }

        public UnknownChunk(ref ShockwaveReader input, ChunkHeader header)
            : base(header)
        {
            Data = input.ReadBytes(header.Length).ToArray();
        }

        public override int GetBodySize() => Data.Length;

        public override void WriteBodyTo(ShockwaveWriter output)
        {
            output.Write(Data);
        }
    }
}
