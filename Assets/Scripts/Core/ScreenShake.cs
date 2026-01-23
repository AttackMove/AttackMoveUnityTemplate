using UnityEngine;

/// <summary>
/// High-quality screen shake effect that scales with distance from the player.
/// Add this component to your camera to enable screen shake.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    [Header("Shake Intensity")]
    [Tooltip("Base intensity multiplier for all shake events")]
    [Range(0f, 2f)]
    public float BaseIntensity = 1f;

    [Header("Position Shake")]
    [Tooltip("Maximum position offset in world units")]
    [Range(0f, 5f)]
    public float MaxPositionShake = 1.5f;

    [Tooltip("How much position shake to apply (0 = none, 1 = full)")]
    [Range(0f, 1f)]
    public float PositionShakeAmount = 0.8f;

    [Header("Rotation Shake")]
    [Tooltip("Maximum rotation offset in degrees")]
    [Range(0f, 10f)]
    public float MaxRotationShake = 2f;

    [Tooltip("How much rotation shake to apply (0 = none, 1 = full)")]
    [Range(0f, 1f)]
    public float RotationShakeAmount = 0.5f;

    [Header("Noise Settings")]
    [Tooltip("Frequency of the shake noise (higher = faster shake)")]
    [Range(1f, 50f)]
    public float NoiseFrequency = 15f;

    [Tooltip("How quickly the shake fades out (higher = faster fade)")]
    [Range(0.5f, 100f)]
    public float FadeSpeed = 3f;

    [Header("Distance Scaling")]
    [Tooltip("Maximum distance at which shake has full effect")]
    [Range(5f, 100f)]
    public float MaxDistance = 30f;

    [Tooltip("Minimum distance - shake is always at least this strong even at max distance")]
    [Range(0f, 0.5f)]
    public float MinDistanceFactor = 0.1f;

    [Tooltip("Distance falloff curve (1 = linear, 2 = quadratic, 0.5 = square root)")]
    [Range(0.5f, 3f)]
    public float DistanceFalloff = 1.5f;

    private Vector3 _positionOffset;
    private Vector3 _rotationOffset;
    private float _currentIntensity;
    private float _targetIntensity;
    private float _noiseTime;
    private Vector3 _noiseOffset;

    private void LateUpdate()
    {
        UpdateShake();
    }

    private void UpdateShake()
    {
        // Fade out intensity
        _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, Time.deltaTime * FadeSpeed);

        if (_currentIntensity < 0.001f)
        {
            _currentIntensity = 0f;
            _positionOffset = Vector3.zero;
            _rotationOffset = Vector3.zero;
            return;
        }

        // Update noise time
        _noiseTime += Time.deltaTime * NoiseFrequency;

        // Generate smooth random offsets using Perlin noise
        var noiseX = Mathf.PerlinNoise(_noiseTime, 0f) * 2f - 1f;
        var noiseY = Mathf.PerlinNoise(0f, _noiseTime) * 2f - 1f;
        var noiseZ = Mathf.PerlinNoise(_noiseTime * 0.5f, _noiseTime * 0.5f) * 2f - 1f;

        // Apply intensity and scale
        var intensity = _currentIntensity * BaseIntensity;
        _positionOffset = new Vector3(
            noiseX * MaxPositionShake * PositionShakeAmount * intensity,
            noiseY * MaxPositionShake * PositionShakeAmount * intensity,
            0f // Don't shake in Z (depth) for 2D-style games
        );

        _rotationOffset = new Vector3(
            noiseY * MaxRotationShake * RotationShakeAmount * intensity,
            noiseX * MaxRotationShake * RotationShakeAmount * intensity,
            noiseZ * MaxRotationShake * RotationShakeAmount * intensity
        );
    }

    /// <summary>
    /// Add screen shake from an explosion or impact at a specific position.
    /// Intensity scales with distance from the player.
    /// </summary>
    /// <param name="position">World position of the explosion/impact</param>
    /// <param name="intensity">Base intensity of the shake (0-1)</param>
    /// <param name="duration">How long the shake should last (seconds)</param>
    public void ShakeFromPosition(Vector3 position, float intensity, float duration = 0.5f)
    {
        var world = Get.Instance<GameWorld>();
        if (world == null || world.LocalPlayer == null)
        {
            // Fallback: use camera position if no player available
            Shake(intensity, duration);
            return;
        }

        var playerPosition = world.LocalPlayer.transform.position;
        var distance = Vector3.Distance(position, playerPosition);

        // Calculate distance-based intensity scaling
        var normalizedDistance = Mathf.Clamp01(distance / MaxDistance);
        var distanceFactor = Mathf.Pow(1f - normalizedDistance, DistanceFalloff);
        distanceFactor = Mathf.Max(MinDistanceFactor, distanceFactor);

        var scaledIntensity = intensity * distanceFactor;
        Shake(scaledIntensity, duration);
    }

    /// <summary>
    /// Add screen shake with a specific intensity and duration.
    /// </summary>
    /// <param name="intensity">Intensity of the shake (0-1)</param>
    /// <param name="duration">How long the shake should last (seconds)</param>
    public void Shake(float intensity, float duration = 0.5f)
    {
        // Add to existing intensity rather than replacing it
        _targetIntensity = Mathf.Max(_targetIntensity, intensity);
        _currentIntensity = Mathf.Max(_currentIntensity, intensity);

        // Schedule fade out
        CancelInvoke(nameof(FadeOut));
        Invoke(nameof(FadeOut), duration);
    }

    private void FadeOut()
    {
        _targetIntensity = 0f;
    }

    /// <summary>
    /// Get the current position offset to apply to camera position.
    /// </summary>
    public Vector3 GetPositionOffset()
    {
        return _positionOffset;
    }

    /// <summary>
    /// Get the current rotation offset to apply to camera rotation.
    /// </summary>
    public Vector3 GetRotationOffset()
    {
        return _rotationOffset;
    }
}
