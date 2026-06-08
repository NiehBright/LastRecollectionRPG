#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace RPG.Combat
{
    public static class CombatEditorUtility
    {
        public static void FixAllAnimatorControllers()
        {
            string animFolder = "Assets/_Project/Resources/Animators";
            if (!Directory.Exists(animFolder)) return;

            string[] files = Directory.GetFiles(animFolder, "*.controller", SearchOption.AllDirectories);
            bool modifiedAny = false;

            foreach (string file in files)
            {
                string relativePath = file.Replace("\\", "/");
                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(relativePath);
                if (controller == null) continue;

                bool isModified = false;
                UnityEditor.Animations.AnimatorControllerLayer layer = controller.layers[0];
                var stateMachine = layer.stateMachine;

                foreach (var state in stateMachine.states)
                {
                    if (state.state.motion == null)
                    {
                        AnimationClip dummyClip = new AnimationClip();
                        dummyClip.name = "Dummy_" + state.state.name;
                        
                        AssetDatabase.AddObjectToAsset(dummyClip, controller);
                        state.state.motion = dummyClip;
                        isModified = true;
                    }
                }

                if (isModified)
                {
                    EditorUtility.SetDirty(controller);
                    modifiedAny = true;
                    Debug.Log($"[CombatEditorUtility] Tự động cập nhật Animator Controller: {relativePath}");
                }
            }

            if (modifiedAny)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
