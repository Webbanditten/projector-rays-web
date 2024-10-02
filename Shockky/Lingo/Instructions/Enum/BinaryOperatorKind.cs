﻿namespace Shockky.Lingo.Instructions
{
    public enum BinaryOperatorKind
    {
        Unknown,

        Or,
        And,

        GreaterThan,
        GreaterThanOrEqual,

        LessThan,
        LessThanOrEqual,

        Equality,
        InEquality,

        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,

        JoinString,
        JoinPadString,

        StartsWith,
        ContainsString,

        SpriteIntersects,
        SpriteWithin
    }
}
