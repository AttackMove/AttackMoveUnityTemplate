using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public int Team;
    public Unit PlayerUnitPrefab;

    public Unit PlayerUnit { get; private set; }

    public override void OnStartServer()
    {
        base.OnStartServer();

        var conn = connectionToClient;
        if (conn == null)
            return;

        var unit = Instantiate(PlayerUnitPrefab, Vector3.zero, Quaternion.identity);
        NetworkServer.Spawn(unit.gameObject, conn);

        // Wait a frame so client is ready
        StartCoroutine(SendToClient(conn, unit.netIdentity));
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Get.Instance<GameWorld>().LocalPlayer = this;
    }

    private IEnumerator SendToClient(NetworkConnectionToClient conn, NetworkIdentity unitId)
    {
        yield return new WaitForEndOfFrame();
        TargetSetUnit(conn, unitId);
    }

    [TargetRpc]
    private void TargetSetUnit(NetworkConnectionToClient conn, NetworkIdentity unitId)
    {
        var unit = unitId != null ? unitId.GetComponent<Unit>() : null;
        PlayerUnit = unit;
    }
}
