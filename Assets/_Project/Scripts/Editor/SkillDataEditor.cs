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
            EditorGUILayout.PropertyField(skillId);
            EditorGUILayout.PropertyField(skillName);
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(skillType);
            EditorGUILayout.PropertyField(rangeType);

            // Conditional drawing for Ranged fields
            if (rangeType != null && rangeType.enumValueIndex == (int)SkillRangeType.RANGED)
            {
                EditorGUI.indentLevel++;
                if (rangedVfxType != null)
                {
                    EditorGUILayout.PropertyField(rangedVfxType);
                    if (rangedVfxType.enumValueIndex == (int)RangedVfxType.PROJECTILE && projectileVFX != null)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(projectileVFX);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chỉ số chiến đấu", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cooldown);
            EditorGUILayout.PropertyField(damageMultiplier);
            EditorGUILayout.PropertyField(targetType);
            EditorGUILayout.PropertyField(energyCost);
            EditorGUILayout.PropertyField(energyGenerated);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hiệu ứng đi kèm", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(effects, true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual & Hiệu ứng hình ảnh", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skillColor);
            EditorGUILayout.PropertyField(animDuration);
            EditorGUILayout.PropertyField(skillClip);
            EditorGUILayout.PropertyField(skillImpactVFX);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
