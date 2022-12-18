using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public static Vector3 GetRandomPosInsideCircle(this Vector3 position, Vector2 upwards, float radius)
    {
        Vector3 posInsideCircle = Random.insideUnitCircle * radius;
        Quaternion rotationToUpwards = Quaternion.FromToRotation(Vector3.forward, upwards);

        return position + rotationToUpwards * posInsideCircle;
    }
}
