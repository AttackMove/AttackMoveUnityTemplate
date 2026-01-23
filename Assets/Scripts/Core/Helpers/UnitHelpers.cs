using UnityEngine;

public static class UnitHelpers
{
    /// <summary>
    /// Get the unit from a Transform, this works well when you have a collision on the children
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Unit GetUnit(this Transform t)
    {
        var current = t;

        while (current != null)
        {
            var target = current.GetComponent<Unit>();
            if (target != null)
                return target;

            current = current.parent;
        }

        return null;
    }
}