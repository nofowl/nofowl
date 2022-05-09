using System.Collections.Generic;
using UnityEngine;

namespace NoFowl.Networking
{
    /// <summary>
    /// Struct representing a client with multiple local players.
    /// </summary>
    public struct ClientData
    {
        public Dictionary<int, PlayerData> playerData;
        public ulong clientId;

        public ClientData(ulong clientId, int deviceId, int colorId)
        {
            this.clientId = clientId;

            playerData = new Dictionary<int, PlayerData>();
            playerData.Add(0, new PlayerData(deviceId, colorId, 0));
        }

        public bool AddPlayerData(int deviceId, int colorId, int localId)
        {
            // Prevent adding the same local player multiple times - this also saves us
            if (!playerData.ContainsKey(localId))
            {
                playerData.Add(localId, new PlayerData(deviceId, colorId, localId));
                return true;
            }

            return false;
        }

        public void RemovePlayerData(int localId)
        {
            if (playerData.ContainsKey(localId))
            {
                playerData.Remove(localId);
            }
        }

        public bool HasPlayers()
        {
            return playerData.Count > 0;
        }

        public void SetPlayerColor(int localId, int colorId)
        {
            if (playerData.ContainsKey(localId))
            {
                PlayerData pd = playerData[localId];
                playerData[localId] = new PlayerData(pd.deviceId, colorId, pd.localId);
            }
        }
    }

    public struct PlayerData
    {
        // Representing data for a player
        public int deviceId;
        public int colorId;
        public int localId;

        public PlayerData(int deviceId, int colorId, int localId)
        {
            this.deviceId = deviceId;
            this.colorId = colorId;
            this.localId = localId;
        }
    }
}

