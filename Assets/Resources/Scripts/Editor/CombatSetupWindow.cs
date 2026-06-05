using UnityEngine;
using UnityEditor;

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
    }
}
