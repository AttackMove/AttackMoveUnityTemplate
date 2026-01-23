using UnityEngine;

public static class VectorHelpers
{
    public static Vector3 DifferenceNoY(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x - b.x, 0, a.z - b.z);
    }

    public static Vector3 NoY(this Vector3 a)
    {
        return new Vector3(a.x, 0, a.z);
    }

    public static Vector3 Random(float x, float z)
    {
        return new Vector3(UnityEngine.Random.Range(-x, x), 0, UnityEngine.Random.Range(-z, z));
    }
}
