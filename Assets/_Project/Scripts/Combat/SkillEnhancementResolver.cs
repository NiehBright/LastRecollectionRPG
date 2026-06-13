using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public class EnhancedSkillResult
    {
        public string enhancementName;
        public string description;
        public float damageMultiplierBonus = 0f; // Hệ số cộng thêm (ví dụ +0.4 = +40% sát thương)
        public List<EffectData> additionalEffects = new List<EffectData>();
        public string specialCondition = ""; // Nhãn nhận diện hiệu ứng đặc biệt
        public float specialValue = 0f;
    }

    public static class SkillEnhancementResolver
    {
        /// <summary>
        /// Giải quyết hiệu ứng cường hóa dựa trên Nguyên tố Chỉ Huy, Nguyên tố Đồng minh, và Loại Kỹ Năng
        /// </summary>
        public static EnhancedSkillResult Resolve(ElementType commander, ElementType ally, SkillType skillType)
        {
            // Chỉ cường hóa Basic (Skill 1) và Special (Skill 2). Không cường hóa Ultimate.
            if (skillType == SkillType.ULTIMATE)
            {
                return null;
            }

            // Thử nạp từ ScriptableObject động được xuất từ Editor Tool
            if (RecollectionManager.Instance != null && RecollectionManager.Instance.activeCommander != null)
            {
                string charName = RecollectionManager.Instance.activeCommander.characterData.characterName;
                RecollectionData dynamicData = Resources.Load<RecollectionData>($"RecollectionData/{charName}_RecollectionData");
                if (dynamicData != null)
                {
                    SkillEnhancement enhancement = dynamicData.GetEnhancement(ally, skillType);
                    if (enhancement != null && !string.IsNullOrEmpty(enhancement.enhancementName))
                    {
                        EnhancedSkillResult res = new EnhancedSkillResult();
                        res.enhancementName = enhancement.enhancementName;
                        res.description = enhancement.description;
                        res.damageMultiplierBonus = enhancement.damageMultiplierBonus;
                        res.additionalEffects = new List<EffectData>(enhancement.additionalEffects);
                        res.specialCondition = enhancement.specialCondition;
                        res.specialValue = enhancement.specialValue;
                        return res;
                    }
                }
            }

            EnhancedSkillResult result = new EnhancedSkillResult();

            switch (commander)
            {
                case ElementType.Fire:
                    ResolveFireCommander(ally, skillType, result);
                    break;
                case ElementType.Ice:
                    ResolveIceCommander(ally, skillType, result);
                    break;
                case ElementType.Lightning:
                    ResolveLightningCommander(ally, skillType, result);
                    break;
                case ElementType.Nature:
                    ResolveNatureCommander(ally, skillType, result);
                    break;
                case ElementType.Physical:
                    ResolvePhysicalCommander(ally, skillType, result);
                    break;
                case ElementType.Ether:
                    ResolveEtherCommander(ally, skillType, result);
                    break;
            }

            if (string.IsNullOrEmpty(result.enhancementName))
            {
                return null; // Không có cường hóa tương thích
            }

            return result;
        }

        #region Fire Commander (Lửa)
        private static void ResolveFireCommander(ElementType ally, SkillType skillType, EnhancedSkillResult res)
        {
            if (ally == ElementType.Ice)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Melt & Burn";
                    res.description = "Tấn công gây thêm hiệu ứng Burn (Lửa thiêu) 2 lượt. Nếu kẻ địch bị Freeze, kích hoạt Bùng nổ Freeze+Burn gây thêm 50% damage và hóa giải đóng băng.";
                    res.specialCondition = "FREEZE_BURN_EXPLOSION";
                    res.additionalEffects.Add(CreateTempEffect("burn_melt", "Burn (Melt)", EffectType.BURN, 0.15f, 2, Color.red));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Steam Burst";
                    res.description = "Đòn đánh diện rộng tạo hơi nước cực đại. Nếu mục tiêu bị Freeze, nổ Steam Burst gây sát thương lửa tương đương 30% HP tối đa của chúng.";
                    res.specialCondition = "STEAM_BURST_AOE";
                    res.specialValue = 0.30f;
                }
            }
            else if (ally == ElementType.Lightning)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Overdrive Ignite";
                    res.description = "Đòn đánh tăng 40% sát thương nếu mục tiêu đang bị trạng thái Burn.";
                    res.specialCondition = "DAMAGE_VS_BURN";
                    res.damageMultiplierBonus = 0.40f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Chain Inferno";
                    res.description = "Lan truyền ngọn lửa tích điện qua 3 mục tiêu. Mỗi mục tiêu nhận thêm hiệu ứng Burn, mục tiêu cuối cùng chịu 1.5x sát thương.";
                    res.specialCondition = "CHAIN_BURN_LAST_150";
                    res.damageMultiplierBonus = 0.50f;
                    res.additionalEffects.Add(CreateTempEffect("burn_chain", "Chain Burn", EffectType.BURN, 0.10f, 2, Color.red));
                }
            }
            else if (ally == ElementType.Nature)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Inferno Poison";
                    res.description = "Độc tố bắt lửa! Áp dụng hiệu ứng Poison và Burn đồng thời. Kẻ địch bị cả hai hiệu ứng sẽ chịu sát thương độc gấp đôi mỗi lượt.";
                    res.specialCondition = "INFERNO_POISON";
                    res.additionalEffects.Add(CreateTempEffect("burn_inf", "Inferno Burn", EffectType.BURN, 0.15f, 2, Color.red));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Flame Barrier";
                    res.description = "Hồi phục kết hợp tạo lá chắn lửa. Hồi máu đồng thời tạo Flare Shield phản lại 15% sát thương nhận vào thành sát thương hoả trong 2 lượt.";
                    res.specialCondition = "FLAME_BARRIER";
                    res.additionalEffects.Add(CreateTempEffect("flame_bar", "Flame Barrier", EffectType.REFLECT, 0.15f, 2, Color.red));
                }
            }
            else if (ally == ElementType.Fire)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Blazing Resonance";
                    res.description = "Tăng 30% sát thương của đòn đánh thường hệ Hỏa.";
                    res.damageMultiplierBonus = 0.30f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Supernova";
                    res.description = "Đòn đánh đặc biệt tăng 40% sát thương và có 50% cơ hội thiêu đốt (Burn) diện rộng.";
                    res.damageMultiplierBonus = 0.40f;
                    res.additionalEffects.Add(CreateTempEffect("burn_supernova", "Supernova Burn", EffectType.BURN, 0.20f, 2, Color.red));
                }
            }
            else if (ally == ElementType.Physical)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Melt Armor";
                    res.description = "Đòn tấn công vật lý giảm 20% giáp (DEF) kẻ địch trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("melt_def", "Melt DEF Down", EffectType.DEF_BUFF, -0.20f, 2, Color.red));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Molten Blade";
                    res.description = "Đòn đánh đặc biệt nhận thêm 35% sát thương và gây hiệu ứng Burn.";
                    res.damageMultiplierBonus = 0.35f;
                    res.additionalEffects.Add(CreateTempEffect("burn_molten", "Molten Burn", EffectType.BURN, 0.15f, 2, Color.red));
                }
            }
            else if (ally == ElementType.Ether)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Solar Flare";
                    res.description = "Gây thêm 40% sát thương hệ Ether bỏ qua phòng ngự.";
                    res.damageMultiplierBonus = 0.40f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Cosmic Blast";
                    res.description = "Đòn đánh đặc biệt gây sát thương cực lớn (+50% dmg) và thiêu đốt diện rộng.";
                    res.damageMultiplierBonus = 0.50f;
                    res.additionalEffects.Add(CreateTempEffect("burn_cosmic", "Cosmic Burn", EffectType.BURN, 0.20f, 2, Color.red));
                }
            }
        }
        #endregion

        #region Ice Commander (Băng)
        private static void ResolveIceCommander(ElementType ally, SkillType skillType, EnhancedSkillResult res)
        {
            if (ally == ElementType.Fire)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Chill & Extinguish";
                    res.description = "Gây hiệu ứng Chill giảm 20% Tốc độ đối phương. Nếu đối phương đang bị Burn, dập tắt lửa để kích nổ hơi nước Steam Burst nổ lan.";
                    res.specialCondition = "CHILL_EXTINGUISH";
                    res.additionalEffects.Add(CreateTempEffect("chill_basic", "Chill Speed Down", EffectType.SPEED_CHANGE, -0.20f, 2, Color.cyan));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Ice Mist";
                    res.description = "Bão lửa đóng băng! Sát thương diện rộng có 40% cơ hội Đóng Băng (Freeze) kẻ địch ngay sau hiệu ứng Burn.";
                    res.specialCondition = "ICE_MIST_FREEZE";
                    res.specialValue = 0.40f;
                }
            }
            else if (ally == ElementType.Lightning)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Cryo Shock";
                    res.description = "Kẻ địch bị đóng băng sẽ mất hoàn toàn kháng Sét, nhận thêm 50% sát thương từ kỹ năng Lightning.";
                    res.specialCondition = "CRYO_SHOCK_LIGHTNING";
                    res.damageMultiplierBonus = 0.50f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Thunder Burst";
                    res.description = "Có 30% cơ hội đóng băng mục tiêu trên mỗi đòn đánh. Nếu mục tiêu đã bị đóng băng sẵn, đòn đánh nổ sét gây gấp đôi sát thương (2.0x DMG).";
                    res.specialCondition = "THUNDER_BURST_FREEZE";
                    res.specialValue = 0.30f;
                }
            }
            else if (ally == ElementType.Nature)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Permafrost Poison";
                    res.description = "Chất độc đông giá! Kẻ địch bị dính cả Poison và Chill sẽ bị khóa khả năng hồi phục máu (HP Regen/Heal = 0).";
                    res.specialCondition = "PERMA_POISON";
                    res.additionalEffects.Add(CreateTempEffect("chill_perma", "Permafrost Speed Down", EffectType.SPEED_CHANGE, -0.15f, 2, Color.cyan));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Ice Armor";
                    res.description = "Hồi máu kèm giáp băng. Ally được hồi máu sẽ nhận Ice Armor giảm 30% sát thương phải chịu và có 50% cơ hội đóng băng kẻ tấn công.";
                    res.specialCondition = "ICE_ARMOR";
                    res.additionalEffects.Add(CreateTempEffect("ice_arm", "Ice Armor Def", EffectType.DEF_BUFF, 0.30f, 2, Color.cyan));
                }
            }
            else if (ally == ElementType.Ice)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Frost Resonance";
                    res.description = "Đòn đánh thường làm giảm 15% Speed kẻ địch trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("chill_res", "Frost Speed Down", EffectType.SPEED_CHANGE, -0.15f, 2, Color.cyan));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Glacial Freeze";
                    res.description = "Đòn đánh đặc biệt đóng băng (Freeze) mục tiêu 1 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("freeze_res", "Glacial Freeze", EffectType.FREEZE, 1f, 1, Color.cyan));
                }
            }
            else if (ally == ElementType.Physical)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Brittle Strike";
                    res.description = "Đòn tấn công vật lý gây thêm 30% sát thương lên kẻ địch bị đóng băng.";
                    res.damageMultiplierBonus = 0.30f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Shattering Impact";
                    res.description = "Đòn đánh đặc biệt có khả năng phá vỡ giáp của quái, giảm 30% DEF trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("shatter_def", "Shatter DEF Down", EffectType.DEF_BUFF, -0.30f, 2, Color.cyan));
                }
            }
            else if (ally == ElementType.Ether)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Absolute Zero";
                    res.description = "Đòn đánh thường gây sát thương cực mạnh (+35% dmg) và giảm 20% Tốc độ kẻ địch.";
                    res.damageMultiplierBonus = 0.35f;
                    res.additionalEffects.Add(CreateTempEffect("zero_speed", "Zero Speed Down", EffectType.SPEED_CHANGE, -0.20f, 2, Color.cyan));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Nebula Freeze";
                    res.description = "Đòn đánh đặc biệt đóng băng toàn bộ kẻ địch có tỷ lệ 35%.";
                    res.specialCondition = "ICE_MIST_FREEZE";
                    res.specialValue = 0.35f;
                }
            }
        }
        #endregion

        #region Lightning Commander (Sét)
        private static void ResolveLightningCommander(ElementType ally, SkillType skillType, EnhancedSkillResult res)
        {
            if (ally == ElementType.Fire)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Electro-Burn";
                    res.description = "Hiệu ứng nổ lửa tích điện. Cuối mỗi lượt, kẻ địch bị thiêu cháy sẽ phát nổ điện tích gây thêm sát thương lôi bằng 50% sát thương Burn.";
                    res.specialCondition = "ELECTRO_BURN";
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Plasma Field";
                    res.description = "Thiết lập vùng Plasma cường hóa. Tăng 15% ATK cả đội và đính kèm sát thương Lightning nhỏ trong 3 lượt.";
                    res.specialCondition = "PLASMA_FIELD";
                    res.additionalEffects.Add(CreateTempEffect("plasma_atk", "Plasma Attack Up", EffectType.ATK_BUFF, 0.15f, 3, Color.yellow));
                }
            }
            else if (ally == ElementType.Ice)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Superconduct";
                    res.description = "Siêu dẫn điện! Đòn đánh giảm 35% phòng ngự (DEF) của kẻ địch trong 2 lượt, và khiến mọi sát thương lôi (Lightning) đánh vào chúng được nhân đôi.";
                    res.specialCondition = "SUPERCONDUCT_DEF_DOWN";
                    res.additionalEffects.Add(CreateTempEffect("super_def", "Superconduct DEF Down", EffectType.DEF_BUFF, -0.35f, 2, Color.yellow));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Lightning Rod";
                    res.description = "Cột thu lôi đóng băng. Mục tiêu bị đóng băng sẽ đóng vai trò làm cột thu lôi, hút toàn bộ kỹ năng đơn mục tiêu hệ sét của đồng minh vào nó.";
                    res.specialCondition = "LIGHTNING_ROD";
                }
            }
            else if (ally == ElementType.Nature)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Static Poison";
                    res.description = "Chất độc tích điện. Kẻ địch bị Poison có 30% cơ hội bị Stun (Choáng) ở đầu mỗi lượt do phóng điện tĩnh.";
                    res.specialCondition = "STATIC_POISON";
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Thunder Aura";
                    res.description = "Hào quang tích điện. Đồng minh nhận hồi máu được kích hoạt Thunder Aura, phản lại 20% sát thương nhận vào bằng sát thương lôi trong 3 lượt.";
                    res.specialCondition = "THUNDER_AURA";
                    res.additionalEffects.Add(CreateTempEffect("thun_aura", "Thunder Aura Reflect", EffectType.REFLECT, 0.20f, 3, Color.yellow));
                }
            }
            else if (ally == ElementType.Lightning)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Volt Resonance";
                    res.description = "Đòn đánh thường tăng 30% sát thương.";
                    res.damageMultiplierBonus = 0.30f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Overload Shock";
                    res.description = "Đòn đánh đặc biệt giật sét lan truyền, tăng 40% sát thương và có 30% cơ hội Stun mục tiêu.";
                    res.damageMultiplierBonus = 0.40f;
                    res.additionalEffects.Add(CreateTempEffect("stun_overload", "Overload Stun", EffectType.STUN, 1f, 1, Color.yellow));
                }
            }
            else if (ally == ElementType.Physical)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Conductive Blade";
                    res.description = "Đòn tấn công vật lý tăng 25% sát thương.";
                    res.damageMultiplierBonus = 0.25f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Thunder Strike";
                    res.description = "Đòn đánh đặc biệt gây choáng (Stun) kẻ địch 1 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("stun_strike", "Thunder Stun", EffectType.STUN, 1f, 1, Color.yellow));
                }
            }
            else if (ally == ElementType.Ether)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Cosmic Lightning";
                    res.description = "Đòn đánh thường tăng 30% sát thương và bỏ qua 25% DEF của mục tiêu.";
                    res.damageMultiplierBonus = 0.30f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Supernova Discharge";
                    res.description = "Đòn đánh đặc biệt tăng 45% sát thương và gây Stun diện rộng (30% cơ hội).";
                    res.damageMultiplierBonus = 0.45f;
                    res.specialCondition = "THUNDER_BURST_FREEZE";
                    res.specialValue = 0.30f;
                }
            }
        }
        #endregion

        #region Nature Commander (Thiên Nhiên)
        private static void ResolveNatureCommander(ElementType ally, SkillType skillType, EnhancedSkillResult res)
        {
            if (ally == ElementType.Fire)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Overgrowth Burn";
                    res.description = "Hút máu sinh trưởng! Khi kẻ địch bị Burn nhận sát thương hoả, hồi lại máu cho người tấn công bằng 30% sát thương Burn gây ra.";
                    res.specialCondition = "OVERGROWTH_LIFESTEAL";
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Blossom Heal";
                    res.description = "Gieo mầm nở hoa. Trong 2 lượt tới, bất kỳ kẻ địch nào bị tiêu diệt khi đang bị Burn sẽ lập tức hồi lại 15% HP cho toàn bộ đồng minh.";
                    res.specialCondition = "BLOSSOM_HEAL_ON_DEATH";
                }
            }
            else if (ally == ElementType.Ice)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Glacial Root";
                    res.description = "Rễ băng gai góc. Tăng thời gian Freeze thêm 1 lượt. Mục tiêu bị đóng băng sẽ mọc gai nhọn tự phản lại 20% sát thương cận chiến khi bị đánh.";
                    res.specialCondition = "GLACIAL_ROOT_FREEZE_EXTEND";
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Spore Cloud";
                    res.description = "Đám mây bào tử băng giá. Khi đánh trúng mục tiêu đang bị đóng băng Freeze, bào tử sẽ bung ra Poison (Độc) tất cả kẻ địch xung quanh.";
                    res.specialCondition = "SPORE_CLOUD_AOE_POISON";
                }
            }
            else if (ally == ElementType.Lightning)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Bio-Corrosion";
                    res.description = "Hóa chất ăn mòn! Sự kết hợp giữa Poison và Lightning tạo ra hiệu ứng Corrosion giảm 25% kháng phòng ngự nguyên tố của kẻ địch.";
                    res.specialCondition = "CORROSION_RESIST_DOWN";
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Living Spore";
                    res.description = "Bào tử sống ký sinh. Găm bào tử lôi thảo vào kẻ địch, sau 2 lượt bào tử sẽ tự nổ gây sát thương diện rộng Nature+Lightning.";
                    res.specialCondition = "LIVING_SPORE_BOMB";
                }
            }
            else if (ally == ElementType.Nature)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Vigor Resonance";
                    res.description = "Đòn đánh thường tăng 20% sát thương và hồi 10% HP cho đồng minh thấp máu nhất.";
                    res.damageMultiplierBonus = 0.20f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Forest Shield";
                    res.description = "Đòn đánh đặc biệt tạo lá chắn hấp thụ sát thương bằng 15% HP tối đa trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("forest_shield", "Forest Shield", EffectType.SHIELD, 0.15f, 2, Color.green));
                }
            }
            else if (ally == ElementType.Physical)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Thorny Blade";
                    res.description = "Đòn tấn công vật lý tăng 20% sát thương và phản lại 20% sát thương cận chiến nhận vào.";
                    res.damageMultiplierBonus = 0.20f;
                    res.additionalEffects.Add(CreateTempEffect("thorn_reflect", "Thorn Reflect", EffectType.REFLECT, 0.20f, 2, Color.green));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Rejuvenating Strike";
                    res.description = "Đòn đánh đặc biệt tăng 30% sát thương và hồi 15% HP cho bản thân.";
                    res.damageMultiplierBonus = 0.30f;
                }
            }
            else if (ally == ElementType.Ether)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Universal Growth";
                    res.description = "Đòn đánh thường tăng 30% sát thương và tăng 15% Tốc độ bản thân trong 2 lượt.";
                    res.damageMultiplierBonus = 0.30f;
                    res.additionalEffects.Add(CreateTempEffect("growth_speed", "Growth Speed Up", EffectType.SPEED_CHANGE, 0.15f, 2, Color.green));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Genesis Beam";
                    res.description = "Đòn đánh đặc biệt tăng 40% sát thương và hồi máu cho cả đội 10% HP.";
                    res.damageMultiplierBonus = 0.40f;
                }
            }
        }
        #endregion

        #region Physical Commander (Vật Lý)
        private static void ResolvePhysicalCommander(ElementType ally, SkillType skillType, EnhancedSkillResult res)
        {
            if (ally == ElementType.Physical)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Iron Will";
                    res.description = "Đòn đánh thường tăng 35% sát thương.";
                    res.damageMultiplierBonus = 0.35f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Shield Breaker";
                    res.description = "Đòn đánh đặc biệt giảm 40% DEF kẻ địch trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("phys_def_down", "Shield Breaker DEF Down", EffectType.DEF_BUFF, -0.40f, 2, Color.gray));
                }
            }
            else if (ally == ElementType.Fire)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Combustion Punch";
                    res.description = "Đòn đánh thường gây thêm hiệu ứng Burn.";
                    res.additionalEffects.Add(CreateTempEffect("phys_burn", "Combustion Burn", EffectType.BURN, 0.15f, 2, Color.red));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Explosive Thrust";
                    res.description = "Đòn đánh đặc biệt tăng 40% sát thương và nổ lan ra quái xung quanh.";
                    res.damageMultiplierBonus = 0.40f;
                }
            }
            else if (ally == ElementType.Ice)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Piercing Ice";
                    res.description = "Đòn đánh thường giảm 15% Speed kẻ địch trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("phys_speed_down", "Piercing Ice Slow", EffectType.SPEED_CHANGE, -0.15f, 2, Color.cyan));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Glacial Armor";
                    res.description = "Đòn đánh đặc biệt tăng 30% DEF cho bản thân.";
                    res.additionalEffects.Add(CreateTempEffect("phys_ice_def", "Glacial Armor DEF Up", EffectType.DEF_BUFF, 0.30f, 2, Color.cyan));
                }
            }
            else if (ally == ElementType.Lightning)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Static Charge";
                    res.description = "Đòn đánh thường tăng 20% Crit Rate.";
                    res.damageMultiplierBonus = 0.20f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "High Voltage";
                    res.description = "Đòn đánh đặc biệt có 40% cơ hội gây Stun mục tiêu.";
                    res.additionalEffects.Add(CreateTempEffect("phys_stun", "High Voltage Stun", EffectType.STUN, 1f, 1, Color.yellow));
                }
            }
            else if (ally == ElementType.Nature)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Leech Seed";
                    res.description = "Đòn đánh thường hút máu bằng 20% sát thương gây ra.";
                    res.specialCondition = "OVERGROWTH_LIFESTEAL";
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Wild Growth";
                    res.description = "Đòn đánh đặc biệt hồi 10% HP cho cả đội.";
                    res.damageMultiplierBonus = 0.20f;
                }
            }
            else if (ally == ElementType.Ether)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Void Slash";
                    res.description = "Đòn đánh thường tăng 30% sát thương hệ Ether.";
                    res.damageMultiplierBonus = 0.30f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Dimensional Strike";
                    res.description = "Đòn đánh đặc biệt tăng 45% sát thương và bỏ qua phòng ngự.";
                    res.damageMultiplierBonus = 0.45f;
                }
            }
        }
        #endregion

        #region Ether Commander (Supreme)
        private static void ResolveEtherCommander(ElementType ally, SkillType skillType, EnhancedSkillResult res)
        {
            if (ally == ElementType.Ether)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Supernal Ether Resonance";
                    res.description = "Đòn đánh thường tăng 40% sát thương và hồi 15% HP.";
                    res.damageMultiplierBonus = 0.40f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Singularity Collapse";
                    res.description = "Đòn đánh đặc biệt tăng 50% sát thương và gây Stun mục tiêu trong 1 lượt.";
                    res.damageMultiplierBonus = 0.50f;
                    res.additionalEffects.Add(CreateTempEffect("ether_stun", "Singularity Stun", EffectType.STUN, 1f, 1, new Color(0.65f, 0.25f, 0.95f)));
                }
            }
            else if (ally == ElementType.Physical)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Etheric Blade";
                    res.description = "Đòn tấn công vật lý nhận thêm 30% sát thương Ether và bỏ qua 20% DEF.";
                    res.damageMultiplierBonus = 0.30f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Aetherial Smash";
                    res.description = "Đòn đánh đặc biệt tăng 45% sát thương và giảm 30% DEF kẻ địch.";
                    res.damageMultiplierBonus = 0.45f;
                    res.additionalEffects.Add(CreateTempEffect("ether_def_down", "Aetherial DEF Down", EffectType.DEF_BUFF, -0.30f, 2, new Color(0.65f, 0.25f, 0.95f)));
                }
            }
            else if (ally == ElementType.Fire)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Cosmic Fire";
                    res.description = "Đòn đánh thường tăng 30% sát thương và gây thiêu đốt nặng (Burn 2 lượt).";
                    res.damageMultiplierBonus = 0.30f;
                    res.additionalEffects.Add(CreateTempEffect("ether_burn", "Cosmic Burn", EffectType.BURN, 0.20f, 2, Color.red));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Nebula Explosion";
                    res.description = "Đòn đánh đặc biệt tăng 45% sát thương và gây Burn diện rộng.";
                    res.damageMultiplierBonus = 0.45f;
                    res.additionalEffects.Add(CreateTempEffect("ether_burn_aoe", "Nebula Burn", EffectType.BURN, 0.15f, 2, Color.red));
                }
            }
            else if (ally == ElementType.Ice)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Cosmic Frost";
                    res.description = "Đòn đánh thường giảm 20% Speed quái vật trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("ether_slow", "Cosmic Speed Down", EffectType.SPEED_CHANGE, -0.20f, 2, Color.cyan));
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Astral Freeze";
                    res.description = "Đòn đánh đặc biệt đóng băng mục tiêu 1 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("ether_freeze", "Astral Freeze", EffectType.FREEZE, 1f, 1, Color.cyan));
                }
            }
            else if (ally == ElementType.Lightning)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Cosmic Spark";
                    res.description = "Đòn đánh thường tăng 15% Crit Rate.";
                    res.damageMultiplierBonus = 0.15f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Galactic Overdrive";
                    res.description = "Đòn đánh đặc biệt tăng 40% sát thương và có 35% cơ hội Stun mục tiêu.";
                    res.damageMultiplierBonus = 0.40f;
                    res.additionalEffects.Add(CreateTempEffect("ether_lightning_stun", "Galactic Stun", EffectType.STUN, 1f, 1, Color.yellow));
                }
            }
            else if (ally == ElementType.Nature)
            {
                if (skillType == SkillType.BASIC)
                {
                    res.enhancementName = "Astral Growth";
                    res.description = "Đòn đánh thường hồi 12% HP cho toàn bộ đồng minh.";
                    res.damageMultiplierBonus = 0.20f;
                }
                else if (skillType == SkillType.SPECIAL)
                {
                    res.enhancementName = "Cosmic Shield";
                    res.description = "Đòn đánh đặc biệt tạo khiên bảo vệ bằng 20% HP tối đa trong 2 lượt.";
                    res.additionalEffects.Add(CreateTempEffect("ether_shield", "Cosmic Shield", EffectType.SHIELD, 0.20f, 2, Color.green));
                }
            }
        }
        #endregion

        private static EffectData CreateTempEffect(string id, string name, EffectType type, float val, int duration, Color color)
        {
            EffectData eff = ScriptableObject.CreateInstance<EffectData>();
            eff.effectId = id;
            eff.effectName = name;
            eff.effectType = type;
            eff.modifierValue = val;
            eff.duration = duration;
            eff.effectColor = color;
            return eff;
        }
    }
}
