using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoFowl.Helpers
{
    /// <summary>
    /// Simple class that handles anything needed for singletons.
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        public static T Instance => instance;

        void Awake()
        {
            if (instance != null && instance.GetInstanceID() != GetInstanceID())
            {
                Destroy(gameObject);
                return;
            }

            instance = gameObject.GetComponent<T>();
            DontDestroyOnLoad(gameObject);
        }
    }
}

