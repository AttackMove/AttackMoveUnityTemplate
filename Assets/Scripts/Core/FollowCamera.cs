using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class FollowCamera : MonoBehaviour
{
    public Transform Target;
    public float MinZoom;
    public float MaxZoom;
    public float ZoomSpeed;
    public Vector2 MovementModifier = new Vector2(1, 1);

    private Vector3 _offset;
    private Vector3 _mouseOffset;

    void Start()
    {
        _offset = transform.localPosition;
    }

    private void Update()
    {
        UpdateMouse();
        UpdateTouch();
    }

    private void UpdateTouch()
    {
        var touches = Touch.activeTouches.Where(x => !EventSystem.current.IsPointerOverGameObject(x.touchId)).ToList();
        if (touches.Count == 0)
            return;
    }

    private void UpdateMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        var scroll = -mouse.scroll.value.y;
        if (Mathf.Abs(scroll) < Mathf.Epsilon)
            return;

        // Further we are zoomed out the fast we go
        var offsetSize = _offset.magnitude;
        var maxZoomMultiplier = 4f;
        var sizeAdjust = 1f + (offsetSize / MaxZoom) * maxZoomMultiplier;
        var offsetLength = Mathf.Clamp(offsetSize + ZoomSpeed * scroll * sizeAdjust, MinZoom, MaxZoom);
        var oldOffset = _offset;
        _offset = _offset.normalized * offsetLength;

        // Adjust the mouseOffset by similar amount so we don't lag
        var deltaOffset = (_offset - oldOffset).magnitude;
        _mouseOffset = Vector3.MoveTowards(_mouseOffset, GetMouseOffset(Mouse.current), deltaOffset * 0.1f);
    }

    private Vector3 GetMouseOffset(Mouse mouse)
    {
        if (mouse == null)
            return Vector3.zero;

        var position = mouse.position.value;
        var x = (Mathf.Clamp01(position.x / Screen.width) - 0.5f) * 2f * MovementModifier.x;
        var y = (Mathf.Clamp01(position.y / Screen.height) - 0.5f) * 2f * MovementModifier.y;

        var ctrlDown = Keyboard.current?.ctrlKey.isPressed == true;

        var offset = Vector3.zero;
        var size = ctrlDown ? 0.4f : 0.05f;
        offset += transform.right * x * _offset.magnitude * size;
        offset += transform.up * y * _offset.magnitude * size;
        return offset;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Target == null)
            return;

        var ctrlDown = Keyboard.current?.ctrlKey.isPressed == true;
        var speed = ctrlDown ? 1f : 1f;
        _mouseOffset = Vector3.Lerp(_mouseOffset, GetMouseOffset(Mouse.current), Time.deltaTime * speed);
        transform.position = Target.transform.position + _offset + _mouseOffset;
    }
}
