using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoFowl.Integrations
{
    public class DiscordIntegration : MonoBehaviour
    {
        public Discord.Discord discord;

        private void Start()
        {
            try
            {
                discord = new Discord.Discord(855055702888546304, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);
            }
            catch (Discord.ResultException e)
            {
                Debug.LogWarning("Discord is not open: " + e);
            }

            if (discord != null)
            {
                var activityManager = discord.GetActivityManager();
                var activity = new Discord.Activity
                {
                    State = "Getting Stylish",
                    Assets = {
                   LargeImage = "largeicon"
                }
                };

                activityManager.UpdateActivity(activity, (res) =>
                {
                    Debug.Log("Discord result: " + res);
                });
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (discord != null)
                discord.RunCallbacks();
        }

        public void QuitGame()
        {
            if (discord != null)
            {
                discord.GetActivityManager().ClearActivity((res) =>
                {
                    Debug.Log("Discord close result: " + res);
                    Application.Quit();
                });
            }
            else
            {
                Application.Quit();
            }
        }
    }
}

