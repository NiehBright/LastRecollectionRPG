using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "NewRecollectionData", menuName = "RPG/Combat/Recollection Data")]
    public class RecollectionData : ScriptableObject
    {
        public string characterId;
        public ElementType recollectionElement;
        public float gaugeCapacity = 100f;
        public int turnDuration = 5;
        
        [Header("Ma trận Cường hóa (4 nguyên tố x 2 slots = 8 entries)")]
        public List<SkillEnhancement> enhancedSkillMatrix = new List<SkillEnhancement>();

        public SkillEnhancement GetEnhancement(ElementType allyElement, SkillType slot)
        {
            if (enhancedSkillMatrix == null) return null;
            return enhancedSkillMatrix.Find(e => e.allyElement == allyElement && e.skillSlot == slot);
        }
    }
}
