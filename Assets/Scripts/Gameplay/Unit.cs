using Mirror;
using System;
using System.Linq;
using UnityEngine;

public class Unit : Entity
{
    public bool Visible;
    public BoxCollider SelectionCollider;
    public float Health;

    #region Gameplay Stats
    public float MoveSpeed;
    public float RotateSpeed;
    #endregion

    public bool MouseOver { get; private set; }
    public bool Selected { get; private set; }
    public bool IsDead { get; private set; }
    public bool DontGroupSelect { get; private set; }

    protected Vector3? _destination;
    public Vector3? Destination => _destination;

    protected override void Init()
    {
        _world.Add(this);
    }

    protected override void DoUpdate(float deltaTime)
    {
        UpdateMove(Time.deltaTime);
    }

    protected virtual void UpdateMove(float deltaTime)
    {
        if (!isServer)
            return;

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

            var hitUnitWeWant = hitUnit == unit;
            if (hitUnit)
                return true;

            return false;
        }

        return true;
    }

    protected override void DeInit()
    {
        base.DeInit();
        _world.Remove(this);
    }

    internal virtual void MoveTo(Vector3 moveTo)
    {
        _destination = moveTo;
    }

    [Command]
    public void CmdMoveTo(Vector3 destination)
    {
        MoveTo(destination);
    }

    [Server]
    internal void Damage(float amount)
    {
        Health -= amount;

        if(Health <= 0)
        {
            Get.Instance<Fx>().RpcSpawnExplosion(transform.position, 2f);
            NetworkServer.Destroy(gameObject);
        }
    }
}

public abstract class UnitBase : NetworkBehaviour
{
    protected abstract void Start();
    protected abstract void Update();
}