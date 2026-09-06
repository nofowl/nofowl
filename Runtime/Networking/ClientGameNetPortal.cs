using UnityEngine;
using Unity.Netcode;
using System;
using System.Text;
using UnityEngine.SceneManagement;
using NoFowl.Helpers;

namespace NoFowl.Networking
{
    /// <summary>
    /// Simple class handling client messages and networking.
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : Singleton<ClientGameNetPortal>
    {

        public DisconnectReason DisconnectReason { get; private set; } = new DisconnectReason();

        public event Action<ConnectStatus> OnConnectionFinished;

        public event Action OnNetworkTimedOut;

        private GameNetPortal gameNetPortal;

        private int timeoutDuration = 5;

        #region MONO
        private void Start()
        {
            gameNetPortal = GetComponent<GameNetPortal>();

            gameNetPortal.OnNetworkReadied += HandleNetworkReadied;
            gameNetPortal.OnConnectionFinished += HandleConnectionFinished;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        private void OnDestroy()
        {
            if (gameNetPortal)
            {
                gameNetPortal.OnNetworkReadied -= HandleNetworkReadied;
                gameNetPortal.OnConnectionFinished -= HandleConnectionFinished;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

                if (NetworkManager.Singleton.CustomMessagingManager != null)
                {
                    NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(ReceiveS2C_ConnectResult));
                    NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(ReceiveS2C_SetDisconnectReason));
                }
            }
        }
        #endregion

        public void StartClient()
        {
            string payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                clientGuid = Guid.NewGuid().ToString(),
                inputDeviceId = PlayerPrefs.GetInt("LastDeviceId", -1)
            });

            byte[] payloadData = Encoding.UTF8.GetBytes(payload);

            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadData;
            NetworkManager.Singleton.NetworkConfig.ClientConnectionBufferTimeout = timeoutDuration;

            NetworkManager.Singleton.StartClient();

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveS2C_ConnectResult), ReceiveS2C_ConnectResult);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveS2C_SetDisconnectReason), ReceiveS2C_SetDisconnectReason);
        }

        #region HANDLERS
        private void HandleNetworkReadied()
        {
            gameNetPortal.OnUserDisconnectRequested += HandleUserDisconnectRequested;
        }

        private void HandleUserDisconnectRequested()
        {
            if (NetworkManager.Singleton.IsClient)
            {
                DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);

                if (!NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.Shutdown();

                    // Shutdown does not call the disconnect callback for some stupid reason
                    HandleClientDisconnect(NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        private void HandleConnectionFinished(ConnectStatus status)
        {
            if (status != ConnectStatus.Success)  DisconnectReason.SetDisconnectReason(status);

            OnConnectionFinished?.Invoke(status);
        }

        private void HandleDisconnectReasonReceived(ConnectStatus status)
        {
            DisconnectReason.SetDisconnectReason(status);
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            // Also called on Host when another client dcs. We only want to handle our own dc.
            if (!NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsHost && NetworkManager.Singleton.LocalClientId == clientId)
            {
                // Remove callback
                gameNetPortal.OnUserDisconnectRequested -= HandleUserDisconnectRequested;

                // May want to display someinformation:
                Debug.Log("Disconnected due to: " + DisconnectReason.Reason);

                // On client dc we want to return them to the main menu.
                if (SceneManager.GetActiveScene().name != "Scene_Menu")
                {
                    if (!DisconnectReason.HasTransitionReason) DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);

                    SceneManager.LoadScene("Scene_Menu");
                }
                else
                {
                    OnNetworkTimedOut?.Invoke();
                }

                DisconnectReason.Clear();
            }
        }
        #endregion

        #region MESSAGE HANDLERS
        public static void ReceiveS2C_ConnectResult(ulong clientId, FastBufferReader reader)
        {
            Debug.Log("Connect status message received.");
            reader.ReadValueSafe(out ConnectStatus status);
            Instance.HandleConnectionFinished(status);
        }

        public static void ReceiveS2C_SetDisconnectReason(ulong clientId, FastBufferReader reader)
        {
            Debug.Log("Disconnect reason message received.");
            reader.ReadValueSafe(out ConnectStatus status);
            Instance.HandleDisconnectReasonReceived(status);
        }
        #endregion
    }
}
