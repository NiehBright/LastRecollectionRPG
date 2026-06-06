using UnityEngine;
using UnityEngine.SceneManagement;

namespace RPG.Combat
{
    public static class OverworldTesterSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "DEMO_WASD")
            {
                // Tìm tất cả EnemyDummy trong scene hiện tại
#if UNITY_2023_1_OR_NEWER
                BLINK.Controller.EnemyDummy[] dummies = Object.FindObjectsByType<BLINK.Controller.EnemyDummy>(FindObjectsSortMode.None);
#else
                BLINK.Controller.EnemyDummy[] dummies = (BLINK.Controller.EnemyDummy[])Object.FindObjectsOfType(typeof(BLINK.Controller.EnemyDummy));
#endif

                Debug.Log($"[OverworldTesterSetup] Tìm thấy {dummies.Length} EnemyDummy trong scene. Tiến hành thiết lập OverworldMonster...");

                foreach (var dummy in dummies)
                {
                    if (dummy == null) continue;

                    // Tạo unique ID dựa trên tên và vị trí ban đầu của quái vật để đảm bảo duy nhất và nhất quán
                    string uid = $"dummy_{dummy.name}_{Mathf.RoundToInt(dummy.transform.position.x)}_{Mathf.RoundToInt(dummy.transform.position.y)}_{Mathf.RoundToInt(dummy.transform.position.z)}";

                    // Kiểm tra xem quái vật này đã bị tiêu diệt ở lượt trước chưa
                    if (CombatTeamManager.IsEnteringFromOverworld && 
                        !string.IsNullOrEmpty(CombatTeamManager.MonsterToDestroyId) && 
                        uid == CombatTeamManager.MonsterToDestroyId && 
                        CombatTeamManager.CombatResult == CombatResultType.WIN)
                    {
                        Debug.Log($"[OverworldTesterSetup] Quái vật '{uid}' đã bị tiêu diệt ở trận trước. Tiến hành tiêu hủy.");
                        Object.Destroy(dummy.gameObject);
                        continue;
                    }

                    // Gắn OverworldMonster nếu chưa có
                    OverworldMonster om = dummy.GetComponent<OverworldMonster>();
                    if (om == null)
                    {
                        om = dummy.gameObject.AddComponent<OverworldMonster>();
                    }
                    om.uniqueId = uid;
                    om.detectionRadius = 2.2f;
                }
            }
        }
    }
}
