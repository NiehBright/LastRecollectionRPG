using UnityEngine;
using UnityEditor;

namespace RPG.Combat
{
    public class CombatTeamSelectionUIBuilder : Editor
    {
        [MenuItem("Tools/Build Team Selection Prefab")]
        public static void BuildPrefab()
        {
            // Xác định thư mục tài nguyên phù hợp với cấu trúc project
            string basePath = "Assets";
            if (AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                basePath = "Assets/_Project";
            }

            string resourcesFolder = $"{basePath}/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesFolder))
            {
                AssetDatabase.CreateFolder(basePath, "Resources");
            }

            string prefabsFolder = $"{resourcesFolder}/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsFolder))
            {
                AssetDatabase.CreateFolder(resourcesFolder, "Prefabs");
            }

            GameObject root = new GameObject("CombatTeamSelectionUI_Builder");
            var selectionUI = root.AddComponent<CombatTeamSelectionUI>();
            
            selectionUI.EnsureEventSystem();
            selectionUI.CreateUIElements();
            
            var canvas = selectionUI.canvasGO;
            if (canvas != null)
            {
                canvas.name = "CombatTeamSelectionUI";
                string prefabPath = $"{prefabsFolder}/CombatTeamSelectionUI.prefab";
                PrefabUtility.SaveAsPrefabAsset(canvas, prefabPath);
                Debug.Log($"[CombatTeamSelectionUI] Đã xuất Prefab UI thành công tại: {prefabPath}");
                DestroyImmediate(canvas);
            }
            
            DestroyImmediate(root);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Thành công", $"Đã build thành công Prefab chọn nhân vật tại:\n{prefabsFolder}/CombatTeamSelectionUI.prefab\n\nBạn có thể kéo prefab này vào scene để chỉnh sửa giao diện theo ý muốn!", "OK");
        }
    }
}
