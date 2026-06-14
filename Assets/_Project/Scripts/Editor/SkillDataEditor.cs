using UnityEditor;
using UnityEngine;

namespace RPG.Combat
{
    [CustomEditor(typeof(SkillData))]
    public class SkillDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Get properties
            SerializedProperty skillId = serializedObject.FindProperty("skillId");
            SerializedProperty skillName = serializedObject.FindProperty("skillName");
            SerializedProperty description = serializedObject.FindProperty("description");
            SerializedProperty skillType = serializedObject.FindProperty("skillType");
            SerializedProperty rangeType = serializedObject.FindProperty("rangeType");
            SerializedProperty rangedVfxType = serializedObject.FindProperty("rangedVfxType");
            SerializedProperty projectileVFX = serializedObject.FindProperty("projectileVFX");
            SerializedProperty cooldown = serializedObject.FindProperty("cooldown");
            SerializedProperty damageMultiplier = serializedObject.FindProperty("damageMultiplier");
            SerializedProperty targetType = serializedObject.FindProperty("targetType");
            SerializedProperty energyCost = serializedObject.FindProperty("energyCost");
            SerializedProperty energyGenerated = serializedObject.FindProperty("energyGenerated");
            SerializedProperty effects = serializedObject.FindProperty("effects");
            SerializedProperty skillColor = serializedObject.FindProperty("skillColor");
            SerializedProperty animDuration = serializedObject.FindProperty("animDuration");
            SerializedProperty skillClip = serializedObject.FindProperty("skillClip");
            SerializedProperty skillImpactVFX = serializedObject.FindProperty("skillImpactVFX");

            // Draw fields manually to group them exactly as headers in SkillData.cs
            EditorGUILayout.LabelField("Thông tin cơ bản", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skillId, new GUIContent("Mã kỹ năng (ID)"));
            EditorGUILayout.PropertyField(skillName, new GUIContent("Tên kỹ năng"));
            EditorGUILayout.PropertyField(description, new GUIContent("Mô tả chi tiết"));
            EditorGUILayout.PropertyField(skillType, new GUIContent("Loại kỹ năng"));
            EditorGUILayout.PropertyField(rangeType, new GUIContent("Cự ly tấn công"));

            // Conditional drawing for Ranged fields
            if (rangeType != null && rangeType.enumValueIndex == (int)SkillRangeType.RANGED)
            {
                EditorGUI.indentLevel++;
                if (rangedVfxType != null)
                {
                    EditorGUILayout.PropertyField(rangedVfxType, new GUIContent("Kiểu VFX đánh xa"));
                    if (rangedVfxType.enumValueIndex == (int)RangedVfxType.PROJECTILE && projectileVFX != null)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(projectileVFX, new GUIContent("Prefab đạn bay"));
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chỉ số chiến đấu", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cooldown, new GUIContent("Số lượt hồi chiêu (Cooldown)"));
            EditorGUILayout.PropertyField(damageMultiplier, new GUIContent("Hệ số sát thương"));
            EditorGUILayout.PropertyField(targetType, new GUIContent("Mục tiêu nhắm tới"));
            EditorGUILayout.PropertyField(energyCost, new GUIContent("Năng lượng tiêu tốn"));
            EditorGUILayout.PropertyField(energyGenerated, new GUIContent("Năng lượng hồi lại"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hiệu ứng đi kèm", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(effects, new GUIContent("Danh sách hiệu ứng"), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual & Hiệu ứng hình ảnh", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skillColor, new GUIContent("Màu sắc chủ đạo"));
            EditorGUILayout.PropertyField(animDuration, new GUIContent("Thời gian hoạt ảnh (giây)"));
            EditorGUILayout.PropertyField(skillClip, new GUIContent("Clip hoạt ảnh tấn công"));
            EditorGUILayout.PropertyField(skillImpactVFX, new GUIContent("Prefab VFX khi trúng đòn"));

            // Vẽ trường aoeVfxSpawnMode bằng tiếng Việt trực quan
            SerializedProperty aoeVfxSpawnModeProp = serializedObject.FindProperty("aoeVfxSpawnMode");
            if (aoeVfxSpawnModeProp != null)
            {
                string[] displayNames = new string[] {
                    "Xuất hiện dưới chân từng mục tiêu",
                    "Xuất hiện 1 cái ở giữa trung tâm"
                };
                int selectedIndex = aoeVfxSpawnModeProp.enumValueIndex;
                selectedIndex = EditorGUILayout.Popup("Chế độ xuất hiện VFX (AOE)", selectedIndex, displayNames);
                aoeVfxSpawnModeProp.enumValueIndex = selectedIndex;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
