using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

namespace RPG.Combat
{
    public class AnimatorUpdaterWindow : EditorWindow
    {
        [MenuItem("Tools/RPG Animator Updater")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorUpdaterWindow>("Animator Updater");
        }

        private class UpdateItem
        {
            public CharacterData data;
            public string animPath;
            public bool isSelected = true;
            public bool fileExists = false;
        }

        private List<UpdateItem> updateList = new List<UpdateItem>();
        private Vector2 scrollPosList;
        private Vector2 scrollPosLogs;
        private string logOutput = "";

        private void OnEnable()
        {
            ScanForCharacters();
        }

        private void OnGUI()
        {
            GUILayout.Label("RPG Animator Updater Tool (Dành riêng cho Turnbase)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Công cụ này chỉ quét các CharacterData thuộc hệ thống Turnbase và cập nhật Animator Controller riêng biệt của chúng tại thư mục Resources/Animators. Đảm bảo KHÔNG ảnh hưởng đến các Animator khác của dự án.", MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Quét / Tải Lại Danh Sách Nhân Vật", GUILayout.Height(30)))
            {
                ScanForCharacters();
            }

            GUILayout.Space(10);
            GUILayout.Label("Danh sách Animator sẽ được cập nhật (Tích chọn để xử lý):", EditorStyles.boldLabel);

            if (updateList.Count == 0)
            {
                EditorGUILayout.HelpBox("Không tìm thấy CharacterData nào trong Resources/Characters!", MessageType.Warning);
            }
            else
            {
                scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList, GUILayout.Height(180));
                for (int i = 0; i < updateList.Count; i++)
                {
                    var item = updateList[i];
                    EditorGUILayout.BeginHorizontal();
                    item.isSelected = EditorGUILayout.Toggle(item.isSelected, GUILayout.Width(20));
                    
                    string status = item.fileExists ? "[Cập nhật]" : "[Tạo mới]";
                    GUI.color = item.fileExists ? Color.green : Color.yellow;
                    GUILayout.Label(status, GUILayout.Width(80));
                    GUI.color = Color.white;

                    GUILayout.Label($"{item.data.characterName} ({item.data.characterId})", EditorStyles.boldLabel, GUILayout.Width(180));
                    GUILayout.Label($"-> Path: {item.animPath}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(15);

            EditorGUI.BeginDisabledGroup(updateList.Count == 0);
            if (GUILayout.Button("Cập Nhật Các Animator Đã Chọn", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Xác nhận", "Bạn có chắc chắn muốn cập nhật/tạo mới các Animator Controller đã chọn không? Hành động này chỉ ghi đè lên các file hiển thị ở danh sách trên.", "Đồng ý", "Hủy"))
                {
                    ApplyUpdates();
                }
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(15);
            GUILayout.Label("Nhật ký hoạt động (Logs):", EditorStyles.boldLabel);
            scrollPosLogs = EditorGUILayout.BeginScrollView(scrollPosLogs, GUILayout.Height(150));
            EditorGUILayout.TextArea(logOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ScanForCharacters()
        {
            updateList.Clear();
            
            // Tìm thư mục gốc
            string basePath = "Assets";
            if (AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                basePath = "Assets/_Project";
            }

            string animFolder = $"{basePath}/Resources/Animators";

            // Tải tất cả CharacterData từ Resources/Characters
            CharacterData[] charDatas = Resources.LoadAll<CharacterData>("Characters");
            if (charDatas != null)
            {
                foreach (var data in charDatas)
                {
                    if (data == null || string.IsNullOrEmpty(data.characterName)) continue;
                    
                    string animPath = $"{animFolder}/{data.characterName}_Animator.controller";
                    updateList.Add(new UpdateItem
                    {
                        data = data,
                        animPath = animPath,
                        fileExists = File.Exists(animPath)
                    });
                }
            }
            logOutput = $"Đã quét xong. Tìm thấy {updateList.Count} nhân vật turnbase hợp lệ.\n";
        }

        private void ApplyUpdates()
        {
            logOutput = "Bắt đầu cập nhật các mục đã chọn...\n";

            // Tìm thư mục gốc
            string basePath = "Assets";
            if (AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                basePath = "Assets/_Project";
            }

            string animFolder = $"{basePath}/Resources/Animators";
            if (!AssetDatabase.IsValidFolder(animFolder))
            {
                if (!AssetDatabase.IsValidFolder($"{basePath}/Resources"))
                {
                    AssetDatabase.CreateFolder(basePath, "Resources");
                }
                AssetDatabase.CreateFolder($"{basePath}/Resources", "Animators");
            }

            int updatedCount = 0;
            int createdCount = 0;

            foreach (var item in updateList)
            {
                if (!item.isSelected)
                {
                    logOutput += $"[Bỏ qua] {item.data.characterName} (Người dùng không chọn)\n";
                    continue;
                }

                UnityEditor.Animations.AnimatorController controller;
                if (item.fileExists)
                {
                    try
                    {
                        string backupFolder = $"{animFolder}/Backup";
                        if (!Directory.Exists(backupFolder))
                        {
                            Directory.CreateDirectory(backupFolder);
                        }
                        string backupPath = $"{backupFolder}/{item.data.characterName}_Animator.controller.backup";
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                        File.Copy(item.animPath, backupPath);
                        logOutput += $"  -> Đã sao lưu dự phòng thành công tại: {backupPath}\n";
                    }
                    catch (System.Exception ex)
                    {
                        logOutput += $"  -> CẢNH BÁO: Không thể tạo bản sao lưu: {ex.Message}\n";
                    }

                    controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(item.animPath);
                    logOutput += $"[Cập nhật] Đang xử lý: {item.data.characterName}_Animator.controller\n";
                    updatedCount++;
                }
                else
                {
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(item.animPath);
                    logOutput += $"[Tạo mới] Đang tạo: {item.data.characterName}_Animator.controller\n";
                    createdCount++;
                }

                if (controller == null)
                {
                    logOutput += $"LỖI: Không thể xử lý Animator Controller tại: {item.animPath}\n";
                    continue;
                }

                // Cập nhật các States trong Layer đầu tiên
                AnimatorControllerLayer layer = controller.layers[0];
                AnimatorStateMachine stateMachine = layer.stateMachine;

                // Các state cần có và clip tương ứng
                var targetStates = new Dictionary<string, AnimationClip>();
                targetStates["Idle"] = item.data.idleClip;
                targetStates["Run"] = item.data.runClip;
                targetStates["Attack1"] = item.data.skillBasic != null ? item.data.skillBasic.skillClip : null;
                targetStates["Attack2"] = item.data.skillSpecial != null ? item.data.skillSpecial.skillClip : null;
                targetStates["Ultimate"] = item.data.skillUltimate != null ? item.data.skillUltimate.skillClip : null;
                targetStates["Defend"] = item.data.defendClip;
                targetStates["Hit"] = item.data.hitClip;
                targetStates["Die"] = item.data.dieClip;

                foreach (var stateKvp in targetStates)
                {
                    string stateName = stateKvp.Key;
                    AnimationClip clip = stateKvp.Value;

                    // Tìm xem state đã tồn tại chưa
                    ChildAnimatorState foundChildState = default;
                    bool exists = false;
                    foreach (var s in stateMachine.states)
                    {
                        if (s.state.name == stateName)
                        {
                            foundChildState = s;
                            exists = true;
                            break;
                        }
                    }

                    AnimatorState state;
                    if (exists)
                    {
                        state = foundChildState.state;
                    }
                    else
                    {
                        state = stateMachine.AddState(stateName);
                        logOutput += $"  -> Thêm mới State: '{stateName}'\n";
                    }

                    // Cập nhật clip cho State
                    if (clip != null)
                    {
                        state.motion = clip;
                    }
                }

                EditorUtility.SetDirty(controller);
                logOutput += $"  -> Đồng bộ hoàn tất cho {item.data.characterName}.\n\n";
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            logOutput += $"=== HOÀN TẤT ===\nĐã cập nhật: {updatedCount}\nĐã tạo mới: {createdCount}\n";
            EditorUtility.DisplayDialog("Thành công", $"Đã hoàn tất đồng bộ các Animator Controller đã chọn!\n\nĐã cập nhật: {updatedCount}\nĐã tạo mới: {createdCount}", "OK");
            
            // Quét lại để cập nhật trạng thái UI
            ScanForCharacters();
        }
    }
}
