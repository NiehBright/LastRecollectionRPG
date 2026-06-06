using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using BLINK.Controller;

namespace RPG.Combat
{
    [System.Serializable]
    public class CharacterMenuData
    {
        public string characterName;
        public ElementType element;
        public Color themeColor;
        public float maxHP;
        public float atk;
        public float def;
        public float speed;
        public float critRate;
        public float critDmg;
        [TextArea(2, 4)]
        public string description;
        public GameObject modelPrefab; // Prefab model 3D thật (ví dụ Kazuko)
        public List<string> skills = new List<string>();
    }

    public class CharacterMenuManager : MonoBehaviour
    {
        public static CharacterMenuManager Instance { get; private set; }

        [Header("Character List Data")]
        public List<CharacterMenuData> characters = new List<CharacterMenuData>();

        [Header("Showroom Configuration")]
        public Vector3 showroomPosition = new Vector3(1000f, -1000f, 1000f);
        public float modelScale = 1.0f;

        [Header("Showroom Visuals")]
        public Color groundColor = new Color(0.12f, 0.15f, 0.2f);
        public Color cameraBackgroundColor = new Color(0.06f, 0.08f, 0.1f);
        public GameObject showroomEnvironmentPrefab; // Kéo thả prefab môi trường showroom tùy chọn

        private bool isMenuOpen = false;
        private GameObject spawnedUIInstance;
        private CharacterMenuUIReferences uiRefs;
        private int selectedCharacterIndex = 0;

        // Showroom Runtime Objects
        private GameObject showroomRoot;
        private Camera showroomCamera;
        private RenderTexture showroomRT;
        private GameObject spawnedShowroomModel;
        private Light showroomLight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnStartup()
        {
            if (Instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                Instance = FindFirstObjectByType<CharacterMenuManager>();
#else
                Instance = FindObjectOfType<CharacterMenuManager>();
#endif
            }

            if (Instance == null)
            {
                GameObject go = new GameObject("[CharacterMenuManager]");
                Instance = go.AddComponent<CharacterMenuManager>();
                DontDestroyOnLoad(go);
            }
            else
            {
                DontDestroyOnLoad(Instance.gameObject);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeDefaultData()
        {
            if (characters != null && characters.Count > 0) return;

            characters = new List<CharacterMenuData>();

            // 1. Tự động tải tất cả các nhân vật được xây dựng từ Tool trong Resources/Characters
            CharacterData[] customChars = Resources.LoadAll<CharacterData>("Characters");
            if (customChars != null && customChars.Length > 0)
            {
                foreach (var data in customChars)
                {
                    if (data == null) continue;

                    var cmd = new CharacterMenuData
                    {
                        characterName = data.characterName,
                        element = data.element,
                        themeColor = data.themeColor,
                        maxHP = data.baseMaxHP,
                        atk = data.baseATK,
                        def = data.baseDEF,
                        speed = data.baseSpeed,
                        critRate = data.baseCritRate,
                        critDmg = data.baseCritDMG,
                        description = data.skillBasic != null ? data.skillBasic.description : "Không có mô tả nhân vật.",
                        skills = new List<string>()
                    };

                    if (data.skillBasic != null) cmd.skills.Add(data.skillBasic.skillName);
                    if (data.skillSpecial != null) cmd.skills.Add(data.skillSpecial.skillName);
                    if (data.skillUltimate != null) cmd.skills.Add(data.skillUltimate.skillName);

                    // Tự động tìm mô hình Prefab tương ứng
                    cmd.modelPrefab = Resources.Load<GameObject>($"Prefabs/Characters/{data.characterName}");
                    if (cmd.modelPrefab == null)
                    {
                        // Thử tìm trong thư mục Prefabs nói chung nếu không nằm trong thư mục con
                        cmd.modelPrefab = Resources.Load<GameObject>($"Prefabs/{data.characterName}");
                    }

                    characters.Add(cmd);
                }
            }

            // 2. Nếu không tìm thấy nhân vật tùy chỉnh nào, nạp danh sách nhân vật mặc định
            if (characters.Count == 0)
            {
                // 1. Fire Warrior
                var c1 = new CharacterMenuData
                {
                    characterName = "Fire Warrior",
                    element = ElementType.Fire,
                    themeColor = new Color(0.9f, 0.2f, 0.1f),
                    maxHP = 800f,
                    atk = 120f,
                    def = 60f,
                    speed = 110f,
                    critRate = 0.20f,
                    critDmg = 1.50f,
                    description = "Chiến binh mang sức mạnh ngọn lửa thiêng, có khả năng gây sát thương thiêu đốt liên tục lên mục tiêu đơn lẻ.",
                    skills = new List<string> { "Chém Thường (Basic)", "Hỏa Long Tiễn (Special)", "Hỏa Tiễn Hủy Diệt (Ultimate)" }
                };
                c1.modelPrefab = Resources.Load<GameObject>("ThirdParty/Suriyun/Kazuko/Prefab/Kazuko");
                if (c1.modelPrefab == null)
                {
#if UNITY_EDITOR
                    c1.modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Suriyun/Kazuko/Prefab/Kazuko.prefab");
#endif
                }
                characters.Add(c1);

                // 2. Ice Mage
                var c2 = new CharacterMenuData
                {
                    characterName = "Ice Mage",
                    element = ElementType.Ice,
                    themeColor = new Color(0.2f, 0.6f, 0.9f),
                    maxHP = 600f,
                    atk = 100f,
                    def = 40f,
                    speed = 95f,
                    critRate = 0.10f,
                    critDmg = 1.60f,
                    description = "Pháp sư băng giá kiểm soát trận đấu, có khả năng làm giảm tốc độ toàn bộ kẻ địch bằng bão tuyết sương giá.",
                    skills = new List<string> { "Đạn Băng (Basic)", "Sương Băng Chậm Chạp (Special)", "Tuyệt Đối Băng Phong (Ultimate)" }
                };
                characters.Add(c2);

                // 3. Storm Rogue
                var c3 = new CharacterMenuData
                {
                    characterName = "Storm Rogue",
                    element = ElementType.Lightning,
                    themeColor = new Color(0.9f, 0.8f, 0.1f),
                    maxHP = 700f,
                    atk = 140f,
                    def = 50f,
                    speed = 125f,
                    critRate = 0.30f,
                    critDmg = 1.80f,
                    description = "Sát thủ chớp nhoáng với tốc độ sấm sét, tập trung gây sát thương bạo kích cực lớn và làm choáng kẻ địch.",
                    skills = new List<string> { "Chích Điện (Basic)", "Tia Sét Quá Tải (Special)", "Thiên Lôi Triệu Hồi (Ultimate)" }
                };
                characters.Add(c3);

                // 4. Nature Druid
                var c4 = new CharacterMenuData
                {
                    characterName = "Nature Druid",
                    element = ElementType.Nature,
                    themeColor = new Color(0.2f, 0.8f, 0.3f),
                    maxHP = 1000f,
                    atk = 80f,
                    def = 70f,
                    speed = 100f,
                    critRate = 0.05f,
                    critDmg = 1.20f,
                    description = "Hộ vệ của tự nhiên, tập trung phòng thủ kiên cố, hồi phục sinh mệnh và gia tăng sức tấn công cho toàn đội.",
                    skills = new List<string> { "Lá Cây Sắc Nhọn (Basic)", "Phục Hồi Sinh Mệnh (Special)", "Rừng Già Trỗi Dậy (Ultimate)" }
                };
                characters.Add(c4);

                // 5. Shadow Assassin
                var c5 = new CharacterMenuData
                {
                    characterName = "Shadow Assassin",
                    element = ElementType.Physical,
                    themeColor = new Color(0.6f, 0.2f, 0.8f),
                    maxHP = 650f,
                    atk = 150f,
                    def = 45f,
                    speed = 130f,
                    critRate = 0.35f,
                    critDmg = 1.95f,
                    description = "Sát thủ bóng đêm di chuyển xuất quỷ nhập thần, tập trung vào đòn chí mạng cực mạnh từ phía sau kẻ địch.",
                    skills = new List<string> { "Đâm Lén (Basic)", "Ẩn Mình Nháy Mắt (Special)", "Vũ Điệu Bóng Đêm (Ultimate)" }
                };
                characters.Add(c5);

                // 6. Flame Paladin
                var c6 = new CharacterMenuData
                {
                    characterName = "Flame Paladin",
                    element = ElementType.Fire,
                    themeColor = new Color(0.7f, 0.3f, 0.2f),
                    maxHP = 1200f,
                    atk = 90f,
                    def = 90f,
                    speed = 85f,
                    critRate = 0.08f,
                    critDmg = 1.30f,
                    description = "Kỵ sĩ thánh lửa mang giáp nặng kiên cố, thu hút sự chú ý của kẻ địch và tạo khiên lửa bảo vệ đồng đội.",
                    skills = new List<string> { "Chém Khiên (Basic)", "Khiên Lửa Hộ Thể (Special)", "Thần Hỏa Phán Quyết (Ultimate)" }
                };
                characters.Add(c6);
            }
        }

        private void Update()
        {
            // Chỉ cho phép mở menu nhân vật khi đang ở scene thế giới thực DEMO_WASD và không ở trong combat
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DEMO_WASD")
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    // Tránh mở khi đang chuyển cảnh sang TurnBase
                    var combatCtrl = FindAnyObjectByType<CombatController>();
                    if (combatCtrl != null && combatCtrl.IsAttacking) return;

                    ToggleMenu();
                }
            }
        }

        private void Start()
        {
            isMenuOpen = false; // Reset trạng thái mở menu lúc bắt đầu
            // Khởi tạo showroom ảo 1 lần duy nhất lúc bắt đầu game để tránh lag
            CreateShowroom();
            if (showroomRoot != null)
            {
                showroomRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Giải phóng tài nguyên showroom thực sự khi Manager bị hủy
            CleanShowroom();
        }

        public void ToggleMenu()
        {
            // Tự sửa lỗi trạng thái: Nếu thực tế UI chưa được tạo, ép isMenuOpen về false
            if (spawnedUIInstance == null)
            {
                isMenuOpen = false;
            }

            if (isMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        private void OpenMenu()
        {
            if (isMenuOpen) return;
            isMenuOpen = true;

            // 1. Tắt di chuyển và camera của TẤT CẢ người chơi/controllers trên scene để tránh di chuyển được bên ngoài
            var playerControllers = FindObjectsByType<TopDownWASDController>(FindObjectsSortMode.None);
            foreach (var pc in playerControllers)
            {
                if (pc != null)
                {
                    pc.movementEnabled = false;
                    pc.cameraEnabled = false; // Khóa camera ngoài game
                    // Đảm bảo nhân vật đứng yên lập tức
                    var anim = pc.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.SetFloat("Horizontal", 0f);
                        anim.SetFloat("Vertical", 0f);
                    }
                }
            }

            // 2. Tạo hoặc tải giao diện UI
            LoadOrCreateUI();

            // 3. Kích hoạt Showroom 3D đã có sẵn
            if (showroomRoot != null)
            {
                showroomRoot.SetActive(true);
                // Đảm bảo liên kết lại Render Texture vào RawImage
                if (uiRefs != null && uiRefs.modelRenderImage != null && showroomRT != null)
                {
                    uiRefs.modelRenderImage.texture = showroomRT;
                }
            }
            else
            {
                // Phòng hờ nếu bị mất, tự động tạo lại
                CreateShowroom();
                if (showroomRoot != null) showroomRoot.SetActive(true);
            }

            // 4. Chọn nhân vật đầu tiên mặc định
            SelectCharacter(selectedCharacterIndex);
        }

        private void CloseMenu()
        {
            if (!isMenuOpen) return;
            isMenuOpen = false;

            // 1. Ẩn showroom ảo và hủy model đang hiển thị (tái sử dụng camera/đèn/texture)
            if (spawnedShowroomModel != null)
            {
                Destroy(spawnedShowroomModel);
                spawnedShowroomModel = null;
            }
            if (showroomRoot != null)
            {
                showroomRoot.SetActive(false);
            }

            // 2. Tắt/Xóa UI
            if (spawnedUIInstance != null)
            {
                Destroy(spawnedUIInstance);
                spawnedUIInstance = null;
                uiRefs = null;
            }

            // 3. Bật lại di chuyển và camera cho TẤT CẢ người chơi trên scene
            var playerControllers = FindObjectsByType<TopDownWASDController>(FindObjectsSortMode.None);
            foreach (var pc in playerControllers)
            {
                if (pc != null)
                {
                    pc.movementEnabled = true;
                    pc.cameraEnabled = true; // Mở khóa camera ngoài game
                }
            }
        }

        private void LoadOrCreateUI()
        {
            // Thử load từ Prefab
            GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/CharacterMenuUI");
            if (uiPrefab != null)
            {
                spawnedUIInstance = Instantiate(uiPrefab);
                uiRefs = spawnedUIInstance.GetComponent<CharacterMenuUIReferences>();
                if (uiRefs != null)
                {
                    uiRefs.closeButton.onClick.AddListener(CloseMenu);
                    BuildCharacterListButtons();
                    return;
                }
            }

            // Fallback: Tự tạo UI động bằng code nếu không tìm thấy Prefab
            Debug.LogWarning("[CharacterMenuManager] Không tìm thấy Prefab UI. Tạo UI động bằng code...");
            CreateProceduralUI();
        }

        private void CreateProceduralUI()
        {
            GameObject canvasGO = new GameObject("CharacterMenuUI_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            spawnedUIInstance = canvasGO;

            uiRefs = canvasGO.AddComponent<CharacterMenuUIReferences>();

            // 1. Background (Mờ tối)
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.06f, 0.08f, 0.9f); // Dark translucent background

            // 2. Close Button
            GameObject closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(canvasGO.transform);
            RectTransform closeRect = closeBtnGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -20f);
            closeRect.sizeDelta = new Vector2(40f, 40f);
            
            Image cbImg = closeBtnGO.AddComponent<Image>();
            cbImg.color = new Color(0.3f, 0.1f, 0.1f, 0.8f);
            Button closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseMenu);
            uiRefs.closeButton = closeBtn;

            GameObject closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeBtnGO.transform);
            RectTransform ctRect = closeTextGO.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            Text ct = closeTextGO.AddComponent<Text>();
            ct.text = "X";
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ct.fontSize = 20;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = Color.white;

            // 3. Left Panel (Danh sách nhân vật - Scroll View)
            GameObject leftPanelGO = new GameObject("LeftPanel");
            leftPanelGO.transform.SetParent(canvasGO.transform);
            RectTransform lpRect = leftPanelGO.AddComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(0f, 0.5f);
            lpRect.anchorMax = new Vector2(0f, 0.5f);
            lpRect.pivot = new Vector2(0f, 0.5f);
            lpRect.anchoredPosition = new Vector2(40f, 0f);
            lpRect.sizeDelta = new Vector2(240f, 500f);

            Image lpImg = leftPanelGO.AddComponent<Image>();
            lpImg.color = new Color(0.1f, 0.12f, 0.16f, 0.6f);

            // Tạo Scroll View
            GameObject scrollViewGO = new GameObject("ScrollView");
            scrollViewGO.transform.SetParent(leftPanelGO.transform);
            RectTransform svRect = scrollViewGO.AddComponent<RectTransform>();
            svRect.anchorMin = Vector2.zero;
            svRect.anchorMax = Vector2.one;
            svRect.sizeDelta = Vector2.zero;

            ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Tạo Viewport để che các nút thừa
            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform);
            RectTransform vpRect = viewportGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();

            // Tạo Content chứa các nút
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f); // Neo trên cùng
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup lpLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            lpLayout.childAlignment = TextAnchor.UpperCenter;
            lpLayout.spacing = 12f;
            lpLayout.padding = new RectOffset(10, 10, 15, 15);
            lpLayout.childControlHeight = false;
            lpLayout.childControlWidth = false;

            // ContentSizeFitter tự động giãn chiều cao theo số lượng button nhân vật
            ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Kết nối ScrollRect
            scrollRect.viewport = vpRect;
            scrollRect.content = contentRect;

            uiRefs.characterListParent = contentGO.transform;

            // 4. Middle Panel (Xem mô hình 3D)
            GameObject midPanelGO = new GameObject("MiddlePanel");
            midPanelGO.transform.SetParent(canvasGO.transform);
            RectTransform mpRect = midPanelGO.AddComponent<RectTransform>();
            mpRect.anchorMin = new Vector2(0.5f, 0.5f);
            mpRect.anchorMax = new Vector2(0.5f, 0.5f);
            mpRect.pivot = new Vector2(0.5f, 0.5f);
            mpRect.anchoredPosition = new Vector2(-40f, 0f);
            mpRect.sizeDelta = new Vector2(480f, 500f);

            RawImage ri = midPanelGO.AddComponent<RawImage>();
            ri.color = Color.white;
            uiRefs.modelRenderImage = ri;

            // Thêm ShowroomRotator để xoay model
            ShowroomRotator rotator = midPanelGO.AddComponent<ShowroomRotator>();
            rotator.rotationSpeed = 0.5f;

            // 5. Right Panel (Thông số & kỹ năng)
            GameObject rightPanelGO = new GameObject("RightPanel");
            rightPanelGO.transform.SetParent(canvasGO.transform);
            RectTransform rpRect = rightPanelGO.AddComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(1f, 0.5f);
            rpRect.anchorMax = new Vector2(1f, 0.5f);
            rpRect.pivot = new Vector2(1f, 0.5f);
            rpRect.anchoredPosition = new Vector2(-40f, 0f);
            rpRect.sizeDelta = new Vector2(340f, 500f);

            Image rpImg = rightPanelGO.AddComponent<Image>();
            rpImg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);
            uiRefs.statsPanel = rightPanelGO.transform;

            // Tên Nhân vật
            GameObject nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(rightPanelGO.transform);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(20f, -20f);
            nameRect.sizeDelta = new Vector2(-40f, 30f);
            Text nt = nameGO.AddComponent<Text>();
            nt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nt.fontSize = 24;
            nt.color = Color.white;
            nt.text = "Character Name";
            uiRefs.characterNameText = nt;

            // Hệ nguyên tố
            GameObject elementGO = new GameObject("ElementText");
            elementGO.transform.SetParent(rightPanelGO.transform);
            RectTransform elRect = elementGO.AddComponent<RectTransform>();
            elRect.anchorMin = new Vector2(0f, 1f);
            elRect.anchorMax = new Vector2(1f, 1f);
            elRect.pivot = new Vector2(0.5f, 1f);
            elRect.anchoredPosition = new Vector2(20f, -50f);
            elRect.sizeDelta = new Vector2(-40f, 20f);
            Text elt = elementGO.AddComponent<Text>();
            elt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            elt.fontSize = 14;
            elt.color = Color.yellow;
            elt.text = "Hệ: Lửa";
            uiRefs.characterElementText = elt;

            // Bảng chỉ số stats
            GameObject statsTextGO = new GameObject("StatsText");
            statsTextGO.transform.SetParent(rightPanelGO.transform);
            RectTransform stRect = statsTextGO.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 1f);
            stRect.anchorMax = new Vector2(1f, 1f);
            stRect.pivot = new Vector2(0.5f, 1f);
            stRect.anchoredPosition = new Vector2(20f, -80f);
            stRect.sizeDelta = new Vector2(-40f, 160f);
            Text stt = statsTextGO.AddComponent<Text>();
            stt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stt.fontSize = 13;
            stt.lineSpacing = 1.3f;
            stt.color = new Color(0.85f, 0.85f, 0.85f);
            stt.text = "HP:\nATK:\nDEF:\nSPEED:\nCRIT RATE:\nCRIT DMG:";
            uiRefs.characterStatsText = stt;

            // Divider line
            GameObject divGO = new GameObject("Divider");
            divGO.transform.SetParent(rightPanelGO.transform);
            RectTransform divRect = divGO.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0f, 1f);
            divRect.anchorMax = new Vector2(1f, 1f);
            divRect.pivot = new Vector2(0.5f, 1f);
            divRect.anchoredPosition = new Vector2(20f, -250f);
            divRect.sizeDelta = new Vector2(-40f, 2f);
            Image divImg = divGO.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.35f, 0.4f, 0.5f);

            // Kỹ năng Label
            GameObject skillLabelGO = new GameObject("SkillLabel");
            skillLabelGO.transform.SetParent(rightPanelGO.transform);
            RectTransform slRect = skillLabelGO.AddComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0f, 1f);
            slRect.anchorMax = new Vector2(1f, 1f);
            slRect.pivot = new Vector2(0.5f, 1f);
            slRect.anchoredPosition = new Vector2(20f, -260f);
            slRect.sizeDelta = new Vector2(-40f, 20f);
            Text slt = skillLabelGO.AddComponent<Text>();
            slt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slt.fontSize = 14;
            slt.color = Color.white;
            slt.text = "Danh Sách Kỹ Năng:";

            // Kỹ năng Parent (Skill list vertical layout)
            GameObject skillListGO = new GameObject("SkillList");
            skillListGO.transform.SetParent(rightPanelGO.transform);
            RectTransform sListRect = skillListGO.AddComponent<RectTransform>();
            sListRect.anchorMin = new Vector2(0f, 0f);
            sListRect.anchorMax = new Vector2(1f, 1f);
            sListRect.pivot = new Vector2(0.5f, 0f);
            sListRect.offsetMin = new Vector2(20f, 20f);
            sListRect.offsetMax = new Vector2(-20f, -285f);

            VerticalLayoutGroup skillLayout = skillListGO.AddComponent<VerticalLayoutGroup>();
            skillLayout.childAlignment = TextAnchor.UpperLeft;
            skillLayout.spacing = 8f;
            skillLayout.childControlHeight = false;
            skillLayout.childControlWidth = false;
            uiRefs.skillListParent = skillListGO.transform;

            // Xây dựng danh sách nút nhân vật
            BuildCharacterListButtons();
        }

        private void BuildCharacterListButtons()
        {
            if (uiRefs.characterListParent == null) return;

            // Xóa các nút cũ
            foreach (Transform child in uiRefs.characterListParent)
            {
                Destroy(child.gameObject);
            }

            // Tạo nút cho từng nhân vật
            for (int i = 0; i < characters.Count; i++)
            {
                int index = i;
                GameObject btnGO = new GameObject("CharBtn_" + characters[index].characterName);
                btnGO.transform.SetParent(uiRefs.characterListParent, false);

                RectTransform rRect = btnGO.AddComponent<RectTransform>();
                rRect.sizeDelta = new Vector2(210f, 60f);
                rRect.localPosition = Vector3.zero;
                rRect.localRotation = Quaternion.identity;
                rRect.localScale = Vector3.one;

                Image img = btnGO.AddComponent<Image>();
                img.color = new Color(0.18f, 0.22f, 0.28f, 0.8f);

                Button btn = btnGO.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectCharacter(index));

                // Add character name text inside button
                GameObject txtGO = new GameObject("Text");
                txtGO.transform.SetParent(btnGO.transform, false);
                RectTransform tRect = txtGO.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = new Vector2(15f, 5f);
                tRect.offsetMax = new Vector2(-15f, -5f);
                tRect.localPosition = Vector3.zero;
                tRect.localRotation = Quaternion.identity;
                tRect.localScale = Vector3.one;

                Text t = txtGO.AddComponent<Text>();
                t.text = characters[index].characterName + "\n" + GetElementKoreanName(characters[index].element);
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 13;
                t.alignment = TextAnchor.MiddleLeft;
                t.color = Color.white;
            }
        }

        private void SelectCharacter(int index)
        {
            if (index < 0 || index >= characters.Count) return;
            selectedCharacterIndex = index;
            var data = characters[index];

            // 1. Cập nhật màu các nút bên trái (làm nổi bật nút được chọn)
            if (uiRefs.characterListParent != null)
            {
                int c = 0;
                foreach (Transform child in uiRefs.characterListParent)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = (c == index) ? new Color(0.35f, 0.45f, 0.6f, 0.9f) : new Color(0.18f, 0.22f, 0.28f, 0.8f);
                    }
                    c++;
                }
            }

            // 2. Cập nhật Panel văn bản bên phải
            if (uiRefs.characterNameText != null)
            {
                uiRefs.characterNameText.text = data.characterName;
                uiRefs.characterNameText.color = data.themeColor;
            }

            if (uiRefs.characterElementText != null)
            {
                uiRefs.characterElementText.text = $"Thuộc tính: {GetElementKoreanName(data.element)}";
                uiRefs.characterElementText.color = data.themeColor;
            }

            if (uiRefs.characterStatsText != null)
            {
                uiRefs.characterStatsText.text = 
                    $"HP (Máu Tối Đa):   <color=white>{data.maxHP}</color>\n" +
                    $"ATK (Tấn Công):    <color=white>{data.atk}</color>\n" +
                    $"DEF (Phòng Thủ):   <color=white>{data.def}</color>\n" +
                    $"SPEED (Tốc Độ):    <color=white>{data.speed}</color>\n" +
                    $"CRIT RATE (Tỷ Lệ): <color=white>{data.critRate * 100:F0}%</color>\n" +
                    $"CRIT DMG (Sát Thương): <color=white>{data.critDmg * 100:F0}%</color>\n\n" +
                    $"<color=grey>{data.description}</color>";
            }

            // 3. Cập nhật kỹ năng
            if (uiRefs.skillListParent != null)
            {
                // Clear cũ
                foreach (Transform child in uiRefs.skillListParent)
                {
                    Destroy(child.gameObject);
                }

                // Thêm kỹ năng mới
                foreach (var skill in data.skills)
                {
                    GameObject skillTxtGO = new GameObject("SkillText");
                    skillTxtGO.transform.SetParent(uiRefs.skillListParent, false);
                    RectTransform sRect = skillTxtGO.AddComponent<RectTransform>();
                    sRect.sizeDelta = new Vector2(300f, 24f);
                    sRect.localPosition = Vector3.zero;
                    sRect.localRotation = Quaternion.identity;
                    sRect.localScale = Vector3.one;

                    Text st = skillTxtGO.AddComponent<Text>();
                    st.text = "✦ " + skill;
                    st.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    st.fontSize = 12;
                    st.color = new Color(0.9f, 0.9f, 0.9f);
                }
            }

            // 4. Sinh model 3D tương ứng trong Showroom
            SpawnShowroomModel(data);
        }

        private string GetElementKoreanName(ElementType type)
        {
            switch (type)
            {
                case ElementType.Fire: return "HỎA (Fire)";
                case ElementType.Ice: return "BĂNG (Ice)";
                case ElementType.Lightning: return "LÔI (Lightning)";
                case ElementType.Nature: return "PHONG/MỘC (Nature)";
                case ElementType.Physical: return "VẬT LÝ (Physical)";
                default: return "VÔ HỆ";
            }
        }

        #region Showroom 3D Logic

        private void CreateShowroom()
        {
            // 1. Tạo Root
            showroomRoot = new GameObject("[Showroom_3D]");
            showroomRoot.transform.position = showroomPosition;

            // 2. Tạo bệ đứng (Ground) cho model
            if (showroomEnvironmentPrefab != null)
            {
                GameObject env = Instantiate(showroomEnvironmentPrefab, showroomRoot.transform);
                env.transform.localPosition = Vector3.zero;
                env.transform.localRotation = Quaternion.identity;
                env.transform.localScale = Vector3.one;
            }
            else
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ground.name = "ShowroomGround";
                ground.transform.SetParent(showroomRoot.transform);
                ground.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                ground.transform.localScale = new Vector3(3f, 0.1f, 3f);
                Destroy(ground.GetComponent<Collider>()); // Xóa collider

                Renderer gr = ground.GetComponent<Renderer>();
                Material gm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                gm.color = groundColor;
                gr.material = gm;
            }

            // 3. Tạo Showroom Camera
            GameObject camGO = new GameObject("ShowroomCamera");
            camGO.transform.SetParent(showroomRoot.transform);
            camGO.transform.localPosition = new Vector3(0f, 1.2f, -3.2f);
            camGO.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

            showroomCamera = camGO.AddComponent<Camera>();
            showroomCamera.clearFlags = CameraClearFlags.Color;
            showroomCamera.backgroundColor = cameraBackgroundColor;
            showroomCamera.fieldOfView = 35f;
            showroomCamera.nearClipPlane = 0.1f;
            showroomCamera.farClipPlane = 10f;

            // 4. Tạo Render Texture và liên kết
            showroomRT = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            showroomRT.antiAliasing = 4;
            showroomRT.Create();

            showroomCamera.targetTexture = showroomRT;

            if (uiRefs != null && uiRefs.modelRenderImage != null)
            {
                uiRefs.modelRenderImage.texture = showroomRT;
            }

            // 5. Tạo Ánh sáng Spot Light chiếu rọi model từ trên xuống cho lung linh
            GameObject lightGO = new GameObject("ShowroomLight");
            lightGO.transform.SetParent(showroomRoot.transform);
            lightGO.transform.localPosition = new Vector3(0.5f, 4f, -2f);
            lightGO.transform.rotation = Quaternion.Euler(45f, -15f, 0f);

            showroomLight = lightGO.AddComponent<Light>();
            showroomLight.type = LightType.Directional;
            showroomLight.intensity = 1.3f;
            showroomLight.color = new Color(0.9f, 0.95f, 1.0f);
        }

        private void AdjustModelRotationForShowroom(GameObject model)
        {
            if (model == null) return;
            Transform modelRoot = model.transform.Find("ModelRoot");
            if (modelRoot != null)
            {
                float yRot = modelRoot.localEulerAngles.y;
                if (Mathf.Abs(yRot - 180f) < 5f)
                {
                    model.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    return;
                }
            }
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void SpawnShowroomModel(CharacterMenuData data)
        {
            // Phá hủy model cũ trong showroom
            if (spawnedShowroomModel != null)
            {
                Destroy(spawnedShowroomModel);
                spawnedShowroomModel = null;
            }

            if (showroomRoot == null) return;

            // 1. Nếu nhân vật có Prefab model thật (ví dụ Kazuko)
            if (data.modelPrefab != null)
            {
                spawnedShowroomModel = Instantiate(data.modelPrefab, showroomRoot.transform);
                spawnedShowroomModel.transform.localPosition = Vector3.zero;
                AdjustModelRotationForShowroom(spawnedShowroomModel);
                spawnedShowroomModel.transform.localScale = Vector3.one * modelScale;

                // Tắt các script di chuyển và va chạm trên toàn bộ hierarchy để tránh lỗi showroom và rơi tự do
                foreach (var wasd in spawnedShowroomModel.GetComponentsInChildren<TopDownWASDController>()) { wasd.enabled = false; Destroy(wasd); }
                foreach (var combat in spawnedShowroomModel.GetComponentsInChildren<CombatController>()) { combat.enabled = false; Destroy(combat); }
                foreach (var cc in spawnedShowroomModel.GetComponentsInChildren<CombatCharacter>()) { cc.enabled = false; Destroy(cc); }
                foreach (var characterController in spawnedShowroomModel.GetComponentsInChildren<CharacterController>()) { characterController.enabled = false; Destroy(characterController); }
                foreach (var rb in spawnedShowroomModel.GetComponentsInChildren<Rigidbody>())
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    Destroy(rb);
                }
                foreach (var col in spawnedShowroomModel.GetComponentsInChildren<Collider>()) { col.enabled = false; Destroy(col); }

                // Kích hoạt animation Idle của model thật (nếu có Animator và Controller hợp lệ)
                Animator anim = spawnedShowroomModel.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    // Tải CharacterData để lấy các animation tùy chỉnh
                    CharacterData charData = Resources.Load<CharacterData>($"Characters/{data.characterName}_Data");
                    if (charData == null) charData = Resources.Load<CharacterData>($"Characters/{data.characterName}");
                    if (charData == null) charData = Resources.Load<CharacterData>($"{data.characterName}_Data");

                    if (charData != null)
                    {
                        ApplyRuntimeAnimationOverrides(anim, charData);
                    }

                    if (anim.runtimeAnimatorController != null && anim.layerCount > 0)
                    {
                        bool hasIdle = false;
                        for (int i = 0; i < anim.layerCount; i++)
                        {
                            if (anim.HasState(i, Animator.StringToHash("Idle")))
                            {
                                anim.Play("Idle", i);
                                hasIdle = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // 2. Tạo model Capsule/Cylinder dự phòng bằng code
                spawnedShowroomModel = new GameObject("ShowroomModel_" + data.characterName);
                spawnedShowroomModel.transform.SetParent(showroomRoot.transform);
                spawnedShowroomModel.transform.localPosition = Vector3.zero;
                spawnedShowroomModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Đối diện camera
                spawnedShowroomModel.transform.localScale = Vector3.one * modelScale;

                GameObject body = GameObject.CreatePrimitive(data.element == ElementType.Nature ? PrimitiveType.Cylinder : PrimitiveType.Capsule);
                body.transform.SetParent(spawnedShowroomModel.transform);
                body.transform.localPosition = new Vector3(0f, 1f, 0f);
                Destroy(body.GetComponent<Collider>()); // Xóa collider

                Renderer r = body.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.color = data.themeColor;
                r.material = m;

                // Vũ khí
                GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weapon.transform.SetParent(spawnedShowroomModel.transform);
                weapon.transform.localPosition = new Vector3(0.5f, 1.0f, 0.3f);
                weapon.transform.localRotation = Quaternion.Euler(0, 0, 45);
                weapon.transform.localScale = new Vector3(0.15f, 0.8f, 0.15f);
                Destroy(weapon.GetComponent<Collider>());

                Renderer wr = weapon.GetComponent<Renderer>();
                Material wm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                wm.color = Color.gray;
                wr.material = wm;
            }

            // Gán model 3D vào ShowroomRotator để người dùng có thể kéo xoay
            var rotator = uiRefs?.modelRenderImage?.GetComponent<ShowroomRotator>();
            if (rotator != null)
            {
                rotator.targetModel = spawnedShowroomModel.transform;
            }
        }

        private void CleanShowroom()
        {
            if (spawnedShowroomModel != null)
            {
                Destroy(spawnedShowroomModel);
                spawnedShowroomModel = null;
            }

            if (showroomRoot != null)
            {
                Destroy(showroomRoot);
                showroomRoot = null;
            }

            if (showroomRT != null)
            {
                showroomRT.Release();
                Destroy(showroomRT);
                showroomRT = null;
            }

            showroomCamera = null;
            showroomLight = null;
        }

        private void ApplyRuntimeAnimationOverrides(Animator animator, CharacterData characterData)
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

        #endregion
    }
}
