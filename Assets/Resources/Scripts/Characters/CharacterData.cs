using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "RPG/Combat/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Thông tin chung")]
        public string characterId;
        public string characterName;
        public ElementType element;
        public Sprite avatar;
        public Color themeColor = Color.white; // Màu đại diện cho mô hình 3D procedural

        [Header("Chỉ số cơ bản")]
        public float baseMaxHP = 500f;
        public float baseATK = 100f;
        public float baseDEF = 50f;
        public float baseSpeed = 100f;
        
        [Range(0f, 1f)]
        public float baseCritRate = 0.05f; // Ví dụ: 0.1 = 10%
        
        [Range(1f, 5f)]
        public float baseCritDMG = 1.50f; // Ví dụ: 1.5 = 150%

        [Header("Kỹ năng")]
        public SkillData skillBasic;
        public SkillData skillSpecial;
        public SkillData skillUltimate;
    }
}
