using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RPG.Combat
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Canvas overlayCanvas;

        [Header("UI Panels")]
        private Transform turnQueuePanel;
        private Transform partyPanel;
        private Transform actionPanel;
        private Transform descriptionPanel;
        private Transform endScreenPanel;
        private Transform targetSelectionPanel;
        private Transform recollectionBannerPanel;

        private Transform tooltipPanel;
        private Text tooltipText;
        private static Sprite cachedWhiteSprite;

        private Text descriptionText;
        private Text endScreenText;

        // Lưu trữ các nút kỹ năng để bật/tắt
        private Button basicButton;
        private Button specialButton;
        private Button ultimateButton;
        private Button defendButton;

        private Text basicText;
        private Text specialText;
        private Text ultimateText;

        // Lưu trữ các nút Ultimate của Party để nhấp nháy khi đầy 100%
        private Dictionary<CombatCharacter, Button> partyUltButtons = new Dictionary<CombatCharacter, Button>();

        // Lưu trữ nút Restart của Màn hình kết thúc để đổi tên động
        private Button restartButton;
        private Text restartButtonText;

        // Kỹ năng hiện đang chọn chờ bấm mục tiêu
        private SkillData selectedSkill;
        private CombatCharacter currentCaster;
        private List<CombatCharacter> activeTargetsList = new List<CombatCharacter>();

        private bool eventsRegistered = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                CreateProceduralUI();
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            RegisterEvents();
        }

        private void Start()
        {
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            if (eventsRegistered) return;
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnTurnStart += HandleTurnStart;
                CombatManager.Instance.OnCombatStarted += HandleCombatStarted;
                CombatManager.Instance.OnCombatEnd += HandleCombatEnd;

                if (RecollectionManager.Instance != null)
                {
                    RecollectionManager.Instance.OnRecollectionActivated -= HandleRecollectionActivated;
                    RecollectionManager.Instance.OnRecollectionActivated += HandleRecollectionActivated;
                }

                eventsRegistered = true;
                Debug.Log("[UIManager] Đã đăng ký sự kiện thành công từ CombatManager & RecollectionManager.");
            }
        }

        private void HandleCombatStarted()
        {
            UpdateTurnQueueHUD();
            UpdatePartyPanel();
            HideActionPanel();

            if (CombatManager.Instance != null)
            {
                foreach (var ally in CombatManager.Instance.allies)
                {
                    if (ally != null)
                    {
                        ally.OnHPChanged -= HandleAllyHPChanged;
                        ally.OnHPChanged += HandleAllyHPChanged;
                        ally.OnEnergyChanged -= HandleAllyEnergyChanged;
                        ally.OnEnergyChanged += HandleAllyEnergyChanged;
                    }
                }
            }

            if (RecollectionManager.Instance != null)
            {
                RecollectionManager.Instance.OnRecollectionActivated -= HandleRecollectionActivated;
                RecollectionManager.Instance.OnRecollectionActivated += HandleRecollectionActivated;
            }
        }

        private void HandleAllyHPChanged(CombatCharacter character, float delta, bool isCrit)
        {
            UpdatePartyPanel();
        }

        private void HandleAllyEnergyChanged(CombatCharacter character, float delta)
        {
            UpdatePartyPanel();
        }

        private void HandleCombatEnd(bool isWin)
        {
            if (CombatManager.Instance != null)
            {
                foreach (var ally in CombatManager.Instance.allies)
                {
                    if (ally != null)
                    {
                        ally.OnHPChanged -= HandleAllyHPChanged;
                        ally.OnEnergyChanged -= HandleAllyEnergyChanged;
                    }
                }
            }
        }

        private void HandleTurnStart(CombatCharacter activeChar)
        {
            UpdateTurnQueueHUD();
            UpdatePartyPanel();

            // Ẩn tất cả vòng tròn chọn mục tiêu
            HideAllTargetSelectors();
        }

        #region Xây dựng UI Procedural hoàn toàn bằng C#

        private void CreateProceduralUI()
        {
            // 0. Tạo EventSystem nếu chưa có để nhận diện click chuột
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                DontDestroyOnLoad(eventSystemGO);
                Debug.Log("[UIManager] Đã khởi tạo EventSystem.");
            }

            // Thử tải UI từ Prefab
            GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/CombatUI");
            if (uiPrefab != null)
            {
                GameObject canvasInstance = Instantiate(uiPrefab);
                overlayCanvas = canvasInstance.GetComponent<Canvas>();
                
                CombatUIReferences refs = canvasInstance.GetComponent<CombatUIReferences>();
                if (refs != null)
                {
                    turnQueuePanel = refs.turnQueuePanel;
                    partyPanel = refs.partyPanel;
                    actionPanel = refs.actionPanel;
                    descriptionPanel = refs.descriptionPanel;
                    targetSelectionPanel = refs.targetSelectionPanel;
                    endScreenPanel = refs.endScreenPanel;
                    recollectionBannerPanel = refs.recollectionBannerPanel;

                    descriptionText = refs.descriptionText;
                    endScreenText = refs.endScreenText;

                    basicButton = refs.basicButton;
                    specialButton = refs.specialButton;
                    ultimateButton = refs.ultimateButton;
                    defendButton = refs.defendButton;

                    basicText = refs.basicText;
                    specialText = refs.specialText;
                    ultimateText = refs.ultimateText;

                    // Gán các sự kiện click nút
                    basicButton.onClick.RemoveAllListeners();
                    basicButton.onClick.AddListener(() => OnSkillButtonClicked(SkillType.BASIC));

                    specialButton.onClick.RemoveAllListeners();
                    specialButton.onClick.AddListener(() => OnSkillButtonClicked(SkillType.SPECIAL));

                    ultimateButton.onClick.RemoveAllListeners();
                    ultimateButton.onClick.AddListener(() => OnSkillButtonClicked(SkillType.ULTIMATE));

                    defendButton.onClick.RemoveAllListeners();
                    defendButton.onClick.AddListener(() => OnGuardButtonClicked());

                    if (refs.restartButton != null)
                    {
                        restartButton = refs.restartButton;
                        restartButtonText = restartButton.GetComponentInChildren<Text>();
                        
                        restartButton.onClick.RemoveAllListeners();
                        restartButton.onClick.AddListener(OnRestartButtonClicked);
                    }

                    Debug.Log("[UIManager] Đã tải thành công giao diện từ Prefab!");
                    return;
                }
            }

            Debug.LogWarning("[UIManager] Không tìm thấy Prefab UI. Tạo UI động bằng code...");

            // 1. Tạo Canvas chính
            GameObject canvasGO = new GameObject("CombatUI_OverlayCanvas");
            overlayCanvas = canvasGO.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Tạo Banner dynamic fallback
            CreateProceduralRecollectionBanner();

            // 2. Tạo Bảng hàng chờ Turn Queue (Top Center)
            GameObject turnPanelGO = new GameObject("TurnQueuePanel");
            turnPanelGO.transform.SetParent(overlayCanvas.transform);
            RectTransform turnRect = turnPanelGO.AddComponent<RectTransform>();
            turnRect.anchorMin = new Vector2(0.5f, 1f);
            turnRect.anchorMax = new Vector2(0.5f, 1f);
            turnRect.pivot = new Vector2(0.5f, 1f);
            turnRect.anchoredPosition = new Vector2(0f, -20f);
            turnRect.sizeDelta = new Vector2(600f, 60f);
            turnRect.localScale = Vector3.one;

            Image turnImg = turnPanelGO.AddComponent<Image>();
            turnImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            HorizontalLayoutGroup turnLayout = turnPanelGO.AddComponent<HorizontalLayoutGroup>();
            turnLayout.childAlignment = TextAnchor.MiddleCenter;
            turnLayout.spacing = 10f;
            turnLayout.childControlHeight = false; // Tắt điều khiển chiều cao của con
            turnLayout.childControlWidth = false;  // Tắt điều khiển chiều rộng của con
            turnQueuePanel = turnPanelGO.transform;

            // 3. Tạo Bảng Đội Hình Party Panel (Bottom Left)
            GameObject partyPanelGO = new GameObject("PartyPanel");
            partyPanelGO.transform.SetParent(overlayCanvas.transform);
            RectTransform partyRect = partyPanelGO.AddComponent<RectTransform>();
            partyRect.anchorMin = new Vector2(0f, 0f);
            partyRect.anchorMax = new Vector2(0f, 0f);
            partyRect.pivot = new Vector2(0f, 0f);
            partyRect.anchoredPosition = new Vector2(20f, 20f);
            partyRect.sizeDelta = new Vector2(500f, 180f);
            partyRect.localScale = Vector3.one;

            Image partyImg = partyPanelGO.AddComponent<Image>();
            partyImg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            HorizontalLayoutGroup partyLayout = partyPanelGO.AddComponent<HorizontalLayoutGroup>();
            partyLayout.childAlignment = TextAnchor.MiddleLeft;
            partyLayout.spacing = 15f;
            partyLayout.padding = new RectOffset(10, 10, 10, 10);
            partyLayout.childControlHeight = false;
            partyLayout.childControlWidth = false;
            partyPanel = partyPanelGO.transform;

            // 4. Tạo Bảng kỹ năng Action Panel (Bottom Right)
            GameObject actionPanelGO = new GameObject("ActionPanel");
            actionPanelGO.transform.SetParent(overlayCanvas.transform);
            RectTransform actionRect = actionPanelGO.AddComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(1f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0f);
            actionRect.pivot = new Vector2(1f, 0f);
            actionRect.anchoredPosition = new Vector2(-20f, 20f);
            actionRect.sizeDelta = new Vector2(240f, 180f);
            actionRect.localScale = Vector3.one;

            Image actionImg = actionPanelGO.AddComponent<Image>();
            actionImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            VerticalLayoutGroup actionLayout = actionPanelGO.AddComponent<VerticalLayoutGroup>();
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.spacing = 8f;
            actionLayout.padding = new RectOffset(10, 10, 10, 10);
            actionLayout.childControlHeight = false;
            actionLayout.childControlWidth = false;
            actionPanel = actionPanelGO.transform;

            // Nút Basic Attack
            basicButton = CreateUIButton(actionPanel, "Tấn Công Thường (Basic)", () => OnSkillButtonClicked(SkillType.BASIC));
            basicText = basicButton.GetComponentInChildren<Text>();

            // Nút Special Skill
            specialButton = CreateUIButton(actionPanel, "Kỹ Năng Đặc Biệt (Special)", () => OnSkillButtonClicked(SkillType.SPECIAL));
            specialText = specialButton.GetComponentInChildren<Text>();

            // Nút Ultimate Skill
            ultimateButton = CreateUIButton(actionPanel, "Chiêu Cuối (Ultimate)", () => OnSkillButtonClicked(SkillType.ULTIMATE));
            ultimateText = ultimateButton.GetComponentInChildren<Text>();

            // Nút Guard/Defend
            defendButton = CreateUIButton(actionPanel, "Phòng Thủ (Guard)", () => OnGuardButtonClicked());

            // 4.5 Tạo Bảng Chọn Mục Tiêu (Target Selection Panel) nằm bên trái Action Panel
            GameObject targetPanelGO = new GameObject("TargetSelectionPanel");
            targetPanelGO.transform.SetParent(overlayCanvas.transform);
            RectTransform targetRect = targetPanelGO.AddComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(1f, 0f);
            targetRect.anchorMax = new Vector2(1f, 0f);
            targetRect.pivot = new Vector2(1f, 0f);
            targetRect.anchoredPosition = new Vector2(-280f, 20f);
            targetRect.sizeDelta = new Vector2(240f, 240f);
            targetRect.localScale = Vector3.one;

            Image targetImg = targetPanelGO.AddComponent<Image>();
            targetImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

            VerticalLayoutGroup targetLayout = targetPanelGO.AddComponent<VerticalLayoutGroup>();
            targetLayout.childAlignment = TextAnchor.MiddleCenter;
            targetLayout.spacing = 6f;
            targetLayout.padding = new RectOffset(10, 10, 10, 10);
            targetLayout.childControlHeight = false;
            targetLayout.childControlWidth = false;
            targetSelectionPanel = targetPanelGO.transform;
            targetSelectionPanel.gameObject.SetActive(false);

            // 5. Tạo Bảng mô tả kỹ năng (Description Panel) nằm phía trên Action Panel
            GameObject descPanelGO = new GameObject("DescriptionPanel");
            descPanelGO.transform.SetParent(overlayCanvas.transform);
            RectTransform descRect = descPanelGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(1f, 0f);
            descRect.anchorMax = new Vector2(1f, 0f);
            descRect.pivot = new Vector2(1f, 0f);
            descRect.anchoredPosition = new Vector2(-20f, 190f);
            descRect.sizeDelta = new Vector2(320f, 100f);

            Image descImg = descPanelGO.AddComponent<Image>();
            descImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            GameObject descTextGO = new GameObject("DescriptionText");
            descTextGO.transform.SetParent(descPanelGO.transform);
            RectTransform dtRect = descTextGO.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.sizeDelta = Vector2.zero;
            dtRect.offsetMin = new Vector2(10, 10);
            dtRect.offsetMax = new Vector2(-10, -10);

            descriptionText = descTextGO.AddComponent<Text>();
            descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descriptionText.fontSize = 14;
            descriptionText.color = Color.white;
            descriptionText.text = "Chọn kỹ năng để xem thông tin...";
            descriptionPanel = descPanelGO.transform;

            // 6. Tạo Màn hình kết thúc End Screen (Ẩn mặc định)
            GameObject endGO = new GameObject("EndScreenPanel");
            endGO.transform.SetParent(overlayCanvas.transform);
            RectTransform endRect = endGO.AddComponent<RectTransform>();
            endRect.anchorMin = Vector2.zero;
            endRect.anchorMax = Vector2.one;
            endRect.sizeDelta = Vector2.zero;

            Image endImg = endGO.AddComponent<Image>();
            endImg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

            // Text kết quả
            GameObject endTextGO = new GameObject("EndText");
            endTextGO.transform.SetParent(endGO.transform);
            RectTransform etRect = endTextGO.AddComponent<RectTransform>();
            etRect.anchorMin = new Vector2(0.5f, 0.6f);
            etRect.anchorMax = new Vector2(0.5f, 0.6f);
            etRect.pivot = new Vector2(0.5f, 0.5f);
            etRect.sizeDelta = new Vector2(500f, 80f);

            endScreenText = endTextGO.AddComponent<Text>();
            endScreenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            endScreenText.fontSize = 42;
            endScreenText.alignment = TextAnchor.MiddleCenter;
            endScreenText.color = Color.yellow;
            endScreenText.text = "CHIẾN THẮNG!";

            // Thêm viền
            Outline eo = endTextGO.AddComponent<Outline>();
            eo.effectColor = Color.black;
            eo.effectDistance = new Vector2(2f, -2f);

            // Nút chơi lại
            restartButton = CreateUIButton(endGO.transform, "Chơi Trận Mới", OnRestartButtonClicked);
            restartButtonText = restartButton.GetComponentInChildren<Text>();
            RectTransform resRect = restartButton.GetComponent<RectTransform>();
            resRect.anchorMin = new Vector2(0.5f, 0.4f);
            resRect.anchorMax = new Vector2(0.5f, 0.4f);
            resRect.pivot = new Vector2(0.5f, 0.5f);
            resRect.anchoredPosition = Vector2.zero;
            resRect.sizeDelta = new Vector2(200f, 50f);

            endScreenPanel = endGO.transform;
            endScreenPanel.gameObject.SetActive(false);

            // 7. Tạo Bảng Tooltip (Buff/Debuff Tooltip) - Ẩn mặc định
            GameObject tooltipGO = new GameObject("BuffTooltipPanel");
            tooltipGO.transform.SetParent(overlayCanvas.transform);
            RectTransform toolRect = tooltipGO.AddComponent<RectTransform>();
            toolRect.anchorMin = Vector2.zero;
            toolRect.anchorMax = Vector2.zero;
            toolRect.pivot = new Vector2(0f, 0f); // Pivot góc dưới trái
            toolRect.sizeDelta = new Vector2(200f, 65f);
            toolRect.localScale = Vector3.one;

            Image toolImg = tooltipGO.AddComponent<Image>();
            toolImg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            
            // Thêm viền
            Outline toolOutline = tooltipGO.AddComponent<Outline>();
            toolOutline.effectColor = Color.yellow;
            toolOutline.effectDistance = new Vector2(1f, -1f);

            GameObject toolTextGO = new GameObject("TooltipText");
            toolTextGO.transform.SetParent(tooltipGO.transform);
            RectTransform ttRect = toolTextGO.AddComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.offsetMin = new Vector2(8, 8);
            ttRect.offsetMax = new Vector2(-8, -8);

            tooltipText = toolTextGO.AddComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tooltipText.fontSize = 11;
            tooltipText.color = Color.white;
            tooltipText.alignment = TextAnchor.UpperLeft;
            tooltipText.text = "";

            tooltipPanel = tooltipGO.transform;
            tooltipPanel.gameObject.SetActive(false);
        }

        private Button CreateUIButton(Transform parent, string labelText, UnityEngine.Events.UnityAction onClickAction)
        {
            GameObject btnGO = new GameObject("Button_" + labelText);
            btnGO.transform.SetParent(parent);
            
            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(210f, 32f);
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(onClickAction);

            // Thêm Text nhãn
            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform);
            RectTransform txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            txtRect.localPosition = Vector3.zero;
            txtRect.localRotation = Quaternion.identity;
            txtRect.localScale = Vector3.one;

            Text txt = txtGO.AddComponent<Text>();
            txt.text = labelText;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            // Thêm hiệu ứng di chuột (hover) để thay đổi màu sắc
            var colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f);
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            btn.colors = colors;

            return btn;
        }

        #endregion

        #region Cập nhật thông tin lên UI

        /// <summary>
        /// Vẽ HUD Turn Queue ở trên cùng màn hình.
        /// </summary>
        public void UpdateTurnQueueHUD()
        {
            if (turnQueuePanel == null || CombatManager.Instance == null) return;

            // Xóa các nhãn cũ
            foreach (Transform child in turnQueuePanel)
            {
                Destroy(child.gameObject);
            }

            // Lấy danh sách hàng chờ hành động đã được sắp xếp
            List<CombatCharacter> sorted = CombatManager.Instance.turnQueue.GetSortedQueue();

            // Chỉ hiển thị tối đa 6 nhân vật tiếp theo
            int displayCount = Mathf.Min(sorted.Count, 6);
            for (int i = 0; i < displayCount; i++)
            {
                CombatCharacter character = sorted[i];
                float remainingAV = CombatManager.Instance.turnQueue.GetRemainingAV(character);

                GameObject cardGO = new GameObject("TurnCard");
                cardGO.transform.SetParent(turnQueuePanel);
                RectTransform cardRect = cardGO.AddComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(80f, 50f);
                cardRect.localScale = Vector3.one;
                cardRect.localPosition = Vector3.zero;
                cardRect.localRotation = Quaternion.identity;

                Image img = cardGO.AddComponent<Image>();
                img.color = character.isAlly ? new Color(0.1f, 0.3f, 0.6f, 0.8f) : new Color(0.7f, 0.1f, 0.1f, 0.8f);

                // Outline làm nổi card của nhân vật đang trong lượt
                if (character == CombatManager.Instance.activeCharacter && CombatManager.Instance.currentState != CombatState.BUSY)
                {
                    Outline outline = cardGO.AddComponent<Outline>();
                    outline.effectColor = Color.yellow;
                    outline.effectDistance = new Vector2(2f, -2f);
                }

                GameObject nameGO = new GameObject("NameText");
                nameGO.transform.SetParent(cardGO.transform);
                RectTransform nRect = nameGO.AddComponent<RectTransform>();
                nRect.anchorMin = new Vector2(0f, 0.4f);
                nRect.anchorMax = new Vector2(1f, 1f);
                nRect.offsetMin = Vector2.zero;
                nRect.offsetMax = Vector2.zero;
                nRect.localPosition = Vector3.zero;
                nRect.localScale = Vector3.one;
                nRect.localRotation = Quaternion.identity;

                Text nTxt = nameGO.AddComponent<Text>();
                nTxt.text = character.characterData.characterName;
                nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nTxt.fontSize = 10;
                nTxt.alignment = TextAnchor.MiddleCenter;
                nTxt.color = Color.white;

                GameObject avGO = new GameObject("AVText");
                avGO.transform.SetParent(cardGO.transform);
                RectTransform aRect = avGO.AddComponent<RectTransform>();
                aRect.anchorMin = new Vector2(0f, 0f);
                aRect.anchorMax = new Vector2(1f, 0.4f);
                aRect.offsetMin = Vector2.zero;
                aRect.offsetMax = Vector2.zero;
                aRect.localPosition = Vector3.zero;
                aRect.localScale = Vector3.one;
                aRect.localRotation = Quaternion.identity;

                Text aTxt = avGO.AddComponent<Text>();
                aTxt.text = $"AV: {remainingAV:F0}";
                aTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                aTxt.fontSize = 9;
                aTxt.alignment = TextAnchor.MiddleCenter;
                aTxt.color = Color.yellow;
            }
        }

        /// <summary>
        /// Vẽ thông tin 4 đồng minh ở góc dưới trái.
        /// </summary>
        public void UpdatePartyPanel()
        {
            if (partyPanel == null || CombatManager.Instance == null) return;

            // Xóa cũ
            foreach (Transform child in partyPanel)
            {
                Destroy(child.gameObject);
            }
            partyUltButtons.Clear();

            // Vẽ thông số từng ally
            foreach (var ally in CombatManager.Instance.allies)
            {
                GameObject cardGO = new GameObject("PartyCard_" + ally.characterData.characterName);
                cardGO.transform.SetParent(partyPanel);
                RectTransform cardRect = cardGO.AddComponent<RectTransform>();
                // Tăng sizeDelta chiều cao lên để chứa thêm thông tin Recollection và Buffs
                cardRect.sizeDelta = new Vector2(115f, 175f);
                cardRect.localScale = Vector3.one;
                cardRect.localPosition = Vector3.zero;
                cardRect.localRotation = Quaternion.identity;

                Image img = cardGO.AddComponent<Image>();
                
                // Đổi màu nền card nếu là Commander
                if (ally.isCommander)
                {
                    Color elemCol = CombatManager.Instance.GetElementColor(ally.characterData.element);
                    elemCol.a = 0.5f;
                    img.color = elemCol;
                }
                else
                {
                    img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }

                VerticalLayoutGroup group = cardGO.AddComponent<VerticalLayoutGroup>();
                group.childAlignment = TextAnchor.UpperLeft;
                group.padding = new RectOffset(8, 8, 8, 8);
                group.spacing = 3f;
                group.childControlHeight = false;
                group.childControlWidth = false;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = false;

                // Tên nhân vật
                GameObject nameGO = new GameObject("NameText");
                nameGO.transform.SetParent(cardGO.transform);
                RectTransform nameRect = nameGO.AddComponent<RectTransform>();
                nameRect.sizeDelta = new Vector2(99f, 16f);
                nameRect.localScale = Vector3.one;
                Text nTxt = nameGO.AddComponent<Text>();
                nTxt.raycastTarget = false;
                nTxt.text = ally.characterData.characterName;
                nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nTxt.fontSize = 11;
                nTxt.fontStyle = FontStyle.Bold;
                nTxt.color = ally.isDead ? Color.gray : Color.white;

                // Vai trò nhân vật (Role)
                GameObject roleGO = new GameObject("RoleText");
                roleGO.transform.SetParent(cardGO.transform);
                RectTransform roleRect = roleGO.AddComponent<RectTransform>();
                roleRect.sizeDelta = new Vector2(99f, 12f);
                roleRect.localScale = Vector3.one;
                Text roleTxt = roleGO.AddComponent<Text>();
                roleTxt.raycastTarget = false;
                roleTxt.text = $"{ally.characterData.role} ({ally.characterData.element})";
                roleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                roleTxt.fontSize = 8;
                roleTxt.color = new Color(0.7f, 0.7f, 0.7f);

                // HP
                GameObject hpGO = new GameObject("HPText");
                hpGO.transform.SetParent(cardGO.transform);
                RectTransform hpRect = hpGO.AddComponent<RectTransform>();
                hpRect.sizeDelta = new Vector2(99f, 14f);
                hpRect.localScale = Vector3.one;
                Text hpTxt = hpGO.AddComponent<Text>();
                hpTxt.raycastTarget = false;
                hpTxt.text = $"HP: {ally.currentHP:F0}/{ally.maxHP:F0}";
                hpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hpTxt.fontSize = 9;
                hpTxt.color = new Color(0.4f, 0.9f, 0.4f);

                // Energy
                GameObject enGO = new GameObject("EnergyText");
                enGO.transform.SetParent(cardGO.transform);
                RectTransform enRect = enGO.AddComponent<RectTransform>();
                enRect.sizeDelta = new Vector2(99f, 14f);
                enRect.localScale = Vector3.one;
                Text enTxt = enGO.AddComponent<Text>();
                enTxt.raycastTarget = false;
                enTxt.text = $"Energy: {ally.currentEnergy:F0}/100";
                enTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                enTxt.fontSize = 9;
                enTxt.color = new Color(0.3f, 0.7f, 1f);

                // Vẽ Recollection Section
                if (ally.isCommander)
                {
                    // Là Chỉ Huy Recollection đang hoạt động
                    GameObject recGO = new GameObject("RecActiveText");
                    recGO.transform.SetParent(cardGO.transform);
                    RectTransform recRect = recGO.AddComponent<RectTransform>();
                    recRect.sizeDelta = new Vector2(99f, 14f);
                    recRect.localScale = Vector3.one;
                    Text recTxt = recGO.AddComponent<Text>();
                    recTxt.raycastTarget = false;
                    recTxt.text = "RECOLLECT ACTIVE";
                    recTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    recTxt.fontSize = 8;
                    recTxt.fontStyle = FontStyle.Bold;
                    recTxt.color = Color.yellow;

                    // Vẽ Pips đếm ngược
                    GameObject pipsGO = new GameObject("PipsText");
                    pipsGO.transform.SetParent(cardGO.transform);
                    RectTransform pipsRect = pipsGO.AddComponent<RectTransform>();
                    pipsRect.sizeDelta = new Vector2(99f, 14f);
                    pipsRect.localScale = Vector3.one;
                    Text pipsTxt = pipsGO.AddComponent<Text>();
                    pipsTxt.raycastTarget = false;
                    string pipStr = "";
                    for (int p = 0; p < 5; p++)
                    {
                        if (RecollectionManager.Instance != null && p < RecollectionManager.Instance.turnsRemaining)
                            pipStr += "● ";
                        else
                            pipStr += "○ ";
                    }
                    pipsTxt.text = $"Lượt: {pipStr}";
                    pipsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    pipsTxt.fontSize = 9;
                    pipsTxt.color = Color.yellow;
                }
                else
                {
                    // Nhân vật bình thường hàng trước
                    GameObject recGO = new GameObject("RecGaugeText");
                    recGO.transform.SetParent(cardGO.transform);
                    RectTransform recRect = recGO.AddComponent<RectTransform>();
                    recRect.sizeDelta = new Vector2(99f, 14f);
                    recRect.localScale = Vector3.one;
                    Text recTxt = recGO.AddComponent<Text>();
                    recTxt.raycastTarget = false;
                    recTxt.text = $"RecGauge: {ally.recollectionGauge:F0}%";
                    recTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    recTxt.fontSize = 9;
                    recTxt.color = Color.magenta;

                    // Nút bấm Recollection khi đầy 100%
                    GameObject recBtnGO = new GameObject("RecollectionButton");
                    recBtnGO.transform.SetParent(cardGO.transform);
                    RectTransform recBtnRect = recBtnGO.AddComponent<RectTransform>();
                    recBtnRect.sizeDelta = new Vector2(99f, 20f);
                    recBtnRect.localScale = Vector3.one;

                    LayoutElement le = recBtnGO.AddComponent<LayoutElement>();
                    le.preferredWidth = 99f;
                    le.preferredHeight = 20f;

                    Image rbImg = recBtnGO.AddComponent<Image>();
                    rbImg.color = new Color(0.4f, 0f, 0.4f);

                    Button recBtn = recBtnGO.AddComponent<Button>();
                    recBtn.targetGraphic = rbImg;
                    recBtn.onClick.AddListener(() => {
                        if (RecollectionManager.Instance != null)
                        {
                            RecollectionManager.Instance.ActivateRecollection(ally);
                        }
                    });

                    GameObject rbtGO = new GameObject("RecBtnText");
                    rbtGO.transform.SetParent(recBtnGO.transform);
                    RectTransform rbtRect = rbtGO.AddComponent<RectTransform>();
                    rbtRect.anchorMin = Vector2.zero;
                    rbtRect.anchorMax = Vector2.one;
                    rbtRect.offsetMin = Vector2.zero;
                    rbtRect.offsetMax = Vector2.zero;

                    Text rbtTxt = rbtGO.AddComponent<Text>();
                    rbtTxt.raycastTarget = false;
                    rbtTxt.text = "RECOLLECT";
                    rbtTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    rbtTxt.fontSize = 8;
                    rbtTxt.alignment = TextAnchor.MiddleCenter;
                    rbtTxt.color = Color.white;

                    // Bật nút nếu đủ 100% gauge và không có Chỉ huy nào đang hoạt động
                    bool canRecollect = ally.recollectionGauge >= 99.9f && 
                                       !ally.isDead && 
                                       RecollectionManager.Instance != null && 
                                       !RecollectionManager.Instance.IsRecollectionActive;

                    // In log ra Console khi vẽ UI để biết lý do tại sao nút bị khóa
                    Debug.Log($"[RecollectCheck] {ally.characterData.characterName}: Gauge={ally.recollectionGauge:F1}%, isDead={ally.isDead}, ManagerIsNull={(RecollectionManager.Instance == null)}, ActiveRec={((RecollectionManager.Instance != null) ? RecollectionManager.Instance.IsRecollectionActive : false)}, canRec={canRecollect}");

                    if (canRecollect)
                    {
                        recBtn.interactable = true;
                        rbImg.color = Color.magenta;
                        
                        Outline rbo = recBtnGO.AddComponent<Outline>();
                        rbo.effectColor = Color.yellow;
                        rbo.effectDistance = new Vector2(1f, -1f);
                    }
                    else
                    {
                        recBtn.interactable = false;
                        rbImg.color = new Color(0.15f, 0f, 0.15f, 0.5f);
                        rbtTxt.color = Color.gray;
                    }
                }

                // Nút Ultimate Cắt Lượt
                if (!ally.isCommander)
                {
                    GameObject ultBtnGO = new GameObject("UltimateButton");
                    ultBtnGO.transform.SetParent(cardGO.transform);
                    RectTransform utRect = ultBtnGO.AddComponent<RectTransform>();
                    utRect.sizeDelta = new Vector2(99f, 20f);
                    utRect.localScale = Vector3.one;

                    LayoutElement leUlt = ultBtnGO.AddComponent<LayoutElement>();
                    leUlt.preferredWidth = 99f;
                    leUlt.preferredHeight = 20f;

                    Image bImg = ultBtnGO.AddComponent<Image>();
                    bImg.color = Color.red;

                    Button ultBtn = ultBtnGO.AddComponent<Button>();
                    ultBtn.targetGraphic = bImg;
                    ultBtn.onClick.AddListener(() => {
                        CombatManager.Instance.RequestUltimateCast(ally);
                    });

                    GameObject btGO = new GameObject("BtnText");
                    btGO.transform.SetParent(ultBtnGO.transform);
                    RectTransform btRect = btGO.AddComponent<RectTransform>();
                    btRect.anchorMin = Vector2.zero;
                    btRect.anchorMax = Vector2.one;
                    btRect.offsetMin = Vector2.zero;
                    btRect.offsetMax = Vector2.zero;

                    Text btTxt = btGO.AddComponent<Text>();
                    btTxt.raycastTarget = false;
                    btTxt.text = "ULTIMATE";
                    btTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    btTxt.fontSize = 8;
                    btTxt.alignment = TextAnchor.MiddleCenter;
                    btTxt.color = Color.white;

                    if (ally.currentEnergy >= 100f && !ally.isDead)
                    {
                        ultBtn.interactable = true;
                        bImg.color = Color.yellow;
                        btTxt.color = Color.black;
                        
                        Outline bo = ultBtnGO.AddComponent<Outline>();
                        bo.effectColor = Color.red;
                        bo.effectDistance = new Vector2(1f, -1f);
                    }
                    else
                    {
                        ultBtn.interactable = false;
                        bImg.color = new Color(0.4f, 0.1f, 0.1f, 0.5f);
                        btTxt.color = Color.gray;
                    }

                    partyUltButtons[ally] = ultBtn;
                }

                // --- VẼ BUFF/DEBUFF CHO PARTY MEMBER ---
                GameObject buffsGO = new GameObject("PartyBuffContainer");
                buffsGO.transform.SetParent(cardGO.transform, false);
                RectTransform buffsRect = buffsGO.AddComponent<RectTransform>();
                buffsRect.sizeDelta = new Vector2(99f, 16f);
                buffsRect.localScale = Vector3.one;
                buffsRect.localPosition = Vector3.zero;
                buffsRect.localRotation = Quaternion.identity;

                LayoutElement leBuffs = buffsGO.AddComponent<LayoutElement>();
                leBuffs.preferredWidth = 99f;
                leBuffs.preferredHeight = 16f;

                HorizontalLayoutGroup layout = buffsGO.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 3f;
                layout.childControlHeight = false;
                layout.childControlWidth = false;

                foreach (var effect in ally.activeEffects)
                {
                    GameObject iconGO = new GameObject("PartyBuffIcon");
                    iconGO.transform.SetParent(buffsGO.transform);
                    
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
                    else
                    {
                        iconImg.sprite = GetDefaultWhiteSprite();
                    }
                    iconImg.raycastTarget = true;

                    // Text số lượt còn lại
                    GameObject textGO = new GameObject("TurnText");
                    textGO.transform.SetParent(iconGO.transform);
                    RectTransform textRect = textGO.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    textRect.localPosition = Vector3.zero;
                    textRect.localScale = Vector3.one;

                    Text txt = textGO.AddComponent<Text>();
                    txt.text = effect.turnsRemaining.ToString();
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txt.fontSize = 8;
                    txt.alignment = TextAnchor.LowerRight;
                    txt.color = Color.white;

                    Outline outline = textGO.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(1f, -1f);

                    // Thêm Tooltip Trigger
                    BuffTooltipTrigger trigger = iconGO.AddComponent<BuffTooltipTrigger>();
                    trigger.Initialize(effect);
                }
            }
        }

        public void UpdateUltimateButtons()
        {
            UpdatePartyPanel();
        }

        #endregion

        #region Quản lý Action Panel điều khiển của Player

        public void ShowActionPanel(CombatCharacter character)
        {
            currentCaster = character;
            actionPanel.gameObject.SetActive(true);
            descriptionPanel.gameObject.SetActive(true);

            selectedSkill = null;
            HideAllTargetSelectors();

            // Reset màu nền các nút về mặc định (Trắng)
            if (basicButton != null) basicButton.GetComponent<Image>().color = Color.white;
            if (specialButton != null) specialButton.GetComponent<Image>().color = Color.white;
            if (ultimateButton != null) ultimateButton.GetComponent<Image>().color = Color.white;

            // Kiểm tra trạng thái Recollection để đổi màu viền kỹ năng cường hóa
            EnhancedSkillResult basicEnh = null;
            EnhancedSkillResult specialEnh = null;

            if (RecollectionManager.Instance != null && RecollectionManager.Instance.IsRecollectionActive && character.isAlly && !character.isCommander)
            {
                basicEnh = SkillEnhancementResolver.Resolve(RecollectionManager.Instance.activeCommander.characterData.element, character.characterData.element, SkillType.BASIC);
                specialEnh = SkillEnhancementResolver.Resolve(RecollectionManager.Instance.activeCommander.characterData.element, character.characterData.element, SkillType.SPECIAL);
            }

            // Skill 1: Basic Attack (CD 0)
            basicButton.interactable = true;
            if (basicEnh != null)
            {
                basicButton.GetComponent<Image>().color = CombatManager.Instance.GetElementColor(RecollectionManager.Instance.activeCommander.characterData.element);
                basicText.text = $"{character.characterData.skillBasic.skillName} [CƯỜNG HÓA]";
            }
            else
            {
                basicText.text = $"{character.characterData.skillBasic.skillName} (CD: 0)";
            }

            // Skill 2: Special (CD 2-3)
            if (character.characterData.skillSpecial != null)
            {
                if (character.specialSkillCDRemaining > 0)
                {
                    specialButton.interactable = false;
                    specialText.text = $"{character.characterData.skillSpecial.skillName} (CD còn: {character.specialSkillCDRemaining} lượt)";
                }
                else
                {
                    specialButton.interactable = true;
                    if (specialEnh != null)
                    {
                        specialButton.GetComponent<Image>().color = CombatManager.Instance.GetElementColor(RecollectionManager.Instance.activeCommander.characterData.element);
                        specialText.text = $"{character.characterData.skillSpecial.skillName} [CƯỜNG HÓA]";
                    }
                    else
                    {
                        specialText.text = $"{character.characterData.skillSpecial.skillName} (CD: {character.characterData.skillSpecial.cooldown})";
                    }
                }
            }
            else
            {
                specialButton.interactable = false;
                specialText.text = "Không có Special Skill";
            }

            // Skill 3: Ultimate (Energy >= 100)
            if (character.characterData.skillUltimate != null)
            {
                if (character.currentEnergy >= 100f)
                {
                    ultimateButton.interactable = true;
                    ultimateText.text = $"{character.characterData.skillUltimate.skillName} (KÍCH HOẠT!)";
                }
                else
                {
                    ultimateButton.interactable = false;
                    ultimateText.text = $"{character.characterData.skillUltimate.skillName} (Năng lượng: {character.currentEnergy:F0}/100)";
                }
            }
            else
            {
                ultimateButton.interactable = false;
                ultimateText.text = "Không có Ultimate Skill";
            }

            descriptionText.text = $"Lượt của {character.characterData.characterName}. Hãy chọn một kỹ năng hành động!";
        }

        public void HideActionPanel()
        {
            if (actionPanel != null) actionPanel.gameObject.SetActive(false);
            if (descriptionPanel != null) descriptionPanel.gameObject.SetActive(false);
            if (targetSelectionPanel != null) targetSelectionPanel.gameObject.SetActive(false);
            activeTargetsList.Clear();
        }

        private void OnSkillButtonClicked(SkillType type)
        {
            if (currentCaster == null) return;

            SkillData skill = null;
            switch (type)
            {
                case SkillType.BASIC:
                    skill = currentCaster.characterData.skillBasic;
                    break;
                case SkillType.SPECIAL:
                    skill = currentCaster.characterData.skillSpecial;
                    break;
                case SkillType.ULTIMATE:
                    skill = currentCaster.characterData.skillUltimate;
                    break;
            }

            if (skill == null) return;

            selectedSkill = skill;
            descriptionText.text = $"<b>{skill.skillName}</b>\n{skill.description}\nSát thương: ATK * {skill.damageMultiplier:F1} | Loại mục tiêu: {skill.targetType}";

            // Bổ sung mô tả cường hóa nếu có Recollection active
            if (RecollectionManager.Instance != null && RecollectionManager.Instance.IsRecollectionActive && currentCaster.isAlly && !currentCaster.isCommander)
            {
                EnhancedSkillResult enhancement = SkillEnhancementResolver.Resolve(
                    RecollectionManager.Instance.activeCommander.characterData.element,
                    currentCaster.characterData.element,
                    type
                );
                if (enhancement != null)
                {
                    descriptionText.text += $"\n\n<color=magenta><b>[CƯỜNG HÓA - {enhancement.enhancementName}]:</b> {enhancement.description}</color>";
                }
            }

            // Ẩn các target cũ
            HideAllTargetSelectors();

            // Kiểm tra loại mục tiêu để quyết định có cần click chọn mục tiêu hay không
            if (skill.targetType == TargetType.SINGLE)
            {
                // Đồng minh hay Kẻ địch?
                // Mặc định hầu hết kỹ năng là tấn công đơn mục tiêu kẻ địch,
                // hồi máu / buff là đồng minh.
                // Ở đây ta xem nếu tên kỹ năng hoặc hiệu ứng có Healing/ATK_BUFF/DEF_BUFF thì là buff đồng minh, ngược lại tấn công kẻ địch.
                bool targetAllies = false;
                foreach (var effect in skill.effects)
                {
                    if (effect.effectType == EffectType.ATK_BUFF || effect.effectType == EffectType.DEF_BUFF)
                    {
                        targetAllies = true;
                    }
                }
                if (skill.damageMultiplier <= 0) // Kỹ năng không gây damg thường là hỗ trợ đồng minh
                {
                    targetAllies = true;
                }

                ShowTargetSelectors(targetAllies);
                ShowScreenSpaceTargetSelectors(targetAllies);
                descriptionText.text += "\n<color=yellow>Chọn 1 mục tiêu (Click Model 3D, Phím 1-4, hoặc Click UI)!</color>";
            }
            else
            {
                // AOE, ALL_ENEMIES, ALL_ALLIES, SELF -> Thi triển luôn lập tức không cần click chọn mục tiêu!
                List<CombatCharacter> targets = new List<CombatCharacter>();
                if (skill.targetType == TargetType.AOE || skill.targetType == TargetType.ALL_ENEMIES)
                {
                    targets.AddRange(CombatManager.Instance.GetAliveEnemies());
                }
                else if (skill.targetType == TargetType.ALL_ALLIES)
                {
                    targets.AddRange(CombatManager.Instance.GetAliveAllies());
                }
                else if (skill.targetType == TargetType.SELF)
                {
                    targets.Add(currentCaster);
                }

                if (targets.Count > 0)
                {
                    CombatManager.Instance.ExecuteAction(currentCaster, selectedSkill, targets);
                }
            }
        }

        private void OnGuardButtonClicked()
        {
            if (currentCaster == null) return;
            CombatManager.Instance.ExecuteGuard(currentCaster);
        }

        #endregion

        #region Cơ chế chọn mục tiêu trực quan (World Space Target Buttons)

        private void ShowTargetSelectors(bool isTargetingAllies)
        {
            List<CombatCharacter> targetList = isTargetingAllies ? 
                CombatManager.Instance.GetAliveAllies() : 
                CombatManager.Instance.GetAliveEnemies();

            foreach (var target in targetList)
            {
                // Thêm một nút chọn mục tiêu tròn đỏ nổi trên Canvas đầu của target
                CreateWorldSpaceTargetButton(target);
            }
        }

        private void CreateWorldSpaceTargetButton(CombatCharacter target)
        {
            Canvas targetCanvas = target.GetComponentInChildren<Canvas>();
            if (targetCanvas == null) return;

            // Xóa nút chọn cũ nếu đã tồn tại để tránh trùng lặp
            Transform oldBtn = targetCanvas.transform.Find("VFX_TargetSelector");
            if (oldBtn != null) Destroy(oldBtn.gameObject);

            GameObject btnGO = new GameObject("VFX_TargetSelector");
            btnGO.transform.SetParent(targetCanvas.transform);
            
            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(25f, 25f);
            rect.localScale = Vector3.one;
            rect.localPosition = new Vector3(0f, 30f, 0f); // Tương đương 0.3 mét trên đầu HP bar
            rect.localRotation = Quaternion.identity;

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(1f, 0.2f, 0.2f, 0.8f);

            // Thêm Text "TARGET" hoặc tâm ngắm nhỏ
            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform);
            
            RectTransform txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            txtRect.localPosition = Vector3.zero;
            txtRect.localScale = Vector3.one;
            txtRect.localRotation = Quaternion.identity;

            Text txt = txtGO.AddComponent<Text>();
            txt.text = "🎯";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        private void ShowScreenSpaceTargetSelectors(bool isTargetingAllies)
        {
            if (targetSelectionPanel == null) return;

            targetSelectionPanel.gameObject.SetActive(true);
            activeTargetsList.Clear();

            // Clear old buttons
            foreach (Transform child in targetSelectionPanel)
            {
                Destroy(child.gameObject);
            }

            // Get target list
            List<CombatCharacter> targetList = isTargetingAllies ? 
                CombatManager.Instance.GetAliveAllies() : 
                CombatManager.Instance.GetAliveEnemies();

            activeTargetsList.AddRange(targetList);

            // Title
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(targetSelectionPanel);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(210f, 20f);
            titleRect.localScale = Vector3.one;
            titleRect.localPosition = Vector3.zero;
            titleRect.localRotation = Quaternion.identity;
            
            Text titleTxt = titleGO.AddComponent<Text>();
            titleTxt.text = "CHỌN MỤC TIÊU";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 12;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = Color.yellow;

            // Create buttons for each target
            for (int i = 0; i < targetList.Count; i++)
            {
                CombatCharacter target = targetList[i];
                int index = i + 1;
                string label = $"[{index}] {target.characterData.characterName} ({target.currentHP:F0}/{target.maxHP:F0})";
                
                Button btn = CreateUIButton(targetSelectionPanel, label, () => {
                    OnScreenSpaceTargetSelected(target);
                });
                
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isTargetingAllies ? new Color(0.1f, 0.5f, 0.2f, 0.9f) : new Color(0.6f, 0.1f, 0.1f, 0.9f);
                }
            }

            // Cancel Button
            CreateUIButton(targetSelectionPanel, "Hủy Chọn (Esc)", () => {
                HideAllTargetSelectors();
            });
        }

        private void OnScreenSpaceTargetSelected(CombatCharacter target)
        {
            HideAllTargetSelectors();

            if (currentCaster != null && selectedSkill != null)
            {
                List<CombatCharacter> targets = new List<CombatCharacter> { target };
                CombatManager.Instance.ExecuteAction(currentCaster, selectedSkill, targets);
            }
        }

        private void Update()
        {
            if (targetSelectionPanel != null && targetSelectionPanel.gameObject.activeSelf)
            {
                // 1. Esc to cancel
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    HideAllTargetSelectors();
                    return;
                }

                // 2. Keyboard shortcut 1-4
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    int selectedIndex = -1;
                    if (kb.digit1Key.wasPressedThisFrame) selectedIndex = 0;
                    else if (kb.digit2Key.wasPressedThisFrame) selectedIndex = 1;
                    else if (kb.digit3Key.wasPressedThisFrame) selectedIndex = 2;
                    else if (kb.digit4Key.wasPressedThisFrame) selectedIndex = 3;

                    if (selectedIndex >= 0 && selectedIndex < activeTargetsList.Count)
                    {
                        OnScreenSpaceTargetSelected(activeTargetsList[selectedIndex]);
                        return;
                    }
                }

                // 3. Click directly on 3D Model
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    Ray ray = Camera.main.ScreenPointToRay(mousePos);
                    RaycastHit[] hits = Physics.RaycastAll(ray);
                    foreach (var hit in hits)
                    {
                        CombatCharacter targetChar = hit.collider.GetComponentInParent<CombatCharacter>();
                        if (targetChar != null && activeTargetsList.Contains(targetChar))
                        {
                            OnScreenSpaceTargetSelected(targetChar);
                            return;
                        }
                    }
                }
            }
        }

        private void HideAllTargetSelectors()
        {
            if (targetSelectionPanel != null) targetSelectionPanel.gameObject.SetActive(false);
            activeTargetsList.Clear();

            if (CombatManager.Instance == null) return;

            List<CombatCharacter> all = CombatManager.Instance.GetAliveCharacters();
            foreach (var c in all)
            {
                Canvas targetCanvas = c.GetComponentInChildren<Canvas>();
                if (targetCanvas != null)
                {
                    Transform btn = targetCanvas.transform.Find("VFX_TargetSelector");
                    if (btn != null)
                    {
                        Destroy(btn.gameObject);
                    }
                }
            }
        }

        private void HideAllTargetSelectors(bool unusedVal)
        {
            HideAllTargetSelectors();
        }

        #endregion

        #region Màn hình Thắng Thua

        public void ShowEndScreen(bool win)
        {
            HideActionPanel();
            HideAllTargetSelectors();

            endScreenPanel.gameObject.SetActive(true);
            endScreenText.text = win ? "CHIẾN THẮNG!" : "THẤT BẠI!";
            endScreenText.color = win ? Color.yellow : Color.red;

            if (restartButtonText != null)
            {
                if (CombatTeamManager.IsEnteringFromOverworld)
                {
                    restartButtonText.text = "Trở Về Thế Giới";
                }
                else
                {
                    restartButtonText.text = "Chơi Trận Mới";
                }
            }
        }

        private void OnRestartButtonClicked()
        {
            // Tắt Tooltip trước khi đổi Scene
            HideTooltip();

            // Chủ động hủy Canvas chính của Combat để tránh trôi nổi
            if (overlayCanvas != null)
            {
                Destroy(overlayCanvas.gameObject);
            }

            if (CombatTeamManager.IsEnteringFromOverworld)
            {
                SceneManager.LoadScene("DEMO_WASD");
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        #endregion

        #region Recollection Banner UI

        private void HandleRecollectionActivated(CombatCharacter commander)
        {
            ShowRecollectionBanner(commander);
        }

        private void CreateProceduralRecollectionBanner()
        {
            if (overlayCanvas == null) return;

            GameObject bannerGO = new GameObject("RecollectionBannerUI");
            bannerGO.transform.SetParent(overlayCanvas.transform, false);

            RectTransform bannerRect = bannerGO.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0f, 0.5f);
            bannerRect.anchorMax = new Vector2(1f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerRect.sizeDelta = new Vector2(0f, 150f); // Chiều cao banner 150px
            bannerRect.localScale = new Vector3(1f, 0f, 1f); // Mặc định thu hẹp Y

            CanvasGroup group = bannerGO.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Image bgImage = bannerGO.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.07f, 0.85f); // Đen mờ sang trọng

            GameObject borderTop = new GameObject("BorderTop");
            borderTop.transform.SetParent(bannerGO.transform, false);
            RectTransform btRect = borderTop.AddComponent<RectTransform>();
            btRect.anchorMin = new Vector2(0f, 1f);
            btRect.anchorMax = new Vector2(1f, 1f);
            btRect.pivot = new Vector2(0.5f, 1f);
            btRect.anchoredPosition = Vector2.zero;
            btRect.sizeDelta = new Vector2(0f, 3f);
            Image btImg = borderTop.AddComponent<Image>();
            btImg.color = Color.white;

            GameObject borderBottom = new GameObject("BorderBottom");
            borderBottom.transform.SetParent(bannerGO.transform, false);
            RectTransform bbRect = borderBottom.AddComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0f, 0f);
            bbRect.anchorMax = new Vector2(1f, 0f);
            bbRect.pivot = new Vector2(0.5f, 0f);
            bbRect.anchoredPosition = Vector2.zero;
            bbRect.sizeDelta = new Vector2(0f, 3f);
            Image bbImg = borderBottom.AddComponent<Image>();
            bbImg.color = Color.white;

            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(bannerGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 15f);
            titleRect.sizeDelta = new Vector2(0f, 60f);

            Text titleTxt = titleGO.AddComponent<Text>();
            titleTxt.text = "RECOLLECTION";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 42;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = Color.white;
            titleTxt.fontStyle = FontStyle.Bold;

            Outline titleOutline = titleGO.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            titleOutline.effectDistance = new Vector2(2f, -2f);

            GameObject subTitleGO = new GameObject("SubTitleText");
            subTitleGO.transform.SetParent(bannerGO.transform, false);
            RectTransform subTitleRect = subTitleGO.AddComponent<RectTransform>();
            subTitleRect.anchorMin = new Vector2(0f, 0.5f);
            subTitleRect.anchorMax = new Vector2(1f, 0.5f);
            subTitleRect.pivot = new Vector2(0.5f, 0.5f);
            subTitleRect.anchoredPosition = new Vector2(0f, -25f);
            subTitleRect.sizeDelta = new Vector2(0f, 30f);

            Text subTitleTxt = subTitleGO.AddComponent<Text>();
            subTitleTxt.text = "— COMMANDER AWAKENING —";
            subTitleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTitleTxt.fontSize = 14;
            subTitleTxt.alignment = TextAnchor.MiddleCenter;
            subTitleTxt.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            subTitleTxt.fontStyle = FontStyle.Normal;

            Outline subOutline = subTitleGO.AddComponent<Outline>();
            subOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            subOutline.effectDistance = new Vector2(1.5f, -1.5f);

            recollectionBannerPanel = bannerGO.transform;
            bannerGO.SetActive(false);
        }

        public void ShowRecollectionBanner(CombatCharacter commander)
        {
            if (recollectionBannerPanel == null || commander == null) return;

            Color elementColor = CombatManager.Instance.GetElementColor(commander.characterData.element);

            // Bật banner hoạt động
            recollectionBannerPanel.gameObject.SetActive(true);

            // Cập nhật màu sắc cho các thành phần con
            Transform bt = recollectionBannerPanel.Find("BorderTop");
            if (bt != null)
            {
                Image img = bt.GetComponent<Image>();
                if (img != null) img.color = elementColor;
            }

            Transform bb = recollectionBannerPanel.Find("BorderBottom");
            if (bb != null)
            {
                Image img = bb.GetComponent<Image>();
                if (img != null) img.color = elementColor;
            }

            Transform title = recollectionBannerPanel.Find("TitleText");
            if (title != null)
            {
                Text txt = title.GetComponent<Text>();
                if (txt != null) txt.color = elementColor;
            }

            StartCoroutine(CoShowRecollectionBanner(elementColor));
        }

        private System.Collections.IEnumerator CoShowRecollectionBanner(Color themeColor)
        {
            RectTransform bannerRect = recollectionBannerPanel.GetComponent<RectTransform>();
            CanvasGroup group = recollectionBannerPanel.GetComponent<CanvasGroup>();

            Transform title = recollectionBannerPanel.Find("TitleText");
            RectTransform titleRect = title != null ? title.GetComponent<RectTransform>() : null;

            if (bannerRect == null || group == null) yield break;

            // Đặt trạng thái ban đầu
            group.alpha = 0f;
            bannerRect.localScale = new Vector3(1f, 0f, 1f);
            if (titleRect != null) titleRect.localScale = Vector3.one * 1.4f;

            // Chạy hiệu ứng động
            float elapsed = 0f;
            float fadeInTime = 0.3f;
            float holdTime = 1.2f;
            float fadeOutTime = 0.4f;

            Vector3 startScale = new Vector3(1f, 0f, 1f);
            Vector3 targetScale = Vector3.one;

            // Phase 1: Fade In & Scale Y
            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInTime;
                t = Mathf.SmoothStep(0f, 1f, t);

                group.alpha = t;
                bannerRect.localScale = Vector3.Lerp(startScale, targetScale, t);
                if (titleRect != null) titleRect.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, t);
                yield return null;
            }

            group.alpha = 1f;
            bannerRect.localScale = Vector3.one;
            if (titleRect != null) titleRect.localScale = Vector3.one;

            // Phase 2: Giữ nguyên
            yield return new WaitForSeconds(holdTime);

            // Phase 3: Fade Out & Scale Y về 0
            elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutTime;
                t = Mathf.SmoothStep(0f, 1f, t);

                group.alpha = 1f - t;
                bannerRect.localScale = Vector3.Lerp(targetScale, startScale, t);
                yield return null;
            }

            // Tắt hoạt động thay vì Hủy để giữ lại prefab
            recollectionBannerPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnTurnStart -= HandleTurnStart;
                CombatManager.Instance.OnCombatStarted -= HandleCombatStarted;
                CombatManager.Instance.OnCombatEnd -= HandleCombatEnd;
            }
            if (RecollectionManager.Instance != null)
            {
                RecollectionManager.Instance.OnRecollectionActivated -= HandleRecollectionActivated;
            }
        }

        #endregion
        public void ShowTooltip(string title, string description, Color titleColor, Vector2 screenPos)
        {
            if (tooltipPanel == null || tooltipText == null) return;
            
            tooltipPanel.gameObject.SetActive(true);
            tooltipText.text = $"<b><color=#{ColorUtility.ToHtmlStringRGBA(titleColor)}>{title}</color></b>\n{description}";
            
            RectTransform rect = tooltipPanel.GetComponent<RectTransform>();
            float width = rect.sizeDelta.x;
            float height = rect.sizeDelta.y;
            
            float posX = screenPos.x + 15f;
            float posY = screenPos.y + 15f;
            
            if (posX + width > Screen.width)
            {
                posX = screenPos.x - width - 15f;
            }
            if (posY + height > Screen.height)
            {
                posY = screenPos.y - height - 15f;
            }
            
            rect.position = new Vector2(posX, posY);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.gameObject.SetActive(false);
            }
        }

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
    }
}
