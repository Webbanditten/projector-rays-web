﻿using Shockky.IO;
using Shockky.Chunks;

namespace Shockky.Lingo
{
    public class LingoArgument : LingoItem
    {
        private readonly LingoHandler _handler;

        public int NameIndex { get; set; }
        public string Name => Script.Pool.GetName(NameIndex);

        public LingoArgument(LingoScriptChunk script, LingoHandler handler)
            : base(script)
        {
            _handler = handler;
        }

        public override int GetBodySize()
        {
            return sizeof(short);
        }

        public override void WriteTo(ShockwaveWriter output)
        {
            output.Write((short)NameIndex);
        }

        protected override string DebuggerDisplay => ToString();
        public override string ToString() => Name;
    }
}