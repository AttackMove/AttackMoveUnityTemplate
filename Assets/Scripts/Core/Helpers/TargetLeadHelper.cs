using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TargetLeadHelper
{
    public static Vector3 GetTargetLead(Vector3 shooter, Vector3 target, Vector3 enemyVelocity, float projectileSpeed, out float time, float leadAmount)
    {
        var toTarget = target - shooter;
        var a = Vector3.Dot(enemyVelocity, enemyVelocity) - (projectileSpeed * projectileSpeed);
        var b = 2 * Vector3.Dot(enemyVelocity, toTarget);
        var c = Vector3.Dot(toTarget, toTarget);

        var aIsZero = a < Mathf.Epsilon && a > -Mathf.Epsilon;
        if (aIsZero)
        {
            time = 0;
            return target;
        }

        var p = -b / (2 * a);
        var q = Mathf.Sqrt((b * b) - 4 * a * c) / (2 * a);

        var t1 = p - q;
        var t2 = p + q;
        float t;

        if (t1 > t2 && t2 > 0)
            t = t2;
        else
            t = t1;

        // Negative time - not valid - this happens if it's moving faster than us and heading away
        if (t < 0f)
        {
            time = 0f;
            return target;
        }

        time = t * leadAmount;
        if (float.IsNaN(time))
            return target;

        // Lead position from enemy movement
        var leadPosition = target + enemyVelocity * time;
        return leadPosition;
    }
}
