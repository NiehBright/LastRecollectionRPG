using UnityEngine;

namespace RPG.Combat
{
    public static class DamageCalculator
    {
        /// <summary>
        /// Tính toán sát thương cuối cùng gây ra từ kẻ tấn công lên mục tiêu.
        /// </summary>
        public static float Calculate(CombatCharacter attacker, CombatCharacter defender, SkillData skill, ElementWeaknessDatabase weaknessDb, out bool isCrit)
        {
            isCrit = false;

            // 1. Lấy ATK của attacker (đã áp dụng buff/debuff)
            float attackerATK = attacker.GetModifiedATK();

            // 2. Base DMG = Attacker ATK × Skill Multiplier
            float baseDMG = attackerATK * skill.damageMultiplier;

            // 3. Element DMG = Base DMG × Element Weakness
            float weaknessMultiplier = 1.0f;
            if (weaknessDb != null)
            {
                weaknessMultiplier = weaknessDb.GetMultiplier(attacker.characterData.element, defender.characterData.element);
            }
            float elementDMG = baseDMG * weaknessMultiplier;

            // 4. Critical DMG = Element DMG × Crit DMG% (nếu chí mạng)
            float critRate = attacker.GetModifiedCritRate();
            float critDMG = attacker.GetModifiedCritDMG();
            
            float critRoll = Random.value;
            float afterCritDMG = elementDMG;
            if (critRoll <= critRate)
            {
                isCrit = true;
                afterCritDMG = elementDMG * critDMG;
            }

            // 5. Defense Reduction = Critical DMG × (1 - Defender DEF%)
            // Định nghĩa Defender DEF% = DEF / (DEF + 300.0f).
            float defenderDEF = defender.GetModifiedDEF();
            float defPercent = defenderDEF / (defenderDEF + 300.0f);
            float afterDefDMG = afterCritDMG * (1.0f - defPercent);

            // 6. Final DMG = Áp dụng Buff/Debuff Modifier & Trạng thái Guard
            float finalDMG = afterDefDMG;

            // Áp dụng giảm 50% nếu mục tiêu đang trong trạng thái phòng thủ (Guard/Defend)
            if (defender.isGuarding)
            {
                finalDMG *= 0.5f;
            }

            // Đảm bảo sát thương tối thiểu là 1
            if (finalDMG < 1.0f)
            {
                finalDMG = 1.0f;
            }

            return Mathf.Round(finalDMG);
        }
    }
}
