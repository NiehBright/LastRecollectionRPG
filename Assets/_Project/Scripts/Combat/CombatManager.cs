using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public enum CombatState
    {
        START,
        PLAYERTURN,
        ENEMYTURN,
        BUSY,
        WIN,
        LOSE
    }

    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Trạng thái")]
        public CombatState currentState;

        [Header("Danh sách nhân vật")]
        public List<CombatCharacter> allies = new List<CombatCharacter>();
        public List<CombatCharacter> enemies = new List<CombatCharacter>();

        [Header("Hàng chờ lượt đi")]
        public TurnQueue turnQueue = new TurnQueue();
        public CombatCharacter activeCharacter;

        [Header("Cơ sở dữ liệu tương khắc")]
        public ElementWeaknessDatabase weaknessDatabase;

        [Header("Hàng chờ Chiêu cuối (Ultimate)")]
        private Queue<CombatCharacter> queuedUltimates = new Queue<CombatCharacter>();
        private bool isExecutingUltimateInterrupt = false;

        // Bộ đếm lượt bị động để cộng 10% năng lượng
        private Dictionary<CombatCharacter, int> passiveTurnCounters = new Dictionary<CombatCharacter, int>();

        // Events
        public event Action OnCombatStarted;
        public event Action<CombatCharacter> OnTurnStart;
        public event Action<CombatCharacter, SkillData, List<CombatCharacter>> OnSkillCast;
        public event Action<CombatCharacter, float, bool> OnDamageDealt;
        public event Action<CombatCharacter, EffectData> OnEffectApplied;
        public event Action<CombatCharacter> OnCharacterDeath;
        public event Action<bool> OnCombatEnd; // true = Win, false = Lose

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
        /// Bắt đầu trận đấu mới.
        /// </summary>
        public void StartCombat(List<CombatCharacter> combatAllies, List<CombatCharacter> combatEnemies, ElementWeaknessDatabase db)
        {
            allies = combatAllies;
            enemies = combatEnemies;
            weaknessDatabase = db;
            currentState = CombatState.START;

            turnQueue.Clear();
            queuedUltimates.Clear();
            passiveTurnCounters.Clear();
            isExecutingUltimateInterrupt = false;

            // Khởi tạo hàng chờ và các sự kiện của nhân vật
            List<CombatCharacter> allChars = new List<CombatCharacter>();
            allChars.AddRange(allies);
            allChars.AddRange(enemies);

            foreach (var character in allChars)
            {
                turnQueue.AddCharacter(character);
                passiveTurnCounters[character] = 0;

                // Lắng nghe sự kiện chết của nhân vật
                character.OnDeath += HandleCharacterDeath;
            }

            OnCombatStarted?.Invoke();
            Debug.Log("[CombatManager] Trận đấu bắt đầu! Đã nạp " + allChars.Count + " nhân vật vào TurnQueue.");

            // Bắt đầu lượt đi đầu tiên
            StartCoroutine(CoNextTurnDelay());
        }

        private IEnumerator CoNextTurnDelay()
        {
            yield return new WaitForSeconds(1.0f);
            NextTurn();
        }

        /// <summary>
        /// Chuyển sang lượt hành động tiếp theo.
        /// </summary>
        private void NextTurn()
        {
            // 1. Kiểm tra điều kiện kết thúc trận đấu trước
            if (CheckCombatEnd()) return;

            // 2. Kiểm tra nếu có Chiêu cuối (Ultimate) đang được xếp hàng chờ cắt lượt
            if (queuedUltimates.Count > 0)
            {
                StartCoroutine(CoExecuteQueuedUltimate());
                return;
            }

            currentState = CombatState.BUSY;

            // 3. Lấy nhân vật tiếp theo từ hàng chờ
            float elapsedAV;
            activeCharacter = turnQueue.GetNextCharacter(out elapsedAV);

            if (activeCharacter == null)
            {
                Debug.LogError("[CombatManager] Không tìm thấy nhân vật tiếp theo hành động!");
                return;
            }

            // Cập nhật Turn Indicator trên đầu nhân vật
            foreach (var ally in allies) ally.ShowTurnIndicator(false);
            foreach (var enemy in enemies) enemy.ShowTurnIndicator(false);
            activeCharacter.ShowTurnIndicator(true);

            Debug.Log($"[CombatManager] Lượt của: {activeCharacter.characterData.characterName} (AV trôi qua: {elapsedAV:F1})");
            OnTurnStart?.Invoke(activeCharacter);

            // Cộng dồn lượt bị động cho tất cả các nhân vật khác
            IncrementPassiveTurnsForOthers(activeCharacter);

            // Trừ Cooldown kỹ năng và xóa Guard
            activeCharacter.StartTurnCDDecrement();

            // 4. Xử lý các hiệu ứng đầu lượt (Dot, Stun, Freeze)
            bool isSkipped = EffectManager.Instance.ProcessEffectsStartTurn(activeCharacter);
            
            if (activeCharacter.isDead)
            {
                // Nếu chết do DOT đầu lượt
                turnQueue.RemoveCharacter(activeCharacter);
                NextTurn();
                return;
            }

            if (isSkipped)
            {
                // Tạo chữ số trạng thái nhảy lên trên đầu
                FloatingText.Instance.SpawnText(
                    activeCharacter.transform.position + Vector3.up * 2f, 
                    activeCharacter.IsFrozen() ? "BỊ ĐÓNG BĂNG!" : "BỊ CHOÁNG!", 
                    Color.cyan, 
                    1.2f
                );

                // Reset Action Value của nhân vật này và chuyển lượt
                turnQueue.ResetCharacterAV(activeCharacter);
                StartCoroutine(CoNextTurnDelay());
                return;
            }

            // 5. Vào pha hành động chính
            if (activeCharacter.isAlly)
            {
                currentState = CombatState.PLAYERTURN;
                // Báo UIManager hiển thị bảng kỹ năng cho người chơi chọn
                UIManager.Instance.ShowActionPanel(activeCharacter);
            }
            else
            {
                currentState = CombatState.ENEMYTURN;
                // AI kẻ địch tự hành động
                StartCoroutine(CoEnemyTurnAI());
            }
        }

        /// <summary>
        /// Cộng lượt bị động cho các nhân vật không hành động. Đạt 3 lượt thì +10% Năng lượng.
        /// </summary>
        private void IncrementPassiveTurnsForOthers(CombatCharacter currentActive)
        {
            List<CombatCharacter> allAlive = GetAliveCharacters();
            foreach (var character in allAlive)
            {
                if (character != currentActive)
                {
                    passiveTurnCounters[character]++;
                    if (passiveTurnCounters[character] >= 3)
                    {
                        character.AddEnergy(10f); // Cộng 10% năng lượng
                        passiveTurnCounters[character] = 0;
                        Debug.Log($"[CombatManager] {character.characterData.characterName} nhận 10% Energy từ lượt bị động.");
                    }
                }
            }
        }

        #region Kẻ địch AI

        private IEnumerator CoEnemyTurnAI()
        {
            yield return new WaitForSeconds(1.0f); // Tạo độ trễ tự nhiên cho AI suy nghĩ

            if (activeCharacter == null || activeCharacter.isDead)
            {
                NextTurn();
                yield break;
            }

            // AI chọn kỹ năng:
            // Nếu kỹ năng đặc biệt (Special) đã hồi chiêu, có 50% dùng Special, ngược lại dùng Basic Attack.
            SkillData chosenSkill = activeCharacter.characterData.skillBasic;
            if (activeCharacter.specialSkillCDRemaining <= 0 && activeCharacter.characterData.skillSpecial != null)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    chosenSkill = activeCharacter.characterData.skillSpecial;
                }
            }

            // AI chọn mục tiêu:
            List<CombatCharacter> targets = SelectAITargets(activeCharacter, chosenSkill);

            if (targets.Count > 0)
            {
                ExecuteAction(activeCharacter, chosenSkill, targets);
            }
            else
            {
                // Nếu không có mục tiêu hợp lệ, chuyển sang Guard
                ExecuteGuard(activeCharacter);
            }
        }

        private List<CombatCharacter> SelectAITargets(CombatCharacter attacker, SkillData skill)
        {
            List<CombatCharacter> targets = new List<CombatCharacter>();

            switch (skill.targetType)
            {
                case TargetType.SINGLE:
                    // Tấn công đơn mục tiêu đồng minh: 70% tập trung tấn công kẻ ít HP nhất, 30% ngẫu nhiên
                    List<CombatCharacter> aliveAllies = GetAliveAllies();
                    if (aliveAllies.Count > 0)
                    {
                        if (UnityEngine.Random.value < 0.7f)
                        {
                            // Tìm đồng minh ít HP nhất
                            CombatCharacter target = aliveAllies[0];
                            foreach (var ally in aliveAllies)
                            {
                                if (ally.currentHP < target.currentHP)
                                {
                                    target = ally;
                                }
                            }
                            targets.Add(target);
                        }
                        else
                        {
                            // Ngẫu nhiên
                            targets.Add(aliveAllies[UnityEngine.Random.Range(0, aliveAllies.Count)]);
                        }
                    }
                    break;

                case TargetType.AOE:
                case TargetType.ALL_ENEMIES:
                    // Tấn công diện rộng toàn bộ đồng minh
                    targets.AddRange(GetAliveAllies());
                    break;

                case TargetType.SELF:
                    targets.Add(attacker);
                    break;

                case TargetType.ALL_ALLIES:
                    // Hồi máu/buff toàn bộ kẻ địch khác
                    targets.AddRange(GetAliveEnemies());
                    break;
            }

            return targets;
        }

        #endregion

        #region Thực thi kỹ năng

        /// <summary>
        /// Thi triển một kỹ năng lên các mục tiêu.
        /// </summary>
        public void ExecuteAction(CombatCharacter attacker, SkillData skill, List<CombatCharacter> targets)
        {
            currentState = CombatState.BUSY;
            UIManager.Instance.HideActionPanel();
            attacker.HideTurnVFX(); // Ẩn hiệu ứng lượt đi khi bắt đầu hành động

            OnSkillCast?.Invoke(attacker, skill, targets);
            Debug.Log($"[CombatManager] {attacker.characterData.characterName} dùng '{skill.skillName}' lên {targets.Count} mục tiêu.");

            // Trừ năng lượng / hồi năng lượng cho người dùng kỹ năng
            attacker.UseSkill(skill);

            // Tìm vị trí trung tâm để dash (nếu AOE) hoặc vị trí mục tiêu đơn lẻ
            Vector3 targetPos = Vector3.zero;
            if (skill.targetType == TargetType.SINGLE && targets.Count > 0)
            {
                targetPos = targets[0].transform.position;
            }
            else if (targets.Count > 0)
            {
                // Lấy trung điểm của các mục tiêu
                foreach (var t in targets)
                {
                    targetPos += t.transform.position;
                }
                targetPos /= targets.Count;
            }

            // Gọi chuyển động tấn công procedural
            attacker.PlayAttackAnimation(targetPos, 
                // Khi chạm mục tiêu (Impact)
                () => {
                    bool spawnedCustomVFX = false;

                    // Lấy VFX thích hợp từ characterData của attacker
                    GameObject customVFXPrefab = null;
                    if (attacker.characterData != null)
                    {
                        if (skill.skillType == SkillType.BASIC)
                            customVFXPrefab = attacker.characterData.basicAttackImpactVFX;
                        else if (skill.skillType == SkillType.SPECIAL)
                            customVFXPrefab = attacker.characterData.specialAttackImpactVFX;
                        else if (skill.skillType == SkillType.ULTIMATE)
                            customVFXPrefab = attacker.characterData.ultimateVFX;
                    }

                    if (customVFXPrefab != null)
                    {
                        foreach (var target in targets)
                        {
                            if (target.isDead) continue;
                            GameObject vfx = Instantiate(customVFXPrefab, target.transform.position, Quaternion.identity);
                            // Tự hủy sau 3 giây để tránh rác bộ nhớ
                            Destroy(vfx, 3f);
                        }
                        spawnedCustomVFX = true;
                    }

                    if (!spawnedCustomVFX)
                    {
                        // Tạo hiệu ứng hạt mặc định
                        ProceduralVFX.Instance.SpawnVFX(skill, targetPos);
                    }

                    // Giải quyết sát thương và buff lên từng mục tiêu
                    foreach (var target in targets)
                    {
                        if (target.isDead) continue;

                        // Tính sát thương
                        bool isCrit;
                        float damage = DamageCalculator.Calculate(attacker, target, skill, weaknessDatabase, out isCrit);

                        // Gây sát thương
                        target.TakeDamage(damage, attacker.characterData.element, isCrit);

                        // Nổi số sát thương
                        Color numberColor = GetElementColor(attacker.characterData.element);
                        string damageText = damage.ToString();
                        if (isCrit)
                        {
                            damageText += "!";
                        }
                        FloatingText.Instance.SpawnText(target.transform.position + Vector3.up * 1.5f, damageText, numberColor, isCrit ? 1.5f : 1.0f);

                        // Áp dụng hiệu ứng buff/debuff đi kèm của skill
                        foreach (var effect in skill.effects)
                        {
                            EffectManager.Instance.ApplyEffect(attacker, target, effect);
                        }

                        OnDamageDealt?.Invoke(target, damage, isCrit);
                    }
                }, 
                // Khi hoàn thành quay về chỗ cũ
                () => {
                    // Đưa nhân vật về lại Turn Queue (Nếu không phải Ultimate Interrupt cắt lượt)
                    if (!isExecutingUltimateInterrupt)
                    {
                        turnQueue.ResetCharacterAV(attacker);
                    }
                    
                    isExecutingUltimateInterrupt = false;

                    // Tiếp tục vòng lặp
                    StartCoroutine(CoNextTurnDelay());
                }
            );
        }

        /// <summary>
        /// Thực thi hành động Phòng thủ (Guard/Defend) cho nhân vật.
        /// </summary>
        public void ExecuteGuard(CombatCharacter character)
        {
            currentState = CombatState.BUSY;
            UIManager.Instance.HideActionPanel();
            character.HideTurnVFX(); // Ẩn hiệu ứng lượt đi khi bắt đầu phòng thủ

            character.isGuarding = true;
            Debug.Log($"[CombatManager] {character.characterData.characterName} vào trạng thái phòng thủ (Guard).");

            // Nổi chữ GUARD
            FloatingText.Instance.SpawnText(character.transform.position + Vector3.up * 1.5f, "GUARD!", Color.gray, 1.0f);

            // Tạo vfx bảo vệ nhỏ
            ProceduralVFX.Instance.SpawnGuardVFX(character.transform.position);

            // Hồi một ít năng lượng khi phòng thủ (+10)
            character.AddEnergy(10f);

            turnQueue.ResetCharacterAV(character);
            StartCoroutine(CoNextTurnDelay());
        }

        #endregion

        #region Quản lý Cắt lượt bằng Chiêu cuối (Ultimate Interrupt)

        /// <summary>
        /// Người chơi hoặc AI yêu cầu xếp hàng thi triển Chiêu cuối cắt lượt.
        /// </summary>
        public void RequestUltimateCast(CombatCharacter character)
        {
            if (character.isDead || character.currentEnergy < 100f) return;

            if (queuedUltimates.Contains(character)) return;

            queuedUltimates.Enqueue(character);
            Debug.Log($"[CombatManager] Đã xếp hàng chờ Chiêu cuối cho: {character.characterData.characterName}");

            // Tạo chữ nổi thông báo cắt lượt
            FloatingText.Instance.SpawnText(character.transform.position + Vector3.up * 2f, "ULTIMATE READY!", Color.yellow, 1.3f);

            // Cập nhật UIManager để biểu thị nút Ultimate đang kích hoạt
            UIManager.Instance.UpdateUltimateButtons();

            // Nếu trò chơi đang ở trạng thái nhàn rỗi (PlayerTurn chờ input), ta có thể kích hoạt ngay!
            if (currentState == CombatState.PLAYERTURN)
            {
                StartCoroutine(CoExecuteQueuedUltimate());
            }
        }

        private IEnumerator CoExecuteQueuedUltimate()
        {
            if (queuedUltimates.Count == 0) yield break;

            isExecutingUltimateInterrupt = true;
            currentState = CombatState.BUSY;
            UIManager.Instance.HideActionPanel();

            CombatCharacter ultChar = queuedUltimates.Dequeue();
            yield return new WaitForSeconds(0.2f);

            // Nổi chữ thi triển chiêu cuối
            FloatingText.Instance.SpawnText(ultChar.transform.position + Vector3.up * 2.5f, "ULTIMATE CUT-IN!", Color.yellow, 1.8f);
            
            // Xoay camera cận cảnh nếu cần thiết (Visual effect)
            Debug.Log($"[CombatManager] CẮT LƯỢT: {ultChar.characterData.characterName} thi triển Ultimate!");

            SkillData ultSkill = ultChar.characterData.skillUltimate;

            // Tự động tìm mục tiêu cho chiêu cuối:
            // Với ally: Nếu là chiêu đơn, chọn kẻ địch ít máu nhất; diện rộng thì chọn tất cả kẻ địch.
            // Nếu là buff/heal: Chọn ally ít máu nhất; diện rộng thì chọn tất cả ally.
            List<CombatCharacter> targets = new List<CombatCharacter>();
            if (ultSkill.targetType == TargetType.SINGLE)
            {
                if (ultChar.isAlly)
                {
                    List<CombatCharacter> aliveEnemies = GetAliveEnemies();
                    if (aliveEnemies.Count > 0)
                    {
                        // Tìm kẻ địch ít HP nhất
                        CombatCharacter target = aliveEnemies[0];
                        foreach (var e in aliveEnemies)
                        {
                            if (e.currentHP < target.currentHP) target = e;
                        }
                        targets.Add(target);
                    }
                }
                else
                {
                    List<CombatCharacter> aliveAllies = GetAliveAllies();
                    if (aliveAllies.Count > 0)
                    {
                        CombatCharacter target = aliveAllies[0];
                        foreach (var a in aliveAllies)
                        {
                            if (a.currentHP < target.currentHP) target = a;
                        }
                        targets.Add(target);
                    }
                }
            }
            else if (ultSkill.targetType == TargetType.AOE || ultSkill.targetType == TargetType.ALL_ENEMIES)
            {
                targets.AddRange(ultChar.isAlly ? GetAliveEnemies() : GetAliveAllies());
            }
            else if (ultSkill.targetType == TargetType.ALL_ALLIES)
            {
                targets.AddRange(ultChar.isAlly ? GetAliveAllies() : GetAliveEnemies());
            }
            else if (ultSkill.targetType == TargetType.SELF)
            {
                targets.Add(ultChar);
            }

            if (targets.Count > 0)
            {
                ExecuteAction(ultChar, ultSkill, targets);
            }
            else
            {
                // Nếu không có mục tiêu, hủy chiêu cuối (trả lại energy hoặc reset)
                ultChar.AddEnergy(100f);
                isExecutingUltimateInterrupt = false;
                NextTurn();
            }
        }

        #endregion

        #region Hỗ trợ và Quản lý sự kiện

        private void HandleCharacterDeath(CombatCharacter character)
        {
            Debug.Log($"[CombatManager] {character.characterData.characterName} đã gục ngã.");
            turnQueue.RemoveCharacter(character);
            OnCharacterDeath?.Invoke(character);

            // Kiểm tra ngay điều kiện kết thúc trận đấu
            CheckCombatEnd();
        }

        public void HandleSpeedChanged(CombatCharacter character, float oldSpeed, float newSpeed)
        {
            turnQueue.OnSpeedChanged(character, oldSpeed, newSpeed);
            UIManager.Instance.UpdateTurnQueueHUD();
        }

        public void TriggerEffectApplied(CombatCharacter target, EffectData effect)
        {
            OnEffectApplied?.Invoke(target, effect);
        }

        private bool CheckCombatEnd()
        {
            if (GetAliveEnemies().Count == 0)
            {
                // Thắng
                currentState = CombatState.WIN;
                Debug.Log("[CombatManager] CHIẾN THẮNG! Tất cả kẻ địch đã bị tiêu diệt.");
                OnCombatEnd?.Invoke(true);
                UIManager.Instance.ShowEndScreen(true);
                return true;
            }

            if (GetAliveAllies().Count == 0)
            {
                // Thua
                currentState = CombatState.LOSE;
                Debug.Log("[CombatManager] THẤT BẠI! Đội hình đồng minh đã gục ngã hoàn toàn.");
                OnCombatEnd?.Invoke(false);
                UIManager.Instance.ShowEndScreen(false);
                return true;
            }

            return false;
        }

        #endregion

        #region Helpers lấy danh sách nhân vật

        public List<CombatCharacter> GetAliveCharacters()
        {
            List<CombatCharacter> list = new List<CombatCharacter>();
            foreach (var ally in allies)
            {
                if (!ally.isDead) list.Add(ally);
            }
            foreach (var enemy in enemies)
            {
                if (!enemy.isDead) list.Add(enemy);
            }
            return list;
        }

        public List<CombatCharacter> GetAliveAllies()
        {
            List<CombatCharacter> list = new List<CombatCharacter>();
            foreach (var ally in allies)
            {
                if (!ally.isDead) list.Add(ally);
            }
            return list;
        }

        public List<CombatCharacter> GetAliveEnemies()
        {
            List<CombatCharacter> list = new List<CombatCharacter>();
            foreach (var enemy in enemies)
            {
                if (!enemy.isDead) list.Add(enemy);
            }
            return list;
        }

        public Color GetElementColor(ElementType type)
        {
            switch (type)
            {
                case ElementType.Fire: return new Color(1.0f, 0.3f, 0.1f); // Đỏ lửa
                case ElementType.Ice: return new Color(0.3f, 0.7f, 1.0f);  // Xanh băng
                case ElementType.Lightning: return new Color(1.0f, 0.9f, 0.2f); // Vàng sét
                case ElementType.Nature: return new Color(0.2f, 0.8f, 0.3f); // Xanh lá
                case ElementType.Physical: return new Color(0.8f, 0.8f, 0.8f); // Xám vật lý
                default: return Color.white;
            }
        }

        #endregion
    }
}
