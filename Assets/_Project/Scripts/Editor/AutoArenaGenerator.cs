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
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            CombatSetup setup = Object.FindAnyObjectByType<CombatSetup>();
            if (setup != null)
            {
                // Check if already generated to avoid marking scene dirty and messing up hierarchy selection
                GameObject arenaGO = GameObject.Find("CombatArena");
                if (arenaGO != null)
                {
                    Transform allies = arenaGO.transform.Find("SpawnPoints_Allies");
                    Transform enemies = arenaGO.transform.Find("SpawnPoints_Enemies");
                    if (allies != null && allies.childCount >= 4 && enemies != null && enemies.childCount >= 4)
                    {
                        return;
                    }
                }

                setup.GenerateCombatArena();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[AutoArenaGenerator] Đã tự động sinh cấu trúc Combat Arena cho Scene hiện tại!");
        }
    }
}
