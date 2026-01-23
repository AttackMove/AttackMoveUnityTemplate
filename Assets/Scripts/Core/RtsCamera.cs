using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class RtsCamera : MonoBehaviour
{
    public InputActionReference MoveInputAction;
    public float MoveSpeed;
    public float ZoomSpeed;

    public float MinZoomHeight;
    public float MaxZoomHeight;
    
    // Allow zooming out more in portrait   
    public float AdjustedMaxZoomHeight => Screen.width > Screen.height ? MaxZoomHeight : MaxZoomHeight * 1.5f;

    private Vector3 _focusPoint;
    private Vector3 _offset;
    private GameInput _gameInput;

    // Touches
    Vector3? _midAnchor;      // world point under midpoint when we’re panning

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnhancedTouchSupport.Enable();
        _gameInput = Get.Instance<GameInput>();
        MoveInputAction.action.Enable();

        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (GameInput.GroundPlane.Raycast(ray, out float enter))
            _focusPoint = ray.GetPoint(enter);

        _offset = transform.position - _focusPoint;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMove(Time.deltaTime);
        UpdateZoom(Time.deltaTime);
        UpdateKeyboard();
        UpdateMouse();
        UpdateTouchMove();

        transform.position = _focusPoint + _offset;
    }

    private bool _mouseMove;
    private void UpdateMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            var unit = _gameInput.GetUnitAt(mouse.position.value, -1);
            _mouseMove = unit == null && !EventSystem.current.IsPointerOverGameObject();
        }
        else if (_mouseMove && mouse.leftButton.isPressed)
        {
            var last = mouse.position.value - mouse.delta.value;
            var current = mouse.position.value;
            PanCamera(Camera.main, last, current);
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
            _mouseMove = false;
    }

    private void UpdateKeyboard()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.spaceKey.isPressed)
        {
            var focus = GetFocus();
            focus.y = 0;
            _focusPoint = focus;
        }
    }

    private Vector3 GetFocus()
    {
        var playerUnit = Get.Instance<GameWorld>().LocalPlayer?.PlayerUnit;
        if (playerUnit.IsUnityNull())
            return _focusPoint;

        var position = playerUnit.transform.position;
        // Offset "down" from focus so it's higher on screen
        var offset = Vector3.back * -transform.position.y / 10f;
        return position + offset;
    }

    private Vector2 _move;
    private void UpdateMove(float deltaTime)
    {
        var forward = transform.up;
        forward.y = 0;
        forward.Normalize();

        var right = transform.right;
        right.y = 0;
        right.Normalize();

        // More zoomed out faster we go
        var normalizedHeight = Mathf.Clamp01((_offset.magnitude - MinZoomHeight) / (AdjustedMaxZoomHeight - MinZoomHeight));
        var zoomModifier = 1f + normalizedHeight * 4f;

        var moveInput = MoveInputAction.action.ReadValue<Vector2>();
        moveInput += GetMouseEdgeMove();
        _move = Vector2.MoveTowards(_move, moveInput, deltaTime / 0.1f);

        _focusPoint += forward * _move.y * deltaTime * MoveSpeed * zoomModifier;
        _focusPoint += right * _move.x * deltaTime * MoveSpeed * zoomModifier;
    }

    private bool _singleTouchDrag;
    private float _singleTouchStart;
    void UpdateTouchMove()
    {
        var touches = Touch.activeTouches;
        var singleTouch = touches.Count == 1;

        var cam = Camera.main;
        if (!cam)
            return;

        if (!singleTouch)
            _singleTouchDrag = false;

        if (touches.Count != 2)
        { 
            _midAnchor = null; 

            if(touches.Count == 1)
            {
                var touch = touches[0];
                if (touch.began)
                {
                    _singleTouchDrag = _gameInput.IsCameraMove(touch);
                    if (_singleTouchDrag)
                        _singleTouchStart = Time.realtimeSinceStartup;
                }
                else if(_singleTouchDrag)
                {
                    var overDragTime = (Time.realtimeSinceStartup - _singleTouchStart) > GameInput.DragTime;
                    var overDragDistance = (touch.screenPosition - touch.startScreenPosition).magnitude > GameInput.DragDistance;
                    var doPan = overDragTime || overDragDistance;

                    if (doPan)
                    {
                        var last = touch.screenPosition - touch.delta;
                        var current = touch.screenPosition;
                        PanCamera(cam, last, current);
                    }
                }
            }

            return; 
        }

        var t0 = touches[0];
        var t1 = touches[1];
        if (t0.history.Count == 0 || t1.history.Count == 0) 
            return;

        // Screen points (prev via deltas -> time-aligned)
        var p0 = t0.screenPosition; var p1 = t1.screenPosition;
        var p0p = p0 - t0.delta; var p1p = p1 - t1.delta;

        // World hits on the plane
        if (!GroundHit(cam, p0, out var q0) || !GroundHit(cam, p1, out var q1) ||
            !GroundHit(cam, p0p, out var q0p) || !GroundHit(cam, p1p, out var q1p))
            return;

        // -------- 1) ZOOM (adjust _offset instead of moving camera directly) --------
        var spanNow = Vector3.Distance(q0, q1);
        var spanPrev = Vector3.Distance(q0p, q1p);
        if (spanNow > 1e-2f && spanPrev > 1e-2f)
        {
            // Positive when fingers moved apart (zoom in), negative when together (zoom out)
            var worldSpanDelta = spanNow - spanPrev;

            // Tweak zoomScale (start around 0.5f–1.0f) for desired sensitivity
            var zoomScale = 0.7f;
            var dolly = worldSpanDelta * zoomScale;

            // Move along the offset direction (camera forward relative to focus)
            var dir = _offset.normalized;
            _offset -= dir * dolly * 5f; // subtract to move camera toward focus when pinching apart
            var offsetLength = _offset.magnitude;
            
            // Clamp still
            if (offsetLength > AdjustedMaxZoomHeight)
                _offset = _offset.normalized * AdjustedMaxZoomHeight;
            else if(offsetLength < MinZoomHeight)
                _offset = _offset.normalized * MinZoomHeight;
        }

        // -------- 2) PAN (translate focus point) --------
        // Midpoint on screen
        var mid = (p0 + p1) * 0.5f;

        // Coherence (0..1): 1 when both fingers move same direction/speed; 0 when pure pinch
        var coher = Coherence(t0.delta, t1.delta);

        // Init anchor when starting to pan
        if (_midAnchor == null && coher > 0.1f)
        {
            if (GroundHit(cam, mid, out var a)) 
                _midAnchor = a;
        }

        // Re-snap midpoint to anchor -> translate _focusPoint (fade by coherence so pinch doesn't drag)
        if (_midAnchor != null && GroundHit(cam, mid, out var hitNow))
        {
            var want = _midAnchor.Value - hitNow;    // planar translation needed
            _focusPoint += coher * want;
        }

    }

    private void PanCamera(Camera cam, Vector2 last, Vector2 current)
    {
        if (!GroundHit(cam, last, out Vector3 lastWorld))
            return;
        if (!GroundHit(cam, current, out Vector3 currentWorld))
            return;

        var delta = lastWorld - currentWorld;
        delta.y = 0;
        _focusPoint += delta;
    }

    // Helpers
    bool GroundHit(Camera cam, Vector2 screenPosition, out Vector3 hit)
    {
        var ray = cam.ScreenPointToRay(screenPosition);
        if (GameInput.GroundPlane.Raycast(ray, out var t)) 
        { 
            hit = ray.GetPoint(t); 
            return true; 
        }
        
        hit = default; 
        return false;
    }

    float Coherence(Vector2 d0, Vector2 d1)
    {
        var m0 = d0.magnitude; var m1 = d1.magnitude;
        if (m0 < 1e-4f || m1 < 1e-4f) return 0f;
        var dir = Mathf.Max(0f, Vector2.Dot(d0 / m0, d1 / m1)); // 0..1
        var mag = 1f - Mathf.Min(1f, Mathf.Abs(m0 - m1) / Mathf.Max(m0, m1)); // 0..1
        return dir * mag; // 0..1
    }
    private Vector2 GetMouseEdgeMove()
    {
        var move = Vector2.zero;

        var mouse = Mouse.current;
        if (mouse == null)
            return move;

        // To fix bug in WebGL mobile
        if(!Get.Instance<MouseDetection>().HasRealMouse())
            return move;

        // Tolerance includes both inside and outside so if its way outside the game area it doesn't count
        var tolerance = 10f;
        var mousePosition = mouse.position.value;
        if (mousePosition.x > Screen.width - tolerance && mousePosition.x < Screen.width + tolerance)
            move.x = 1f;
        if (mousePosition.y > Screen.height - tolerance && mousePosition.y < Screen.height + tolerance)
            move.y = 1f;
        if (mousePosition.x < tolerance && mousePosition.x > -tolerance)
            move.x = -1f;
        if (mousePosition.y < tolerance && mousePosition.y > -tolerance)
            move.y = -1f;

        return move;
    }

    private void UpdateZoom(float deltaTime)
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        var scroll = mouse.scroll.value.y;
        if (Mathf.Abs(scroll) <= Mathf.Epsilon)
            return;

        var startMouseRay = Camera.main.ScreenPointToRay(mouse.position.value);
        if (!GameInput.GroundPlane.Raycast(startMouseRay, out float enterStart))
            return;

        var startMouseWorld = startMouseRay.GetPoint(enterStart);

        var zoomMove = transform.forward * scroll * ZoomSpeed;
        var position = _offset + zoomMove;

        // Clip movement so y doesn't go beyond min/max
        if (position.y < MinZoomHeight)
        {
            var overflow = MinZoomHeight - position.y;
            var validPercent = Mathf.Max((-zoomMove.y - overflow) / zoomMove.y, 0f);
            position = _offset + validPercent * zoomMove;
        }
        else if (position.y > AdjustedMaxZoomHeight)
        {
            var overflow = position.y - AdjustedMaxZoomHeight;
            var validPercent = Mathf.Max((zoomMove.y - overflow) / zoomMove.y, 0f);
            position = _offset + validPercent * zoomMove;
        }

        _offset = position;
        transform.position = _focusPoint + _offset;

        var endMouseRay = Camera.main.ScreenPointToRay(mouse.position.value);
        if (!GameInput.GroundPlane.Raycast(endMouseRay, out float enterEnd))
            return;

        var endMouseWorld = endMouseRay.GetPoint(enterEnd);

        var zoomToMouse = true;
        if (zoomToMouse)
        {
            var delta = endMouseWorld - startMouseWorld;
            delta.y = 0;
            _focusPoint -= delta;
            transform.position = _focusPoint + _offset;
        }
    }

    internal void Focus(Unit unit)
    {
        _focusPoint = unit.transform.position;
        _focusPoint.y = 0;
    }
}
