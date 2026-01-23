using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class MouseDetection : MonoBehaviour, ISingletonInstance
{
    private bool _hasRealMouse = false;
    private readonly float mouseMovementThreshold = 10f; // pixels

    public List<GameObject> OnForMouse;
    public List<GameObject> OffForMouse;

    private bool _isIPadOrIOS = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    // JavaScript interop for reliable iPad/iOS detection via user agent
    [DllImport("__Internal")]
    private static extern int IsIPadOrIOSJS();
#endif

    void Start()
    {
        // Detect iPad/iOS - Safari on iPad simulates mouse events for touch
        _isIPadOrIOS = IsIPadOrIOS();
    }

    void Update()
    {
        // If we're on iPad/iOS, never detect mouse (Safari simulates mouse for touch)
        if (_isIPadOrIOS)
        {
            UpdateObjects();
            return;
        }

        if (Mouse.current == null)
        {
            UpdateObjects();
            return;
        }

        var touch = Touchscreen.current;
        var hasTouchDown = touch != null && touch.touches.Any(t => t.press.isPressed);

        // If mouse has moved significantly, we have a real mouse
        var mouseDelta = Mouse.current.delta.ReadValue();
        var mouseMoved = mouseDelta.sqrMagnitude > 0.0001f;

        if (!_hasRealMouse && mouseMoved && !hasTouchDown)
            _hasRealMouse = true;

        UpdateObjects();
    }

    private bool IsIPadOrIOS()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Use JavaScript user agent detection - most reliable for WebGL
        // This matches the detection used in the HTML template
        return IsIPadOrIOSJS() != 0;
#elif UNITY_IPHONE || UNITY_IOS
        return true;
#else
        // Fallback: try SystemInfo.operatingSystem (may not be reliable in WebGL)
        string osName = SystemInfo.operatingSystem.ToLower();
        return osName.Contains("iphone") || osName.Contains("ipad");
#endif
    }

    private void UpdateObjects()
    {
        var hasMouse = HasRealMouse();
        OnForMouse.ForEach(x => x.SetActive(hasMouse));
        OffForMouse.ForEach(x => x.SetActive(!hasMouse));
    }

    public bool HasRealMouse() => _hasRealMouse;// && !Application.isEditor;
}