using Mirror;
using System;
using System.Linq;
using UnityEngine;

public class Unit : NetworkBehaviour
{
    public int Team;
    public bool Visible;
    public BoxCollider SelectionCollider;

    #region Gameplay Stats
    public float MoveSpeed;
    public float RotateSpeed;
    #endregion

    public bool MouseOver { get; private set; }
    public bool Selected { get; private set; }
    public bool IsDead { get; private set; }
    public bool DontGroupSelect { get; private set; }

    public static int Team0 = 0;
    public static int Team1 = 1;
    public static int AnyTeam = -1;

    private GameWorld _world;
    private Vector3? _destination;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _world = Get.Instance<GameWorld>();
        _world.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMove(Time.deltaTime);
    }

    protected virtual void UpdateMove(float deltaTime)
    {
        if (_destination == null)
            return;

        var toDestination = _destination.Value - transform.position;
        transform.forward = Vector3.RotateTowards(transform.forward, toDestination.normalized, RotateSpeed * Mathf.Deg2Rad * deltaTime, 0f);
        transform.position = Vector3.MoveTowards(transform.position, _destination.Value, MoveSpeed * deltaTime);

        var reachedDestination = toDestination.sqrMagnitude < 0.01f;
        if (reachedDestination)
        {
            transform.position = _destination.Value;
            _destination = null;
            return;
        }
    }

    internal void SetSelected(bool selected)
    {
        Selected = selected;
    }

    internal void SetMouseOver(bool mouseOver)
    {
        MouseOver = mouseOver;
    }

    public bool CanSee(Unit unit)
    {
        var direction = unit.transform.position - transform.position;
        var ray = new Ray(transform.position, direction);
        var hits = Physics.RaycastAll(ray, direction.magnitude, _world.BlockVisionLayerMask).OrderBy(x => x.distance);

        foreach (var hit in hits)
        {
            var hitUnit = hit.transform.GetUnit();
            if (hitUnit != null)
            {
                if (hitUnit.Team == Team)
                    continue;
            }

            // Hit the unit we were looking for
            if (hitUnit == unit)
                return true;

            return false;
        }

        return true;
    }

    public void OnDestroy()
    {
        if (Get.ShuttingDown)
            return;

        _world.Remove(this);
    }

    [Command]
    internal void CmdMoveTo(Vector3 moveTo)
    {
        _destination = moveTo;
    }
}
