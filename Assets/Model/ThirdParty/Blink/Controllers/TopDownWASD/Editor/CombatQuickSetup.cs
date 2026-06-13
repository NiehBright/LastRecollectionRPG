using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

namespace BLINK.Controller.Editor
{
    /// <summary>
    /// One-click setup for Combat System + Input System
    /// </summary>
    public static class CombatQuickSetup
    {
        [MenuItem("BLINK/Combat/Quick Setup - Enable Combat System")]
        public static void QuickSetupCombatSystem()
        {
            var selectedPlayer = Selection.activeGameObject;
            if (selectedPlayer == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select the Player GameObject first!", "OK");
                return;
            }

            // Step 1: Ensure Animator
            var animator = selectedPlayer.GetComponent<Animator>();
            if (animator == null)
            {
                EditorUtility.DisplayDialog("Error", "Player must have Animator component!", "OK");
                return;
            }

            // Step 2: Ensure CharacterController
            var charController = selectedPlayer.GetComponent<CharacterController>();
            if (charController == null)
            {
                EditorUtility.DisplayDialog("Warning", "Player should have CharacterController. Adding now...", "OK");
                selectedPlayer.AddComponent<CharacterController>();
            }

            // Step 3: Add CombatController if missing
            var combatController = selectedPlayer.GetComponent<CombatController>();
            if (combatController == null)
            {
                selectedPlayer.AddComponent<CombatController>();
                EditorUtility.SetDirty(selectedPlayer);
                Debug.Log("[QuickSetup] Added CombatController");
            }

            // Step 4: Ensure GameStartup in scene
            var gameStartup = Object.FindAnyObjectByType<GameStartup>();
            if (gameStartup == null)
            {
                var startupGo = new GameObject("[GameStartup]");
                startupGo.AddComponent<GameStartup>();
                Debug.Log("[QuickSetup] Created GameStartup object");
            }

            // Step 5: Ensure InputManager in scene
            var inputMgr = Object.FindAnyObjectByType<InputManager>();
            if (inputMgr == null)
            {
                var inputGo = new GameObject("[InputManager]");
                inputGo.AddComponent<InputManager>();
                Debug.Log("[QuickSetup] Created InputManager object");
            }

            // Step 6: Ensure PlayerInput
            var playerInput = selectedPlayer.GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = selectedPlayer.AddComponent<PlayerInput>();
                
                // Find InputActionAsset
                var inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                if (inputActionAsset != null)
                {
                    playerInput.actions = inputActionAsset;
                    playerInput.defaultActionMap = "Player";
                    playerInput.defaultControlScheme = "Keyboard&Mouse";
                    EditorUtility.SetDirty(playerInput);
                    Debug.Log("[QuickSetup] Added PlayerInput with InputSystem_Actions");
                }
                else
                {
                    Debug.LogWarning("[QuickSetup] Could not find InputSystem_Actions.inputactions!");
                }
            }

            EditorUtility.DisplayDialog("Setup Complete!", 
                "✅ Combat system components added!\n\n" +
                "Next steps:\n" +
                "1. Run menu: BLINK → Combat → Setup Combat Assets\n" +
                "2. Place EnemyDummy prefab in scene\n" +
                "3. Press Play and test!\n\n" +
                "Debug: Open Console to see input logs",
                "OK");

            Debug.Log("[QuickSetup] ✅ Combat system quick setup complete!");
        }
    }
}

