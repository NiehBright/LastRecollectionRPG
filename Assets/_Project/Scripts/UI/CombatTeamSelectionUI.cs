using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using BLINK.Controller;

namespace RPG.Combat
{
    public class CombatTeamSelectionUI : MonoBehaviour
    {
        public static CombatTeamSelectionUI Instance { get; private set; }

        private OverworldMonster currentMonster;
        private CharacterData[] slots = new CharacterData[4]; // 4 Slot đội hình
        private int activeSelectingSlot = -1;

        // UI GameObjects
        public GameObject canvasGO;
        private Canvas selectionCanvas;
        private RawImage showroomRawImage;
        
        // 3D Showroom GameObjects
        private GameObject showroomRoot;
        private Camera showroomCamera;
        private RenderTexture showroomRT;
        private List<GameObject> spawnedModels = new List<GameObject>();
        private Vector3 baseShowroomPos = new Vector3(2000f, -1100f, 2000f);

        // UI References
        private Text[] slotNameTexts = new Text[4];
        private Text[] slotElementTexts = new Text[4];
        private Button[] slotButtons = new Button[4];
        private GameObject characterListPanel;
        private Transform listContentContainer;

        [Header("Showroom Visuals")]
        public Color groundColor = new Color(0.06f, 0.08f, 0.12f);
        public Color cameraBackgroundColor = new Color(0.05f, 0.06f, 0.08f);
        public GameObject showroomEnvironmentPrefab; // Kéo thả prefab môi trường showroom tùy chọn
        public GameObject pedestalPrefab;      // Kéo thả Prefab Bệ đá ở đây để đổi bệ đứng
        public GameObject backgroundPrefab;    // Kéo thả Prefab Phông nền ở đây để đổi phông

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Mở giao diện chọn đội hình khi chạm trán quái ở Overworld
        /// </summary>
        public void OpenUI(OverworldMonster monster)
        {
#if UNITY_EDITOR
            CombatEditorUtility.FixAllAnimatorControllers();
#endif
            currentMonster = monster;
            activeSelectingSlot = -1;

            // Nạp lại dữ liệu cũ hoặc mặc định (lấy từ CharacterMenuManager nếu có, ngược lại lấy từ Resources)
            List<CharacterData> availableList = new List<CharacterData>();
            if (CharacterMenuManager.Instance != null && CharacterMenuManager.Instance.characters != null)
            {
                foreach (var cmd in CharacterMenuManager.Instance.characters)
                {
                    CharacterData cd = ConvertMenuDataToCharacterData(cmd);
                    if (cd != null) availableList.Add(cd);
                }
            }

            if (availableList.Count == 0)
            {
                CharacterData[] availableChars = Resources.LoadAll<CharacterData>("Characters");
                availableList.AddRange(availableChars);
            }

            for (int i = 0; i < 4; i++)
            {
                if (i < availableList.Count)
                {
                    slots[i] = availableList[i];
                }
                else
                {
                    slots[i] = null;
                }
            }

            // Đảm bảo có EventSystem
            EnsureEventSystem();

            // Thử tải UI từ Prefab trong Resources
            GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/CombatTeamSelectionUI");
            if (uiPrefab != null)
            {
                canvasGO = Instantiate(uiPrefab);
                DontDestroyOnLoad(canvasGO);
                
                var refs = canvasGO.GetComponent<CombatTeamSelectionUIReferences>();
                if (refs != null)
                {
                    showroomRawImage = refs.showroomRawImage;
                    slotNameTexts = refs.slotNameTexts;
                    slotElementTexts = refs.slotElementTexts;
                    characterListPanel = refs.characterListPanel;
                    listContentContainer = refs.listContentContainer;
                    
                    for (int i = 0; i < 4; i++)
                    {
                        int index = i;
                        if (refs.slotButtons[i] != null)
                        {
                            refs.slotButtons[index].onClick.RemoveAllListeners();
                            refs.slotButtons[index].onClick.AddListener(() => OpenCharacterSelectionList(index));
                        }
                    }
                    
                    if (refs.btnStart != null)
                    {
                        refs.btnStart.onClick.RemoveAllListeners();
                        refs.btnStart.onClick.AddListener(StartCombatBattle);
                    }
                    if (refs.btnCancel != null)
                    {
                        refs.btnCancel.onClick.RemoveAllListeners();
                        refs.btnCancel.onClick.AddListener(CloseUI);
                    }
                }
            }
            else
            {
                // Fallback tạo giao diện động bằng code
                CreateUIElements();
            }

            // 1. Tạo môi trường 3D Showroom
            Setup3DShowroom();

            // 3. Render các mô hình nhân vật
            UpdateShowroomModels();
        }

        private void CloseUI()
        {
            // Hủy mô hình
            ClearShowroomModels();

            // Hủy 3D Showroom
            if (showroomRoot != null) Destroy(showroomRoot);
            if (showroomRT != null)
            {
                showroomRT.Release();
                Destroy(showroomRT);
            }

            // Hủy Canvas UI
            if (canvasGO != null) Destroy(canvasGO);

            // Mở khóa cho người chơi và reset trạng thái chuyển cảnh tấn công
            var playerControllers = FindObjectsByType<TopDownWASDController>(FindObjectsSortMode.None);
            foreach (var pc in playerControllers)
            {
                if (pc != null)
                {
                    pc.movementEnabled = true;
                    pc.cameraEnabled = true;

                    var combatCtrl = pc.GetComponent<BLINK.Controller.CombatController>();
                    if (combatCtrl != null)
                    {
                        combatCtrl._isTransitioning = false;
                    }
                }
            }
        }

        public void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void Setup3DShowroom()
        {
            if (showroomRoot != null) Destroy(showroomRoot);

            showroomRoot = new GameObject("TeamShowroomRoot");
            showroomRoot.transform.position = baseShowroomPos;

            // 1. Tạo ánh sáng
            GameObject lightGO = new GameObject("ShowroomLight");
            lightGO.transform.SetParent(showroomRoot.transform);
            lightGO.transform.localPosition = new Vector3(0f, 5f, -5f);
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.3f;
            lightGO.transform.rotation = Quaternion.Euler(30f, 30f, 0f);

            // 2. Tạo nền sàn
            if (showroomEnvironmentPrefab != null)
            {
                GameObject env = Instantiate(showroomEnvironmentPrefab, showroomRoot.transform);
                env.transform.localPosition = Vector3.zero;
                env.transform.localRotation = Quaternion.identity;
                env.transform.localScale = Vector3.one;
            }
            else
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "ShowroomFloor";
                floor.transform.SetParent(showroomRoot.transform);
                floor.transform.localPosition = Vector3.zero;
                floor.transform.localScale = new Vector3(3f, 1f, 3f);
                DestroyImmediate(floor.GetComponent<Collider>());
                
                Renderer r = floor.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = groundColor;
                r.material = mat;
            }

            // 2.1. Tạo Phông nền phía sau (Background)
            if (backgroundPrefab != null)
            {
                GameObject bgGO = Instantiate(backgroundPrefab, showroomRoot.transform);
                bgGO.transform.localPosition = new Vector3(0f, 0f, 3f); // Phía sau nhân vật
                bgGO.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Tạo một bức tường phông nền mặc định phía sau
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
                wall.name = "DefaultBackgroundWall";
                wall.transform.SetParent(showroomRoot.transform);
                wall.transform.localPosition = new Vector3(0f, 2f, 3.5f); // Phía sau nhân vật
                wall.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Dựng đứng
                wall.transform.localScale = new Vector3(5f, 1f, 3f);
                DestroyImmediate(wall.GetComponent<Collider>());
                
                Renderer wr = wall.GetComponent<Renderer>();
                Material wm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                wm.color = new Color(0.04f, 0.05f, 0.07f); // Màu tối sẫm sang trọng
                wr.material = wm;
            }

            // 3. Tạo Camera
            GameObject cameraGO = new GameObject("ShowroomCamera");
            cameraGO.transform.SetParent(showroomRoot.transform);
            cameraGO.transform.localPosition = new Vector3(0f, 1.3f, -5.2f);
            cameraGO.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);
            showroomCamera = cameraGO.AddComponent<Camera>();
            showroomCamera.clearFlags = CameraClearFlags.SolidColor;
            showroomCamera.backgroundColor = cameraBackgroundColor;
            showroomCamera.fieldOfView = 50f;

            // 4. Tạo Render Texture
            showroomRT = new RenderTexture(1280, 720, 24);
            showroomCamera.targetTexture = showroomRT;
            if (showroomRawImage != null)
            {
                showroomRawImage.texture = showroomRT;
            }
        }

        private void ClearShowroomModels()
        {
            foreach (var go in spawnedModels)
            {
                if (go != null) Destroy(go);
            }
            spawnedModels.Clear();
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

        private void UpdateShowroomModels()
        {
            ClearShowroomModels();

            // Tọa độ X cho 4 Slot đứng cạnh nhau
            float[] slotXOffsets = new float[] { -2.2f, -0.7f, 0.7f, 2.2f };

            for (int i = 0; i < 4; i++)
            {
                CharacterData data = slots[i];
                Vector3 spawnPos = baseShowroomPos + new Vector3(slotXOffsets[i], 0f, 0.2f);

                if (data != null)
                {
                    // Tạo Bệ đá cho nhân vật này (nếu có thiết lập, ngược lại tự tạo bệ hình Cylinder xám đá)
                    GameObject pedestalGO;
                    if (pedestalPrefab != null)
                    {
                        pedestalGO = Instantiate(pedestalPrefab, spawnPos, Quaternion.identity, showroomRoot.transform);
                    }
                    else
                    {
                        pedestalGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pedestalGO.name = $"Pedestal_{data.characterName}";
                        pedestalGO.transform.SetParent(showroomRoot.transform);
                        pedestalGO.transform.position = spawnPos + new Vector3(0f, -0.05f, 0f);
                        pedestalGO.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
                        DestroyImmediate(pedestalGO.GetComponent<Collider>());
                        
                        Renderer pRend = pedestalGO.GetComponent<Renderer>();
                        Material pMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        pMat.color = new Color(0.25f, 0.27f, 0.3f); // Xám đá tối cổ kính
                        pRend.material = pMat;
                    }
                    spawnedModels.Add(pedestalGO);

                    // Thử nạp Prefab nhân vật từ CharacterMenuManager trước để khớp 100% với model thật, sau đó mới tìm trong Resources
                    GameObject prefab = null;
                    if (CharacterMenuManager.Instance != null && CharacterMenuManager.Instance.characters != null)
                    {
                        var menuChar = CharacterMenuManager.Instance.characters.Find(c => c.characterName == data.characterName);
                        if (menuChar != null) prefab = menuChar.modelPrefab;
                    }
                    if (prefab == null) prefab = Resources.Load<GameObject>($"Prefabs/Characters/{data.characterName}");
                    if (prefab == null) prefab = Resources.Load<GameObject>($"Prefabs/{data.characterName}");

                    GameObject spawnedGO;
                    if (prefab != null)
                    {
                        spawnedGO = Instantiate(prefab, spawnPos, Quaternion.identity, showroomRoot.transform);
                        AdjustModelRotationForShowroom(spawnedGO);
                        
                        // Tắt toàn bộ di chuyển và vật lý trên model thực để tránh lệch vị trí
                        foreach (var wasd in spawnedGO.GetComponentsInChildren<TopDownWASDController>()) { wasd.enabled = false; DestroyImmediate(wasd); }
                        foreach (var combat in spawnedGO.GetComponentsInChildren<CombatController>()) { combat.enabled = false; DestroyImmediate(combat); }
                        foreach (var cc in spawnedGO.GetComponentsInChildren<CombatCharacter>()) { cc.enabled = false; DestroyImmediate(cc); }
                        foreach (var muryo in spawnedGO.GetComponentsInChildren<Muryotaisu.MuryotaisuController>()) { muryo.enabled = false; DestroyImmediate(muryo); }
                        foreach (var characterController in spawnedGO.GetComponentsInChildren<CharacterController>()) { characterController.enabled = false; DestroyImmediate(characterController); }
                        foreach (var rb in spawnedGO.GetComponentsInChildren<Rigidbody>())
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                            DestroyImmediate(rb);
                        }
                        foreach (var col in spawnedGO.GetComponentsInChildren<Collider>()) { col.enabled = false; DestroyImmediate(col); }

                        // Nạp các animation tùy chỉnh của nhân vật từ CharacterData
                        Animator anim = spawnedGO.GetComponentInChildren<Animator>();
                        if (anim != null)
                        {
                            ApplyRuntimeAnimationOverrides(anim, data);
                            if (anim.runtimeAnimatorController != null && anim.layerCount > 0)
                            {
                                if (anim.HasState(0, Animator.StringToHash("Idle")))
                                {
                                    anim.Play("Idle");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Sinh mô hình Capsule giả lập giống y như trong game
                        spawnedGO = new GameObject($"ProceduralModel_{data.characterName}");
                        spawnedGO.transform.SetParent(showroomRoot.transform);
                        spawnedGO.transform.position = spawnPos;
                        spawnedGO.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Đối diện camera

                        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        body.transform.SetParent(spawnedGO.transform);
                        body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                        body.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
                        Destroy(body.GetComponent<Collider>());

                        Renderer renderer = body.GetComponent<Renderer>();
                        Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        bodyMat.color = data.themeColor;
                        renderer.material = bodyMat;

                        // Vũ khí
                        GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        weapon.transform.SetParent(spawnedGO.transform);
                        weapon.transform.localPosition = new Vector3(0.5f, 1.0f, 0.3f);
                        weapon.transform.localRotation = Quaternion.Euler(0, 0, 45);
                        weapon.transform.localScale = new Vector3(0.15f, 0.8f, 0.15f);
                        Destroy(weapon.GetComponent<Collider>());
                        Renderer weaponRenderer = weapon.GetComponent<Renderer>();
                        Material weaponMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        weaponMat.color = Color.gray;
                        weaponRenderer.material = weaponMat;
                    }

                    spawnedModels.Add(spawnedGO);

                    // Cập nhật Text Tên & Hệ
                    if (slotNameTexts[i] != null) slotNameTexts[i].text = data.characterName;
                    if (slotElementTexts[i] != null)
                    {
                        slotElementTexts[i].text = data.element.ToString().ToUpper();
                        slotElementTexts[i].color = GetElementColor(data.element);
                    }
                }
                else
                {
                    // Slot trống: Hiện thị dấu cộng gợi ý chọn nhân vật
                    if (slotNameTexts[i] != null) slotNameTexts[i].text = "TRỐNG";
                    if (slotElementTexts[i] != null)
                    {
                        slotElementTexts[i].text = "Click để chọn";
                        slotElementTexts[i].color = Color.gray;
                    }
                }
            }
        }

        public void CreateUIElements()
        {
            if (canvasGO != null) Destroy(canvasGO);

            canvasGO = new GameObject("TeamSelectionUI");
            selectionCanvas = canvasGO.AddComponent<Canvas>();
            selectionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            selectionCanvas.sortingOrder = 999;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel nền mờ phong cách HSR
            GameObject bgPanel = new GameObject("BG_Panel");
            bgPanel.transform.SetParent(canvasGO.transform);
            RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgPanel.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.04f, 0.06f, 0.65f); // Overlay phủ mờ nhẹ

            // RawImage hiển thị Showroom 3D
            GameObject rawGO = new GameObject("Showroom_RawImage");
            rawGO.transform.SetParent(canvasGO.transform);
            RectTransform rawRect = rawGO.AddComponent<RectTransform>();
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = new Vector2(0f, 100f);
            rawRect.offsetMax = new Vector2(0f, -100f);
            showroomRawImage = rawGO.AddComponent<RawImage>();
            showroomRawImage.texture = showroomRT;

            // Panel Tiêu đề
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(canvasGO.transform);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.95f);
            titleRect.anchorMax = new Vector2(0.5f, 0.95f);
            titleRect.anchoredPosition = new Vector2(0f, -30f);
            titleRect.sizeDelta = new Vector2(600f, 50f);
            Text titleTxt = titleGO.AddComponent<Text>();
            titleTxt.text = "THIẾT LẬP ĐỘI HÌNH CHIẾN ĐẤU";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 28;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = Color.white;
            Outline titleOutline = titleGO.AddComponent<Outline>();
            titleOutline.effectColor = Color.black;

            // Thiết lập 4 Slot trên Canvas
            float[] uiSlotXPositions = new float[] { -300f, -100f, 100f, 300f };

            for (int i = 0; i < 4; i++)
            {
                int slotIndex = i; // Khóa biến loop
                
                // Panel chứa thông tin trên đầu Model (Name + Element)
                GameObject infoPanel = new GameObject($"InfoPanel_{i}");
                infoPanel.transform.SetParent(canvasGO.transform);
                RectTransform infoRect = infoPanel.AddComponent<RectTransform>();
                infoRect.anchorMin = new Vector2(0.5f, 0.5f);
                infoRect.anchorMax = new Vector2(0.5f, 0.5f);
                infoRect.anchoredPosition = new Vector3(uiSlotXPositions[i], 180f, 0f);
                infoRect.sizeDelta = new Vector2(180f, 80f);

                // Nền thông tin mờ nhẹ
                Image infoBg = infoPanel.AddComponent<Image>();
                infoBg.color = new Color(0.08f, 0.11f, 0.16f, 0.8f);

                // Text Tên Nhân Vật
                GameObject nameGO = new GameObject("NameText");
                nameGO.transform.SetParent(infoPanel.transform);
                RectTransform nameRect = nameGO.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0f, 0.5f);
                nameRect.anchorMax = new Vector2(1f, 1f);
                nameRect.offsetMin = new Vector2(5f, 0f);
                nameRect.offsetMax = new Vector2(-5f, -5f);
                Text nTxt = nameGO.AddComponent<Text>();
                nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nTxt.fontSize = 18;
                nTxt.alignment = TextAnchor.LowerCenter;
                nTxt.color = Color.white;
                slotNameTexts[i] = nTxt;

                // Text Hệ
                GameObject elementGO = new GameObject("ElementText");
                elementGO.transform.SetParent(infoPanel.transform);
                RectTransform elRect = elementGO.AddComponent<RectTransform>();
                elRect.anchorMin = new Vector2(0f, 0f);
                elRect.anchorMax = new Vector2(1f, 0.5f);
                elRect.offsetMin = new Vector2(5f, 5f);
                elRect.offsetMax = new Vector2(-5f, 0f);
                Text eTxt = elementGO.AddComponent<Text>();
                eTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                eTxt.fontSize = 14;
                eTxt.alignment = TextAnchor.UpperCenter;
                slotElementTexts[i] = eTxt;

                // Button vô hình bao phủ lấy 3D Model để click
                GameObject btnGO = new GameObject($"SlotButton_{i}");
                btnGO.transform.SetParent(canvasGO.transform);
                RectTransform btnRect = btnGO.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 0.5f);
                btnRect.anchorMax = new Vector2(0.5f, 0.5f);
                btnRect.anchoredPosition = new Vector3(uiSlotXPositions[i], -10f, 0f);
                btnRect.sizeDelta = new Vector2(180f, 280f);
                
                // Nút trong suốt hoàn toàn
                Image btnImg = btnGO.AddComponent<Image>();
                btnImg.color = new Color(1f, 1f, 1f, 0.01f);
                Button btn = btnGO.AddComponent<Button>();
                btn.onClick.AddListener(() => OpenCharacterSelectionList(slotIndex));
                slotButtons[i] = btn;
            }

            // --- NÚT BẮT ĐẦU VÀ HỦY ---
            // Bắt Đầu
            GameObject startGO = new GameObject("Button_Start");
            startGO.transform.SetParent(canvasGO.transform);
            RectTransform startRect = startGO.AddComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0.08f);
            startRect.anchorMax = new Vector2(0.5f, 0.08f);
            startRect.anchoredPosition = new Vector2(150f, 0f);
            startRect.sizeDelta = new Vector2(180f, 48f);
            Image startImg = startGO.AddComponent<Image>();
            startImg.color = new Color(0.12f, 0.72f, 0.33f, 1f); // Màu xanh lá bắt mắt
            Button startBtn = startGO.AddComponent<Button>();
            startBtn.onClick.AddListener(StartCombatBattle);

            GameObject startTextGO = new GameObject("Text");
            startTextGO.transform.SetParent(startGO.transform);
            RectTransform stRect = startTextGO.AddComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;
            Text stTxt = startTextGO.AddComponent<Text>();
            stTxt.text = "BẮT ĐẦU";
            stTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stTxt.fontSize = 20;
            stTxt.alignment = TextAnchor.MiddleCenter;
            stTxt.color = Color.white;

            // Hủy
            GameObject cancelGO = new GameObject("Button_Cancel");
            cancelGO.transform.SetParent(canvasGO.transform);
            RectTransform cancelRect = cancelGO.AddComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0.08f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.08f);
            cancelRect.anchoredPosition = new Vector2(-150f, 0f);
            cancelRect.sizeDelta = new Vector2(180f, 48f);
            Image cancelImg = cancelGO.AddComponent<Image>();
            cancelImg.color = new Color(0.78f, 0.23f, 0.23f, 1f); // Màu đỏ
            Button cancelBtn = cancelGO.AddComponent<Button>();
            cancelBtn.onClick.AddListener(CloseUI);

            GameObject cancelTextGO = new GameObject("Text");
            cancelTextGO.transform.SetParent(cancelGO.transform);
            RectTransform ctRect = cancelTextGO.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.offsetMin = Vector2.zero;
            ctRect.offsetMax = Vector2.zero;
            Text ctTxt = cancelTextGO.AddComponent<Text>();
            ctTxt.text = "HỦY";
            ctTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctTxt.fontSize = 20;
            ctTxt.alignment = TextAnchor.MiddleCenter;
            ctTxt.color = Color.white;

            // --- PANEL POPUP CHỌN NHÂN VẬT ---
            characterListPanel = new GameObject("CharacterListPanel");
            characterListPanel.transform.SetParent(canvasGO.transform);
            RectTransform panelRect = characterListPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(350f, 400f);
            panelRect.anchoredPosition = Vector2.zero;
            Image pImg = characterListPanel.AddComponent<Image>();
            pImg.color = new Color(0.06f, 0.09f, 0.14f, 0.95f); // Màu tối đặc nền
            Outline pOutline = characterListPanel.AddComponent<Outline>();
            pOutline.effectColor = Color.gray;

            // Tiêu đề popup
            GameObject popupTitle = new GameObject("PopupTitle");
            popupTitle.transform.SetParent(characterListPanel.transform);
            RectTransform ptRect = popupTitle.AddComponent<RectTransform>();
            ptRect.anchorMin = new Vector2(0.5f, 1f);
            ptRect.anchorMax = new Vector2(0.5f, 1f);
            ptRect.anchoredPosition = new Vector2(0f, -20f);
            ptRect.sizeDelta = new Vector2(300f, 30f);
            Text ptTxt = popupTitle.AddComponent<Text>();
            ptTxt.text = "CHỌN THÀNH VIÊN";
            ptTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ptTxt.fontSize = 18;
            ptTxt.alignment = TextAnchor.MiddleCenter;
            ptTxt.color = Color.yellow;

            // Nút Close Popup
            GameObject closePopGO = new GameObject("CloseButton");
            closePopGO.transform.SetParent(characterListPanel.transform);
            RectTransform cpRect = closePopGO.AddComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(1f, 1f);
            cpRect.anchorMax = new Vector2(1f, 1f);
            cpRect.anchoredPosition = new Vector2(-15f, -15f);
            cpRect.sizeDelta = new Vector2(25f, 25f);
            Image cpImg = closePopGO.AddComponent<Image>();
            cpImg.color = Color.gray;
            Button cpBtn = closePopGO.AddComponent<Button>();
            cpBtn.onClick.AddListener(() => characterListPanel.SetActive(false));
            GameObject cpText = new GameObject("Text");
            cpText.transform.SetParent(closePopGO.transform);
            Text cpt = cpText.AddComponent<Text>();
            cpt.text = "X";
            cpt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cpt.fontSize = 14;
            cpt.color = Color.white;
            cpt.alignment = TextAnchor.MiddleCenter;
            cpText.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);

            // Vùng ScrollView chứa danh sách
            GameObject scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(characterListPanel.transform);
            RectTransform scrollRectTransform = scrollGO.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0.05f);
            scrollRectTransform.anchorMax = new Vector2(1f, 0.82f);
            scrollRectTransform.offsetMin = new Vector2(15f, 15f);
            scrollRectTransform.offsetMax = new Vector2(-15f, -15f);
            
            // Image che chắn mask
            scrollGO.AddComponent<Image>().color = new Color(1,1,1,0.01f);
            Mask scrollMask = scrollGO.AddComponent<Mask>();
            scrollMask.showMaskGraphic = false;

            ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Content
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 400f);
            contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
            contentRect.offsetMax = new Vector2(0f, contentRect.offsetMax.y);

            VerticalLayoutGroup vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.spacing = 8f;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;

            ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            listContentContainer = contentGO.transform;

            characterListPanel.SetActive(false); // Ẩn popup mặc định

            // Thiết lập references để đóng gói Prefab
            var refs = canvasGO.AddComponent<CombatTeamSelectionUIReferences>();
            refs.showroomRawImage = showroomRawImage;
            refs.characterListPanel = characterListPanel;
            refs.listContentContainer = listContentContainer;
            refs.btnStart = startBtn;
            refs.btnCancel = cancelBtn;
            for (int i = 0; i < 4; i++)
            {
                refs.slotNameTexts[i] = slotNameTexts[i];
                refs.slotElementTexts[i] = slotElementTexts[i];
                refs.slotButtons[i] = slotButtons[i];
            }
        }

        private void OpenCharacterSelectionList(int slotIndex)
        {
            activeSelectingSlot = slotIndex;
            characterListPanel.SetActive(true);

            // Xóa danh sách cũ
            foreach (Transform child in listContentContainer)
            {
                Destroy(child.gameObject);
            }

            // Nạp toàn bộ nhân vật có sẵn (lấy từ E menu - CharacterMenuManager)
            List<CharacterData> allChars = new List<CharacterData>();
            if (CharacterMenuManager.Instance != null && CharacterMenuManager.Instance.characters != null)
            {
                foreach (var cmd in CharacterMenuManager.Instance.characters)
                {
                    CharacterData cd = ConvertMenuDataToCharacterData(cmd);
                    if (cd != null) allChars.Add(cd);
                }
            }

            if (allChars.Count == 0)
            {
                CharacterData[] availableChars = Resources.LoadAll<CharacterData>("Characters");
                allChars.AddRange(availableChars);
            }

            // Nút "GỠ BỎ NHÂN VẬT" (Remove)
            GameObject removeGO = new GameObject("RemoveButton");
            removeGO.transform.SetParent(listContentContainer);
            RectTransform rRect = removeGO.AddComponent<RectTransform>();
            rRect.sizeDelta = new Vector2(280f, 40f);
            Image rImg = removeGO.AddComponent<Image>();
            rImg.color = new Color(0.5f, 0.1f, 0.1f, 0.8f);
            Button rBtn = removeGO.AddComponent<Button>();
            rBtn.onClick.AddListener(() => SelectCharacterForActiveSlot(null));
            
            GameObject rTextGO = new GameObject("Text");
            rTextGO.transform.SetParent(removeGO.transform);
            Text rTxt = rTextGO.AddComponent<Text>();
            rTxt.text = "[ TRỐNG / GỠ BỎ ]";
            rTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rTxt.fontSize = 16;
            rTxt.color = Color.white;
            rTxt.alignment = TextAnchor.MiddleCenter;
            rTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 40);

            foreach (var charData in allChars)
            {
                if (charData == null) continue;

                // Kiểm tra xem nhân vật này đã được chọn ở slot khác chưa để tránh chọn trùng
                bool isAlreadyPicked = false;
                for (int i = 0; i < 4; i++)
                {
                    if (i != slotIndex && slots[i] != null && slots[i].characterId == charData.characterId)
                    {
                        isAlreadyPicked = true;
                        break;
                    }
                }

                GameObject itemGO = new GameObject("CharacterItem_" + charData.characterName);
                itemGO.transform.SetParent(listContentContainer);
                RectTransform itemRect = itemGO.AddComponent<RectTransform>();
                itemRect.sizeDelta = new Vector2(280f, 50f);

                Image itemImg = itemGO.AddComponent<Image>();
                itemImg.color = isAlreadyPicked ? new Color(0.15f, 0.18f, 0.22f, 0.5f) : new Color(0.12f, 0.16f, 0.22f, 0.9f);

                Button itemBtn = itemGO.AddComponent<Button>();
                if (!isAlreadyPicked)
                {
                    itemBtn.onClick.AddListener(() => SelectCharacterForActiveSlot(charData));
                }

                // Text thông tin nhân vật
                GameObject txtGO = new GameObject("Text");
                txtGO.transform.SetParent(itemGO.transform);
                RectTransform tRect = txtGO.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = new Vector2(10f, 0f);
                tRect.offsetMax = new Vector2(-10f, 0f);

                Text txt = txtGO.AddComponent<Text>();
                txt.text = $"{charData.characterName} ({charData.element})";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 15;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = isAlreadyPicked ? Color.gray : Color.white;
            }
        }

        private void SelectCharacterForActiveSlot(CharacterData data)
        {
            if (activeSelectingSlot >= 0 && activeSelectingSlot < 4)
            {
                slots[activeSelectingSlot] = data;
                UpdateShowroomModels();
            }

            characterListPanel.SetActive(false);
        }

        private void StartCombatBattle()
        {
            // Kiểm tra xem đội hình có ít nhất 1 người không
            List<CharacterData> alliesToFight = new List<CharacterData>();
            for (int i = 0; i < 4; i++)
            {
                if (slots[i] != null) alliesToFight.Add(slots[i]);
            }

            if (alliesToFight.Count == 0)
            {
                EnsureEventSystem();
                Debug.LogWarning("[CombatTeamSelectionUI] Bạn phải chọn ít nhất 1 nhân vật tham chiến!");
                return;
            }

            // Ghi nhận đội hình vào CombatTeamManager
            CombatTeamManager.Clear();
            CombatTeamManager.SelectedAllies = alliesToFight;

            // Xác định danh sách kẻ địch chiến đấu và lọc bỏ các phần tử null
            if (currentMonster != null)
            {
                List<CharacterData> cleanEnemyTeam = new List<CharacterData>();
                if (currentMonster.enemyTeam != null)
                {
                    foreach (var e in currentMonster.enemyTeam)
                    {
                        if (e != null) cleanEnemyTeam.Add(e);
                    }
                }

                if (cleanEnemyTeam.Count > 0)
                {
                    CombatTeamManager.SelectedEnemies = cleanEnemyTeam;
                }
                else
                {
                    // Tự động gán quái vật mặc định nếu kẻ địch trống
                    CombatTeamManager.SelectedEnemies = currentMonster.GetDefaultEnemyTeam();
                }

                CombatTeamManager.MonsterToDestroyId = currentMonster.uniqueId;
            }

            CombatTeamManager.IsEnteringFromOverworld = true;
            CombatTeamManager.CombatResult = CombatResultType.NONE;

            // Giải phóng UI và Showroom trước khi load scene
            ClearShowroomModels();
            if (showroomRoot != null) Destroy(showroomRoot);
            if (showroomRT != null)
            {
                showroomRT.Release();
                Destroy(showroomRT);
            }
            if (canvasGO != null) Destroy(canvasGO);

            Debug.Log("[CombatTeamSelectionUI] Chuyển cảnh sang TurnBase...");
            SceneManager.LoadScene("TurnBase");
        }

        private Color GetElementColor(ElementType type)
        {
            switch (type)
            {
                case ElementType.Fire: return new Color(1.0f, 0.3f, 0.1f);
                case ElementType.Ice: return new Color(0.3f, 0.7f, 1.0f);
                case ElementType.Lightning: return new Color(1.0f, 0.9f, 0.2f);
                case ElementType.Nature: return new Color(0.2f, 0.8f, 0.3f);
                case ElementType.Physical: return new Color(0.8f, 0.8f, 0.8f);
                case ElementType.Ether: return new Color(0.65f, 0.25f, 0.95f);
                default: return Color.white;
            }
        }
        private CharacterData ConvertMenuDataToCharacterData(CharacterMenuData menuData)
        {
            if (menuData == null) return null;

            // Kiểm tra xem đã có CharacterData thực tế trong Resources trùng tên chưa (thử cả hậu tố _Data)
            CharacterData loaded = Resources.Load<CharacterData>($"Characters/{menuData.characterName}_Data");
            if (loaded == null) loaded = Resources.Load<CharacterData>($"Characters/{menuData.characterName}");
            if (loaded == null) loaded = Resources.Load<CharacterData>($"{menuData.characterName}_Data");
            if (loaded == null) loaded = Resources.Load<CharacterData>($"{menuData.characterName}");
            if (loaded != null) return loaded;

            // Nếu không có, tạo động một bản in-memory giống như CombatSetup làm
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = "menu_" + menuData.characterName.ToLower().Replace(" ", "_");
            data.characterName = menuData.characterName;
            data.element = menuData.element;
            data.themeColor = menuData.themeColor;
            data.baseMaxHP = menuData.maxHP;
            data.baseATK = menuData.atk;
            data.baseDEF = menuData.def;
            data.baseSpeed = menuData.speed;
            data.baseCritRate = menuData.critRate;
            data.baseCritDMG = menuData.critDmg;

            // Tạo các kỹ năng cơ bản
            data.skillBasic = ScriptableObject.CreateInstance<SkillData>();
            data.skillBasic.skillName = menuData.skills.Count > 0 ? menuData.skills[0] : "Tấn Công Thường";
            data.skillBasic.skillType = SkillType.BASIC;
            data.skillBasic.damageMultiplier = 1.0f;
            data.skillBasic.targetType = TargetType.SINGLE;

            if (menuData.skills.Count > 1)
            {
                data.skillSpecial = ScriptableObject.CreateInstance<SkillData>();
                data.skillSpecial.skillName = menuData.skills[1];
                data.skillSpecial.skillType = SkillType.SPECIAL;
                data.skillSpecial.damageMultiplier = 1.4f;
                data.skillSpecial.targetType = TargetType.SINGLE;
            }

            if (menuData.skills.Count > 2)
            {
                data.skillUltimate = ScriptableObject.CreateInstance<SkillData>();
                data.skillUltimate.skillName = menuData.skills[2];
                data.skillUltimate.skillType = SkillType.ULTIMATE;
                data.skillUltimate.damageMultiplier = 2.2f;
                data.skillUltimate.targetType = TargetType.AOE;
                data.skillUltimate.energyCost = 100f;
            }

            return data;
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
    }
}
