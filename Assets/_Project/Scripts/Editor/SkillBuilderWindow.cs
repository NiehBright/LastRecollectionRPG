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

        private Vector2 scrollPos;
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

            GUILayout.Space(10);

            if (GUILayout.Button("Quét danh sách nhân vật", GUILayout.Height(25)))
            {
                ScanForCharacters();
            }

            if (allCharacters.Count == 0)
            {
                EditorGUILayout.HelpBox("Không tìm thấy CharacterData nào trong thư mục Resources/Characters!", MessageType.Warning);
                return;
            }

            // Chọn nhân vật
            int newIdx = EditorGUILayout.Popup("Chọn nhân vật: ", selectedCharIndex, characterNames);
            if (newIdx != selectedCharIndex || selectedChar == null)
            {
                selectedCharIndex = newIdx;
                selectedChar = allCharacters[selectedCharIndex];
                LoadRecollectionDataForSelectedChar();
            }

            if (selectedChar == null) return;

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1)); // Dòng kẻ ngang

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Phần 1: Thông tin nhân vật
            EditorGUILayout.LabelField("1. THÔNG TIN NHÂN VẬT", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Tên nhân vật: {selectedChar.characterName}");
            EditorGUILayout.LabelField($"Nguyên tố gốc: {selectedChar.element}");
            EditorGUILayout.LabelField($"Vai trò: {selectedChar.role}");

            GUILayout.Space(10);

            // Phần 2: Quản lý File RecollectionData ScriptableObject
            EditorGUILayout.LabelField("2. FILE RECOLLECTION DATA CONFIG", EditorStyles.boldLabel);
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
                EditorGUILayout.ObjectField("Đang chỉnh sửa file: ", selectedRecData, typeof(RecollectionData), false);
                
                GUILayout.Space(15);

                // Phần 3: Ma trận Cường hóa (Matrix Grid 4x2)
                EditorGUILayout.LabelField("3. MA TRẬN CƯỜNG HÓA NGUYÊN TỐ (Chỉ Huy x Đồng Minh)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Chọn ô trong ma trận để cấu hình hiệu ứng cường hóa khi nhân vật này làm Chỉ Huy, truyền nguyên tố vào skill của đồng minh hàng trước.", MessageType.None);

                ElementType[] allyElements = new ElementType[] { ElementType.Fire, ElementType.Ice, ElementType.Lightning, ElementType.Nature };
                SkillType[] slots = new SkillType[] { SkillType.BASIC, SkillType.SPECIAL };

                // Vẽ Table Header
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Đồng Minh Element", EditorStyles.boldLabel, GUILayout.Width(130));
                GUILayout.Label("Basic Skill (Skill 1)", EditorStyles.boldLabel, GUILayout.Width(200));
                GUILayout.Label("Special Skill (Skill 2)", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();

                foreach (var el in allyElements)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(el.ToString(), EditorStyles.boldLabel, GUILayout.Width(130));

                    foreach (var slot in slots)
                    {
                        SkillEnhancement enh = selectedRecData.GetEnhancement(el, slot);
                        string btnText = enh != null && !string.IsNullOrEmpty(enh.enhancementName) 
                            ? $"{enh.enhancementName}" 
                            : "[ + Cấu hình mới ]";
                        
                        GUI.color = (activeAllyElement == el && activeSkillSlot == slot) ? Color.yellow : (enh != null && !string.IsNullOrEmpty(enh.enhancementName) ? Color.green : Color.white);

                        if (GUILayout.Button(btnText, GUILayout.Width(200), GUILayout.Height(30)))
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

                GUILayout.Space(20);

                // Phần 4: Panel cấu hình chi tiết cho ô đang chọn
                if (activeEnhanceEdit != null)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label($"BẢNG CẤU HÌNH CHI TIẾT: [{selectedChar.element} Chỉ Huy] x [{activeAllyElement} Đồng Minh] - {activeSkillSlot}", EditorStyles.boldLabel);

                    activeEnhanceEdit.enhancementName = EditorGUILayout.TextField("Tên Cường Hóa: ", activeEnhanceEdit.enhancementName);
                    
                    GUILayout.Label("Mô tả hiệu ứng cường hóa:");
                    activeEnhanceEdit.description = EditorGUILayout.TextArea(activeEnhanceEdit.description, GUILayout.Height(50));

                    activeEnhanceEdit.damageMultiplierBonus = EditorGUILayout.FloatField("Hệ số Nhân Sát Thương Cộng Thêm: ", activeEnhanceEdit.damageMultiplierBonus);
                    activeEnhanceEdit.specialCondition = EditorGUILayout.TextField("Nhãn Điều Kiện Đặc Biệt (Condition): ", activeEnhanceEdit.specialCondition);
                    activeEnhanceEdit.specialValue = EditorGUILayout.FloatField("Giá Trị Điều Kiện Đặc Biệt (Value): ", activeEnhanceEdit.specialValue);

                    // Danh sách Effect bổ sung
                    GUILayout.Space(10);
                    GUILayout.Label("Các hiệu ứng EffectData cộng kèm (Kéo thả asset vào đây):", EditorStyles.boldLabel);
                    
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

                    EditorGUILayout.EndVertical();
                }

                GUILayout.Space(20);

                // Nút Lưu và Validation
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("LƯU CẤU HÌNH (EXPORT ASSET)", GUILayout.Height(40)))
                {
                    SaveConfiguration();
                }

                if (GUILayout.Button("Duplicate (Nhân Bản Base)", GUILayout.Height(40)))
                {
                    DuplicateSelectedConfiguration();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
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
