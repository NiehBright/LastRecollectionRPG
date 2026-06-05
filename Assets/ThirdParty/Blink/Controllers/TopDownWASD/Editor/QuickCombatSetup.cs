using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Reflection;

namespace Blink.Controllers.TopDownWASD.Editor
{
    public static class QuickCombatSetup
    {
        [MenuItem("BLINK/Combat/Quick Add Combat Layer")]
        public static void QuickAddCombatLayer()
        {
            string controllerPath = "Assets/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller";
            AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (ac == null)
            {
                EditorUtility.DisplayDialog("Error", "Cannot find:\n" + controllerPath, "OK");
                return;
            }

            string baseFolder = "Assets/Blink/Controllers/TopDownWASD/Prefabs";
            if (!AssetDatabase.IsValidFolder(baseFolder))
            {
                AssetDatabase.CreateFolder("Assets/Blink/Controllers/TopDownWASD", "Prefabs");
            }

            try
            {
                // 1. Create animation clips as subassets
                for (int i = 1; i <= 4; i++)
                {
                    string clipPath = baseFolder + "/Attack" + i + ".anim";
                    if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) == null)
                    {
                        AnimationClip clip = new AnimationClip();
                        clip.name = "Attack" + i;
                        var curve = AnimationCurve.EaseInOut(0f, 0f, 0.35f, 0.12f);
                        clip.SetCurve("", typeof(Transform), "localPosition.z", curve);
                        AssetDatabase.CreateAsset(clip, clipPath);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 2. Create Combat layer state machine
                var combatStateMachine = new AnimatorStateMachine { name = "Combat" };
                AssetDatabase.AddObjectToAsset(combatStateMachine, ac);

                AnimatorState idle = combatStateMachine.AddState("Idle");
                
                // Create attacks
                for (int i = 1; i <= 4; i++)
                {
                    string clipPath = baseFolder + "/Attack" + i + ".anim";
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

                    AnimatorState attack = combatStateMachine.AddState("Attack" + i);
                    if (clip != null) attack.motion = clip;

                    // Idle -> Attack
                    var t1 = idle.AddTransition(attack);
                    t1.AddCondition(AnimatorConditionMode.If, 0, "Attack" + i);
                    t1.hasExitTime = false;
                    t1.duration = 0f;

                    // Attack -> Idle
                    var t2 = attack.AddTransition(idle);
                    t2.hasExitTime = true;
                    t2.exitTime = 0.9f;
                    t2.duration = 0.1f;

                    // Combo chain
                    if (i < 4)
                    {
                        AnimatorState nextAttack = combatStateMachine.AddState("Attack" + (i + 1));
                        var combo = attack.AddTransition(nextAttack);
                        combo.AddCondition(AnimatorConditionMode.If, 0, "Attack" + (i + 1));
                        combo.hasExitTime = false;
                        combo.duration = 0f;
                    }
                }

                // 3. Create Combat layer
                var combatLayer = new AnimatorControllerLayer();
                combatLayer.name = "Combat";
                combatLayer.stateMachine = combatStateMachine;
                combatLayer.defaultWeight = 1f;
                combatLayer.syncedLayerIndex = -1;

                // 4. Get current layers
                var layersField = typeof(AnimatorController).GetField("m_AnimatorLayers", BindingFlags.NonPublic | BindingFlags.Instance);
                if (layersField == null)
                {
                    Debug.LogError("Could not find m_AnimatorLayers field!");
                    return;
                }

                var currentLayers = (AnimatorControllerLayer[])layersField.GetValue(ac);
                
                // Check if Combat layer already exists
                bool exists = false;
                foreach (var layer in currentLayers)
                {
                    if (layer.name == "Combat")
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    // Create new array with additional layer
                    var newLayers = new AnimatorControllerLayer[currentLayers.Length + 1];
                    System.Array.Copy(currentLayers, newLayers, currentLayers.Length);
                    newLayers[currentLayers.Length] = combatLayer;
                    
                    // Set the layers back
                    layersField.SetValue(ac, newLayers);
                    Debug.Log("Combat layer added to controller!");
                }
                else
                {
                    Debug.Log("Combat layer already exists!");
                }

                EditorUtility.SetDirty(ac);
                EditorUtility.SetDirty(combatStateMachine);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success!", "✓ Combat layer created!\n✓ Close and reopen Animator window\n✓ Click Attack states to edit", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error: " + ex.Message + "\n" + ex.StackTrace);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }
    }
}

