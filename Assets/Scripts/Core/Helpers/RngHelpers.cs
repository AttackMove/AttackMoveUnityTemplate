using UnityEngine;

public static class RngHelpers
{
    public static bool GetRng(float randomChance)
    {
        var rng = Random.Range(0f, 1f);
        return rng < randomChance;
    }
}
