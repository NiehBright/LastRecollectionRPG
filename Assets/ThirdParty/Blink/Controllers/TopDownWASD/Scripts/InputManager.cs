using UnityEngine;
using UnityEngine.InputSystem;

namespace BLINK.Controller
{
    /// <summary>
    /// Manages Input System setup for the game.
    /// Ensures InputActionAsset is enabled on startup.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        private static InputManager _instance;
        private InputActionAsset _inputActionAsset;
        private InputActionMap _playerActionMap;

        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Try to find InputActionAsset in scene via PlayerInput
            var playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput != null)
            {
                _inputActionAsset = playerInput.actions;
                Debug.Log("[InputManager] Found InputActionAsset from PlayerInput in scene");
            }

            if (_inputActionAsset != null)
            {
                _playerActionMap = _inputActionAsset.FindActionMap("Player");
                if (_playerActionMap != null && !_playerActionMap.enabled)
                {
                    _playerActionMap.Enable();
                    Debug.Log("[InputManager] Player action map enabled");
                }
                else if (_playerActionMap == null)
                {
                    Debug.LogWarning("[InputManager] 'Player' action map not found in InputActionAsset");
                }
            }
            else
            {
                Debug.LogWarning("[InputManager] InputActionAsset not found. Combat input may not work.");
            }
        }

        private void OnDestroy()
        {
            if (_playerActionMap != null && _playerActionMap.enabled)
            {
                _playerActionMap.Disable();
            }
        }

        /// <summary>
        /// Get a specific action from the Player action map.
        /// </summary>
        public static InputAction GetAction(string actionName)
        {
            if (_instance == null || _instance._playerActionMap == null)
                return null;

            return _instance._playerActionMap.FindAction(actionName);
        }
    }
}
