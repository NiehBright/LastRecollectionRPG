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
            
            canvasGO.AddComponent<CanvasScaler>();
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
            string folderPath = "Assets/Resources/Prefabs";
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
    }
}
