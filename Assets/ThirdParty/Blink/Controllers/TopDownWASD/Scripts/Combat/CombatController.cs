using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Combat system for Top-down RPG
// - Input System: nhấn để tạo combo 4 đòn (Attack1..Attack4) trong khoảng timeout
// - Mỗi đòn gây cùng lượng sát thương
// - Triggers animator parameters: "Attack1","Attack2","Attack3","Attack4"
// Attach to the player (same object that has the Animator). Does not modify existing code.
namespace BLINK.Controller
{
    public class CombatController : MonoBehaviour
    {
        [Header("References")]
        public Animator animator; // assign the player's animator (TopDownWASDAnimator layer should have triggers)
        [Tooltip("Optional: prefab for damage popup. If left empty the built-in TextMesh popup will be used.")]
        public GameObject damagePopupPrefab;
        [Tooltip("Optional: prefab for hit effect on impact.")]
        public GameObject hitEffectPrefab;

        [Header("Input System")]
        [Tooltip("Input Action cho tấn công (ví dụ: Attack trong Input System).")]
        public InputActionReference attackAction;

        [Header("Attack Settings")]
        public float attackRange = 1.6f;
        public float attackRadius = 0.8f;
        public LayerMask enemyLayer = ~0;
        [Tooltip("Tất cả đòn đánh dùng chung sát thương này.")]
        public float baseDamage = 60f;
        [Tooltip("Normalized time (0-1) within the animation clip when the hit applies.")]
        public float[] hitNormalizedTimes = new float[] { 0.35f, 0.40f, 0.45f, 0.50f };
        [Tooltip("Use Animation Events (OnAttackHit with int parameter). If event is missing, a fallback hit-time is used.")]
        public bool useAnimationEvents = true;

        [Header("Combo Timing")]
        [Tooltip("Thời gian tối đa giữa các lần bấm để nối combo (giây).")]
        public float comboInputTimeout = 4f;
        [Tooltip("Earliest normalized time in the current attack animation where the next combo input is accepted.")]
        public float[] comboWindowStarts = new float[] { 0.45f, 0.45f, 0.45f, 0.45f };
        [Tooltip("Latest normalized time. After this, combo resets to Attack1.")]
        public float[] comboWindowEnds = new float[] { 0.90f, 0.90f, 0.90f, 0.90f };
        [Tooltip("Transition blend duration (seconds) between attack animations.")]
        public float comboTransitionDuration = 0.08f;

        [Header("Movement During Attack")]
        [Tooltip("Forward lunge distance for each attack in the combo.")]
        public float[] attackMoveDistances = new float[] { 0.4f, 0.35f, 0.3f, 0.6f };
        [Tooltip("Normalized time range [start,end] during which the lunge occurs. Lunge peaks at the middle.")]
        public float lungePeakNormalized = 0.35f;
        [Tooltip("Curve for attack lunge. X = normalized time, Y = distance factor (0 to 1 and back to 0).")]
        public AnimationCurve lungeCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(0.35f, 1f, 0f, 0f),
            new Keyframe(0.7f, 0f, -2f, 0f)
        );

        // --- State ---
        int _comboIndex; // 0..3
        bool _isAttacking;
        bool _comboQueued;
        bool _activeAttackHitConsumed;
        int _activeAttackIndex = -1;
        float _lastAttackInputTime = -10f;
        Coroutine _attackCoroutine;
        bool _attackActionEnabledByUs;
        public bool _isTransitioning = false;

        // --- Cached references ---
        CharacterController _cc;
        int _combatLayerIndex = -1;
        static readonly int[] AttackTriggerHashes = new int[4];

        // --- Pre-allocated NonAlloc buffer ---
        readonly Collider[] _hitBuffer = new Collider[16];

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            _cc = GetComponent<CharacterController>();

            // Cache trigger hashes (avoid runtime string allocation)
            for (int i = 0; i < 4; i++)
                AttackTriggerHashes[i] = Animator.StringToHash("Attack" + (i + 1));

            // Cache combat layer index
            if (animator != null)
            {
                _combatLayerIndex = animator.GetLayerIndex("Combat");
                if (_combatLayerIndex < 0 && animator.layerCount <= 1)
                    Debug.LogWarning("[CombatController] Animator does not have a 'Combat' layer. Run 'BLINK → Combat → Setup Combat Assets' first.");
            }

            // Try to find InputActionReference if not assigned
            if (attackAction == null)
            {
                // Try to find from PlayerInput component
                var playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    var attackInputAction = playerInput.actions.FindAction("Attack");
                    if (attackInputAction != null)
                    {
                        Debug.Log("[CombatController] Auto-found Attack action from PlayerInput");
                        // Note: We'll subscribe manually in OnEnable
                    }
                }
                else
                {
                    Debug.LogWarning("[CombatController] attackAction not assigned and no PlayerInput found. Combat will not work!");
                }
            }
        }

        void OnEnable()
        {
            InputAction actionToUse = null;

            // Priority 1: InputActionReference (manually assigned in inspector)
            if (attackAction != null)
            {
                actionToUse = attackAction.action;
                Debug.Log("[CombatController] Using InputActionReference");
            }
            // Priority 2: InputManager static helper
            else if (InputManager.GetAction("Attack") != null)
            {
                actionToUse = InputManager.GetAction("Attack");
                Debug.Log("[CombatController] Using InputManager.GetAction fallback");
            }
            // Priority 3: PlayerInput component
            else
            {
                var playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    actionToUse = playerInput.actions.FindAction("Attack");
                    Debug.Log("[CombatController] Using PlayerInput component fallback");
                }
            }

            if (actionToUse != null)
            {
                if (!actionToUse.enabled)
                {
                    actionToUse.Enable();
                    _attackActionEnabledByUs = true;
                }
                actionToUse.performed += OnAttackPerformed;
                Debug.Log("[CombatController] Attack action subscribed successfully");
            }
            else
            {
                Debug.LogError("[CombatController] Failed to find Attack action! Combat will not work.");
            }
        }

        void OnDisable()
        {
            InputAction actionToUse = null;

            if (attackAction != null)
                actionToUse = attackAction.action;
            else if (InputManager.GetAction("Attack") != null)
                actionToUse = InputManager.GetAction("Attack");
            else
            {
                var playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                    actionToUse = playerInput.actions.FindAction("Attack");
            }

            if (actionToUse != null)
            {
                actionToUse.performed -= OnAttackPerformed;
                if (_attackActionEnabledByUs)
                {
                    actionToUse.Disable();
                    _attackActionEnabledByUs = false;
                }
            }
        }

        void OnAttackPerformed(InputAction.CallbackContext ctx)
        {
            Debug.Log("[CombatController] OnAttackPerformed called from Input System");
            OnAttackInput();
        }

        // ─────────── INPUT ───────────
        void OnAttackInput()
        {
            if (_isAttacking) return; // Chỉ cho phép đánh 1 đòn duy nhất khi rảnh

            Debug.Log("[CombatController] Starting single attack (Honkai Style)");
            _comboIndex = 0;
            _comboQueued = false;
            StartAttack(0);
        }

        void StartAttack(int index)
        {
            if (_attackCoroutine != null)
                StopCoroutine(_attackCoroutine);

            _attackCoroutine = StartCoroutine(PerformAttack(index));
        }

        // ─────────── ATTACK COROUTINE ───────────
        IEnumerator PerformAttack(int index)
        {
            _isAttacking = true;
            _activeAttackIndex = index;
            _activeAttackHitConsumed = false;
            _comboQueued = false;

            // --- Enable Combat layer ---
            if (_combatLayerIndex >= 0)
                animator.SetLayerWeight(_combatLayerIndex, 1f);

            // --- Fire trigger ---
            animator.SetTrigger(AttackTriggerHashes[index]);
            // Reset ALL other attack triggers to prevent state machine confusion
            for (int i = 0; i < 4; i++)
            {
                if (i != index)
                    animator.ResetTrigger(AttackTriggerHashes[i]);
            }

            // --- Wait for the animator to actually enter the attack state ---
            // This ensures we can read normalized time correctly
            yield return null; // wait 1 frame for trigger to be consumed

            // Wait until the animator is in the correct attack state
            int targetStateHash = Animator.StringToHash("Attack" + (index + 1));
            int layerToCheck = _combatLayerIndex >= 0 ? _combatLayerIndex : 0;
            float safetyTimer = 0f;
            while (!animator.GetCurrentAnimatorStateInfo(layerToCheck).shortNameHash.Equals(targetStateHash))
            {
                safetyTimer += Time.deltaTime;
                if (safetyTimer > 0.3f) break; // safety fallback
                yield return null;
            }

            // --- Main attack loop: drive lunge + hit detection + combo window ---
            float lungeDistance = (index < attackMoveDistances.Length) ? attackMoveDistances[index] : 0.4f;
            float hitNormTime = (index < hitNormalizedTimes.Length) ? hitNormalizedTimes[index] : 0.4f;
            float windowStart = (index < comboWindowStarts.Length) ? comboWindowStarts[index] : 0.45f;
            float windowEnd = (index < comboWindowEnds.Length) ? comboWindowEnds[index] : 0.90f;
            float prevLungeFactor = 0f;
            bool comboAccepted = false;

            while (true)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(layerToCheck);

                // Check if we've left the attack state (transition to Idle completed)
                if (!stateInfo.shortNameHash.Equals(targetStateHash))
                {
                    // If we are in transition into a DIFFERENT attack state, that's the combo continuing
                    var nextInfo = animator.GetNextAnimatorStateInfo(layerToCheck);
                    if (animator.IsInTransition(layerToCheck) && nextInfo.shortNameHash != 0)
                        break; // combo transition is happening
                    if (stateInfo.normalizedTime >= 0.95f)
                        break; // attack ended
                }

                float normTime = stateInfo.normalizedTime % 1f;

                // --- Apply lunge movement ---
                if (_cc != null && lungeDistance > 0f)
                {
                    float currentFactor = lungeCurve.Evaluate(normTime);
                    float delta = (currentFactor - prevLungeFactor) * lungeDistance;
                    if (delta > 0f)
                    {
                        // Kiểm tra xem có quái vật nào quá gần không để chặn lunge (tránh lỗi bay lên trời)
                        bool enemyTooClose = false;
                        int closeEnemyCount = Physics.OverlapSphereNonAlloc(transform.position, 1.1f, _hitBuffer, enemyLayer, QueryTriggerInteraction.Ignore);
                        for (int e = 0; e < closeEnemyCount; e++)
                        {
                            if (_hitBuffer[e] != null && _hitBuffer[e].CompareTag("Enemy"))
                            {
                                enemyTooClose = true;
                                break;
                            }
                        }

                        if (!enemyTooClose)
                        {
                            // Chỉ chiếu hướng lunge lên mặt phẳng ngang (XZ) để không bị bay lên
                            Vector3 lungeDir = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                            _cc.Move(lungeDir * delta);
                        }
                    }
                    prevLungeFactor = currentFactor;
                }

                // --- Fallback hit detection (if not using animation events) ---
                if (!useAnimationEvents && !_activeAttackHitConsumed && normTime >= hitNormTime)
                    ApplyHit(index);

                // --- Animation event fallback safety ---
                if (useAnimationEvents && !_activeAttackHitConsumed && normTime >= hitNormTime + 0.15f)
                    ApplyHit(index); // safety fallback if event was missed

                // --- Interrupt sheathing with movement (Vô hiệu hóa để chạy hết animation chém) ---
                /*
                if (normTime >= windowStart && HasMovementInput())
                {
                    break;
                }
                */

                // --- Combo window (Disabled for Single Strike) ---

                // --- Exit condition: attack animation has fully played ---
                if (normTime >= 0.95f && !animator.IsInTransition(layerToCheck))
                    break;

                yield return null;
            }

            // --- Chain into next attack or end combo ---
            if (comboAccepted)
            {
                // Let the next attack coroutine take over
                _attackCoroutine = StartCoroutine(PerformAttack(_comboIndex));
                yield break;
            }

            // --- Combo ended: clean up ---
            EndCombo();
        }

        void EndCombo()
        {
            _isAttacking = false;
            _comboQueued = false;
            _comboIndex = 0;
            _activeAttackIndex = -1;
            _attackCoroutine = null;

            // Clear all attack triggers
            for (int i = 0; i < 4; i++)
                animator.ResetTrigger(AttackTriggerHashes[i]);

            // Smoothly fade out Combat layer weight to return to Locomotion
            if (_combatLayerIndex >= 0 && gameObject.activeInHierarchy)
            {
                StartCoroutine(FadeOutCombatLayer(0.15f));
            }
        }

        IEnumerator FadeOutCombatLayer(float duration)
        {
            float startWeight = animator.GetLayerWeight(_combatLayerIndex);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_isAttacking) yield break; // Cancel fade-out if we start a new attack
                elapsed += Time.deltaTime;
                animator.SetLayerWeight(_combatLayerIndex, Mathf.Lerp(startWeight, 0f, elapsed / duration));
                yield return null;
            }
            if (!_isAttacking)
                animator.SetLayerWeight(_combatLayerIndex, 0f);
        }

        bool HasMovementInput()
        {
            if (Keyboard.current == null) return false;
            return Keyboard.current.wKey.isPressed ||
                   Keyboard.current.sKey.isPressed ||
                   Keyboard.current.aKey.isPressed ||
                   Keyboard.current.dKey.isPressed;
        }

        // ─────────── ANIMATION EVENT TARGET ───────────
        // Add this event to attack clips and pass 0..3 index.
        public void OnAttackHit(int index)
        {
            if (!useAnimationEvents) return;
            if (_activeAttackHitConsumed) return;
            if (_activeAttackIndex >= 0 && index != _activeAttackIndex) return;
            ApplyHit(index);
        }

        // ─────────── HIT DETECTION ───────────
        void ApplyHit(int index)
        {
            _activeAttackHitConsumed = true;

            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 centre = origin + transform.forward * attackRange;

            // Use NonAlloc to avoid GC allocation every frame
            int hitCount = Physics.OverlapSphereNonAlloc(centre, attackRadius, _hitBuffer, enemyLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider c = _hitBuffer[i];
                if (c == null || !c.CompareTag("Enemy")) continue;

                // Tìm quái vật ở Overworld để mở UI chọn đội hình
                var monster = c.GetComponentInParent<RPG.Combat.OverworldMonster>();
                if (monster == null)
                {
                    var dummy = c.GetComponentInParent<EnemyDummy>();
                    if (dummy != null)
                    {
                        monster = dummy.GetComponent<RPG.Combat.OverworldMonster>();
                        if (monster == null)
                        {
                            monster = dummy.gameObject.AddComponent<RPG.Combat.OverworldMonster>();
                            monster.uniqueId = $"dummy_{dummy.name}_{Mathf.RoundToInt(dummy.transform.position.x)}_{Mathf.RoundToInt(dummy.transform.position.y)}_{Mathf.RoundToInt(dummy.transform.position.z)}";
                            monster.detectionRadius = 2.2f;
                        }
                    }
                }

                if (monster != null)
                {
                    if (!_isTransitioning)
                    {
                        _isTransitioning = true;
                        
                        // Tắt di chuyển của Player để tránh chạy lung tung khi chuyển cảnh
                        var wasdController = GetComponent<TopDownWASDController>();
                        if (wasdController != null)
                        {
                            wasdController.movementEnabled = false;
                            wasdController.cameraEnabled = false;
                        }

                        // Mở UI Chọn Đội Hình
                        if (RPG.Combat.CombatTeamSelectionUI.Instance != null)
                        {
                            RPG.Combat.CombatTeamSelectionUI.Instance.OpenUI(monster);
                        }
                        else
                        {
                            GameObject selectionUIGO = new GameObject("[CombatTeamSelectionUI]");
                            var ui = selectionUIGO.AddComponent<RPG.Combat.CombatTeamSelectionUI>();
                            ui.OpenUI(monster);
                        }
                    }
                }

                // Đã loại bỏ UI số sát thương bay lên (Damage Popup) ở thế giới thực

                if (hitEffectPrefab != null)
                    Instantiate(hitEffectPrefab, c.bounds.center + Vector3.up * 0.2f, Quaternion.identity);
            }
        }

        private IEnumerator CoTriggerTurnBaseTransition()
        {
            Debug.Log("[CombatController] Chém trúng quái! Chờ chạy hết animation chém...");
            // Chờ cho đến khi đòn đánh kết thúc hoàn chỉnh (IsAttacking / _isAttacking trở về false)
            while (_isAttacking)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.2f); // Thêm trễ ngắn 0.2s cho mượt mà
            UnityEngine.SceneManagement.SceneManager.LoadScene("TurnBase");
        }

        // ─────────── PUBLIC API ───────────
        /// <summary>Returns true if the player is currently in an attack animation.</summary>
        public bool IsAttacking => _isAttacking;

        /// <summary>Current combo step (0..3). -1 if not attacking.</summary>
        public int CurrentComboIndex => _isAttacking ? _comboIndex : -1;

        /// <summary>Force-cancel the current combo (e.g. when player gets hit, dodge, etc.)</summary>
        public void CancelCombo()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
            EndCombo();
        }

        // ─────────── DEBUG GIZMOS ───────────
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 centre = origin + transform.forward * attackRange;
            Gizmos.DrawWireSphere(centre, attackRadius);
        }
    }
}
