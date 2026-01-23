using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Vision : MonoBehaviour, ISingletonInstance
{
    private float _nextVisionCheck;
    private float _visionCheckFrequency = 0.2f;

    private GameWorld _gameWorld;
    private const int NUM_TEAMS = 2;
    public static int PlayerTeam = 0;

    private readonly Dictionary<Unit, bool>[] _unitVisions = new Dictionary<Unit, bool>[NUM_TEAMS];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for( var i = 0; i < NUM_TEAMS; i++)
            _unitVisions[i] = new();
        _gameWorld = Get.Instance<GameWorld>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_nextVisionCheck > Time.time)
            return;

        DoVisionUpdate();

        _nextVisionCheck = Time.time + _visionCheckFrequency;
    }

    private void DoVisionUpdate()
    {
        var units = _gameWorld.Units;

        for(var team = 0; team < NUM_TEAMS; team++)
        {
            var viewerTeam = units.Where(x => x.Team == team);
            var canSeeTeam = units.Where(x => x.Team == 1 - team);            

            foreach (var canSeeUnit in canSeeTeam)
            {
                var seen = false;
                foreach( var viewer in viewerTeam)
                {
                    if (!viewer.CanSee(canSeeUnit))
                        continue;

                    seen = true;
                    break;
                }

                _unitVisions[team][canSeeUnit] = seen;

                // Update visibility for player
                canSeeUnit.Visible = CanTeamSee(PlayerTeam, canSeeUnit);
            }
        }
    }

    public bool CanTeamSee(int team, Unit unit)
    {
        if (unit.Team == team)
            return true;

        if (!_unitVisions[team].ContainsKey(unit))
            return false;

        return _unitVisions[team][unit];
    }
}