using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using System.Text;
using NoFowl.Helpers;

namespace NoFowl.Networking
{
    /// <summary>
    /// Class handling the server side of networking.
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ServerGameNetPortal : Singleton<ServerGameNetPortal>
    {
        [Header("Settings")]
        [SerializeField] private int maxPlayers = 8;

        public event Action<ulong, int> OnLocalPlayerConnect;
        public event Action<ulong, int> OnLocalPlayerDisconnect;

        private Dictionary<string, ClientData> clientData;
       [SerializeField] private Dictionary<ulong, string> clientGuids;

        private bool gameInProgress;

        private GameNetPortal gameNetPortal;

        #region MONO

        private void Start()
        {
            gameNetPortal = GetComponent<GameNetPortal>();
            gameNetPortal.OnNetworkReadied += HandleNetworkReadied;

            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;

            clientData = new Dictionary<string, ClientData>();
            clientGuids = new Dictionary<ulong, string>();
        }

        private void OnDestroy()
        {
            if (gameNetPortal)
            {
                gameNetPortal.OnNetworkReadied -= HandleNetworkReadied;

                if (NetworkManager.Singleton)
                {
                    NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
                    NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
                }
            }
        }
        #endregion

        public ClientData? GetClientData(ulong clientId)
        {
            if (clientGuids.TryGetValue(clientId, out string clientGuid))
            {
                if (clientData.TryGetValue(clientGuid, out ClientData data))
                {
                    return data;
                }
                else
                {
                    Debug.LogWarning("Failed to find data for client: " + clientId);
                }
            }
            else
            {
                Debug.LogWarning("Failed to find guid for client: " + clientId);
            }

            return null;
        }

        public void SetPlayerColor(ulong clientId, int localId, int colorIndex)
        {
            ClientData? client = GetClientData(clientId);

            if (client.HasValue)
            {
                client.Value.SetPlayerColor(localId, colorIndex);
            }
        }

        public int PlayerCount()
        {
            int players = 0;

            foreach (KeyValuePair<string, ClientData> pair in clientData)
            {
                players += pair.Value.playerData.Count;
            }

            return players;
        }

        public int PlayerCount(ulong clientId)
        {
            ClientData? data = GetClientData(clientId);

            if (data.HasValue)
            {
                return data.Value.playerData.Count;
            }

            return 0;
        }

        public bool ServerFull => PlayerCount() >= maxPlayers;

        // Adds an extra local player
        public ConnectStatus ConnectLocalPlayer(ulong clientId, int localId, int deviceId)
        {
            if (ServerFull) return ConnectStatus.ServerFull;

            ClientData? clientData = GetClientData(clientId);

            if (clientData.HasValue)
            {
                if (clientData.Value.AddPlayerData(deviceId, 0, localId))
                {
                    OnLocalPlayerConnect?.Invoke(clientId, localId);
                }
            }

            return ConnectStatus.Undefined;
        }

        // Removes a local player. Returns number of local players remaining
        public int DisconnectLocalPlayer(ulong clientId, int localId)
        {
            ClientData? clientData = GetClientData(clientId);

            if (clientData.HasValue)
            {
                clientData.Value.RemovePlayerData(localId);

                OnLocalPlayerDisconnect?.Invoke(clientId, localId);

                return clientData.Value.playerData.Count;
            }

            return 0;
        }

        public void StartGame()
        {
            gameInProgress = true;

            NetworkManager.Singleton.SceneManager.LoadScene("Scene_Main", LoadSceneMode.Single);
        }

        public void ReturnToLobby()
        {
            gameInProgress = false;

            NetworkManager.Singleton.SceneManager.LoadScene("Scene_Lobby", LoadSceneMode.Single);
        }

        private void ClearData()
        {
            clientData.Clear();
            clientGuids.Clear();
        }

        private void ApprovalCheck(byte[] data, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                callback(false, null, true, null, null);
                return;
            }

            string payload = Encoding.UTF8.GetString(data);
            ConnectionPayload connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

            ConnectStatus connectStatus = ConnectStatus.Success;

            if (gameInProgress)
            {
                connectStatus = ConnectStatus.GameInProgress;
            }
            else if (ServerFull)
            {
                connectStatus = ConnectStatus.ServerFull;
            }

            if (connectStatus == ConnectStatus.Success)
            {
                clientGuids[clientId] = connectionPayload.clientGuid;
                clientData[connectionPayload.clientGuid] = new ClientData(clientId, connectionPayload.inputDeviceId, 0);
            }

            if (connectStatus == ConnectStatus.Success)
            {
                bool createPlayerObject = false;
                Vector3 objectPos = Vector3.zero;
                Quaternion objectRot = Quaternion.identity;

                callback(createPlayerObject, null, true, objectPos, objectRot);

                S2C_ConnectResult(clientId, connectStatus);
            }
            else
            {
                // This will be improved in future netcode updates
                S2C_ConnectResult(clientId, connectStatus);
                S2C_SetDisconnectReason(clientId, connectStatus);

                StartCoroutine(WaitToDenyApproval(callback));
            }
        }

        static IEnumerator WaitToDenyApproval(NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            yield return new WaitForSeconds(0.5f);
            connectionApprovedCallback(false, 0, false, null, null);
        }

        private void KickClient(ulong clientId)
        {
            NetworkObject netObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

            if (netObj)
            {
                netObj.Despawn(true);
            }

            NetworkManager.Singleton.DisconnectClient(clientId);
        }

        #region HANDLERS
        private void HandleNetworkReadied()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            gameNetPortal.OnUserDisconnectRequested += HandleUserDisconnectRequested;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            NetworkManager.Singleton.SceneManager.LoadScene("Scene_Lobby", LoadSceneMode.Single);
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (clientGuids.TryGetValue(clientId, out string guid))
            {
                clientGuids.Remove(clientId);
                clientData.Remove(guid);
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                gameNetPortal.OnUserDisconnectRequested -= HandleUserDisconnectRequested;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }

            Debug.Log("Client disconnected from server.");
        }

        private void HandleUserDisconnectRequested()
        {
            // If host, tell all clients they are being disconnected
            if (NetworkManager.Singleton.IsHost)
            {
                S2C_SetDisconnectReasonAllClients(ConnectStatus.HostEndedSession);

                StartCoroutine(WaitToShutdown());
            }

            // Shutdown does not call the disconnect callback for some stupid reason
            HandleClientDisconnect(NetworkManager.Singleton.LocalClientId);

            ClearData();

            SceneManager.LoadScene("Scene_Menu");
        }

        private void HandleServerStarted()
        {
            if (!NetworkManager.Singleton.IsHost) return;

            string clientGuid = Guid.NewGuid().ToString();

            // Register local user as a player
            clientData.Add(clientGuid, new ClientData(NetworkManager.Singleton.LocalClientId, PlayerPrefs.GetInt("LastDeviceId"), 0));
            clientGuids.Add(NetworkManager.Singleton.LocalClientId, clientGuid);
        }
        #endregion

        private IEnumerator WaitToShutdown()
        {
            yield return null;
            NetworkManager.Singleton.Shutdown();
        }

        #region MESSAGE SENDERS
        static void S2C_ConnectResult(ulong netId, ConnectStatus status)
        {
            FastBufferWriter writer = new FastBufferWriter(sizeof(ConnectStatus), Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveS2C_ConnectResult), netId, writer);
        }

        static void S2C_SetDisconnectReason(ulong netId, ConnectStatus status)
        {
            FastBufferWriter writer = new FastBufferWriter(sizeof(ConnectStatus), Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveS2C_SetDisconnectReason), netId, writer);
        }

        static void S2C_SetDisconnectReasonAllClients(ConnectStatus status)
        {
            FastBufferWriter writer = new FastBufferWriter(sizeof(ConnectStatus), Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(nameof(ClientGameNetPortal.ReceiveS2C_SetDisconnectReason), writer);
        }
        #endregion
    }
}
