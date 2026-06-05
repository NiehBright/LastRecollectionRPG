using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace RPG.Combat
{
    public class CombatSetupWindow : EditorWindow
    {
        [MenuItem("Tools/RPG Combat Setup")]
        public static void ShowWindow()
        {
            GetWindow<CombatSetupWindow>("RPG Combat Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("RPG Combat Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("Sử dụng công cụ này để nhanh chóng thiết lập môi trường chiến đấu RPG 3D trong Scene hiện tại. Công cụ sẽ tự động thêm camera, ánh sáng, mặt đất và Game Manager.", MessageType.Info);
            
            GUILayout.Space(15);

            if (GUILayout.Button("Thiết Lập Trận Đấu (Setup Combat Scene)", GUILayout.Height(40)))
            {
                SetupScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Khởi Tạo UI Prefab (Build UI Prefab)", GUILayout.Height(40)))
            {
                BuildCombatUIPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Khởi Tạo Menu Nhân Vật Prefab (Build Character Menu Prefab)", GUILayout.Height(40)))
            {
                BuildCharacterMenuUIPrefab();
            }
        }

        private void SetupScene()
        {
            // 1. Tìm hoặc tạo GameObject Setup chính
            GameObject setupGO = GameObject.Find("_GameSetup");
            if (setupGO == null)
            {
                setupGO = new GameObject("_GameSetup");
                Undo.RegisterCreatedObjectUndo(setupGO, "Create _GameSetup");
            }

            // 2. Thêm script CombatSetup
            CombatSetup setupComponent = setupGO.GetComponent<CombatSetup>();
            if (setupComponent == null)
            {
                setupComponent = setupGO.AddComponent<CombatSetup>();
                Undo.AddComponent<CombatSetup>(setupGO);
            }

            // 3. Đảm bảo có Camera chính
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                mainCam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(camGO, "Create Camera");
            }

            // Cấu hình vị trí camera mặc định
            mainCam.transform.position = new Vector3(0f, 6.5f, -12f);
            mainCam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            mainCam.backgroundColor = new Color(0.1f, 0.12f, 0.15f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;

            // 4. Đảm bảo có Directional Light
            Light dirLight = FindAnyObjectByType<Light>();
            if (dirLight == null || dirLight.type != LightType.Directional)
            {
                GameObject lightGO = new GameObject("DirectionalLight");
                Light light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1.2f;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");
            }

            // Chọn GameObject mới tạo trong Hierarchy
            Selection.activeGameObject = setupGO;

            // Hiển thị thông báo
            EditorUtility.DisplayDialog(
                "Thiết lập thành công!", 
                "Đã khởi tạo môi trường chiến đấu RPG 3D thành công.\n\n" +
                "GameObject '_GameSetup' đã được chọn và cấu hình sẵn sàng. Hãy bấm Play để chơi thử!", 
                "Tuyệt vời"
            );

            Debug.Log("[CombatSetupWindow] Thiết lập Scene hoàn tất.");
        }

        private static void BuildCombatUIPrefab()
        {
            // 1. Tạo Canvas chính
            GameObject canvasGO = new GameObject("CombatUI_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            
            CombatUIReferences refs = canvasGO.AddComponent<CombatUIReferences>();

            // 2. Tạo Hàng chờ Turn Queue
            GameObject turnPanelGO = new GameObject("TurnQueuePanel");
            turnPanelGO.transform.SetParent(canvasGO.transform);
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
            turnLayout.childControlHeight = false;
            turnLayout.childControlWidth = false;
            refs.turnQueuePanel = turnPanelGO.transform;

            // 3. Tạo Party Panel (Bottom Left)
            GameObject partyPanelGO = new GameObject("PartyPanel");
            partyPanelGO.transform.SetParent(canvasGO.transform);
            RectTransform partyRect = partyPanelGO.AddComponent<RectTransform>();
            partyRect.anchorMin = new Vector2(0f, 0f);
            partyRect.anchorMax = new Vector2(0f, 0f);
            partyRect.pivot = new Vector2(0f, 0f);
            partyRect.anchoredPosition = new Vector2(20f, 20f);
            partyRect.sizeDelta = new Vector2(500f, 140f);
            partyRect.localScale = Vector3.one;

            Image partyImg = partyPanelGO.AddComponent<Image>();
            partyImg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            HorizontalLayoutGroup partyLayout = partyPanelGO.AddComponent<HorizontalLayoutGroup>();
            partyLayout.childAlignment = TextAnchor.MiddleLeft;
            partyLayout.spacing = 15f;
            partyLayout.padding = new RectOffset(10, 10, 10, 10);
            partyLayout.childControlHeight = false;
            partyLayout.childControlWidth = false;
            refs.partyPanel = partyPanelGO.transform;

            // 4. Bảng kỹ năng Action Panel (Bottom Right)
            GameObject actionPanelGO = new GameObject("ActionPanel");
            actionPanelGO.transform.SetParent(canvasGO.transform);
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
            refs.actionPanel = actionPanelGO.transform;

            // Nút kỹ năng
            refs.basicButton = CreateUIButtonPrefab(actionPanelGO.transform, "Tấn Công Thường (Basic)");
            refs.basicText = refs.basicButton.GetComponentInChildren<Text>();

            refs.specialButton = CreateUIButtonPrefab(actionPanelGO.transform, "Kỹ Năng Đặc Biệt (Special)");
            refs.specialText = refs.specialButton.GetComponentInChildren<Text>();

            refs.ultimateButton = CreateUIButtonPrefab(actionPanelGO.transform, "Chiêu Cuối (Ultimate)");
            refs.ultimateText = refs.ultimateButton.GetComponentInChildren<Text>();

            refs.defendButton = CreateUIButtonPrefab(actionPanelGO.transform, "Phòng Thủ (Guard)");

            // 4.5 Target Selection Panel (Bottom Right, left of Action Panel)
            GameObject targetPanelGO = new GameObject("TargetSelectionPanel");
            targetPanelGO.transform.SetParent(canvasGO.transform);
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
            refs.targetSelectionPanel = targetPanelGO.transform;
            targetPanelGO.SetActive(false);

            // 5. Bảng mô tả Description Panel
            GameObject descPanelGO = new GameObject("DescriptionPanel");
            descPanelGO.transform.SetParent(canvasGO.transform);
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

            refs.descriptionText = descTextGO.AddComponent<Text>();
            refs.descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            refs.descriptionText.fontSize = 14;
            refs.descriptionText.color = Color.white;
            refs.descriptionText.text = "Chọn kỹ năng để xem thông tin...";
            refs.descriptionPanel = descPanelGO.transform;

            // 6. Màn hình kết thúc End Screen
            GameObject endGO = new GameObject("EndScreenPanel");
            endGO.transform.SetParent(canvasGO.transform);
            RectTransform endRect = endGO.AddComponent<RectTransform>();
            endRect.anchorMin = Vector2.zero;
            endRect.anchorMax = Vector2.one;
            endRect.sizeDelta = Vector2.zero;

            Image endImg = endGO.AddComponent<Image>();
            endImg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

            GameObject endTextGO = new GameObject("EndText");
            endTextGO.transform.SetParent(endGO.transform);
            RectTransform etRect = endTextGO.AddComponent<RectTransform>();
            etRect.anchorMin = new Vector2(0.5f, 0.6f);
            etRect.anchorMax = new Vector2(0.5f, 0.6f);
            etRect.pivot = new Vector2(0.5f, 0.5f);
            etRect.sizeDelta = new Vector2(500f, 80f);

            refs.endScreenText = endTextGO.AddComponent<Text>();
            refs.endScreenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            refs.endScreenText.fontSize = 42;
            refs.endScreenText.alignment = TextAnchor.MiddleCenter;
            refs.endScreenText.color = Color.yellow;
            refs.endScreenText.text = "CHIẾN THẮNG!";

            Outline eo = endTextGO.AddComponent<Outline>();
            eo.effectColor = Color.black;
            eo.effectDistance = new Vector2(2f, -2f);

            refs.restartButton = CreateUIButtonPrefab(endGO.transform, "Chơi Trận Mới");
            RectTransform resRect = refs.restartButton.GetComponent<RectTransform>();
            resRect.anchorMin = new Vector2(0.5f, 0.4f);
            resRect.anchorMax = new Vector2(0.5f, 0.4f);
            resRect.pivot = new Vector2(0.5f, 0.5f);
            resRect.anchoredPosition = Vector2.zero;
            resRect.sizeDelta = new Vector2(200f, 50f);

            refs.endScreenPanel = endGO.transform;
            endGO.SetActive(false);

            // 7. Tạo thư mục Prefabs nếu chưa có
            string folderPath = "Assets/_Project/Resources/Prefabs";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // 8. Lưu thành Prefab
            string prefabPath = folderPath + "/CombatUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
            DestroyImmediate(canvasGO);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Thành công", "Đã tạo/Cập nhật UI Prefab thành công tại: " + prefabPath + "\n\nBây giờ bạn có thể kéo thả và chỉnh sửa visual của Prefab này theo ý muốn!", "Tuyệt vời");
        }

        private static Button CreateUIButtonPrefab(Transform parent, string labelText)
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

            var colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f);
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            btn.colors = colors;

            return btn;
        }

        private static void BuildCharacterMenuUIPrefab()
        {
            // 1. Tạo Canvas chính
            GameObject canvasGO = new GameObject("CharacterMenuUI_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            
            CharacterMenuUIReferences refs = canvasGO.AddComponent<CharacterMenuUIReferences>();

            // 2. Background tối mờ
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.localPosition = Vector3.zero;
            bgRect.localRotation = Quaternion.identity;
            bgRect.localScale = Vector3.one;
            
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.06f, 0.08f, 0.9f);

            // 3. Nút Đóng (Close Button)
            GameObject closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(canvasGO.transform, false);
            RectTransform closeRect = closeBtnGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -20f);
            closeRect.sizeDelta = new Vector2(40f, 40f);
            closeRect.localPosition = new Vector3(closeRect.localPosition.x, closeRect.localPosition.y, 0f);
            closeRect.localRotation = Quaternion.identity;
            closeRect.localScale = Vector3.one;
            
            Image cbImg = closeBtnGO.AddComponent<Image>();
            cbImg.color = new Color(0.3f, 0.1f, 0.1f, 0.8f);
            refs.closeButton = closeBtnGO.AddComponent<Button>();

            GameObject closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeBtnGO.transform, false);
            RectTransform ctRect = closeTextGO.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            ctRect.localPosition = Vector3.zero;
            ctRect.localRotation = Quaternion.identity;
            ctRect.localScale = Vector3.one;
            
            Text ct = closeTextGO.AddComponent<Text>();
            ct.text = "X";
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ct.fontSize = 20;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = Color.white;

            // 4. Panel bên trái (LeftPanel - Scroll View)
            GameObject leftPanelGO = new GameObject("LeftPanel");
            leftPanelGO.transform.SetParent(canvasGO.transform, false);
            RectTransform lpRect = leftPanelGO.AddComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(0f, 0.5f);
            lpRect.anchorMax = new Vector2(0f, 0.5f);
            lpRect.pivot = new Vector2(0f, 0.5f);
            lpRect.anchoredPosition = new Vector2(40f, 0f);
            lpRect.sizeDelta = new Vector2(240f, 500f);
            lpRect.localPosition = new Vector3(lpRect.localPosition.x, lpRect.localPosition.y, 0f);
            lpRect.localRotation = Quaternion.identity;
            lpRect.localScale = Vector3.one;

            Image lpImg = leftPanelGO.AddComponent<Image>();
            lpImg.color = new Color(0.1f, 0.12f, 0.16f, 0.6f);

            // Tạo Scroll View
            GameObject scrollViewGO = new GameObject("ScrollView");
            scrollViewGO.transform.SetParent(leftPanelGO.transform, false);
            RectTransform svRect = scrollViewGO.AddComponent<RectTransform>();
            svRect.anchorMin = Vector2.zero;
            svRect.anchorMax = Vector2.one;
            svRect.sizeDelta = Vector2.zero;
            svRect.localPosition = Vector3.zero;
            svRect.localRotation = Quaternion.identity;
            svRect.localScale = Vector3.one;

            ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Tạo Viewport để che các nút thừa
            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            RectTransform vpRect = viewportGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpRect.localPosition = Vector3.zero;
            vpRect.localRotation = Quaternion.identity;
            vpRect.localScale = Vector3.one;
            viewportGO.AddComponent<RectMask2D>();

            // Tạo Content chứa các nút
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f); // Neo trên cùng
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentRect.localPosition = Vector3.zero;
            contentRect.localRotation = Quaternion.identity;
            contentRect.localScale = Vector3.one;

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

            refs.characterListParent = contentGO.transform;

            // 5. Panel xem mô hình 3D (MiddlePanel)
            GameObject midPanelGO = new GameObject("MiddlePanel");
            midPanelGO.transform.SetParent(canvasGO.transform, false);
            RectTransform mpRect = midPanelGO.AddComponent<RectTransform>();
            mpRect.anchorMin = new Vector2(0.5f, 0.5f);
            mpRect.anchorMax = new Vector2(0.5f, 0.5f);
            mpRect.pivot = new Vector2(0.5f, 0.5f);
            mpRect.anchoredPosition = new Vector2(-40f, 0f);
            mpRect.sizeDelta = new Vector2(480f, 500f);
            mpRect.localPosition = new Vector3(mpRect.localPosition.x, mpRect.localPosition.y, 0f);
            mpRect.localRotation = Quaternion.identity;
            mpRect.localScale = Vector3.one;

            refs.modelRenderImage = midPanelGO.AddComponent<RawImage>();
            refs.modelRenderImage.color = Color.white;

            // Thêm ShowroomRotator
            ShowroomRotator rotator = midPanelGO.AddComponent<ShowroomRotator>();
            rotator.rotationSpeed = 0.5f;

            // 6. Panel bên phải (RightPanel)
            GameObject rightPanelGO = new GameObject("RightPanel");
            rightPanelGO.transform.SetParent(canvasGO.transform, false);
            RectTransform rpRect = rightPanelGO.AddComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(1f, 0.5f);
            rpRect.anchorMax = new Vector2(1f, 0.5f);
            rpRect.pivot = new Vector2(1f, 0.5f);
            rpRect.anchoredPosition = new Vector2(-40f, 0f);
            rpRect.sizeDelta = new Vector2(340f, 500f);
            rpRect.localPosition = new Vector3(rpRect.localPosition.x, rpRect.localPosition.y, 0f);
            rpRect.localRotation = Quaternion.identity;
            rpRect.localScale = Vector3.one;

            Image rpImg = rightPanelGO.AddComponent<Image>();
            rpImg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);
            refs.statsPanel = rightPanelGO.transform;

            // Tên nhân vật
            GameObject nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(rightPanelGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(20f, -20f);
            nameRect.sizeDelta = new Vector2(-40f, 30f);
            nameRect.localPosition = new Vector3(nameRect.localPosition.x, nameRect.localPosition.y, 0f);
            nameRect.localRotation = Quaternion.identity;
            nameRect.localScale = Vector3.one;
            
            refs.characterNameText = nameGO.AddComponent<Text>();
            refs.characterNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            refs.characterNameText.fontSize = 24;
            refs.characterNameText.color = Color.white;
            refs.characterNameText.text = "Character Name";

            // Hệ nguyên tố
            GameObject elementGO = new GameObject("ElementText");
            elementGO.transform.SetParent(rightPanelGO.transform, false);
            RectTransform elRect = elementGO.AddComponent<RectTransform>();
            elRect.anchorMin = new Vector2(0f, 1f);
            elRect.anchorMax = new Vector2(1f, 1f);
            elRect.pivot = new Vector2(0.5f, 1f);
            elRect.anchoredPosition = new Vector2(20f, -50f);
            elRect.sizeDelta = new Vector2(-40f, 20f);
            elRect.localPosition = new Vector3(elRect.localPosition.x, elRect.localPosition.y, 0f);
            elRect.localRotation = Quaternion.identity;
            elRect.localScale = Vector3.one;
            
            refs.characterElementText = elementGO.AddComponent<Text>();
            refs.characterElementText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            refs.characterElementText.fontSize = 14;
            refs.characterElementText.color = Color.yellow;
            refs.characterElementText.text = "Thuộc tính: Lửa";

            // Chỉ số stats
            GameObject statsTextGO = new GameObject("StatsText");
            statsTextGO.transform.SetParent(rightPanelGO.transform, false);
            RectTransform stRect = statsTextGO.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 1f);
            stRect.anchorMax = new Vector2(1f, 1f);
            stRect.pivot = new Vector2(0.5f, 1f);
            stRect.anchoredPosition = new Vector2(20f, -80f);
            stRect.sizeDelta = new Vector2(-40f, 160f);
            stRect.localPosition = new Vector3(stRect.localPosition.x, stRect.localPosition.y, 0f);
            stRect.localRotation = Quaternion.identity;
            stRect.localScale = Vector3.one;
            
            refs.characterStatsText = statsTextGO.AddComponent<Text>();
            refs.characterStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            refs.characterStatsText.fontSize = 13;
            refs.characterStatsText.lineSpacing = 1.3f;
            refs.characterStatsText.color = new Color(0.85f, 0.85f, 0.85f);
            refs.characterStatsText.text = "HP:\nATK:\nDEF:\nSPEED:\nCRIT RATE:\nCRIT DMG:";

            // Divider line
            GameObject divGO = new GameObject("Divider");
            divGO.transform.SetParent(rightPanelGO.transform, false);
            RectTransform divRect = divGO.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0f, 1f);
            divRect.anchorMax = new Vector2(1f, 1f);
            divRect.pivot = new Vector2(0.5f, 1f);
            divRect.anchoredPosition = new Vector2(20f, -250f);
            divRect.sizeDelta = new Vector2(-40f, 2f);
            divRect.localPosition = new Vector3(divRect.localPosition.x, divRect.localPosition.y, 0f);
            divRect.localRotation = Quaternion.identity;
            divRect.localScale = Vector3.one;
            Image divImg = divGO.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.35f, 0.4f, 0.5f);

            // Kỹ năng Label
            GameObject skillLabelGO = new GameObject("SkillLabel");
            skillLabelGO.transform.SetParent(rightPanelGO.transform, false);
            RectTransform slRect = skillLabelGO.AddComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0f, 1f);
            slRect.anchorMax = new Vector2(1f, 1f);
            slRect.pivot = new Vector2(0.5f, 1f);
            slRect.anchoredPosition = new Vector2(20f, -260f);
            slRect.sizeDelta = new Vector2(-40f, 20f);
            slRect.localPosition = new Vector3(slRect.localPosition.x, slRect.localPosition.y, 0f);
            slRect.localRotation = Quaternion.identity;
            slRect.localScale = Vector3.one;
            Text slt = skillLabelGO.AddComponent<Text>();
            slt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slt.fontSize = 14;
            slt.color = Color.white;
            slt.text = "Danh Sách Kỹ Năng:";

            // Kỹ năng list parent
            GameObject skillListGO = new GameObject("SkillList");
            skillListGO.transform.SetParent(rightPanelGO.transform, false);
            RectTransform sListRect = skillListGO.AddComponent<RectTransform>();
            sListRect.anchorMin = new Vector2(0f, 0f);
            sListRect.anchorMax = new Vector2(1f, 1f);
            sListRect.pivot = new Vector2(0.5f, 0f);
            sListRect.offsetMin = new Vector2(20f, 20f);
            sListRect.offsetMax = new Vector2(-20f, -285f);
            sListRect.localPosition = new Vector3(sListRect.localPosition.x, sListRect.localPosition.y, 0f);
            sListRect.localRotation = Quaternion.identity;
            sListRect.localScale = Vector3.one;

            VerticalLayoutGroup skillLayout = skillListGO.AddComponent<VerticalLayoutGroup>();
            skillLayout.childAlignment = TextAnchor.UpperLeft;
            skillLayout.spacing = 8f;
            skillLayout.childControlHeight = false;
            skillLayout.childControlWidth = false;
            refs.skillListParent = skillListGO.transform;

            // 7. Tạo thư mục Prefabs nếu chưa có
            string folderPath = "Assets/_Project/Resources/Prefabs";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
                UnityEditor.AssetDatabase.Refresh();
            }

            // 8. Lưu thành Prefab
            string prefabPath = folderPath + "/CharacterMenuUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
            DestroyImmediate(canvasGO);

            UnityEditor.AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Thành công", "Đã tạo/Cập nhật Menu Nhân Vật Prefab thành công tại: " + prefabPath + "\n\nBây giờ bạn có thể kéo thả và chỉnh sửa visual của Prefab này theo ý muốn!", "Tuyệt vời");
        }
    }
}
