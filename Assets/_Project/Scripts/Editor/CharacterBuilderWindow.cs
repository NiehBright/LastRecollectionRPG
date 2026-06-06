using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace RPG.Combat
{
    public class CharacterBuilderWindow : EditorWindow
    {
        [MenuItem("Tools/RPG Character Builder")]
        public static void ShowWindow()
        {
            GetWindow<CharacterBuilderWindow>("RPG Character Builder");
        }

        // Thông tin chung (General Info)
        private string charId = "new_character";
        private string charName = "Kazuko";
        private ElementType element = ElementType.Physical;
        private Sprite avatar;
        private Color themeColor = Color.red;

        // Chỉ số cơ bản (Stats)
        private float maxHP = 800f;
        private float atk = 120f;
        private float def = 60f;
        private float speed = 110f;
        private float critRate = 0.20f;
        private float critDmg = 1.50f;

        // Mô hình & Hoạt ảnh (Model & Animations)
        private GameObject modelPrefab;
        private AnimationClip idleClip;
        private AnimationClip runClip;
        private AnimationClip attack1Clip;
        private AnimationClip attack2Clip;
        private AnimationClip ultimateClip;
        private AnimationClip defendClip;

        // Hiệu ứng kỹ năng (VFX)
        private GameObject turnVFXPrefab;
        private GameObject basicAttackImpactVFX;
        private GameObject specialAttackImpactVFX;
        private GameObject ultimateVFX;

        private Vector2 scrollPos;

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("RPG Character Builder Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sử dụng công cụ này để tạo dữ liệu nhân vật (CharacterData ScriptableObject), tự động sinh cấu trúc Animator Controller và đóng gói thành Prefab nhân vật hoàn chỉnh.", MessageType.Info);

            GUILayout.Space(10);

            // 1. General Info
            GUILayout.Label("1. Thông tin chung (General Info)", EditorStyles.boldLabel);
            charId = EditorGUILayout.TextField("ID Nhân vật (Unique ID)", charId);
            charName = EditorGUILayout.TextField("Tên Nhân vật", charName);
            element = (ElementType)EditorGUILayout.EnumPopup("Thuộc tính (Element)", element);
            avatar = (Sprite)EditorGUILayout.ObjectField("Ảnh đại diện (Avatar)", avatar, typeof(Sprite), false);
            themeColor = EditorGUILayout.ColorField("Màu chủ đạo (Theme Color)", themeColor);

            GUILayout.Space(10);

            // 2. Stats
            GUILayout.Label("2. Chỉ số chiến đấu (Stats)", EditorStyles.boldLabel);
            maxHP = EditorGUILayout.FloatField("Máu tối đa (Max HP)", maxHP);
            atk = EditorGUILayout.FloatField("Sức tấn công (ATK)", atk);
            def = EditorGUILayout.FloatField("Sức phòng thủ (DEF)", def);
            speed = EditorGUILayout.FloatField("Tốc độ (Speed)", speed);
            critRate = EditorGUILayout.Slider("Tỷ lệ bạo kích (Crit Rate)", critRate, 0f, 1f);
            critDmg = EditorGUILayout.Slider("Sát thương bạo kích (Crit DMG)", critDmg, 1f, 5f);

            GUILayout.Space(10);

            // 3. Model & Animations
            GUILayout.Label("3. Mô hình & Hoạt ảnh (Model & Animations)", EditorStyles.boldLabel);
            modelPrefab = (GameObject)EditorGUILayout.ObjectField("Mô hình 3D (Character Model Prefab)", modelPrefab, typeof(GameObject), false);
            
            EditorGUILayout.HelpBox("Kéo thả các Clip hoạt ảnh (Animations) tương ứng bên dưới. Animator Controller sẽ tự động liên kết các hoạt ảnh này.", MessageType.None);
            idleClip = (AnimationClip)EditorGUILayout.ObjectField("Đứng yên (Idle Clip)", idleClip, typeof(AnimationClip), false);
            runClip = (AnimationClip)EditorGUILayout.ObjectField("Chạy đến (Run Clip)", runClip, typeof(AnimationClip), false);
            attack1Clip = (AnimationClip)EditorGUILayout.ObjectField("Tấn công thường - Chiêu 1 (Attack 1 Clip)", attack1Clip, typeof(AnimationClip), false);
            attack2Clip = (AnimationClip)EditorGUILayout.ObjectField("Tấn công đặc biệt - Chiêu 2 (Attack 2 Clip)", attack2Clip, typeof(AnimationClip), false);
            ultimateClip = (AnimationClip)EditorGUILayout.ObjectField("Chiêu cuối - Ultimate Clip", ultimateClip, typeof(AnimationClip), false);
            defendClip = (AnimationClip)EditorGUILayout.ObjectField("Phòng thủ - Defend Clip", defendClip, typeof(AnimationClip), false);

            GUILayout.Space(10);

            // 4. VFX Configuration
            GUILayout.Label("4. Hiệu ứng kỹ năng (VFX)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Kéo thả các Prefab hiệu ứng tương ứng bên dưới để hiển thị tại runtime.", MessageType.None);
            turnVFXPrefab = (GameObject)EditorGUILayout.ObjectField("VFX Lượt Đi (Turn Start/Idle)", turnVFXPrefab, typeof(GameObject), false);
            basicAttackImpactVFX = (GameObject)EditorGUILayout.ObjectField("VFX Đòn Đánh Thường (Basic Impact)", basicAttackImpactVFX, typeof(GameObject), false);
            specialAttackImpactVFX = (GameObject)EditorGUILayout.ObjectField("VFX Chiêu Đặc Biệt (Special Impact)", specialAttackImpactVFX, typeof(GameObject), false);
            ultimateVFX = (GameObject)EditorGUILayout.ObjectField("VFX Chiêu Cuối (Ultimate VFX)", ultimateVFX, typeof(GameObject), false);

            GUILayout.Space(20);

            if (GUILayout.Button("Tạo Nhân Vật (Build Character)", GUILayout.Height(40)))
            {
                BuildCharacter();
            }

            EditorGUILayout.EndScrollView();
        }

        private void BuildCharacter()
        {
            if (string.IsNullOrEmpty(charName))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng nhập Tên Nhân vật!", "OK");
                return;
            }

            // Đảm bảo các thư mục tài nguyên tồn tại trong Resources
            string animFolder = "Assets/_Project/Resources/Animators";
            string skillsFolder = "Assets/_Project/Resources/Skills";
            string charDataFolder = "Assets/_Project/Resources/Characters";
            string prefabFolder = "Assets/_Project/Resources/Prefabs/Characters";

            CreateFolderIfNotExist("Assets/_Project/Resources");
            CreateFolderIfNotExist(animFolder);
            CreateFolderIfNotExist(skillsFolder);
            CreateFolderIfNotExist(charDataFolder);
            CreateFolderIfNotExist("Assets/_Project/Resources/Prefabs");
            CreateFolderIfNotExist(prefabFolder);

            // 1. Tạo Animator Controller
            string animPath = $"{animFolder}/{charName}_Animator.controller";
            UnityEditor.Animations.AnimatorController animatorController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(animPath);
            
            // Lấy layer mặc định và thêm các State hoạt ảnh
            UnityEditor.Animations.AnimatorControllerLayer layer = animatorController.layers[0];
            UnityEditor.Animations.AnimatorStateMachine stateMachine = layer.stateMachine;

            AddStateWithClip(stateMachine, "Idle", idleClip);
            AddStateWithClip(stateMachine, "Run", runClip);
            AddStateWithClip(stateMachine, "Attack1", attack1Clip);
            AddStateWithClip(stateMachine, "Attack2", attack2Clip);
            AddStateWithClip(stateMachine, "Ultimate", ultimateClip);
            AddStateWithClip(stateMachine, "Defend", defendClip);

            Debug.Log($"[CharacterBuilder] Đã tạo Animator Controller thành công tại: {animPath}");

            // 2. Tự động sinh ra 3 tệp dữ liệu kỹ năng SkillData tương ứng
            SkillData basicSkill = CreateSkillAsset($"{charName}_SkillBasic", $"{charId}_basic", "Tấn Công Thường", "Gây sát thương vật lý/thuộc tính chuẩn lên một mục tiêu.", SkillType.BASIC, 0, 1.0f, TargetType.SINGLE, 0f, 10f, skillsFolder);
            SkillData specialSkill = CreateSkillAsset($"{charName}_SkillSpecial", $"{charId}_special", "Kỹ Năng Đặc Biệt", "Bộc phát kỹ năng gây sát thương lớn và tạo hiệu ứng đặc biệt lên mục tiêu.", SkillType.SPECIAL, 2, 1.5f, TargetType.SINGLE, 0f, 15f, skillsFolder);
            SkillData ultimateSkill = CreateSkillAsset($"{charName}_SkillUltimate", $"{charId}_ultimate", "Chiêu Cuối (Ultimate)", "Chiêu cuối hủy diệt gây sát thương diện rộng cực lớn.", SkillType.ULTIMATE, 0, 2.2f, TargetType.AOE, 100f, 0f, skillsFolder);

            // 3. Tạo CharacterData ScriptableObject
            CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.characterId = charId;
            charData.characterName = charName;
            charData.element = element;
            charData.avatar = avatar;
            charData.themeColor = themeColor;
            charData.baseMaxHP = maxHP;
            charData.baseATK = atk;
            charData.baseDEF = def;
            charData.baseSpeed = speed;
            charData.baseCritRate = critRate;
            charData.baseCritDMG = critDmg;
            
            charData.skillBasic = basicSkill;
            charData.skillSpecial = specialSkill;
            charData.skillUltimate = ultimateSkill;

            // Gán các trường VFX
            charData.turnVFXPrefab = turnVFXPrefab;
            charData.basicAttackImpactVFX = basicAttackImpactVFX;
            charData.specialAttackImpactVFX = specialAttackImpactVFX;
            charData.ultimateVFX = ultimateVFX;

            string charDataPath = $"{charDataFolder}/{charName}_Data.asset";
            AssetDatabase.CreateAsset(charData, charDataPath);
            Debug.Log($"[CharacterBuilder] Đã tạo CharacterData tại: {charDataPath}");

            // 4. Tạo Prefab nhân vật hoàn chỉnh
            GameObject tempGO = new GameObject($"Ally_{charName}");
            CombatCharacter cc = tempGO.AddComponent<CombatCharacter>();
            cc.isAlly = true; // Mặc định gán là phe ta
            cc.characterData = charData;

            if (modelPrefab != null)
            {
                // Instantiate mô hình 3D thực tế
                GameObject instantiatedModel = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, tempGO.transform);
                instantiatedModel.name = "ModelRoot";
                
                // Thiết lập góc quay mặc định (180 độ Y) để quay mặt về phía camera đấu trường
                instantiatedModel.transform.localPosition = Vector3.zero;
                instantiatedModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                cc.modelRoot = instantiatedModel.transform;

                // Thêm/Gán Animator và Animator Controller cho mô hình 3D
                Animator animator = instantiatedModel.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instantiatedModel.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = animatorController;
            }
            else
            {
                // Nếu chưa có mô hình 3D thực, tạo gốc model trống
                GameObject modelRootGO = new GameObject("ModelRoot");
                modelRootGO.transform.SetParent(tempGO.transform);
                cc.modelRoot = modelRootGO.transform;
            }

            string prefabPath = $"{prefabFolder}/{charName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
            DestroyImmediate(tempGO);

            Debug.Log($"[CharacterBuilder] Đã lưu Prefab nhân vật thành công tại: {prefabPath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Thành công", $"Nhân vật '{charName}' đã được tạo hoàn chỉnh!\n\n" +
                $"1. Animator Controller: {animPath}\n" +
                $"2. Dữ liệu kỹ năng và chỉ số: {charDataPath}\n" +
                $"3. Prefab Nhân vật: {prefabPath}\n\n" +
                $"Nhân vật này sẽ tự động xuất hiện trong Menu E ở thế giới thực!", "Tuyệt vời");
        }

        private void CreateFolderIfNotExist(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private void AddStateWithClip(UnityEditor.Animations.AnimatorStateMachine stateMachine, string name, AnimationClip clip)
        {
            UnityEditor.Animations.AnimatorState state = stateMachine.AddState(name);
            if (clip != null)
            {
                state.motion = clip;
            }
        }

        private SkillData CreateSkillAsset(string assetName, string skillId, string skillName, string desc, SkillType type, int cd, float dmgMult, TargetType target, float cost, float generated, string folder)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.description = desc;
            skill.skillType = type;
            skill.cooldown = cd;
            skill.damageMultiplier = dmgMult;
            skill.targetType = target;
            skill.energyCost = cost;
            skill.energyGenerated = generated;
            skill.skillColor = type == SkillType.BASIC ? Color.white : (type == SkillType.SPECIAL ? Color.yellow : Color.red);

            string path = $"{folder}/{assetName}.asset";
            AssetDatabase.CreateAsset(skill, path);
            return skill;
        }
    }
}
