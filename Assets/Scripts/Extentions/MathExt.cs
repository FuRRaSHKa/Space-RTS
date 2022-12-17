using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;

public static class MathExt
{
    public static float ZeroClamp(float value)

    {
        return value > 0 ? value : 0;
    }

    public static int ZeroClamp(int value)

    {
        return value > 0 ? value : 0;
    }
}
