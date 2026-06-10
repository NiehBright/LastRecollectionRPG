using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RPG.Combat
{
    [InitializeOnLoad]
    public static class AutoArenaGenerator
    {
        static AutoArenaGenerator()
        {
            EditorApplication.delayCall += Generate;
        }

        private static void Generate()
        {
            CombatSetup setup = Object.FindFirstObjectByType<CombatSetup>();
            if (setup != null)
            {
                setup.GenerateCombatArena();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[AutoArenaGenerator] Đã tự động sinh cấu trúc Combat Arena cho Scene hiện tại!");
            }
            else
            {
                Debug.LogWarning("[AutoArenaGenerator] Không tìm thấy CombatSetup component trong Scene hiện tại.");
            }
        }
    }
}
