using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class GameInput : MonoBehaviour, ISingletonInstance
{
    private int _selectionMask;
    private GameWorld _gameWorld;

    public static Plane GroundPlane;

    // Drag starts after this many seconds
    public static float DragTime = 0.2f;
    // Or drag starts if you move more than this many pixels
    public static float DragDistance = 20f;

    public static float DefaultHeight = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _gameWorld = Get.Instance<GameWorld>();
        _selectionMask = LayerMask.GetMask("Selection");
        GroundPlane = new Plane(Vector3.down, DefaultHeight);

        // Disable keyboard/gamepad input on ui in game as it navigates around buttons sometimes
        var es = EventSystem.current;
        if (es != null)
            es.sendNavigationEvents = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateUnitActionForMouse();
    }

    private void UpdateUnitActionForMouse()
    {
        var playerUnit = _gameWorld.LocalPlayer?.PlayerUnit;
        var nothingSelected = playerUnit.IsUnityNull();
        if (nothingSelected)
            return;

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        var rightMousePressed = mouse.rightButton.wasPressedThisFrame;
        if (!rightMousePressed)
            return;

        var moveTo = GetGroundPoint(mouse.position.value);
        playerUnit.CmdMoveTo(moveTo);
    }

    private Vector3 GetGroundPoint(Vector2 screenPoint)
    {
        var ray = Camera.main.ScreenPointToRay(screenPoint);
        GroundPlane.Raycast(ray, out float enter);
        return ray.GetPoint(enter).NoY();
    }

    public Unit GetUnitAt(Vector2 screenPoint, int excludeTeam)
    {
        var ray = Camera.main.ScreenPointToRay(screenPoint);
        Unit under = null;
        var maxDistance = float.MaxValue;

        var units = Get.Instance<GameWorld>().Units.Where(x => x.Team != excludeTeam && x.Visible);
        foreach (var unit in units)
        {
            if (!unit.SelectionCollider.Raycast(ray, out RaycastHit hit, 10000))
                continue;

            var distance = (hit.point - unit.transform.position).magnitude;
            if (distance < maxDistance)
            {
                maxDistance = distance;
                under = unit;
            }
        }

        return under;
    }

    internal bool IsCameraMove(Touch touch)
    {
        var screenPosition = touch.screenPosition;
        if (GetUnitAt(screenPosition, -1) != null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return false;

        return true;
    }  
}
