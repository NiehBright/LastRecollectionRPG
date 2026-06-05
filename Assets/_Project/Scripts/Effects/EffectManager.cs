using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Áp dụng hiệu ứng (Buff/Debuff) lên mục tiêu.
        /// </summary>
        public void ApplyEffect(CombatCharacter caster, CombatCharacter target, EffectData effectData)
        {
            if (target.isDead || effectData == null) return;

            // Lưu Speed cũ để tính toán thay đổi hàng chờ nếu có thay đổi Speed
            float oldSpeed = target.GetModifiedSpeed();

            // Tìm hiệu ứng cùng loại đã có sẵn trên mục tiêu
            ActiveEffect existingEffect = target.activeEffects.Find(e => e.data.effectId == effectData.effectId);

            if (existingEffect != null)
            {
                // Làm mới (Refresh) thời gian kéo dài của hiệu ứng
                existingEffect.turnsRemaining = effectData.duration;
                existingEffect.caster = caster;
                Debug.Log($"[EffectManager] Làm mới hiệu ứng '{effectData.effectName}' trên {target.characterData.characterName} ({effectData.duration} lượt).");
            }
            else
            {
                // Thêm hiệu ứng mới
                ActiveEffect newEffect = new ActiveEffect(effectData, caster);
                target.activeEffects.Add(newEffect);
                Debug.Log($"[EffectManager] Áp dụng '{effectData.effectName}' lên {target.characterData.characterName} ({effectData.duration} lượt).");
            }

            // Cập nhật UI nổi của nhân vật
            target.UpdateFloatingHUD();

            // Kiểm tra thay đổi Speed
            float newSpeed = target.GetModifiedSpeed();
            if (oldSpeed != newSpeed)
            {
                CombatManager.Instance.HandleSpeedChanged(target, oldSpeed, newSpeed);
            }

            // Kích hoạt Event
            CombatManager.Instance.TriggerEffectApplied(target, effectData);
        }

        /// <summary>
        /// Kích hoạt và trừ lượt buff/debuff khi nhân vật BẮT ĐẦU lượt của họ.
        /// Trả về true nếu nhân vật bị Stun hoặc Freeze (không thể hành động).
        /// </summary>
        public bool ProcessEffectsStartTurn(CombatCharacter character)
        {
            if (character.isDead) return false;

            bool isSkippingTurn = false;
            List<ActiveEffect> expiredEffects = new List<ActiveEffect>();

            // Lưu Speed trước khi xử lý (để kiểm tra nếu buff speed hết hạn)
            float oldSpeed = character.GetModifiedSpeed();

            // Duyệt qua tất cả hiệu ứng hiện có
            // Lưu ý: Copy danh sách để tránh lỗi sửa đổi danh sách khi đang lặp
            List<ActiveEffect> activeCopy = new List<ActiveEffect>(character.activeEffects);

            foreach (var effect in activeCopy)
            {
                // 1. Kích hoạt hiệu ứng Damage Over Time (DOT) nếu có
                if (effect.data.effectType == EffectType.DAMAGE_OVER_TIME)
                {
                    float dotDamage = effect.caster.GetModifiedATK() * effect.data.modifierValue;
                    dotDamage = Mathf.Round(dotDamage);
                    
                    Debug.Log($"[EffectManager] DOT '{effect.data.effectName}' gây {dotDamage} sát thương lên {character.characterData.characterName}.");
                    
                    // Gây sát thương không chí mạng
                    character.TakeDamage(dotDamage, ElementType.Physical, false);
                }

                // 2. Kiểm tra các hiệu ứng hạn chế hành động (Stun/Freeze)
                if (effect.data.effectType == EffectType.FREEZE || effect.data.effectType == EffectType.STUN)
                {
                    isSkippingTurn = true;
                    string statusType = effect.data.effectType == EffectType.FREEZE ? "Đóng băng" : "Choáng";
                    Debug.Log($"[EffectManager] {character.characterData.characterName} đang bị {statusType}, bỏ qua lượt này.");
                }

                // 3. Trừ số lượt hoạt động còn lại
                effect.turnsRemaining--;
                if (effect.turnsRemaining <= 0)
                {
                    expiredEffects.Add(effect);
                }
            }

            // Xóa các hiệu ứng hết hạn
            foreach (var expired in expiredEffects)
            {
                character.activeEffects.Remove(expired);
                Debug.Log($"[EffectManager] Hiệu ứng '{expired.data.effectName}' trên {character.characterData.characterName} đã hết hạn.");
            }

            character.UpdateFloatingHUD();

            // Kiểm tra thay đổi Speed sau khi hiệu ứng hết hạn
            float newSpeed = character.GetModifiedSpeed();
            if (oldSpeed != newSpeed)
            {
                CombatManager.Instance.HandleSpeedChanged(character, oldSpeed, newSpeed);
            }

            return isSkippingTurn;
        }
    }
}
