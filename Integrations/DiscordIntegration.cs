using Discord.Sdk;
using NoFowl.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoFowl.Integrations
{
    public class DiscordIntegration : Singleton<DiscordIntegration>
    {
        [SerializeField]
        private ulong clientId; // Set this in the Unity Inspector from the dev portal

        private Client client;

        private Activity activity;
        private ActivityTimestamps timestamps;

        void Start()
        {
            client = new Client();

            // Modifying LoggingSeverity will show you more or less logging information
            client.AddLogCallback(OnLog, LoggingSeverity.Error);
            client.SetStatusChangedCallback(OnStatusChanged);
            client.SetApplicationId(clientId);

            activity = new Activity();
            timestamps= new ActivityTimestamps();
            activity.SetStatusDisplayType(StatusDisplayTypes.Name);
            activity.SetType(ActivityTypes.Playing);
            activity.SetName("Tactris");
            SetDetails("In The Menu");
            UpdatePresence();
        }

        private void OnDestroy()
        {
            client.ClearRichPresence();
        }

        private void OnLog(string message, LoggingSeverity severity)
        {
            Debug.Log($"Log: {severity} - {message}");
        }

        private void OnStatusChanged(Client.Status status, Client.Error error, int errorCode)
        {
            Debug.Log($"Status changed: {status}");
            if (error != Client.Error.None)
            {
                Debug.LogError($"Error: {error}, code: {errorCode}");
            }
        }

        public void SetDetails(string details)
        {
            activity.SetDetails(details);
        }

        public void SetState(string state)
        {
            activity.SetState(state);
        }

        public void ResetTimestamp()
        {
            timestamps.SetStart((ulong)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            activity.SetTimestamps(timestamps);
        }

        public void ClearTimestamp()
        {
            activity.SetTimestamps(null);
        }

        public void UpdatePresence()
        {
            client.UpdateRichPresence(activity, DiscordPresenceChanged);
        }

        private void DiscordPresenceChanged(ClientResult result)
        {
            if (result.Successful())
            {
                Debug.Log("Rich presence updated!");
            }
            else
            {
                Debug.LogError("Failed to update rich presence");
            }
        }
    }
}

