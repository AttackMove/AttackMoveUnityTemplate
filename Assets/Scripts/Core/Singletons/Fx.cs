using Mirror;
using UnityEngine;

public class Fx : NetworkBehaviour, ISingletonInstance
{
    public ParticleSystem ExplosionPrefab;


    [ClientRpc]
    public void RpcSpawnExplosion(Vector3 position, float scale)
    {
        if (ExplosionPrefab == null)
            return;

        var explosion = Instantiate(ExplosionPrefab, position, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * scale;
        
        ScreenShake(position, scale);
    }

    private static void ScreenShake(Vector3 position, float scale)
    {
        if (scale < 2f)
            return;

        // Trigger screen shake based on explosion scale
        // Scale is typically 0.1-2.0, map to shake intensity 0.1-1.0
        var shakeIntensity = Mathf.Clamp01(scale * 0.5f);
        var shakeDuration = Mathf.Clamp(scale * 0.3f, 0.2f, 1.0f);

        var camera = Camera.main;
        if (camera != null)
        {
            var screenShake = camera.GetComponent<ScreenShake>();
            if (screenShake != null)
            {
                screenShake.ShakeFromPosition(position, shakeIntensity, shakeDuration);
            }
        }
    }
}
