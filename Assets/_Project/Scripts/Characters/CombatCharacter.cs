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
        
        private float _individualEnergy = 0f;
        public float currentEnergy
        {
            get
            {
                if (isAlly && CombatManager.Instance != null)
                {
                    return CombatManager.Instance.sharedEnergy;
                }
                return _individualEnergy;
            }
            set
            {
                if (isAlly && CombatManager.Instance != null)
                {
                    CombatManager.Instance.sharedEnergy = Mathf.Clamp(value, 0f, 100f);
                }
                else
                {
                    _individualEnergy = Mathf.Clamp(value, 0f, 100f);
                }
            }
        }
        
        public int specialSkillCDRemaining = 0;
        public bool isDead = false;
        public bool isGuarding = false;
        public bool isWaitingForQTE = false;

        [Header("Chỉ số Recollection")]
        public float recollectionGauge = 0f;
        public bool isCommander = false;

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
        private GameObject spawnedTurnVFX;

        // Events
        public event Action<CombatCharacter, float, bool> OnHPChanged; // character, delta, isCrit
        public event Action<CombatCharacter, float> OnEnergyChanged;
        public event Action<CombatCharacter> OnDeath;

        public void AddRecollectionGauge(float amount)
        {
            if (isDead || isCommander || characterData == null || !characterData.isRecollectionUnlocked) return;
            recollectionGauge = Mathf.Clamp(recollectionGauge + amount, 0f, 100f);
            UpdateFloatingHUD();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePartyPanel();
            }
        }

        public void SetOriginalPosition(Vector3 newPosition)
        {
            originalPosition = newPosition;
        }

        public void Initialize(CharacterData data, bool isAllySide)
        {
            characterData = data;
            isAlly = isAllySide;

            if (isAlly && characterData != null)
            {
                characterData.isRecollectionUnlocked = true;
            }

            maxHP = data.baseMaxHP;
            currentHP = maxHP;

            // Khởi tạo năng lượng ngẫu nhiên từ 30% đến 50% cho kẻ địch
            if (!isAlly)
            {
                currentEnergy = UnityEngine.Random.Range(30f, 50f);
            }
            recollectionGauge = UnityEngine.Random.Range(20f, 40f); // Tích sẵn 1 phần để người dùng dễ test
            isDead = false;
            isGuarding = false;
            specialSkillCDRemaining = 0;
            activeEffects.Clear();

            originalPosition = transform.position;

            // Tạo Mô hình procedural 3D dựa trên theme color nếu chưa có
            CreateProceduralModel();

            // Tạo Giao diện nổi trên đầu (chỉ hiển thị cho kẻ địch)
            if (!isAlly)
            {
                CreateFloatingHUD();
            }

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // Nạp các animation tùy chỉnh từ CharacterData thay cho Animator Controller gốc
                ApplyRuntimeAnimationOverrides(anim);

                // Thêm receiver để bắt sự kiện đòn đánh của hoạt ảnh và nuốt cảnh báo
                if (anim.gameObject.GetComponent<AnimationEventReceiver>() == null)
                {
                    anim.gameObject.AddComponent<AnimationEventReceiver>();
                }
                
                if (anim.runtimeAnimatorController != null && anim.layerCount > 0)
                {
                    if (anim.HasState(0, Animator.StringToHash("Idle")))
                    {
                        anim.Play("Idle");
                    }
                }
            }
        }

        private void ApplyRuntimeAnimationOverrides(Animator animator)
        {
            if (animator == null || characterData == null || animator.runtimeAnimatorController == null) return;

            RuntimeAnimatorController baseController = animator.runtimeAnimatorController;
            if (baseController is AnimatorOverrideController overrideCtrl)
            {
                baseController = overrideCtrl.runtimeAnimatorController;
            }

            AnimatorOverrideController newOverrideController = new AnimatorOverrideController(baseController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            var originalClips = newOverrideController.animationClips;

            foreach (var originalClip in originalClips)
            {
                if (originalClip == null) continue;
                string clipName = originalClip.name.ToLower();
                AnimationClip targetClip = null;

                if (clipName.Contains("idle") && characterData.idleClip != null) targetClip = characterData.idleClip;
                else if (clipName.Contains("run") && characterData.runClip != null) targetClip = characterData.runClip;
                else if ((clipName.Contains("attack1") || clipName.Contains("basic") || clipName.Contains("attack_1") || clipName.Contains("combo01")) && characterData.skillBasic != null && characterData.skillBasic.skillClip != null) targetClip = characterData.skillBasic.skillClip;
                else if ((clipName.Contains("attack2") || clipName.Contains("special") || clipName.Contains("attack_2") || clipName.Contains("combo02")) && characterData.skillSpecial != null && characterData.skillSpecial.skillClip != null) targetClip = characterData.skillSpecial.skillClip;
                else if ((clipName.Contains("ultimate") || clipName.Contains("ult") || clipName.Contains("skill03")) && characterData.skillUltimate != null && characterData.skillUltimate.skillClip != null) targetClip = characterData.skillUltimate.skillClip;
                else if ((clipName.Contains("defend") || clipName.Contains("guard") || clipName.Contains("block")) && characterData.defendClip != null) targetClip = characterData.defendClip;
                else if ((clipName.Contains("hit") || clipName.Contains("damage") || clipName.Contains("hurt")) && characterData.hitClip != null) targetClip = characterData.hitClip;
                else if ((clipName.Contains("die") || clipName.Contains("dead") || clipName.Contains("death")) && characterData.dieClip != null) targetClip = characterData.dieClip;

                if (targetClip != null)
                {
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, targetClip));
                }
            }

            if (overrides.Count > 0)
            {
                newOverrideController.ApplyOverrides(overrides);
                animator.runtimeAnimatorController = newOverrideController;
            }
        }

        private void CreateProceduralModel()
        {
            // 1. Kiểm tra xem người dùng đã gán sẵn modelRoot trong Inspector chưa
            if (modelRoot != null && modelRoot != transform)
            {
                Debug.Log($"[CombatCharacter] {characterData?.characterName ?? name} đã có sẵn modelRoot. Bỏ qua tạo model procedural.");
                originalPosition = transform.position;
                return;
            }

            // 2. Tìm xem có Animator nào có sẵn trong các gameobject con không (chứng tỏ có model 3D thực tế)
            Animator existingAnimator = GetComponentInChildren<Animator>();
            if (existingAnimator != null && existingAnimator.gameObject != gameObject)
            {
                Debug.Log($"[CombatCharacter] {characterData?.characterName ?? name} đã có sẵn model thật chứa Animator: {existingAnimator.gameObject.name}. Sử dụng model này.");
                modelRoot = existingAnimator.transform;
                originalPosition = transform.position;
                return;
            }

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

        private static Sprite cachedWhiteSprite;
        private Sprite GetDefaultWhiteSprite()
        {
            if (cachedWhiteSprite != null) return cachedWhiteSprite;
            Texture2D tex = new Texture2D(2, 2);
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();
            cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            return cachedWhiteSprite;
        }

        private void CreateFloatingHUD()
        {
            // Tạo Canvas thế giới (Không SetParent vào transform để tránh bị ảnh hưởng bởi scale bất thường của nhân vật/quái)
            GameObject canvasGO = new GameObject("FloatingHUD");
            canvasGO.transform.position = transform.position + new Vector3(0, 2.3f, 0);

            hudCanvas = canvasGO.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.WorldSpace;
            hudCanvas.worldCamera = Camera.main; // Gán camera để raycast chính xác
            canvasGO.AddComponent<GraphicRaycaster>(); // Bắt buộc phải có để click được nút World Space Canvas
            
            // Xóa CanvasScaler để tránh co giãn biến dạng UI do tỷ lệ khung hình màn hình thay đổi

            RectTransform rect = hudCanvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 40f);
            rect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Tỷ lệ chuẩn (100 pixel = 1 mét)
            rect.localRotation = Quaternion.identity;

            // Thêm script điều hướng theo nhân vật và billboard
            WorldSpaceHUD hudFollow = canvasGO.AddComponent<WorldSpaceHUD>();
            hudFollow.target = transform;
            hudFollow.offset = new Vector3(0f, 2.3f, 0f);

            // Tạo Panel nền cho HP Bar
            GameObject hpBgGO = new GameObject("HPBar_Bg");
            hpBgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform hpBgRect = hpBgGO.AddComponent<RectTransform>();
            hpBgRect.sizeDelta = new Vector2(120f, 12f);
            hpBgRect.localScale = Vector3.one;
            hpBgRect.anchoredPosition = new Vector2(0f, 10f);
            hpBgRect.localRotation = Quaternion.identity;
            Image hpBgImg = hpBgGO.AddComponent<Image>();
            hpBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Tạo HP Fill
            GameObject hpFillGO = new GameObject("HPBar_Fill");
            hpFillGO.transform.SetParent(hpBgGO.transform, false);
            RectTransform hpFillRect = hpFillGO.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpFillRect.localScale = Vector3.one;
            hpFillRect.localRotation = Quaternion.identity;
            
            hpBarFill = hpFillGO.AddComponent<Image>();
            hpBarFill.color = isAlly ? new Color(0.1f, 0.8f, 0.1f) : new Color(0.9f, 0.1f, 0.1f);

            // Tạo Energy Bar
            GameObject energyBgGO = new GameObject("EnergyBar_Bg");
            energyBgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform enBgRect = energyBgGO.AddComponent<RectTransform>();
            enBgRect.sizeDelta = new Vector2(120f, 8f);
            enBgRect.localScale = Vector3.one;
            enBgRect.anchoredPosition = new Vector2(0f, -2f);
            enBgRect.localRotation = Quaternion.identity;
            Image enBgImg = energyBgGO.AddComponent<Image>();
            enBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Tạo Energy Fill
            GameObject energyFillGO = new GameObject("EnergyBar_Fill");
            energyFillGO.transform.SetParent(energyBgGO.transform, false);
            RectTransform enFillRect = energyFillGO.AddComponent<RectTransform>();
            enFillRect.anchorMin = Vector2.zero;
            enFillRect.anchorMax = Vector2.one;
            enFillRect.offsetMin = Vector2.zero;
            enFillRect.offsetMax = Vector2.zero;
            enFillRect.localScale = Vector3.one;
            enFillRect.localRotation = Quaternion.identity;
            
            energyBarFill = energyFillGO.AddComponent<Image>();
            energyBarFill.color = new Color(0.1f, 0.6f, 0.9f);

            // Tạo Container chứa các buff/debuff
            GameObject buffsGO = new GameObject("BuffContainer");
            buffsGO.transform.SetParent(canvasGO.transform, false);
            RectTransform buffsRect = buffsGO.AddComponent<RectTransform>();
            buffsRect.sizeDelta = new Vector2(120f, 15f);
            buffsRect.localScale = Vector3.one;
            buffsRect.anchoredPosition = new Vector2(0f, -15f);
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
                float fillAmount = maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f;
                RectTransform rt = hpBarFill.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(fillAmount, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            if (energyBarFill != null)
            {
                float fillAmount = Mathf.Clamp01(currentEnergy / 100f);
                RectTransform rt = energyBarFill.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(fillAmount, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
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

                    // Đảm bảo raycastTarget được bật và gắn BuffTooltipTrigger để xem thông tin buff khi hover chuột
                    iconImg.raycastTarget = true;
                    BuffTooltipTrigger trigger = iconGO.AddComponent<BuffTooltipTrigger>();
                    trigger.Initialize(effect);
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

        public void TakeDamage(float damage, ElementType element, CombatCharacter attacker = null, bool isCrit = false)
        {
            if (isDead) return;

            float finalDamage = damage;

            // 1. Xử lý hiệu ứng Shield (nếu có)
            ActiveEffect shieldEffect = activeEffects.Find(e => e.data.effectType == EffectType.SHIELD);
            if (shieldEffect != null && shieldEffect.data.modifierValue > 0)
            {
                if (shieldEffect.data.modifierValue >= finalDamage)
                {
                    shieldEffect.data.modifierValue -= finalDamage;
                    finalDamage = 0f;
                }
                else
                {
                    finalDamage -= shieldEffect.data.modifierValue;
                    shieldEffect.data.modifierValue = 0f;
                    activeEffects.Remove(shieldEffect);
                }

                FloatingText.Instance.SpawnText(transform.position + Vector3.up * 1.8f, "SHIELD BLOCK!", Color.blue, 1.0f);
            }

            // 2. Trừ HP thực tế
            currentHP -= finalDamage;
            if (currentHP <= 0f)
            {
                currentHP = 0f;
                isDead = true;
            }

            // 3. Phản sát thương (Reflect) nếu có và không phải phản đòn của phản đòn
            if (attacker != null && attacker != this && !attacker.isDead && finalDamage > 0)
            {
                float totalReflectVal = 0f;
                ElementType reflectElement = ElementType.Physical;

                foreach (var effect in activeEffects)
                {
                    if (effect.data.effectType == EffectType.REFLECT)
                    {
                        totalReflectVal += effect.data.modifierValue;
                        if (effect.data.effectColor == Color.red) reflectElement = ElementType.Fire;
                        else if (effect.data.effectColor == Color.yellow) reflectElement = ElementType.Lightning;
                        else if (effect.data.effectColor == Color.cyan) reflectElement = ElementType.Ice;
                    }
                }

                if (totalReflectVal > 0)
                {
                    float reflectedDamage = finalDamage * totalReflectVal;
                    reflectedDamage = Mathf.Round(reflectedDamage);
                    if (reflectedDamage > 0)
                    {
                        attacker.TakeDamage(reflectedDamage, reflectElement, null, false);
                        FloatingText.Instance.SpawnText(attacker.transform.position + Vector3.up * 1.5f, $"{reflectedDamage} (Phản đòn!)", Color.magenta, 1.0f);
                    }
                }
            }

            // Bị đánh hồi năng lượng (+10-20%)
            float energyGained = UnityEngine.Random.Range(10f, 20f);
            AddEnergy(energyGained);

            // Bị đánh tích lũy Recollection Gauge (+12)
            AddRecollectionGauge(12f);

            UpdateFloatingHUD();
            OnHPChanged?.Invoke(this, -finalDamage, isCrit);

            // Animation bị đánh
            PlayHitShake();
            PlayHitAnimation();

            if (isDead)
            {
                // Khi đồng đội chết, tăng gauge cho các đồng đội còn sống khác
                if (isAlly && CombatManager.Instance != null)
                {
                    foreach (var ally in CombatManager.Instance.GetAliveAllies())
                    {
                        if (ally != this)
                        {
                            ally.AddRecollectionGauge(25f);
                            FloatingText.Instance.SpawnText(ally.transform.position + Vector3.up * 1.5f, "Ý chí thức tỉnh +25!", Color.magenta, 1.0f);
                        }
                    }
                }

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

            if (isAlly && CombatManager.Instance != null)
            {
                CombatManager.Instance.AddSharedEnergy(amount);
            }
            else
            {
                _individualEnergy = Mathf.Clamp(_individualEnergy + amount, 0f, 100f);
                UpdateFloatingHUD();
            }
            OnEnergyChanged?.Invoke(this, amount);
        }

        public void ConsumeEnergy(float amount)
        {
            if (isAlly && CombatManager.Instance != null)
            {
                CombatManager.Instance.ConsumeSharedEnergy(amount);
            }
            else
            {
                _individualEnergy = Mathf.Clamp(_individualEnergy - amount, 0f, 100f);
                UpdateFloatingHUD();
            }
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
                AddRecollectionGauge(15f); // Special: +15
            }
            else if (skill.skillType == SkillType.BASIC)
            {
                AddRecollectionGauge(8f); // Basic: +8
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

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
            {
                if (anim.HasState(0, Animator.StringToHash("Idle")))
                {
                    anim.CrossFadeInFixedTime("Idle", 0.15f);
                }
            }
        }

        public void ShowTurnIndicator(bool show)
        {
            if (turnIndicator != null)
            {
                turnIndicator.SetActive(show);
            }

            if (show)
            {
                if (characterData != null && characterData.turnVFXPrefab != null && spawnedTurnVFX == null)
                {
                    spawnedTurnVFX = Instantiate(characterData.turnVFXPrefab, transform.position, Quaternion.identity, transform);
                }
            }
            else
            {
                HideTurnVFX();
            }
        }

        public void HideTurnVFX()
        {
            if (spawnedTurnVFX != null)
            {
                Destroy(spawnedTurnVFX);
                spawnedTurnVFX = null;
            }
        }

        private void Die()
        {
            isDead = true;
            HideTurnVFX();
            OnDeath?.Invoke(this);
            PlayDeathAnimation();
        }
        #endregion

        #region Procedural Animations

        public void PlayHitAnimation()
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
            {
                if (anim.HasState(0, Animator.StringToHash("Hit")))
                {
                    anim.CrossFadeInFixedTime("Hit", 0.1f);
                    StartCoroutine(CoReturnToIdleAfterHit(0.5f));
                }
            }
        }

        private IEnumerator CoReturnToIdleAfterHit(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!isDead && activeAnimCoroutine == null)
            {
                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
                {
                    string endState = isGuarding ? "Defend" : "Idle";
                    if (anim.HasState(0, Animator.StringToHash(endState)))
                    {
                        anim.CrossFadeInFixedTime(endState, 0.15f);
                    }
                }
            }
        }

        public void PlayAttackAnimation(Vector3 targetPosition, Action onImpact, Action onComplete)
        {
            PlayAttackAnimation(targetPosition, null, onImpact, onComplete);
        }

        public void PlayAttackAnimation(Vector3 targetPosition, SkillData skill, Action onImpact, Action onComplete)
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(CoAttackAnimation(targetPosition, skill, onImpact, onComplete));
        }

        private Action pendingImpactCallback;
        private bool hasReceivedHitEvent = false;

        public void OnAnimationHitEventReceived()
        {
            if (pendingImpactCallback != null && !hasReceivedHitEvent)
            {
                hasReceivedHitEvent = true;
                var callback = pendingImpactCallback;
                pendingImpactCallback = null;
                callback?.Invoke();
            }
        }

        private IEnumerator CoAttackAnimation(Vector3 targetPosition, SkillData skill, Action onImpact, Action onComplete)
        {
            Vector3 startPos = originalPosition;
            bool isRanged = skill != null && skill.rangeType == SkillRangeType.RANGED;
            bool isProjectile = isRanged && skill != null && skill.rangedVfxType == RangedVfxType.PROJECTILE && skill.projectileVFX != null;
            bool isProjectileFlying = false;
            
            Animator anim = GetComponentInChildren<Animator>();

            if (!isRanged)
            {
                if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
                {
                    if (anim.HasState(0, Animator.StringToHash("Run")))
                    {
                        anim.CrossFadeInFixedTime("Run", 0.1f);
                    }
                }

                // 1. Dash tới mục tiêu (0.35s)
                float elapsed = 0f;
                float duration = 0.35f;
                Vector3 strikePos = targetPosition + (startPos - targetPosition).normalized * 1.2f; // Dừng lại trước mục tiêu 1.2m

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    transform.position = Vector3.Lerp(startPos, strikePos, t * t); // Lerp mượt toàn bộ nhân vật
                    yield return null;
                }
                transform.position = strikePos;
            }
            else
            {
                // Ranged: Không di chuyển
                transform.position = startPos;
            }

            // Thiết lập callback nhận sự kiện đòn đánh
            hasReceivedHitEvent = false;
            if (isProjectile)
            {
                isProjectileFlying = true;
                pendingImpactCallback = () => {
                    StartCoroutine(CoFlyProjectile(startPos, targetPosition, skill.projectileVFX, onImpact, () => {
                        isProjectileFlying = false;
                    }));
                };
            }
            else
            {
                pendingImpactCallback = onImpact;
            }

            // 2. Chạy hoạt ảnh tấn công
            if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
            {
                string stateName = "Attack1";
                if (skill != null)
                {
                    if (skill.skillType == SkillType.SPECIAL) stateName = "Attack2";
                    else if (skill.skillType == SkillType.ULTIMATE) stateName = "Ultimate";
                }
                if (anim.HasState(0, Animator.StringToHash(stateName)))
                {
                    anim.CrossFadeInFixedTime(stateName, 0.1f);
                }
            }

            // Đợi một chút để chuyển cảnh hoạt ảnh kết thúc
            yield return new WaitForSeconds(0.1f);

            // Xác định thời lượng clip hoạt ảnh thực tế
            float animLength = 0.5f; // Fallback mặc định
            if (anim != null)
            {
                var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                animLength = stateInfo.length;
            }

            // Chờ nhận sự kiện OnAttackHit từ AnimationEventReceiver hoặc tự động kích hoạt sau một thời gian
            float elapsedWait = 0f;
            float hitThreshold = animLength * 0.5f; // Gây sát thương ở 50% thời lượng nếu không có event
            
            while (!hasReceivedHitEvent && elapsedWait < animLength)
            {
                elapsedWait += Time.deltaTime;
                if (elapsedWait >= hitThreshold && !hasReceivedHitEvent)
                {
                    OnAnimationHitEventReceived();
                }
                yield return null;
            }

            // Đảm bảo đòn đánh đã kích hoạt sát thương
            if (!hasReceivedHitEvent)
            {
                OnAnimationHitEventReceived();
            }

            // Chờ Parry QTE giải quyết xong (nếu có) trước khi lùi về
            while (isWaitingForQTE)
            {
                yield return null;
            }

            // Đợi cho đến khi hoạt ảnh chạy hết hoàn toàn trước khi lùi về
            if (elapsedWait < animLength)
            {
                yield return new WaitForSeconds(animLength - elapsedWait);
            }

            if (!isRanged)
            {
                // 4. Lùi về vị trí cũ (0.3s)
                if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
                {
                    if (anim.HasState(0, Animator.StringToHash("Run")))
                    {
                        anim.CrossFadeInFixedTime("Run", 0.1f);
                    }
                }

                float elapsed = 0f;
                float duration = 0.25f;
                Vector3 strikePos = transform.position;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    transform.position = Vector3.Lerp(strikePos, startPos, t);
                    yield return null;
                }
            }

            transform.position = startPos;

            if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
            {
                string endState = isGuarding ? "Defend" : "Idle";
                if (anim.HasState(0, Animator.StringToHash(endState)))
                {
                    anim.CrossFadeInFixedTime(endState, 0.15f);
                }
            }

            // Chờ đạn bay tới đích (nếu có projectile)
            while (isProjectileFlying)
            {
                yield return null;
            }

            activeAnimCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator CoFlyProjectile(Vector3 startPos, Vector3 targetPos, GameObject projectilePrefab, Action onImpact, Action onArrival)
        {
            Vector3 spawnPos = startPos + Vector3.up * 1.0f;
            Vector3 destination = targetPos + Vector3.up * 1.0f;

            Quaternion rotation = Quaternion.identity;
            Vector3 direction = destination - spawnPos;
            if (direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(direction);
            }

            GameObject projectileInstance = Instantiate(projectilePrefab, spawnPos, rotation);

            float speed = 15f; 
            float distance = Vector3.Distance(spawnPos, destination);
            float duration = distance / speed;
            if (duration < 0.1f) duration = 0.1f; 

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (projectileInstance != null)
                {
                    projectileInstance.transform.position = Vector3.Lerp(spawnPos, destination, t);
                }
                yield return null;
            }

            if (projectileInstance != null)
            {
                var renderers = projectileInstance.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers) r.enabled = false;
                var colliders = projectileInstance.GetComponentsInChildren<Collider>();
                foreach (var c in colliders) c.enabled = false;
                var particles = projectileInstance.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particles)
                {
                    var emission = ps.emission;
                    emission.enabled = false;
                }
                Destroy(projectileInstance, 1.5f);
            }

            onImpact?.Invoke();
            onArrival?.Invoke();
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

                transform.position = new Vector3(startPos.x + x, startPos.y, startPos.z + zOffset);
                yield return null;
            }

            transform.position = startPos;
        }

        private void PlayDeathAnimation()
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            StartCoroutine(CoDeathAnimation());
        }

        private IEnumerator CoDeathAnimation()
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null && anim.layerCount > 0)
            {
                if (anim.HasState(0, Animator.StringToHash("Die")))
                {
                    anim.CrossFadeInFixedTime("Die", 0.15f);
                    yield return new WaitForSeconds(1.5f);
                }
            }

            float duration = 1.0f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Xoay tròn và chìm xuống đất
                transform.Rotate(Vector3.up, 360f * Time.deltaTime * 2);
                transform.position = new Vector3(startPos.x, startPos.y - (t * 2f), startPos.z);
                
                // Mờ dần (thu nhỏ toàn bộ gameobject nhân vật)
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                yield return null;
            }
            
            gameObject.SetActive(false);
        }

        #endregion
    }

    /// <summary>
    /// Component hỗ trợ di chuyển HUD theo mục tiêu và xoay luôn hướng về phía Camera chính (Billboard)
    /// độc lập hoàn toàn với scale và rotation của mục tiêu đó để tránh bị méo/co giãn UI.
    /// </summary>
    public class WorldSpaceHUD : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 2.3f, 0);
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            // Đồng bộ trạng thái active/deactive với target
            if (!target.gameObject.activeInHierarchy)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            else
            {
                if (!gameObject.activeSelf) gameObject.SetActive(true);
            }

            // Đồng bộ vị trí
            transform.position = target.position + offset;

            // Xoay HUD về phía Camera (Billboard)
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                 mainCamera.transform.rotation * Vector3.up);
            }
        }
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

    /// <summary>
    /// Receiver lắng nghe và tiêu thụ các AnimationEvent (như OnAttackHit) của các animation pack (Blink, Suriyun,...)
    /// nhằm tránh lỗi cảnh báo "no receiver" và đồng bộ hóa đòn đánh với thời điểm va chạm chuẩn xác.
    /// </summary>
    public class AnimationEventReceiver : MonoBehaviour
    {
        private CombatCharacter character;

        void Start()
        {
            character = GetComponentInParent<CombatCharacter>();
        }

        public void OnAttackHit() => ForwardHit();
        public void OnAttackHitEvent() => ForwardHit();
        public void OnHit() => ForwardHit();
        public void OnDefaultHit() => ForwardHit();
        public void Hit() => ForwardHit();

        private void ForwardHit()
        {
            if (character != null)
            {
                character.OnAnimationHitEventReceived();
            }
        }
    }
}
