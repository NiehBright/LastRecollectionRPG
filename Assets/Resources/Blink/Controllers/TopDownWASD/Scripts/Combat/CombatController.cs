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
    public float[] hitTimes = new float[] { 0.18f, 0.22f, 0.26f, 0.32f }; // when the hit actually applies after animation start
    public float[] animationDurations = new float[] { 0.6f, 0.6f, 0.6f, 0.6f }; // duration of each attack animation (SET THIS CORRECTLY!)
    public float comboInputTimeout = 0.8f; // time allowed to input next combo
    [Tooltip("Use Animation Events (OnAttackHit with int parameter). If event is missing, a fallback hit-time is used.")]
    public bool useAnimationEvents = true;

    [Header("Movement During Attack")]
    public float attackMoveDistance = 0.3f; // how far to move forward during attack
    public float[] attackMoveDurations = new float[] { 0.35f, 0.35f, 0.35f, 0.35f }; // duration to move forward for each attack

    [Header("Direct Attack Clips (assign these to play attacks)")]
    [Tooltip("Assign 4 attack animation clips here. If empty, the script will fallback to Animator triggers.")]
    public AnimationClip[] attackClips = new AnimationClip[4];

    [Header("Hit Streak / Scaling")]
    public int hitStreak = 0;
    public float streakDecayTime = 5f; // seconds without hitting to reset streak

    // thresholds: at >=10 and >=20 hits we increase damage
    public int threshold1 = 10;
    public int threshold2 = 20;

    int _comboIndex = 0; // 0..3
    float _lastInputTime = -10f;
    Coroutine _currentAttackCoroutine;
    float _lastHitTime = -10f;
    int _activeAttackIndex = -1;
    bool _activeAttackHitConsumed;
    int _queuedComboIndex = -1; // for better input queue


    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        // Ensure Combat layer exists (for separate attack animations)
        if (animator != null && animator.layerCount <= 1)
        {
            Debug.LogWarning("Animator does not have a 'Combat' layer. Run 'BLINK → Combat → Setup Combat Assets' first.");
        }
    }

    void OnEnable()
    {
        // No PlayableGraph needed anymore - using animator layers instead
    }

    void OnDisable()
    {
        // No cleanup needed for animator-based playback
    }

    void Update()
    {
        // decay hit streak
        if (hitStreak > 0 && Time.time - _lastHitTime > streakDecayTime)
        {
            hitStreak = 0;
        }

        // read left mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnAttackInput();
        }
    }

    void OnAttackInput()
    {
        // if last input was too long ago, reset combo
        if (Time.time - _lastInputTime > comboInputTimeout)
        {
            _comboIndex = 0;
            _queuedComboIndex = -1;
        }

        _lastInputTime = Time.time;

        // queue or start attack
        if (_currentAttackCoroutine == null)
        {
            // Start first attack immediately
            _currentAttackCoroutine = StartCoroutine(PerformAttack(_comboIndex));
            _queuedComboIndex = -1;
        }
        else
        {
            // Queue next attack - store it instead of immediately incrementing
            _queuedComboIndex = Mathf.Clamp(_comboIndex + 1, 0, 3);
        }
    }

    IEnumerator PerformAttack(int index)
    {
        // Trigger attack animation via animator (on Combat layer)
        string trig = "Attack" + (index + 1);
        
        if (animator != null && AnimatorHasParameter(animator, trig))
        {
            // Enable Combat layer during attack
            int combatLayerIndex = animator.GetLayerIndex("Combat");
            if (combatLayerIndex >= 0)
            {
                animator.SetLayerWeight(combatLayerIndex, 1f);
            }
            
            animator.SetTrigger(trig);
        }

        _activeAttackIndex = index;
        _activeAttackHitConsumed = false;

        // Move forward during attack
        float moveDuration = (index >= 0 && index < attackMoveDurations.Length) ? attackMoveDurations[index] : 0.35f;
        float moveDistance = attackMoveDistance;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.forward * moveDistance;
        float elapsedMoveTime = 0f;

        // Get the animation duration
        float animDuration = (index >= 0 && index < animationDurations.Length) ? animationDurations[index] : 0.6f;
        float animStartTime = Time.time;

        // Wait for hit time and apply fallback hit if animation event is missing
        float wait = (index >= 0 && index < hitTimes.Length) ? hitTimes[index] : 0.25f;
        yield return new WaitForSeconds(wait);
        
        if (!useAnimationEvents && !_activeAttackHitConsumed)
        {
            ApplyHit(index);
        }
        else if (useAnimationEvents)
        {
            // Wait a bit more for animation event
            yield return new WaitForSeconds(0.2f);
            if (!_activeAttackHitConsumed)
            {
                ApplyHit(index);
            }
        }

        // Continue moving and wait for animation to FULLY complete
        while (Time.time - animStartTime < animDuration)
        {
            elapsedMoveTime += Time.deltaTime;
            
            // Move if still in move duration
            if (elapsedMoveTime < moveDuration)
            {
                float t = Mathf.Clamp01(elapsedMoveTime / moveDuration);
                CharacterController cc = GetComponent<CharacterController>();
                if (cc != null)
                {
                    Vector3 moveDir = (targetPosition - transform.position).normalized;
                    cc.Move(moveDir * moveDistance * Time.deltaTime / moveDuration);
                }
            }
            
            yield return null;
        }

        // Animation is now fully complete - check for queued combo attack
        if (_queuedComboIndex >= 0 && Time.time - _lastInputTime <= comboInputTimeout)
        {
            // Start queued attack
            int nextIndex = _queuedComboIndex;
            _comboIndex = nextIndex;
            _queuedComboIndex = -1;
            _currentAttackCoroutine = StartCoroutine(PerformAttack(nextIndex));
            yield break;
        }

        // reset combo and disable Combat layer
        _comboIndex = 0;
        _queuedComboIndex = -1;
        _currentAttackCoroutine = null;
        _activeAttackIndex = -1;

        // Disable Combat layer and return to Locomotion
        if (animator != null)
        {
            int combatLayerIndex = animator.GetLayerIndex("Combat");
            if (combatLayerIndex >= 0)
            {
                animator.SetLayerWeight(combatLayerIndex, 0f);
            }
        }
    }

    // Animation Event target: add this event to attack clips and pass 0..3 index.
    public void OnAttackHit(int index)
    {
        if (!useAnimationEvents) return;
        if (_activeAttackHitConsumed) return;
        if (_activeAttackIndex >= 0 && index != _activeAttackIndex) return;
        ApplyHit(index);
    }

    void ApplyHit(int index)
    {
        _activeAttackHitConsumed = true;

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 centre = origin + transform.forward * attackRange;

        Collider[] hits = Physics.OverlapSphere(centre, attackRadius, enemyLayer, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            if (!c.CompareTag("Enemy")) continue;

            var enemy = c.GetComponent<EnemyDummy>();
            float baseDamage = (index >= 0 && index < baseDamages.Length) ? baseDamages[index] : 10f;
            float damage = baseDamage * GetStreakMultiplier();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            Vector3 popupPos = c.transform.position + Vector3.up * 1.2f;
            if (damagePopupPrefab != null)
            {
                DamagePopup.SpawnFromPrefab(damagePopupPrefab, popupPos, "Đòn " + (index + 1), Mathf.RoundToInt(damage));
            }
            else
            {
                DamagePopup.Spawn(popupPos, "Đòn " + (index + 1), Mathf.RoundToInt(damage));
            }

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, c.bounds.center + Vector3.up * 0.2f, Quaternion.identity);
            }

            hitStreak++;
            _lastHitTime = Time.time;
        }
    }


    float GetStreakMultiplier()
    {
        if (hitStreak >= threshold2) return 1.5f; // +50% at 20 hits
        if (hitStreak >= threshold1) return 1.2f; // +20% at 10 hits
        return 1f;
    }

    bool AnimatorHasParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
        {
            if (p.name == paramName) return true;
        }
        Debug.LogWarning($"Animator parameter '{paramName}' does not exist on animator '{anim.name}'. Skipping trigger.");
        return false;
    }

    // debug gizmos to show attack area
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 centre = origin + transform.forward * attackRange;
            Gizmos.DrawWireSphere(centre, attackRadius);
        }
    }
}

