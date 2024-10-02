﻿using System.Diagnostics;

namespace Shockky.Lingo.Instructions
{
    public class GetIns : Instruction
    {
        public GetIns(LingoHandler handler, int value)
            : base(OPCode.Get, handler)
        {
            Value = value;
            //Debug.WriteLine("GET: " + value);
        }
    }
}