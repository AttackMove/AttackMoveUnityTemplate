using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Logic to handle group selection of units in a game. This includes rendering outlines and standard RTS style selection
/// It can also be used for single unit selection
/// </summary>
public class GroupSelect
{
    public IReadOnlyList<Unit> SelectedUnits => _selectedUnits;
    public IReadOnlyList<Unit> SelectedOwnUnits => _selectedUnits.Where(x => x.Team == _playerTeam).ToList();

    public bool MouseDown { get; private set; }
    public Vector2 MouseDownStart { get; private set; }
    private float _mouseDownStartTime;

    private readonly List<Unit> _selectedUnits = new List<Unit>();
    private readonly List<Unit> _tempSelectedUnits = new List<Unit>();
    private readonly List<Unit> _mouseOverUnits = new List<Unit>();
    private readonly IReadOnlyList<Unit> _units;
    private bool _mouseMovedThisSelect;
    private int _playerTeam = 0;
    private bool _supportTouches = false;

    public Unit MouseOverUnit;

    public bool DragSelectEnabled => false;

    public event Action OnSelectionChanged;

    public GroupSelect(IReadOnlyList<Unit> units, int team)
    {
        _units = units;
        _playerTeam = team;

        Get.Instance<GameWorld>().UnitDestroyed += OnUnitDestroyed;
    }

    private void OnUnitDestroyed(Unit unit)
    {
        if (_selectedUnits.Contains(unit))
            _selectedUnits.Remove(unit);

        unit.SetSelected(false);
    }

    public void DoUpdate()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        var mouse = Mouse.current;

        // Ignore when right mouse button is being pressed as we're doing something other than group selecting
        if (mouse != null && mouse.rightButton.isPressed)
            return;

        var leftMouseDown = PrimaryPressedThisFrame();
        if (leftMouseDown)
        {
            var pos = GetPrimaryPointerPosition() ?? (mouse != null ? (Vector2?)mouse.position.value : null);
            if (pos == null)
                return;
            MouseDownStart = pos.Value;
            MouseDown = true;
            _mouseMovedThisSelect = false;
            _mouseDownStartTime = Time.time;

            if (!AddToSelection() && DragSelectEnabled)
            {
                if (_selectedUnits.Count > 0)
                {
                    _selectedUnits.ForEach(x => x.SetSelected(false));
                    _selectedUnits.Clear();
                    OnSelectionChanged();
                }
            }
        }

        var leftMouseUp = PrimaryReleasedThisFrame();
        if (leftMouseUp)
        {
            MouseDown = false;

            if (!DragSelectEnabled)
            {
                // When dragging mouse, don't do select
                var downTime = Time.time - _mouseDownStartTime;
                var wasDrag = _mouseMovedThisSelect || downTime > 0.2f;
                if (wasDrag)
                {
                    _tempSelectedUnits.Clear();
                    return;
                }
            }

            // Convert temp to full
            if (_tempSelectedUnits.Count > 0 || !DragSelectEnabled)
            {
                // When not drag selecting we must call set selected at the end of drag
                if (!DragSelectEnabled)
                {
                    if (!AddToSelection())
                    {
                        _selectedUnits.ForEach(x => x.SetSelected(false));
                        _selectedUnits.Clear();
                    }

                    _tempSelectedUnits.ForEach(x => x.SetSelected(true));
                }

                _selectedUnits.AddRange(_tempSelectedUnits);
                _tempSelectedUnits.Clear();
                if (OnSelectionChanged != null)
                    OnSelectionChanged();
            }

            return;
        }

        var leftMouseisDown = PrimaryIsPressed();
        if (leftMouseisDown)
        {
            var currentPos = GetPrimaryPointerPosition() ?? (mouse != null ? (Vector2?)mouse.position.value : null);
            if (currentPos == null)
                return;

            var delta = (currentPos.Value - MouseDownStart).magnitude;
            if (delta > 10)
                _mouseMovedThisSelect = true;

            var selected = GetSelectedUnits();

            foreach (var tempSelected in _tempSelectedUnits)
            {
                if (_selectedUnits.Contains(tempSelected))
                    continue;

                tempSelected.SetSelected(false);
            }
            _tempSelectedUnits.Clear();

            // If mouse hasn't moved, will only select one and therefore need to find closest to camera
            if (!_mouseMovedThisSelect && selected.Count > 1)
            {
                selected = selected.OrderBy(x => Vector3.Distance(Camera.transform.position, x.transform.position)).ToList();
            }

            foreach (var unit in selected)
            {
                if (DragSelectEnabled)
                    unit.SetSelected(true);

                _tempSelectedUnits.Add(unit);

                // Only select first one found
                if (!_mouseMovedThisSelect)
                    break;
            }
        }

        // Always update mouse over
        _mouseOverUnits.ForEach(x => x.SetMouseOver(false));
        _mouseOverUnits.Clear();

        // When drag selecting we can be over multiple units
        if (DragSelectEnabled)
        {
            var mouseOver = GetMouseOverUnits(Unit.AnyTeam);
            mouseOver.ForEach(x => x.SetMouseOver(true));            
            _mouseOverUnits.AddRange(mouseOver);
        }
        else
        {
            // Non drag select only ever allow single unit to be moused over
            var mouseOver = GetMouseOverUnit(Unit.AnyTeam);
            if(mouseOver != null)
            {
                mouseOver.SetMouseOver(true);
                _mouseOverUnits.Add(mouseOver);
            }
        }


        // Remove dead
        foreach (var unit in _selectedUnits.ToList())
        {
            if (unit.IsDead)
                _selectedUnits.Remove(unit);
        }

        // Check if mouse is over any unit
        MouseOverUnit = GetMouseOverUnit(Unit.AnyTeam);
    }

    private Camera Camera => Camera.main;

    public List<Unit> GetMouseOverUnits(int team)
    {
        var units = new List<Unit>();
        foreach (var unit in _units.Where(x => (team == Unit.AnyTeam || x.Team == team) && !x.IsDead))
        {
            if (IsMouseOver(unit))
                units.Add(unit);
        }

        // Order by closest to mouse
        if (units.Count > 1)
        {
            var mOpt = GetPointerForHover();
            var m = mOpt ?? (Mouse.current != null ? (Vector2?)Mouse.current.position.value : Vector2.zero);
            units = units.OrderBy(x => Vector2.Distance(m.Value, Camera.WorldToScreenPoint(x.transform.position))).ToList();
        }

        return units;
    }

    private Vector2? GetPointerPosition()
    {
        return GetPointerForHover() ?? (Mouse.current != null ? Mouse.current.position.value : null);
    }

    private Unit GetMouseOverUnit(int team)
    {
        return GetMouseOverUnits(team).FirstOrDefault();
    }

    internal bool IsMouseOver(Unit unit)
    {
        var world = Get.Instance<GameWorld>();
        var mousePositionOpt = GetPointerPosition();
        if (mousePositionOpt == null)
            return false;

        var mousePosition = mousePositionOpt.Value;

        var collider = unit.SelectionCollider;
        if (collider == null)
            return false;

        if (collider.Raycast(Camera.ScreenPointToRay(mousePosition), out RaycastHit hit, 10000f))
            return true;

        return false;
    }

    private List<Unit> GetSelectedUnits()
    {
        var pointerOpt = GetPrimaryPointerPosition() ?? (Mouse.current != null ? (Vector2?)Mouse.current.position.value : null);
        if (pointerOpt == null)
            return new List<Unit>();

        var selected = new List<Unit>();

        if (!DragSelectEnabled)
        {
            // Just return unit under mouse
            var underMouse = GetMouseOverUnit(Unit.AnyTeam);
            if (underMouse != null)
                selected.Add(underMouse);

            return selected;
        }

        var mousePosition = pointerOpt.Value;
        var mouseMin = new Vector2(Mathf.Min(MouseDownStart.x, mousePosition.x), Mathf.Min(MouseDownStart.y, mousePosition.y));
        var mouseMax = new Vector2(Mathf.Max(MouseDownStart.x, mousePosition.x), Mathf.Max(MouseDownStart.y, mousePosition.y));

        var rect = new Rect(mouseMin.x, mouseMin.y, mouseMax.x - mouseMin.x, mouseMax.y - mouseMin.y);

        var world = Get.Instance<GameWorld>();
        //var localPlayer = world.LocalPlayer;

        //if (localPlayer == null)
        //    return selected;

        //var team = localPlayer.Team;
        var team = 0;

        foreach (var unit in _units.Where(x => x.Team == team && !x.IsDead && x.isOwned))
        {
            if (unit.DontGroupSelect && _mouseMovedThisSelect)
                continue;

            var collider = unit.SelectionCollider;
            if (collider == null)
                continue;

            var inside = CheckIsInside(mousePosition, ref rect, world, unit, collider);

            if (inside)
                selected.Add(unit);
        }

        // If there are none then we should check enemies and select 1 if it was clicked on
        if (selected.Count == 0)
        {
            foreach (var unit in _units.Where(x => x.Team != team && !x.IsDead))
            {
                if (IsMouseOver(unit))
                {
                    selected.Add(unit);
                    break;
                }
            }
        }

        return selected;
    }

    private bool CheckIsInside(Vector2 mousePosition, ref Rect rect, GameWorld world, Unit unit, Collider collider)
    {
        var corners = GetWorldSpaceCorners(collider);

        foreach (var corner in corners)
        {
            var screenPos = Camera.WorldToScreenPoint(corner);
            if (rect.Contains(screenPos))
            {
                return true;
            }
        }

        // Check if mouse inside the collider
        return IsMousewithin(mousePosition, collider);
    }

    private bool IsMousewithin(Vector2 mousePosition, Collider collider)
    {
        var mouseInsideUnit = collider.Raycast(Camera.ScreenPointToRay(mousePosition), out RaycastHit hitInfo, 10000f);
        return mouseInsideUnit;
    }

    private Vector3[] GetWorldSpaceCorners(Collider collider)
    {
        var center = collider.bounds.center;
        var worldSize = collider.transform.lossyScale;
        var size = new Vector3(worldSize.x * collider.bounds.size.x, worldSize.y * collider.bounds.size.y, worldSize.z * collider.bounds.size.z) / 2.0f;

        var corners = new Vector3[8];

        corners[0] = center + new Vector3(-size.x, -size.y, -size.z);
        corners[1] = center + new Vector3(size.x, -size.y, -size.z);
        corners[2] = center + new Vector3(size.x, -size.y, size.z);
        corners[3] = center + new Vector3(-size.x, -size.y, size.z);
        corners[4] = center + new Vector3(-size.x, size.y, -size.z);
        corners[5] = center + new Vector3(size.x, size.y, -size.z);
        corners[6] = center + new Vector3(size.x, size.y, size.z);
        corners[7] = center + new Vector3(-size.x, size.y, size.z);

        return corners;
    }

    private bool AddToSelection()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard.shiftKey.isPressed;
    }

    public void DrawOutline()
    {
        if (!MouseDown || !DragSelectEnabled)
            return;

        var selectionRect = new Rect();
        var pointerOpt = GetPrimaryPointerPosition() ?? (Mouse.current != null ? (Vector2?)Mouse.current.position.value : null);
        if (pointerOpt == null)
            return;
        var pointer = pointerOpt.Value;
        selectionRect.xMin = Mathf.Min(MouseDownStart.x, pointer.x);
        selectionRect.xMax = Mathf.Max(MouseDownStart.x, pointer.x);
        selectionRect.yMin = Screen.height - Mathf.Max(MouseDownStart.y, pointer.y);
        selectionRect.yMax = Screen.height - Mathf.Min(MouseDownStart.y, pointer.y);

        var borderWidth = 2f;
        GUI.color = new Color(1, 1, 1, 1f);

        // Draw top border
        GUI.DrawTexture(new Rect(selectionRect.xMin, selectionRect.yMin, selectionRect.width, borderWidth), Texture2D.whiteTexture);
        // Draw left border
        GUI.DrawTexture(new Rect(selectionRect.xMin, selectionRect.yMin, borderWidth, selectionRect.height), Texture2D.whiteTexture);
        // Draw bottom border
        GUI.DrawTexture(new Rect(selectionRect.xMin, selectionRect.yMax - borderWidth, selectionRect.width, borderWidth), Texture2D.whiteTexture);
        // Draw right border
        GUI.DrawTexture(new Rect(selectionRect.xMax - borderWidth, selectionRect.yMin, borderWidth, selectionRect.height), Texture2D.whiteTexture);

        GUI.color = new Color(1, 1, 1, 0.1f); // Semi-transparent white
        GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);
    }

    // Primary input abstraction: left mouse or single touch for selection
    private int? _primaryTouchId;

    private bool PrimaryPressedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            _primaryTouchId = null;
            return true;
        }

        if (!_supportTouches)
            return false;

        var touch = GetSingleActiveTouch();
        if (touch != null && touch.press.wasPressedThisFrame)
        {
            _primaryTouchId = touch.touchId.value;
            return true;
        }

        return false;
    }

    private bool PrimaryIsPressed()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
            return true;

        if (!_supportTouches)
            return false;

        var touch = GetTrackedOrSingleActiveTouch();
        return touch != null && touch.press.isPressed;
    }

    private bool PrimaryReleasedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            return true;

        var ts = Touchscreen.current;
        if (ts == null || !_supportTouches)
            return false;

        if (_primaryTouchId != null)
        {
            var t = ts.touches.FirstOrDefault(x => x.touchId.value == _primaryTouchId.Value);
            if (t != null && t.press.wasReleasedThisFrame)
            {
                _primaryTouchId = null;
                return true;
            }
            if (t == null)
            {
                _primaryTouchId = null;
                return false;
            }
        }
        else
        {
            var single = GetSingleActiveTouch();
            if (single != null && single.press.wasReleasedThisFrame)
                return true;
        }

        return false;
    }

    private Vector2? GetPrimaryPointerPosition()
    {
        var mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.isPressed || mouse.leftButton.wasPressedThisFrame))
            return mouse.position.value;

        if (!_supportTouches)
            return null;

        var touch = GetTrackedOrSingleActiveTouch();
        if (touch != null)
            return touch.position.value;

        return null;
    }

    private Vector2? GetPointerForHover()
    {
        var primary = GetPrimaryPointerPosition();
        if (primary != null)
            return primary;
        var mouse = Mouse.current;
        if (mouse != null)
            return mouse.position.value;
        return null;
    }

    private TouchControl GetTrackedOrSingleActiveTouch()
    {
        var ts = Touchscreen.current;
        if (ts == null || !_supportTouches)
            return null;

        if (_primaryTouchId != null)
        {
            var tracked = ts.touches.FirstOrDefault(x => x.touchId.value == _primaryTouchId.Value);
            if (tracked != null)
                return tracked;
        }

        return GetSingleActiveTouch();
    }

    private TouchControl GetSingleActiveTouch()
    {
        var ts = Touchscreen.current;
        if (ts == null || !_supportTouches)
            return null;

        var active = ts.touches.Where(t => t.press.isPressed).OrderBy(t => t.touchId.value).ToList();
        if (active.Count == 1)
            return active[0];
        return null;
    }

    public void AddUnit(Unit unit)
    {
        if (unit == null || _selectedUnits.Contains(unit))
            return;

        var additive = AddToSelection();
        if (!additive)
            _selectedUnits.ToList().ForEach(x => RemoveUnit(x));

        _selectedUnits.Add(unit);
        unit.SetSelected(true);
        if (OnSelectionChanged != null)
            OnSelectionChanged();
    }

    internal void RemoveUnit(Unit unit)
    {
        if (unit == null)
            return;

        if (!_selectedUnits.Contains(unit))
            return;

        _selectedUnits.Remove(unit);
        unit.SetSelected(false);
    }

    internal void UnitGotInto(Unit unit, Unit into)
    {
        // If the only unit selected just got into a unit, select the unit it got into
        if (_selectedUnits.Count == 1 && _selectedUnits[0] == unit && !into.DontGroupSelect)
            AddUnit(into);

        RemoveUnit(unit);
    }

    internal void ClearUnits()
    {
        foreach (var unit in _selectedUnits.ToList())
        {
            RemoveUnit(unit);
        }
    }
}
