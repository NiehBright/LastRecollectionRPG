using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Combat system for Top-down RPG
// - Left click cycles through a 4-hit combo (Attack1..Attack4)
// - Damage scales with a hit-streak; streak decays after a timeout
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

        [Header("Attack Settings")]
        public float attackRange = 1.6f;
        public float attackRadius = 0.8f;
        public LayerMask enemyLayer = ~0;
        public float[] baseDamages = new float[] { 40f, 55f, 75f, 110f };
        [Tooltip("Normalized time (0-1) within the animation clip when the hit applies.")]
        public float[] hitNormalizedTimes = new float[] { 0.35f, 0.40f, 0.45f, 0.50f };
        [Tooltip("Use Animation Events (OnAttackHit with int parameter). If event is missing, a fallback hit-time is used.")]
        public bool useAnimationEvents = true;

        [Header("Combo Timing")]
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

        [Header("Hit Streak / Scaling")]
        public int hitStreak = 0;
        public float streakDecayTime = 5f; // seconds without hitting to reset streak
        // thresholds: at >=10 and >=20 hits we increase damage
        public int threshold1 = 10;
        public int threshold2 = 20;

        // --- State ---
        int _comboIndex = 0; // 0..3
        bool _isAttacking;
        bool _comboQueued;
        bool _activeAttackHitConsumed;
        int _activeAttackIndex = -1;
        float _lastHitTime = -10f;
        Coroutine _attackCoroutine;

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
        }

        void Update()
        {
            // Decay hit streak
            if (hitStreak > 0 && Time.time - _lastHitTime > streakDecayTime)
                hitStreak = 0;

            // Read left mouse button
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                OnAttackInput();
        }

        // ─────────── INPUT ───────────
        void OnAttackInput()
        {
            if (!_isAttacking)
            {
                // Start fresh combo from Attack1
                _comboIndex = 0;
                _comboQueued = false;
                StartAttack(_comboIndex);
            }
            else
            {
                // We're mid-attack: check if we're inside the combo window
                // The actual queueing is time-checked in the coroutine,
                // but we set the flag here so we don't miss the input
                _comboQueued = true;
            }
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
                        _cc.Move(transform.forward * delta);
                    prevLungeFactor = currentFactor;
                }

                // --- Fallback hit detection (if not using animation events) ---
                if (!useAnimationEvents && !_activeAttackHitConsumed && normTime >= hitNormTime)
                    ApplyHit(index);

                // --- Animation event fallback safety ---
                if (useAnimationEvents && !_activeAttackHitConsumed && normTime >= hitNormTime + 0.15f)
                    ApplyHit(index); // safety fallback if event was missed

                // --- Interrupt sheathing with movement ---
                if (normTime >= windowStart && HasMovementInput())
                {
                    break;
                }

                // --- Combo window ---
                if (_comboQueued && !comboAccepted
                    && normTime >= windowStart && normTime <= windowEnd
                    && index < 3) // can't combo past Attack4
                {
                    comboAccepted = true;
                    int nextIndex = index + 1;
                    _comboIndex = nextIndex;
                    _comboQueued = false;
                    // Trigger the next attack now (the Animator transition will handle blending)
                    animator.SetTrigger(AttackTriggerHashes[nextIndex]);
                }

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

                var enemy = c.GetComponent<EnemyDummy>();
                float baseDamage = (index >= 0 && index < baseDamages.Length) ? baseDamages[index] : 10f;
                float damage = baseDamage * GetStreakMultiplier();

                if (enemy != null)
                    enemy.TakeDamage(damage);

                Vector3 popupPos = c.transform.position + Vector3.up * 1.2f;
                if (damagePopupPrefab != null)
                    DamagePopup.SpawnFromPrefab(damagePopupPrefab, popupPos, "Đòn " + (index + 1), Mathf.RoundToInt(damage));
                else
                    DamagePopup.Spawn(popupPos, "Đòn " + (index + 1), Mathf.RoundToInt(damage));

                if (hitEffectPrefab != null)
                    Instantiate(hitEffectPrefab, c.bounds.center + Vector3.up * 0.2f, Quaternion.identity);

                hitStreak++;
                _lastHitTime = Time.time;
            }
        }

        // ─────────── STREAK ───────────
        float GetStreakMultiplier()
        {
            if (hitStreak >= threshold2) return 1.5f; // +50% at 20 hits
            if (hitStreak >= threshold1) return 1.2f; // +20% at 10 hits
            return 1f;
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
