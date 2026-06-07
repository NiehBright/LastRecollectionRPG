using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RPG.Combat
{
    public class SkillBuilderWindow : EditorWindow
    {
        [MenuItem("Tools/RPG Skill Builder")]
        public static void ShowWindow()
        {
            GetWindow<SkillBuilderWindow>("RPG Skill Builder");
        }

        private CharacterData selectedChar;
        private RecollectionData selectedRecData;
        
        // Trạng thái ô đang được chọn để sửa trong Matrix Grid
        private ElementType activeAllyElement = ElementType.Fire;
        private SkillType activeSkillSlot = SkillType.BASIC;
        private SkillEnhancement activeEnhanceEdit;

        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private List<CharacterData> allCharacters = new List<CharacterData>();
        private string[] characterNames;
        private int selectedCharIndex = 0;

        private void OnEnable()
        {
            ScanForCharacters();
        }

        private void ScanForCharacters()
        {
            allCharacters.Clear();
            CharacterData[] assets = Resources.LoadAll<CharacterData>("Characters");
            if (assets != null)
            {
                allCharacters.AddRange(assets);
            }

            characterNames = new string[allCharacters.Count];
            for (int i = 0; i < allCharacters.Count; i++)
            {
                characterNames[i] = $"{allCharacters[i].characterName} ({allCharacters[i].role})";
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("RPG SKILL BUILDER & RECOLLECTION MATRIX", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Công cụ này cho phép Game Designer cấu hình kỹ năng và thiết lập Ma trận cường hóa (40 kết hợp nguyên tố) dưới dạng ScriptableObject.", MessageType.Info);

            GUILayout.Space(5);

            if (allCharacters.Count == 0)
            {
                if (GUILayout.Button("Quét danh sách nhân vật", GUILayout.Height(25)))
                {
                    ScanForCharacters();
                }
                EditorGUILayout.HelpBox("Không tìm thấy CharacterData nào trong thư mục Resources/Characters!", MessageType.Warning);
                return;
            }

            // Chia cột
            EditorGUILayout.BeginHorizontal();

            // ==================== CỘT TRÁI (MA TRẬN & FILE) ====================
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width * 0.54f));
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

            EditorGUILayout.LabelField("1. CHỌN NHÂN VẬT & CONFIG FILE", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            int newIdx = EditorGUILayout.Popup("Nhân vật: ", selectedCharIndex, characterNames, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Refresh List", GUILayout.Width(90)))
            {
                ScanForCharacters();
            }
            EditorGUILayout.EndHorizontal();

            if (newIdx != selectedCharIndex || selectedChar == null)
            {
                selectedCharIndex = newIdx;
                selectedChar = allCharacters[selectedCharIndex];
                LoadRecollectionDataForSelectedChar();
            }

            if (selectedChar != null)
            {
                EditorGUILayout.HelpBox($"Nhân vật: {selectedChar.characterName} | Nguyên tố: {selectedChar.element} | Vai trò: {selectedChar.role}", MessageType.None);

                GUILayout.Space(10);

                if (selectedRecData == null)
                {
                    EditorGUILayout.HelpBox($"Không tìm thấy file RecollectionData cho {selectedChar.characterName} tại Resources/RecollectionData/!", MessageType.Warning);
                    if (GUILayout.Button($"Tạo mới RecollectionData cho {selectedChar.characterName}", GUILayout.Height(30)))
                    {
                        CreateNewRecollectionData();
                    }
                }
                else
                {
                    EditorGUILayout.ObjectField("File config: ", selectedRecData, typeof(RecollectionData), false);

                    GUILayout.Space(15);

                    // Ma trận Cường hóa (Matrix Grid 4x2)
                    EditorGUILayout.LabelField("2. MA TRẬN CƯỜNG HÓA NGUYÊN TỐ", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Chọn ô trong ma trận bên dưới để sửa chi tiết hiệu ứng cường hóa kỹ năng ở cột bên phải.", MessageType.None);

                    ElementType[] allyElements = new ElementType[] { ElementType.Fire, ElementType.Ice, ElementType.Lightning, ElementType.Nature };
                    SkillType[] slots = new SkillType[] { SkillType.BASIC, SkillType.SPECIAL };

                    // Vẽ Table Header
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Đồng Minh", EditorStyles.boldLabel, GUILayout.Width(75));
                    GUILayout.Label("Basic Skill (Skill 1)", EditorStyles.boldLabel, GUILayout.Width(130));
                    GUILayout.Label("Special Skill (Skill 2)", EditorStyles.boldLabel, GUILayout.Width(130));
                    EditorGUILayout.EndHorizontal();

                    foreach (var el in allyElements)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(el.ToString(), EditorStyles.boldLabel, GUILayout.Width(75));

                        foreach (var slot in slots)
                        {
                            SkillEnhancement enh = selectedRecData.GetEnhancement(el, slot);
                            string btnText = enh != null && !string.IsNullOrEmpty(enh.enhancementName) 
                                ? $"{enh.enhancementName}" 
                                : "[ + Tạo mới ]";
                            
                            GUI.color = (activeAllyElement == el && activeSkillSlot == slot) ? Color.yellow : (enh != null && !string.IsNullOrEmpty(enh.enhancementName) ? Color.green : Color.white);

                            if (GUILayout.Button(btnText, GUILayout.Width(130), GUILayout.Height(28)))
                            {
                                activeAllyElement = el;
                                activeSkillSlot = slot;
                                
                                if (enh == null)
                                {
                                    enh = new SkillEnhancement();
                                    enh.allyElement = el;
                                    enh.skillSlot = slot;
                                    selectedRecData.enhancedSkillMatrix.Add(enh);
                                }
                                activeEnhanceEdit = enh;
                            }
                        }
                        GUI.color = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            // ==================== CỘT PHẢI (CHỈNH SỬA CHI TIẾT) ====================
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width * 0.42f));
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

            if (selectedChar == null)
            {
                EditorGUILayout.HelpBox("Hãy chọn một nhân vật ở cột bên trái để bắt đầu.", MessageType.Info);
            }
            else if (selectedRecData == null)
            {
                EditorGUILayout.HelpBox("Hãy tạo/liên kết file RecollectionData ở cột bên trái trước.", MessageType.Info);
            }
            else if (activeEnhanceEdit == null)
            {
                EditorGUILayout.HelpBox("Chọn một ô trong Ma trận ở cột bên trái để cấu hình hiệu ứng.", MessageType.Info);
            }
            else
            {
                // Tiêu đề của bảng cấu hình
                EditorGUILayout.LabelField("3. BẢNG CẤU HÌNH CHI TIẾT", EditorStyles.boldLabel);
                GUILayout.Label($"[{selectedChar.element} Chỉ Huy] x [{activeAllyElement} Đồng Minh]\nKỹ năng: {activeSkillSlot}", EditorStyles.boldLabel);
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1)); // Dòng kẻ ngang

                activeEnhanceEdit.enhancementName = EditorGUILayout.TextField("Tên Cường Hóa: ", activeEnhanceEdit.enhancementName);
                
                GUILayout.Label("Mô tả hiệu ứng cường hóa:");
                activeEnhanceEdit.description = EditorGUILayout.TextArea(activeEnhanceEdit.description, GUILayout.Height(60));

                activeEnhanceEdit.damageMultiplierBonus = EditorGUILayout.FloatField("Hệ số DMG cộng thêm: ", activeEnhanceEdit.damageMultiplierBonus);
                activeEnhanceEdit.specialCondition = EditorGUILayout.TextField("Nhãn điều kiện đặc biệt: ", activeEnhanceEdit.specialCondition);
                activeEnhanceEdit.specialValue = EditorGUILayout.FloatField("Giá trị điều kiện đặc biệt: ", activeEnhanceEdit.specialValue);

                // Danh sách Effect bổ sung
                GUILayout.Space(10);
                GUILayout.Label("Hiệu ứng EffectData cộng kèm:", EditorStyles.boldLabel);
                
                if (activeEnhanceEdit.additionalEffects == null)
                {
                    activeEnhanceEdit.additionalEffects = new List<EffectData>();
                }

                int count = EditorGUILayout.IntField("Số lượng Effect: ", activeEnhanceEdit.additionalEffects.Count);
                while (count > activeEnhanceEdit.additionalEffects.Count) activeEnhanceEdit.additionalEffects.Add(null);
                while (count < activeEnhanceEdit.additionalEffects.Count) activeEnhanceEdit.additionalEffects.RemoveAt(activeEnhanceEdit.additionalEffects.Count - 1);

                for (int i = 0; i < activeEnhanceEdit.additionalEffects.Count; i++)
                {
                    activeEnhanceEdit.additionalEffects[i] = (EffectData)EditorGUILayout.ObjectField($"Effect {i + 1}: ", activeEnhanceEdit.additionalEffects[i], typeof(EffectData), false);
                }

                GUILayout.Space(20);

                // Nút Lưu và Validation
                if (GUILayout.Button("LƯU CẤU HÌNH (EXPORT)", GUILayout.Height(35)))
                {
                    SaveConfiguration();
                }

                if (GUILayout.Button("Nhân Bản Cấu Hình (Duplicate)", GUILayout.Height(30)))
                {
                    DuplicateSelectedConfiguration();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void LoadRecollectionDataForSelectedChar()
        {
            if (selectedChar == null) return;
            
            // Tìm trong Resources/RecollectionData/
            selectedRecData = Resources.Load<RecollectionData>($"RecollectionData/{selectedChar.characterName}_RecollectionData");
            activeEnhanceEdit = null;
        }

        private void CreateNewRecollectionData()
        {
            if (selectedChar == null) return;

            string folderPath = "Assets/_Project/Resources/RecollectionData";
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                folderPath = "Assets/Resources/RecollectionData";
            }

            // Tạo thư mục nếu chưa có
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            RecollectionData asset = ScriptableObject.CreateInstance<RecollectionData>();
            asset.characterId = selectedChar.characterId;
            asset.recollectionElement = selectedChar.element;
            asset.gaugeCapacity = 100f;
            asset.turnDuration = 5;
            asset.enhancedSkillMatrix = new List<SkillEnhancement>();

            // Khởi tạo sẵn 8 ô rỗng để designer tiện cấu hình
            ElementType[] allyElements = new ElementType[] { ElementType.Fire, ElementType.Ice, ElementType.Lightning, ElementType.Nature };
            SkillType[] slots = new SkillType[] { SkillType.BASIC, SkillType.SPECIAL };

            foreach (var el in allyElements)
            {
                foreach (var slot in slots)
                {
                    SkillEnhancement se = new SkillEnhancement();
                    se.allyElement = el;
                    se.skillSlot = slot;
                    se.enhancementName = "";
                    se.description = "";
                    se.damageMultiplierBonus = 0f;
                    se.specialCondition = "";
                    se.specialValue = 0f;
                    se.additionalEffects = new List<EffectData>();
                    asset.enhancedSkillMatrix.Add(se);
                }
            }

            string assetPath = $"{folderPath}/{selectedChar.characterName}_RecollectionData.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            selectedRecData = asset;
            activeEnhanceEdit = null;
            Debug.Log($"[SkillBuilder] Đã tạo thành công file RecollectionData tại {assetPath}");
        }

        private void SaveConfiguration()
        {
            if (selectedRecData == null) return;

            // Validate dữ liệu
            foreach (var enh in selectedRecData.enhancedSkillMatrix)
            {
                if (string.IsNullOrEmpty(enh.enhancementName)) continue;
                if (enh.damageMultiplierBonus < 0)
                {
                    EditorUtility.DisplayDialog("Lỗi Validation", $"Hệ số sát thương của [{enh.allyElement} - {enh.skillSlot}] không được nhỏ hơn 0!", "Quay lại sửa");
                    return;
                }
            }

            EditorUtility.SetDirty(selectedRecData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Thành công", $"Đã xuất/lưu file RecollectionData thành công!", "OK");
            Debug.Log($"[SkillBuilder] Đã lưu file cấu hình RecollectionData: {selectedRecData.name}");
        }

        private void DuplicateSelectedConfiguration()
        {
            if (selectedRecData == null) return;

            string path = AssetDatabase.GetAssetPath(selectedRecData);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(path);

            if (AssetDatabase.CopyAsset(path, newPath))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                RecollectionData dup = AssetDatabase.LoadAssetAtPath<RecollectionData>(newPath);
                selectedRecData = dup;
                activeEnhanceEdit = null;
                EditorUtility.DisplayDialog("Nhân bản", $"Đã sao chép cấu hình thành công thành file mới tại: {newPath}", "OK");
            }
        }
    }
}
