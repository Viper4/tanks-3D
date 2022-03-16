using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clamping
{
    public static float ClampAngle(float angle, float min, float max)
    {
        // Formatting angle
        angle = angle > 180 ? angle - 360 : angle;

        return Mathf.Clamp(angle, min, max);
    }
}
