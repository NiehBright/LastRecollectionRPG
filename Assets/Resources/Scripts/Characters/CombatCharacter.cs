using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.Combat
{
    [System.Serializable]
    public class ActiveEffect
    {
        public EffectData data;
        public int turnsRemaining;
        public CombatCharacter caster;

        public ActiveEffect(EffectData data, CombatCharacter caster)
        {
            this.data = data;
            this.turnsRemaining = data.duration;
            this.caster = caster;
        }
    }

    public class CombatCharacter : MonoBehaviour
    {
        [Header("Dữ liệu gốc")]
        public CharacterData characterData;
        public bool isAlly;

        [Header("Chỉ số Runtime")]
        public float currentHP;
        public float maxHP;
        public float currentEnergy; // Phạm vi 0 - 100
        public int specialSkillCDRemaining = 0;
        public bool isDead = false;
        public bool isGuarding = false;

        [Header("Danh sách hiệu ứng")]
        public List<ActiveEffect> activeEffects = new List<ActiveEffect>();

        [Header("Visual References")]
        public Transform modelRoot; // Gốc xoay/di chuyển của mô hình
        private Vector3 originalPosition;
        private Coroutine activeAnimCoroutine;

        [Header("UI nổi (Floating HUD)")]
        private Canvas hudCanvas;
        private Image hpBarFill;
        private Image energyBarFill;
        private Transform buffIconContainer;
        private GameObject buffIconPrefab; // Sẽ tạo động hoặc load
        private GameObject turnIndicator;

        // Events
        public event Action<CombatCharacter, float, bool> OnHPChanged; // character, delta, isCrit
        public event Action<CombatCharacter, float> OnEnergyChanged;
        public event Action<CombatCharacter> OnDeath;

        public void Initialize(CharacterData data, bool isAllySide)
        {
            characterData = data;
            isAlly = isAllySide;

            maxHP = data.baseMaxHP;
            currentHP = maxHP;

            // Khởi tạo năng lượng ngẫu nhiên từ 30% đến 50%
            currentEnergy = UnityEngine.Random.Range(30f, 50f);
            isDead = false;
            isGuarding = false;
            specialSkillCDRemaining = 0;
            activeEffects.Clear();

            originalPosition = transform.position;

            // Tạo Mô hình procedural 3D dựa trên theme color nếu chưa có
            CreateProceduralModel();

            // Tạo Giao diện nổi trên đầu
            CreateFloatingHUD();
        }

        private void CreateProceduralModel()
        {
            // Xóa mô hình cũ nếu có
            if (modelRoot != null)
            {
                Destroy(modelRoot.gameObject);
            }

            GameObject rootGO = new GameObject("ModelRoot");
            rootGO.transform.SetParent(transform);
            rootGO.transform.localPosition = Vector3.zero;
            rootGO.transform.localRotation = Quaternion.identity;
            modelRoot = rootGO.transform;

            // Tạo khối cơ bản (Capsule cho Ally, Cylinder cho Enemy)
            GameObject body = GameObject.CreatePrimitive(isAlly ? PrimitiveType.Capsule : PrimitiveType.Cylinder);
            body.transform.SetParent(modelRoot);
            body.transform.localPosition = new Vector3(0, 1.0f, 0); // Đặt gốc ở chân
            body.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);

            // Gán màu theo hệ của nhân vật hoặc themeColor
            Renderer renderer = body.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = characterData.themeColor;
            renderer.material = mat;

            // Tạo vũ khí tượng trưng để trông đẹp hơn
            GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.transform.SetParent(modelRoot);
            weapon.transform.localPosition = new Vector3(0.5f, 1.0f, 0.3f);
            weapon.transform.localRotation = Quaternion.Euler(0, 0, 45);
            weapon.transform.localScale = new Vector3(0.15f, 0.8f, 0.15f);

            Renderer weaponRenderer = weapon.GetComponent<Renderer>();
            Material weaponMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            weaponMat.color = Color.gray;
            weaponRenderer.material = weaponMat;

            // Tạo chỉ báo lượt đi (bobbing turn indicator) trên đầu
            GameObject indicatorGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicatorGO.name = "TurnIndicator";
            indicatorGO.transform.SetParent(modelRoot);
            indicatorGO.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            indicatorGO.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            Renderer indRenderer = indicatorGO.GetComponent<Renderer>();
            Material indMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            indMat.color = Color.yellow;
            indRenderer.material = indMat;

            // Xóa Collider của indicator để tránh va chạm vật lý
            Destroy(indicatorGO.GetComponent<Collider>());

            turnIndicator = indicatorGO;
            turnIndicator.SetActive(false); // Ẩn mặc định
        }

        private void Update()
        {
            if (turnIndicator != null && turnIndicator.activeSelf)
            {
                // Hoạt ảnh nhấp nhô và xoay tròn nhẹ trên đầu
                float yOffset = Mathf.Sin(Time.time * 6f) * 0.12f;
                turnIndicator.transform.localPosition = new Vector3(0f, 2.4f + yOffset, 0f);
                turnIndicator.transform.Rotate(Vector3.up, 120f * Time.deltaTime);
            }
        }

        private void CreateFloatingHUD()
        {
            // Tạo Canvas thế giới
            GameObject canvasGO = new GameObject("FloatingHUD");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = new Vector3(0, 2.3f, 0); // Nổi phía trên đầu

            hudCanvas = canvasGO.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.WorldSpace;
            hudCanvas.worldCamera = Camera.main; // Gán camera để raycast chính xác
            canvasGO.AddComponent<GraphicRaycaster>(); // Bắt buộc phải có để click được nút World Space Canvas
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            RectTransform rect = hudCanvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 40f);
            rect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Tỷ lệ chuẩn (100 pixel = 1 mét)
            rect.localPosition = new Vector3(0, 2.3f, 0);
            rect.localRotation = Quaternion.identity;

            // Hướng HUD về phía Camera (Billboard)
            canvasGO.AddComponent<BillboardHUD>();

            // Tạo Panel nền cho HP Bar
            GameObject hpBgGO = new GameObject("HPBar_Bg");
            hpBgGO.transform.SetParent(canvasGO.transform);
            hpBgGO.transform.localPosition = new Vector3(0f, 10f, 0f);
            RectTransform hpBgRect = hpBgGO.AddComponent<RectTransform>();
            hpBgRect.sizeDelta = new Vector2(120f, 12f);
            hpBgRect.localScale = Vector3.one;
            hpBgRect.localPosition = new Vector3(0f, 10f, 0f);
            hpBgRect.localRotation = Quaternion.identity;
            Image hpBgImg = hpBgGO.AddComponent<Image>();
            hpBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Tạo HP Fill
            GameObject hpFillGO = new GameObject("HPBar_Fill");
            hpFillGO.transform.SetParent(hpBgGO.transform);
            hpFillGO.transform.localPosition = Vector3.zero;
            RectTransform hpFillRect = hpFillGO.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = new Vector2(1f, 1f);
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpFillRect.localPosition = Vector3.zero;
            hpFillRect.localScale = Vector3.one;
            hpFillRect.localRotation = Quaternion.identity;
            hpBarFill = hpFillGO.AddComponent<Image>();
            hpBarFill.color = isAlly ? new Color(0.1f, 0.8f, 0.1f) : new Color(0.9f, 0.1f, 0.1f);

            // Tạo Energy Bar
            GameObject energyBgGO = new GameObject("EnergyBar_Bg");
            energyBgGO.transform.SetParent(canvasGO.transform);
            energyBgGO.transform.localPosition = new Vector3(0f, -2f, 0f);
            RectTransform enBgRect = energyBgGO.AddComponent<RectTransform>();
            enBgRect.sizeDelta = new Vector2(120f, 8f);
            enBgRect.localScale = Vector3.one;
            enBgRect.localPosition = new Vector3(0f, -2f, 0f);
            enBgRect.localRotation = Quaternion.identity;
            Image enBgImg = energyBgGO.AddComponent<Image>();
            enBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            GameObject energyFillGO = new GameObject("EnergyBar_Fill");
            energyFillGO.transform.SetParent(energyBgGO.transform);
            energyFillGO.transform.localPosition = Vector3.zero;
            RectTransform enFillRect = energyFillGO.AddComponent<RectTransform>();
            enFillRect.anchorMin = Vector2.zero;
            enFillRect.anchorMax = new Vector2(currentEnergy / 100f, 1f);
            enFillRect.offsetMin = Vector2.zero;
            enFillRect.offsetMax = Vector2.zero;
            enFillRect.localPosition = Vector3.zero;
            enFillRect.localScale = Vector3.one;
            enFillRect.localRotation = Quaternion.identity;
            energyBarFill = energyFillGO.AddComponent<Image>();
            energyBarFill.color = new Color(0.1f, 0.6f, 0.9f);

            // Tạo Container chứa các buff/debuff
            GameObject buffsGO = new GameObject("BuffContainer");
            buffsGO.transform.SetParent(canvasGO.transform);
            buffsGO.transform.localPosition = new Vector3(0f, -15f, 0f);
            RectTransform buffsRect = buffsGO.AddComponent<RectTransform>();
            buffsRect.sizeDelta = new Vector2(120f, 15f);
            buffsRect.localScale = Vector3.one;
            buffsRect.localPosition = new Vector3(0f, -15f, 0f);
            buffsRect.localRotation = Quaternion.identity;

            HorizontalLayoutGroup layout = buffsGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 2f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            buffIconContainer = buffsGO.transform;
            
            UpdateFloatingHUD();
        }

        public void UpdateFloatingHUD()
        {
            if (hpBarFill != null)
            {
                hpBarFill.rectTransform.anchorMax = new Vector2(maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f, 1f);
            }
            if (energyBarFill != null)
            {
                energyBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(currentEnergy / 100f), 1f);
            }

            // Vẽ các Icon Buff/Debuff
            if (buffIconContainer != null)
            {
                // Clear cũ
                foreach (Transform child in buffIconContainer)
                {
                    Destroy(child.gameObject);
                }

                // Vẽ các buff hiện có
                foreach (var effect in activeEffects)
                {
                    GameObject iconGO = new GameObject("BuffIcon");
                    iconGO.transform.SetParent(buffIconContainer);
                    
                    RectTransform iconRect = iconGO.AddComponent<RectTransform>();
                    iconRect.sizeDelta = new Vector2(14f, 14f);
                    iconRect.localScale = Vector3.one;
                    iconRect.localPosition = Vector3.zero;
                    iconRect.localRotation = Quaternion.identity;

                    Image iconImg = iconGO.AddComponent<Image>();
                    iconImg.color = effect.data.effectColor;

                    if (effect.data.icon != null)
                    {
                        iconImg.sprite = effect.data.icon;
                    }

                    // Text hiển thị số lượt còn lại
                    GameObject textGO = new GameObject("TurnText");
                    textGO.transform.SetParent(iconGO.transform);
                    
                    RectTransform textRect = textGO.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    textRect.localPosition = Vector3.zero;
                    textRect.localScale = Vector3.one;
                    textRect.localRotation = Quaternion.identity;

                    Text txt = textGO.AddComponent<Text>();
                    txt.text = effect.turnsRemaining.ToString();
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txt.fontSize = 9;
                    txt.alignment = TextAnchor.LowerRight;
                    txt.color = Color.white;

                    // Thêm viền đen cho dễ nhìn
                    Outline outline = textGO.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(1f, -1f);
                }
            }
        }

        #region Tính Toán Stats Có Buff

        public float GetModifiedATK()
        {
            float mult = 1.0f;
            foreach (var effect in activeEffects)
            {
                if (effect.data.effectType == EffectType.ATK_BUFF)
                {
                    mult += effect.data.modifierValue;
                }
            }
            return Mathf.Max(1.0f, characterData.baseATK * mult);
        }

        public float GetModifiedDEF()
        {
            float mult = 1.0f;
            foreach (var effect in activeEffects)
            {
                if (effect.data.effectType == EffectType.DEF_BUFF)
                {
                    mult += effect.data.modifierValue;
                }
            }
            return Mathf.Max(0.0f, characterData.baseDEF * mult);
        }

        public float GetModifiedSpeed()
        {
            float mult = 1.0f;
            foreach (var effect in activeEffects)
            {
                if (effect.data.effectType == EffectType.SPEED_CHANGE)
                {
                    mult += effect.data.modifierValue;
                }
            }
            return Mathf.Max(10.0f, characterData.baseSpeed * mult);
        }

        public float GetModifiedCritRate()
        {
            return characterData.baseCritRate;
        }

        public float GetModifiedCritDMG()
        {
            return characterData.baseCritDMG;
        }

        public bool IsFrozen()
        {
            foreach (var effect in activeEffects)
            {
                if (effect.data.effectType == EffectType.FREEZE) return true;
            }
            return false;
        }

        public bool IsStunned()
        {
            foreach (var effect in activeEffects)
            {
                if (effect.data.effectType == EffectType.STUN) return true;
            }
            return false;
        }

        public bool CanAct()
        {
            return !isDead && !IsFrozen() && !IsStunned();
        }

        #endregion

        #region Tương Tác Gameplay

        public void TakeDamage(float damage, ElementType element, bool isCrit = false)
        {
            if (isDead) return;

            currentHP -= damage;
            if (currentHP <= 0f)
            {
                currentHP = 0f;
                isDead = true;
            }

            // Bị đánh hồi năng lượng (+10-20%)
            float energyGained = UnityEngine.Random.Range(10f, 20f);
            AddEnergy(energyGained);

            UpdateFloatingHUD();
            OnHPChanged?.Invoke(this, -damage, isCrit);

            // Animation bị đánh
            PlayHitShake();

            if (isDead)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;

            currentHP += amount;
            if (currentHP > maxHP)
            {
                currentHP = maxHP;
            }

            UpdateFloatingHUD();
            OnHPChanged?.Invoke(this, amount, false);
        }

        public void AddEnergy(float amount)
        {
            if (isDead) return;

            currentEnergy += amount;
            if (currentEnergy > 100f)
            {
                currentEnergy = 100f;
            }
            
            UpdateFloatingHUD();
            OnEnergyChanged?.Invoke(this, amount);
        }

        public void ConsumeEnergy(float amount)
        {
            currentEnergy -= amount;
            if (currentEnergy < 0f) currentEnergy = 0f;
            UpdateFloatingHUD();
            OnEnergyChanged?.Invoke(this, -amount);
        }

        public void UseSkill(SkillData skill)
        {
            if (skill.skillType == SkillType.ULTIMATE)
            {
                ConsumeEnergy(100f); // Chiêu cuối reset năng lượng về 0
            }
            else
            {
                // Dùng kỹ năng thường/đặc biệt giúp hồi 5-15% năng lượng
                AddEnergy(skill.energyGenerated > 0 ? skill.energyGenerated : UnityEngine.Random.Range(5f, 15f));
            }

            if (skill.skillType == SkillType.SPECIAL)
            {
                specialSkillCDRemaining = skill.cooldown;
            }
        }

        public void StartTurnCDDecrement()
        {
            // Trừ CD của kỹ năng đặc biệt khi bắt đầu lượt đi
            if (specialSkillCDRemaining > 0)
            {
                specialSkillCDRemaining--;
            }
            // Hủy trạng thái Guard của lượt trước
            isGuarding = false;
        }

        public void ShowTurnIndicator(bool show)
        {
            if (turnIndicator != null)
            {
                turnIndicator.SetActive(show);
            }
        }

        private void Die()
        {
            isDead = true;
            OnDeath?.Invoke(this);
            PlayDeathAnimation();
        }

        #endregion

        #region Procedural Animations

        public void PlayAttackAnimation(Vector3 targetPosition, Action onImpact, Action onComplete)
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(CoAttackAnimation(targetPosition, onImpact, onComplete));
        }

        private IEnumerator CoAttackAnimation(Vector3 targetPosition, Action onImpact, Action onComplete)
        {
            Vector3 startPos = originalPosition;
            
            // 1. Dash tới mục tiêu (0.2s)
            float elapsed = 0f;
            float duration = 0.2f;
            Vector3 strikePos = targetPosition + (startPos - targetPosition).normalized * 1.2f; // Dừng lại trước mục tiêu 1.2m

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                modelRoot.position = Vector3.Lerp(startPos, strikePos, t * t); // Lerp mượt
                yield return null;
            }
            modelRoot.position = strikePos;

            // 2. Gây sát thương tại điểm va chạm
            onImpact?.Invoke();

            // 3. Đợi một chút tại điểm va chạm (0.15s)
            yield return new WaitForSeconds(0.15f);

            // 4. Lùi về vị trí cũ (0.3s)
            elapsed = 0f;
            duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                modelRoot.position = Vector3.Lerp(strikePos, startPos, t);
                yield return null;
            }
            modelRoot.position = startPos;
            activeAnimCoroutine = null;

            onComplete?.Invoke();
        }

        public void PlayHitShake()
        {
            if (activeAnimCoroutine == null) // Chỉ chạy shake nếu không đang dash tấn công
            {
                StartCoroutine(CoHitShake());
            }
        }

        private IEnumerator CoHitShake()
        {
            Vector3 startPos = originalPosition;
            float duration = 0.25f;
            float elapsed = 0f;
            float magnitude = 0.25f; // Lực rung lắc

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;
                
                // Giảm dần lực lắc theo thời gian
                float damper = 1.0f - percent;
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                // Knockback nhẹ về phía sau (Ally bị đẩy về -Z, Enemy đẩy về +Z)
                float zOffset = (isAlly ? -0.3f : 0.3f) * damper;

                modelRoot.position = new Vector3(startPos.x + x, startPos.y, startPos.z + zOffset);
                yield return null;
            }

            modelRoot.position = startPos;
        }

        private void PlayDeathAnimation()
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            StartCoroutine(CoDeathAnimation());
        }

        private IEnumerator CoDeathAnimation()
        {
            float duration = 1.0f;
            float elapsed = 0f;
            Vector3 startPos = modelRoot.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Xoay tròn và chìm xuống đất
                modelRoot.Rotate(Vector3.up, 360f * Time.deltaTime * 2);
                modelRoot.position = new Vector3(startPos.x, startPos.y - (t * 2f), startPos.z);
                
                // Mờ dần (nếu dùng shader mờ, ở đây ta thu nhỏ mô hình lại)
                modelRoot.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                yield return null;
            }
            
            gameObject.SetActive(false);
        }

        #endregion
    }

    /// <summary>
    /// Component hỗ trợ xoay HUD luôn hướng về phía Camera chính.
    /// </summary>
    public class BillboardHUD : MonoBehaviour
    {
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                 mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}
