using UnityEngine;
using UnityEngine.SceneManagement;

namespace BLINK.Controller
{
    /// <summary>
    /// Initializes the InputManager on scene load.
    /// Place this script on a GameObject in the first scene or use a bootstrap scene.
    /// </summary>
    public class GameStartup : MonoBehaviour
    {
        private void Awake()
        {
            // Ensure InputManager exists and is initialized
            var inputMgr = FindObjectOfType<InputManager>();
            if (inputMgr == null)
            {
                var go = new GameObject("[InputManager]");
                go.AddComponent<InputManager>();
                DontDestroyOnLoad(go);
            }

            Debug.Log("[GameStartup] Game initialized. InputManager created/found.");
        }
    }
}

