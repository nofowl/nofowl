using Unity.Netcode;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NoFowl.Helpers;

namespace NoFowl.Networking
{
    public enum ConnectStatus
    {
        Undefined,
        Success,
        ServerFull,
        GameInProgress,
        LoggedInAgain,
        UserRequestedDisconnect,
        HostEndedSession,
        GenericDisconnect
    }

    [Serializable]
    public class ConnectionPayload
    {
        public string clientGuid;
        public int inputDeviceId;
    }

    public class DisconnectReason
    {
        public ConnectStatus Reason { get; private set; } = ConnectStatus.Undefined;

        public void SetDisconnectReason(ConnectStatus reason) { Debug.Log("DC Reason: " + reason); Reason = reason; }

        public void Clear() { Reason = ConnectStatus.Undefined; }

        public bool HasTransitionReason => Reason != ConnectStatus.Undefined;
    }

    /// <summary>
    /// Simple class managing non-specific networking.
    /// </summary>

    public class GameNetPortal : Singleton<GameNetPortal>
    {
        public event Action OnNetworkReadied;

        public event Action<ConnectStatus> OnConnectionFinished;

        public event Action<ulong, int> OnClientSceneChanged;

        public event Action OnUserDisconnectRequested;

        #region MONO

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += HandleNetworkReady;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

            SceneManager.LoadScene("Scene_Menu");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= HandleNetworkReady;
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;

                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
                }
            }
        }
        #endregion

        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        public void RequestDisconnect()
        {
            OnUserDisconnectRequested?.Invoke();
        }

        #region HANDLE METHODS
        private void HandleClientConnected(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;

            if (!NetworkManager.Singleton.IsHost) HandleNetworkReady();

            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        }

        private void HandleSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete) return;

            OnClientSceneChanged?.Invoke(sceneEvent.ClientId, SceneManager.GetSceneByName(sceneEvent.SceneName).buildIndex);
        }

        /// <summary>
        /// Called at a similar time to NetworkBehavior::OnNetworkSpawn
        /// </summary>
        private void HandleNetworkReady()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                OnConnectionFinished?.Invoke(ConnectStatus.Success);
            }

            OnNetworkReadied?.Invoke();
        }
        #endregion
    }
}
