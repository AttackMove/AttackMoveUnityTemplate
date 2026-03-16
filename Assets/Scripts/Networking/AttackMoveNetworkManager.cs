using Mirror;
using Mirror.SimpleWeb;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AttackMoveNetworkManager : NetworkManager
{
    public bool AutoStartSingleplayer;
    public float ClientConnectTimeoutSeconds = 5f;

    private Coroutine _clientConnectTimeoutCoroutine;

    private new void Awake()
    {
        base.Awake();

#if UNITY_EDITOR
        if (transport != null && transport is SimpleWebTransport swt)
        {
            swt.sslEnabled = false;
        }
#endif

        if(AutoStartSingleplayer)
            StartSingleplayer();
    }

    public override void Start()
    {
        // Headless/dedicated: only start server (or client per headlessStartMode), never host.
        if (Utils.IsHeadless())
        {
            base.Start();
            return;
        }
    }

    public override void OnClientConnect()
    {
        if (_clientConnectTimeoutCoroutine != null)
        {
            StopCoroutine(_clientConnectTimeoutCoroutine);
            _clientConnectTimeoutCoroutine = null;
        }
        Debug.Log($"[Client] Connected to {networkAddress}");
        base.OnClientConnect();
    }

    public override void OnClientDisconnect()
    {
        Debug.Log($"[Client] Disconnected from {networkAddress}");
        base.OnClientDisconnect();
    }

    public override void OnClientError(TransportError error, string reason)
    {
        Debug.LogError($"[Client] Error: {error} - {reason}");
        base.OnClientError(error, reason);
    }

    public override void OnClientTransportException(System.Exception exception)
    {
        Debug.LogException(exception);
        base.OnClientTransportException(exception);
    }

    public void StartSingleplayer()
    {
        // Only disable listening when we're actually starting; never clobber listen when
        // server is already running (e.g. second NetworkManager or scene reload).
        if (!NetworkServer.active)
        {
            // Singleplayer: start host without accepting external connections
            NetworkServer.listen = false;
            StartHost();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        // After the client has disconnected, check if there are any clients left
        // (excluding the local connection if we're in host mode)
        int remainingConnections = 0;
        foreach (var connection in NetworkServer.connections.Values)
        {
            // Count non-local connections (actual clients)
            if (connection != NetworkServer.localConnection)
            {
                remainingConnections++;
            }
        }

        // If no clients remain, reload the scene to get back to the start state
        if (remainingConnections == 0 && NetworkServer.active)
        {
            StartCoroutine(Restart());
        }
    }

    private IEnumerator Restart()
    {
        Stop();

        // Wait a few frames to close
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public override void OnApplicationQuit()
    {
        Stop();

        // Call base implementation to ensure all cleanup happens
        base.OnApplicationQuit();
    }

    private void Stop()
    {
        // Ensure proper cleanup of server and client before application quits
        // This prevents socket binding errors when restarting
        if (NetworkServer.active)
        {
            StopServer();
        }

        if (NetworkClient.isConnected)
        {
            StopClient();
        }
    }
}