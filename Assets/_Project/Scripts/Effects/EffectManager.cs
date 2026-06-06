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
                // 1. Kích hoạt hiệu ứng Damage Over Time (DOT) / BURN / POISON nếu có
                if (effect.data.effectType == EffectType.DAMAGE_OVER_TIME ||
                    effect.data.effectType == EffectType.BURN ||
                    effect.data.effectType == EffectType.POISON)
                {
                    float modifier = effect.data.modifierValue;
                    
                    // Xử lý Inferno Poison (gấp đôi sát thương nếu bị cả Poison + Burn)
                    if (effect.data.effectType == EffectType.POISON && 
                        character.activeEffects.Exists(e => e.data.effectType == EffectType.BURN))
                    {
                        modifier *= 2f;
                    }

                    float dotDamage = effect.caster.GetModifiedATK() * modifier;
                    dotDamage = Mathf.Round(dotDamage);

                    ElementType dotElement = ElementType.Physical;
                    Color textColor = Color.gray;
                    string label = "DOT";

                    if (effect.data.effectType == EffectType.BURN)
                    {
                        dotElement = ElementType.Fire;
                        textColor = new Color(1.0f, 0.3f, 0.1f);
                        label = "BURN";
                    }
                    else if (effect.data.effectType == EffectType.POISON)
                    {
                        dotElement = ElementType.Nature;
                        textColor = new Color(0.2f, 0.8f, 0.3f);
                        label = "POISON";
                    }

                    Debug.Log($"[EffectManager] {label} '{effect.data.effectName}' gây {dotDamage} sát thương lên {character.characterData.characterName}.");
                    
                    // Gây sát thương
                    character.TakeDamage(dotDamage, dotElement, effect.caster, false);
                    FloatingText.Instance.SpawnText(character.transform.position + Vector3.up * 1.5f, $"{dotDamage} ({label})", textColor);

                    // Xử lý Electro-Burn cuối lượt hoặc đầu lượt nổ điện sét
                    if (effect.data.effectType == EffectType.BURN && RecollectionManager.Instance != null && RecollectionManager.Instance.IsRecollectionActive)
                    {
                        if (RecollectionManager.Instance.activeCommander.characterData.element == ElementType.Lightning &&
                            effect.caster.characterData.element == ElementType.Fire)
                        {
                            float electroDmg = Mathf.Round(dotDamage * 0.5f);
                            character.TakeDamage(electroDmg, ElementType.Lightning, effect.caster, false);
                            FloatingText.Instance.SpawnText(character.transform.position + Vector3.up * 2.0f, $"{electroDmg} (Electro-Burn!)", Color.yellow);
                        }
                    }
                }

                // 2. Kiểm tra các hiệu ứng hạn chế hành động (Stun/Freeze)
                if (effect.data.effectType == EffectType.FREEZE || effect.data.effectType == EffectType.STUN)
                {
                    isSkippingTurn = true;
                    string statusType = effect.data.effectType == EffectType.FREEZE ? "Đóng băng" : "Choáng";
                    Debug.Log($"[EffectManager] {character.characterData.characterName} đang bị {statusType}, bỏ qua lượt này.");
                }

                // Kiểm tra Stun ngẫu nhiên của Static Poison
                if (effect.data.effectType == EffectType.POISON && RecollectionManager.Instance != null && RecollectionManager.Instance.IsRecollectionActive)
                {
                    if (RecollectionManager.Instance.activeCommander.characterData.element == ElementType.Lightning &&
                        effect.caster.characterData.element == ElementType.Nature)
                    {
                        if (UnityEngine.Random.value < 0.30f)
                        {
                            isSkippingTurn = true;
                            FloatingText.Instance.SpawnText(character.transform.position + Vector3.up * 2.0f, "STATIC SHOCK STUN!", Color.yellow, 1.2f);
                        }
                    }
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
