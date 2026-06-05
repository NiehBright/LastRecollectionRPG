using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "NewEffectData", menuName = "RPG/Combat/Effect Data")]
    public class EffectData : ScriptableObject
    {
        [Header("Thông tin chung")]
        public string effectId;
        public string effectName;
        [TextArea(2, 5)]
        public string description;
        public Sprite icon;
        public Color effectColor = Color.white;

        [Header("Cơ chế hoạt động")]
        public EffectType effectType;
        public int duration; // Tính bằng lượt
        public float modifierValue; // Hệ số tác dụng (ví dụ: 0.3 = +30% ATK, -0.2 = -20% Speed, 0.3 = 30% ATK damage DOT)
    }
}
