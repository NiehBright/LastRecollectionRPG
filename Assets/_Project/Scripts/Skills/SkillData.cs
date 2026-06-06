using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "RPG/Combat/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        public string skillId;
        public string skillName;
        [TextArea(3, 10)]
        public string description;
        public SkillType skillType;

        [Header("Chỉ số chiến đấu")]
        public int cooldown; // Số lượt cooldown
        public float damageMultiplier; // Hệ số nhân sát thương (ATK * multiplier)
        public TargetType targetType;
        public float energyCost; // Thường là 0 cho Basic/Special, hoặc 100 cho Ultimate
        public float energyGenerated; // Lượng năng lượng hồi lại khi thi triển (5-15%)

        [Header("Hiệu ứng đi kèm")]
        public List<EffectData> effects = new List<EffectData>();

        [Header("Visual & Hiệu ứng hình ảnh")]
        public Color skillColor = Color.white;
        public float animDuration = 1.0f; // Thời gian chuyển động tấn công
        public AnimationClip skillClip; // Clip hoạt ảnh tương ứng với kỹ năng này
        public GameObject skillImpactVFX; // Hiệu ứng va chạm/đòn đánh khi trúng mục tiêu
    }
}
