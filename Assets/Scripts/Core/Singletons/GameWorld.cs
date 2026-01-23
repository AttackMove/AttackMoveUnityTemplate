using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameWorld : MonoBehaviour, ISingletonInstance
{
    private readonly List<Unit> _units = new();
    public IReadOnlyList<Unit> Units => _units;

    public event Action<Unit> UnitDestroyed;
    public Canvas Canvas;
    public int BlockVisionLayerMask { get; private set; }
    public int CollisionLayerMask { get; private set; }

    private int _frameCount;

    public Player LocalPlayer;

    public void Start()
    {
        Time.timeScale = 1f;
        BlockVisionLayerMask = LayerMask.GetMask("Ground");
        CollisionLayerMask = LayerMask.GetMask("Ground");
    }

    public void Add(Unit unit)
    {
        _units.Add(unit);
    }

    public void Remove(Unit unit)
    {
        _units.Remove(unit);
        if (UnitDestroyed != null)
            UnitDestroyed(unit);
    }

    public bool HasLineOfSight(Vector3 from, Vector3 toTarget)
    {
        var ray = new Ray(from, toTarget);
        var blocked = Physics.Raycast(ray, toTarget.magnitude, BlockVisionLayerMask);
        return !blocked;
    }
}