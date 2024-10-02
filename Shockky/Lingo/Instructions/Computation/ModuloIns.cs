﻿namespace Shockky.Lingo.Instructions
{
    public class ModuloIns : Computation
    {
        public ModuloIns()
            : base(OPCode.Modulo, BinaryOperatorKind.Modulo)
        { }

        protected override object Execute(dynamic left, dynamic right) => (left % right);
    }
}