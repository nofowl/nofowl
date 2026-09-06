using Steamworks;
using UnityEngine;

namespace NoFowl.Integrations
{
    public class SteamHandler : MonoBehaviour
    {
        protected Callback<GameOverlayActivated_t> m_gameOverlayActivated;
        private void OnEnable()
        {
            if (SteamManager.Initialized)
            {
                m_gameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
            }
        }

        private void OnGameOverlayActivated(GameOverlayActivated_t callback)
        {
            if (callback.m_bActive != 0)
            {
                Debug.Log("Steam overlay opened");
            }
            else
            {
                Debug.Log("Steam overlay closed");
            }
        }
    }
}
