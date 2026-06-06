using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    [System.Serializable]
    public class SkillEnhancement
    {
        public ElementType allyElement;
        public SkillType skillSlot; // Chỉ sử dụng BASIC hoặc SPECIAL
        public string enhancementName;
        [TextArea(2, 4)]
        public string description;
        public float damageMultiplierBonus; // ví dụ: 0.4f đại diện cho +40% DMG
        public List<EffectData> additionalEffects = new List<EffectData>();
        public string specialCondition; // ví dụ: "FREEZE_BURN_EXPLOSION"
        public float specialValue;
    }
}
