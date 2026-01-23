using System.Runtime.CompilerServices;
using UnityEngine;

public static class UnityObjectUtility
{
    // Generic / boxed path
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUnityNull(this object obj)
    {
        return obj == null || (obj is Object uo && !uo);
    }

    // Fast path when already typed
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUnityNull(this Object obj)
    {
        return !obj;
    }
}