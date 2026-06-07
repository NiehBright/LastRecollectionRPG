using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public class RecollectionManager : MonoBehaviour
    {
        public static RecollectionManager Instance { get; private set; }

        [Header("Trạng thái Recollection")]
        public CombatCharacter activeCommander;
        public int turnsRemaining = 0;
        public bool IsRecollectionActive => activeCommander != null && turnsRemaining > 0;

        // Vector lưu vị trí ban đầu của Chỉ huy
        private Vector3 commanderOriginalPos;
        private GameObject spawnedAuraVFX;

        // Lưu vị trí ban đầu của toàn bộ đồng đội trước khi dịch chuyển
        private Dictionary<CombatCharacter, Vector3> initialAllyPositions = new Dictionary<CombatCharacter, Vector3>();

        // Các Sự Kiện
        public event Action<CombatCharacter> OnRecollectionActivated;
        public event Action<int> OnRecollectionTurnSpent;
        public event Action<CombatCharacter> OnRecollectionEnded;

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
        /// Kích hoạt trạng thái Thức Tỉnh Chỉ Huy
        /// </summary>
        public void ActivateRecollection(CombatCharacter commander)
        {
            if (IsRecollectionActive || commander == null || commander.isDead) return;

            activeCommander = commander;
            activeCommander.isCommander = true;
            turnsRemaining = 5;
            commanderOriginalPos = commander.transform.position;

            // Reset Gauge của chỉ huy về 0
            commander.recollectionGauge = 0f;
            commander.UpdateFloatingHUD();

            // Loại bỏ chỉ huy khỏi hàng chờ TurnQueue
            if (CombatManager.Instance != null && CombatManager.Instance.turnQueue != null)
            {
                CombatManager.Instance.turnQueue.RemoveCharacter(commander);
            }

            // Lưu vị trí ban đầu của tất cả đồng minh
            initialAllyPositions.Clear();
            if (CombatManager.Instance != null && CombatManager.Instance.allies != null)
            {
                foreach (var ally in CombatManager.Instance.allies)
                {
                    if (ally != null)
                    {
                        initialAllyPositions[ally] = ally.transform.position;
                    }
                }
            }

            // Di chuyển Chỉ Huy lùi về chính giữa hàng sau (0.8s)
            Vector3 commanderTargetPos = new Vector3(0f, commander.transform.position.y, -7.5f);
            commander.SetOriginalPosition(commanderTargetPos);
            StartCoroutine(CoMoveToPosition(commander, commanderTargetPos, 0.8f));

            // Xác định các đồng minh còn sống khác để sắp xếp hàng trước (tam giác)
            List<CombatCharacter> otherAliveAllies = new List<CombatCharacter>();
            if (CombatManager.Instance != null && CombatManager.Instance.allies != null)
            {
                foreach (var ally in CombatManager.Instance.allies)
                {
                    if (ally != null && ally != commander && !ally.isDead)
                    {
                        otherAliveAllies.Add(ally);
                    }
                }
            }

            // Sắp xếp các đồng minh này theo tọa độ X tăng dần để giữ thứ tự trái/phải
            otherAliveAllies.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            // Lấy các vị trí hàng trước và di chuyển
            Vector3[] frontPositions = GetAllyFrontRowPositions(otherAliveAllies.Count);
            for (int i = 0; i < otherAliveAllies.Count; i++)
            {
                CombatCharacter ally = otherAliveAllies[i];
                Vector3 targetPos = frontPositions[i];
                targetPos.y = ally.transform.position.y; // Giữ nguyên Y

                ally.SetOriginalPosition(targetPos);
                StartCoroutine(CoMoveToPosition(ally, targetPos, 0.8f));
            }

            // Tạo hiệu ứng Aura chân nguyên tố (Procedural)
            SpawnCommanderAura(commander);

            // Nổi chữ RECOLLECTION ACTIVATE!
            FloatingText.Instance.SpawnText(
                commander.transform.position + Vector3.up * 2f, 
                $"{commander.characterData.characterName} RECOLLECT!", 
                CombatManager.Instance.GetElementColor(commander.characterData.element), 
                1.5f
            );

            OnRecollectionActivated?.Invoke(commander);
            UIManager.Instance.UpdatePartyPanel();
            UIManager.Instance.UpdateTurnQueueHUD();
        }

        /// <summary>
        /// Hủy kích hoạt Recollection, đưa tất cả nhân vật về vị trí cũ
        /// </summary>
        public void DeactivateRecollection()
        {
            if (activeCommander == null) return;

            CombatCharacter oldCommander = activeCommander;
            oldCommander.isCommander = false;

            // Đưa trở lại hàng chờ TurnQueue và reset Action Value
            if (CombatManager.Instance != null && CombatManager.Instance.turnQueue != null)
            {
                CombatManager.Instance.turnQueue.AddCharacter(oldCommander);
                CombatManager.Instance.turnQueue.ResetCharacterAV(oldCommander);
            }

            // Khôi phục vị trí của tất cả đồng minh
            foreach (var kvp in initialAllyPositions)
            {
                CombatCharacter ally = kvp.Key;
                Vector3 originalPos = kvp.Value;
                if (ally != null)
                {
                    ally.SetOriginalPosition(originalPos);
                    if (!ally.isDead && ally.gameObject.activeInHierarchy)
                    {
                        StartCoroutine(CoMoveToPosition(ally, originalPos, 0.6f));
                    }
                    else
                    {
                        // Nếu đã chết hoặc inactive thì đặt thẳng vị trí
                        ally.transform.position = originalPos;
                    }
                }
            }
            initialAllyPositions.Clear();

            // Hủy Aura VFX
            if (spawnedAuraVFX != null)
            {
                Destroy(spawnedAuraVFX);
                spawnedAuraVFX = null;
            }

            // Reset biến
            activeCommander = null;
            turnsRemaining = 0;

            OnRecollectionEnded?.Invoke(oldCommander);
            UIManager.Instance.UpdatePartyPanel();
            UIManager.Instance.UpdateTurnQueueHUD();
        }

        /// <summary>
        /// Được gọi cuối mỗi lượt hành động của một nhân vật đồng minh để trừ lượt và áp dụng passive buff
        /// </summary>
        public void ProcessTurnSpent()
        {
            if (!IsRecollectionActive) return;

            turnsRemaining--;
            
            // Áp dụng Passive Buff cho cả đội
            ApplyCommanderPassiveBuff();

            OnRecollectionTurnSpent?.Invoke(turnsRemaining);

            if (turnsRemaining <= 0)
            {
                DeactivateRecollection();
            }
            else
            {
                UIManager.Instance.UpdatePartyPanel();
            }
        }

        private void ApplyCommanderPassiveBuff()
        {
            if (activeCommander == null || CombatManager.Instance == null) return;

            ElementType element = activeCommander.characterData.element;
            List<CombatCharacter> aliveAllies = CombatManager.Instance.GetAliveAllies();
            List<CombatCharacter> aliveEnemies = CombatManager.Instance.GetAliveEnemies();

            // Tính toán hiệu lực dựa trên cơ chế Cộng hưởng Thức tỉnh (Recollection Resonance)
            float resonanceMultiplier = 1.0f;
            int matchingElementCount = 0;
            foreach (var ally in aliveAllies)
            {
                if (ally != activeCommander && ally.characterData.element == element)
                {
                    matchingElementCount++;
                }
            }

            if (matchingElementCount == 1) resonanceMultiplier = 1.25f;
            else if (matchingElementCount >= 2) resonanceMultiplier = 1.50f;

            Debug.Log($"[Recollection] Kích hoạt Passive Buff nguyên tố {element}. Số lượng đồng hệ hàng trước: {matchingElementCount}. Resonance Multiplier: {resonanceMultiplier}x");

            switch (element)
            {
                case ElementType.Fire:
                    // ATK +20% cả đội, tối đa stack 2 lần (40% * multiplier)
                    float fireBuffVal = 0.20f * resonanceMultiplier;
                    EffectData fireBuff = CreateTempEffect("Fire_Passive_Buff", "Fire Resonance", EffectType.ATK_BUFF, fireBuffVal, 2);
                    foreach (var ally in aliveAllies)
                    {
                        if (ally != activeCommander)
                        {
                            EffectManager.Instance.ApplyEffect(activeCommander, ally, fireBuff);
                            FloatingText.Instance.SpawnText(ally.transform.position + Vector3.up * 1.5f, "ATK UP!", Color.red, 1.0f);
                        }
                    }
                    break;

                case ElementType.Ice:
                    // Speed enemy -10%
                    float iceDebuffVal = -0.10f * resonanceMultiplier;
                    EffectData iceDebuff = CreateTempEffect("Ice_Passive_Debuff", "Ice Chill", EffectType.SPEED_CHANGE, iceDebuffVal, 2);
                    foreach (var enemy in aliveEnemies)
                    {
                        EffectManager.Instance.ApplyEffect(activeCommander, enemy, iceDebuff);
                        FloatingText.Instance.SpawnText(enemy.transform.position + Vector3.up * 1.5f, "SLOWED!", Color.cyan, 1.0f);
                    }
                    break;

                case ElementType.Lightning:
                    // CD mọi skill giảm 1 lượt/turn
                    int cdReduction = Mathf.RoundToInt(1 * resonanceMultiplier);
                    foreach (var ally in aliveAllies)
                    {
                        if (ally != activeCommander)
                        {
                            if (ally.specialSkillCDRemaining > 0)
                            {
                                ally.specialSkillCDRemaining = Mathf.Max(0, ally.specialSkillCDRemaining - cdReduction);
                                FloatingText.Instance.SpawnText(ally.transform.position + Vector3.up * 1.5f, "CD -1!", Color.yellow, 1.0f);
                            }
                        }
                    }
                    break;

                case ElementType.Nature:
                    // Hồi 8% HP cả đội
                    float healPercent = 0.08f * resonanceMultiplier;
                    foreach (var ally in aliveAllies)
                    {
                        if (ally != activeCommander)
                        {
                            float healAmount = ally.maxHP * healPercent;
                            ally.Heal(healAmount);
                            FloatingText.Instance.SpawnText(ally.transform.position + Vector3.up * 1.5f, $"+{healAmount:F0} HP", Color.green, 1.0f);
                        }
                    }
                    break;

                case ElementType.Physical:
                    // DEF +25%, phản đòn 10%
                    float physDefVal = 0.25f * resonanceMultiplier;
                    float reflectVal = 0.10f * resonanceMultiplier;
                    EffectData defBuff = CreateTempEffect("Phys_Passive_DEF", "Physical Fortitude", EffectType.DEF_BUFF, physDefVal, 2);
                    EffectData reflectBuff = CreateTempEffect("Phys_Passive_Reflect", "Physical Thorns", EffectType.REFLECT, reflectVal, 2);
                    
                    foreach (var ally in aliveAllies)
                    {
                        if (ally != activeCommander)
                        {
                            EffectManager.Instance.ApplyEffect(activeCommander, ally, defBuff);
                            EffectManager.Instance.ApplyEffect(activeCommander, ally, reflectBuff);
                            FloatingText.Instance.SpawnText(ally.transform.position + Vector3.up * 1.5f, "DEF UP & REFLECT!", Color.gray, 1.0f);
                        }
                    }
                    break;
            }
        }

        private EffectData CreateTempEffect(string id, string name, EffectType type, float val, int duration)
        {
            EffectData eff = ScriptableObject.CreateInstance<EffectData>();
            eff.effectId = id;
            eff.effectName = name;
            eff.effectType = type;
            eff.modifierValue = val;
            eff.duration = duration;
            eff.effectColor = CombatManager.Instance.GetElementColor(activeCommander.characterData.element);
            return eff;
        }

        private IEnumerator CoMoveToPosition(CombatCharacter character, Vector3 targetPos, float duration)
        {
            if (character == null) yield break;
            Vector3 startPos = character.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (character == null) yield break;
                character.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (character != null)
            {
                character.transform.position = targetPos;
            }
        }

        private Vector3[] GetAllyFrontRowPositions(int count)
        {
            float z = -4.5f;
            float zOffset = 0.5f; // Đẩy 2 bên cánh lùi lại một chút
            Vector3[] pos = new Vector3[count];

            if (count == 1)
            {
                pos[0] = new Vector3(0f, 0f, z);
            }
            else if (count == 2)
            {
                pos[0] = new Vector3(-1.5f, 0f, z);
                pos[1] = new Vector3(1.5f, 0f, z);
            }
            else if (count == 3)
            {
                pos[0] = new Vector3(-3.0f, 0f, z - zOffset);
                pos[1] = new Vector3(0f, 0f, z);
                pos[2] = new Vector3(3.0f, 0f, z - zOffset);
            }
            else if (count > 3)
            {
                pos[0] = new Vector3(-4.5f, 0f, z - zOffset);
                pos[1] = new Vector3(-1.5f, 0f, z);
                pos[2] = new Vector3(1.5f, 0f, z);
                pos[3] = new Vector3(4.5f, 0f, z - zOffset);
                for (int i = 4; i < count; i++)
                {
                    pos[i] = new Vector3(1.5f * (i - 1.5f), 0f, z - zOffset * 2);
                }
            }
            return pos;
        }

        private void SpawnCommanderAura(CombatCharacter commander)
        {
            if (spawnedAuraVFX != null) Destroy(spawnedAuraVFX);

            // Tạo một vòng sáng procedural dưới chân Chỉ Huy
            spawnedAuraVFX = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spawnedAuraVFX.name = "Recollection_Aura";
            spawnedAuraVFX.transform.SetParent(commander.transform, false);
            spawnedAuraVFX.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            spawnedAuraVFX.transform.localScale = new Vector3(2.5f, 0.02f, 2.5f);

            Destroy(spawnedAuraVFX.GetComponent<Collider>());

            Renderer rend = spawnedAuraVFX.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            Color color = CombatManager.Instance.GetElementColor(commander.characterData.element);
            color.a = 0.4f; // Trong suốt nhẹ
            
            // Thiết lập vật liệu trong suốt
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;

            rend.material = mat;
        }
    }
}
