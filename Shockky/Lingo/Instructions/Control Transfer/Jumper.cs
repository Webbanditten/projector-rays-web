﻿namespace Shockky.Lingo.Instructions
{
    public abstract class Jumper : Instruction
    {
        public int Offset => Value;
        
        protected Jumper(OPCode op)
            : base(op)
        { }
        protected Jumper(OPCode op, LingoHandler handler)
            : base(op, handler)
        { }
        protected Jumper(OPCode op, LingoHandler handler, int offset)
            : base(op, handler)
        {
            Value = offset;
        }

        public abstract bool? RunCondition(LingoMachine machine);

        public static bool IsConditional(OPCode op) => (op == OPCode.IfFalse);

        public static bool IsValid(OPCode op)
        {
            switch (op)
            {
                case OPCode.IfFalse:
                case OPCode.Jump:
                case OPCode.EndRepeat:
                    return true;
                default:
                    return false;
            }
        }
    }
}