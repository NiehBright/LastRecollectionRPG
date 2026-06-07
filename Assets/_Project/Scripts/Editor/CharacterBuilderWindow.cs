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
        private AnimationClip hitClip;
        private AnimationClip dieClip;

        // Hiệu ứng kỹ năng (VFX)
        private GameObject turnVFXPrefab;
        private GameObject basicAttackImpactVFX;
        private GameObject specialAttackImpactVFX;
        private GameObject ultimateVFX;

        private Vector2 scrollPos;
        private int tabIndex = 0;
        private string[] tabNames = new string[] { "Thông tin & Chỉ số", "Mô hình & Hoạt ảnh", "Hiệu ứng VFX", "Tính năng nhanh" };

        private void OnGUI()
        {
            GUILayout.Label("RPG Character Builder Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sử dụng công cụ này để tạo dữ liệu nhân vật (CharacterData ScriptableObject), tự động sinh cấu trúc Animator Controller và đóng gói thành Prefab nhân vật hoàn chỉnh.", MessageType.Info);

            GUILayout.Space(10);
            
            // Vẽ Toolbar Tab Selector
            tabIndex = GUILayout.Toolbar(tabIndex, tabNames, GUILayout.Height(30));
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (tabIndex)
            {
                case 0: // Thông tin & Chỉ số
                    // 1. General Info
                    GUILayout.Label("1. Thông tin chung (General Info)", EditorStyles.boldLabel);
                    charId = EditorGUILayout.TextField("ID Nhân vật (Unique ID)", charId);
                    charName = EditorGUILayout.TextField("Tên Nhân vật", charName);
                    element = (ElementType)EditorGUILayout.EnumPopup("Thuộc tính (Element)", element);
                    avatar = (Sprite)EditorGUILayout.ObjectField("Ảnh đại diện (Avatar)", avatar, typeof(Sprite), false);
                    themeColor = EditorGUILayout.ColorField("Màu chủ đạo (Theme Color)", themeColor);

                    GUILayout.Space(15);

                    // 2. Stats
                    GUILayout.Label("2. Chỉ số chiến đấu (Stats)", EditorStyles.boldLabel);
                    maxHP = EditorGUILayout.FloatField("Máu tối đa (Max HP)", maxHP);
                    atk = EditorGUILayout.FloatField("Sức tấn công (ATK)", atk);
                    def = EditorGUILayout.FloatField("Sức phòng thủ (DEF)", def);
                    speed = EditorGUILayout.FloatField("Tốc độ (Speed)", speed);
                    critRate = EditorGUILayout.Slider("Tỷ lệ bạo kích (Crit Rate)", critRate, 0f, 1f);
                    critDmg = EditorGUILayout.Slider("Sát thương bạo kích (Crit DMG)", critDmg, 1f, 5f);
                    break;

                case 1: // Mô hình & Hoạt ảnh
                    GUILayout.Label("3. Mô hình & Hoạt ảnh (Model & Animations)", EditorStyles.boldLabel);
                    modelPrefab = (GameObject)EditorGUILayout.ObjectField("Mô hình 3D (Character Model Prefab)", modelPrefab, typeof(GameObject), false);
                    
                    EditorGUILayout.HelpBox("Kéo thả các Clip hoạt ảnh (Animations) tương ứng bên dưới. Animator Controller sẽ tự động liên kết các hoạt ảnh này.", MessageType.None);
                    idleClip = (AnimationClip)EditorGUILayout.ObjectField("Đứng yên (Idle Clip)", idleClip, typeof(AnimationClip), false);
                    runClip = (AnimationClip)EditorGUILayout.ObjectField("Chạy đến (Run Clip)", runClip, typeof(AnimationClip), false);
                    attack1Clip = (AnimationClip)EditorGUILayout.ObjectField("Tấn công thường - Chiêu 1 (Attack 1 Clip)", attack1Clip, typeof(AnimationClip), false);
                    attack2Clip = (AnimationClip)EditorGUILayout.ObjectField("Tấn công đặc biệt - Chiêu 2 (Attack 2 Clip)", attack2Clip, typeof(AnimationClip), false);
                    ultimateClip = (AnimationClip)EditorGUILayout.ObjectField("Chiêu cuối - Ultimate Clip", ultimateClip, typeof(AnimationClip), false);
                    defendClip = (AnimationClip)EditorGUILayout.ObjectField("Phòng thủ - Defend Clip", defendClip, typeof(AnimationClip), false);
                    hitClip = (AnimationClip)EditorGUILayout.ObjectField("Bị tấn công (Hit Clip)", hitClip, typeof(AnimationClip), false);
                    dieClip = (AnimationClip)EditorGUILayout.ObjectField("Chết (Die Clip)", dieClip, typeof(AnimationClip), false);
                    break;

                case 2: // Hiệu ứng VFX
                    GUILayout.Label("4. Hiệu ứng kỹ năng (VFX)", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Kéo thả các Prefab hiệu ứng tương ứng bên dưới để hiển thị tại runtime.", MessageType.None);
                    turnVFXPrefab = (GameObject)EditorGUILayout.ObjectField("VFX Lượt Đi (Turn Start/Idle)", turnVFXPrefab, typeof(GameObject), false);
                    basicAttackImpactVFX = (GameObject)EditorGUILayout.ObjectField("VFX Đòn Đánh Thường (Basic Impact)", basicAttackImpactVFX, typeof(GameObject), false);
                    specialAttackImpactVFX = (GameObject)EditorGUILayout.ObjectField("VFX Chiêu Đặc Biệt (Special Impact)", specialAttackImpactVFX, typeof(GameObject), false);
                    ultimateVFX = (GameObject)EditorGUILayout.ObjectField("VFX Chiêu Cuối (Ultimate VFX)", ultimateVFX, typeof(GameObject), false);
                    break;

                case 3: // Tính năng nhanh
                    GUILayout.Label("5. Các tính năng phát triển nhanh (Developer Tools)", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Nhấp vào nút dưới đây để tạo tự động 4 nhân vật mặc định hỗ trợ hệ thống turn-based (Kazuko, Hoshi, Rin, Mei) cùng các file data chỉ số, kỹ năng và prefab hoàn chỉnh vào thư mục Resources.", MessageType.Info);
                    
                    GUILayout.Space(15);
                    if (GUILayout.Button("KHỞI TẠO NHANH 4 NHÂN VẬT DEMO", GUILayout.Height(50)))
                    {
                        SpawnDefaultCharacters();
                    }
                    break;
            }

            // Vẽ nút Build Character cho các Tab điền thông tin (Tab 0, 1, 2)
            if (tabIndex >= 0 && tabIndex <= 2)
            {
                GUILayout.Space(25);
                if (GUILayout.Button($"Tạo Nhân Vật: {charName} (Build Character)", GUILayout.Height(45)))
                {
                    BuildCharacter();
                }
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
            AddStateWithClip(stateMachine, "Hit", hitClip);
            AddStateWithClip(stateMachine, "Die", dieClip);

            Debug.Log($"[CharacterBuilder] Đã tạo Animator Controller thành công tại: {animPath}");

            // 2. Tự động sinh ra 3 tệp dữ liệu kỹ năng SkillData tương ứng
            SkillData basicSkill = CreateSkillAsset($"{charName}_SkillBasic", $"{charId}_basic", "Tấn Công Thường", "Gây sát thương vật lý/thuộc tính chuẩn lên một mục tiêu.", SkillType.BASIC, 0, 1.0f, TargetType.SINGLE, 0f, 10f, attack1Clip, basicAttackImpactVFX, skillsFolder);
            SkillData specialSkill = CreateSkillAsset($"{charName}_SkillSpecial", $"{charId}_special", "Kỹ Năng Đặc Biệt", "Bộc phát kỹ năng gây sát thương lớn và tạo hiệu ứng đặc biệt lên mục tiêu.", SkillType.SPECIAL, 2, 1.5f, TargetType.SINGLE, 0f, 15f, attack2Clip, specialAttackImpactVFX, skillsFolder);
            SkillData ultimateSkill = CreateSkillAsset($"{charName}_SkillUltimate", $"{charId}_ultimate", "Chiêu Cuối (Ultimate)", "Chiêu cuối hủy diệt gây sát thương diện rộng cực lớn.", SkillType.ULTIMATE, 0, 2.2f, TargetType.AOE, 100f, 0f, ultimateClip, ultimateVFX, skillsFolder);

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

            // Gán các trường Animation Clips (Không lưu 3 clip tấn công vào CharacterData)
            charData.idleClip = idleClip;
            charData.runClip = runClip;
            charData.defendClip = defendClip;
            charData.hitClip = hitClip;
            charData.dieClip = dieClip;

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

        private SkillData CreateSkillAsset(string assetName, string skillId, string skillName, string desc, SkillType type, int cd, float dmgMult, TargetType target, float cost, float generated, AnimationClip clip, GameObject vfx, string folder)
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
            skill.skillClip = clip;
            skill.skillImpactVFX = vfx;

            string path = $"{folder}/{assetName}.asset";
            AssetDatabase.CreateAsset(skill, path);
            return skill;
        }

        [MenuItem("Tools/RPG Spawn Default 4 Characters")]
        public static void SpawnDefaultCharacters()
        {
            CreateSingleCharacter("char_kazuko", "Kazuko", ElementType.Fire, CharacterRole.VANGUARD, new Color(0.9f, 0.2f, 0.1f), 700f, 150f, 40f, 110f, 0.30f, 1.80f);
            CreateSingleCharacter("char_hoshi", "Hoshi", ElementType.Ice, CharacterRole.BASTION, new Color(0.2f, 0.6f, 0.9f), 1000f, 80f, 80f, 90f, 0.05f, 1.40f);
            CreateSingleCharacter("char_rin", "Rin", ElementType.Lightning, CharacterRole.ECHO, new Color(0.9f, 0.8f, 0.1f), 650f, 90f, 45f, 130f, 0.15f, 1.60f);
            CreateSingleCharacter("char_mei", "Mei", ElementType.Nature, CharacterRole.WARDEN, new Color(0.2f, 0.8f, 0.3f), 900f, 80f, 60f, 100f, 0.10f, 1.50f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Thành công", "Đã khởi tạo thành công 4 nhân vật demo:\n- Kazuko (Fire - Vanguard)\n- Hoshi (Ice - Bastion)\n- Rin (Lightning - Echo)\n- Mei (Nature - Warden)\n\nCác file dữ liệu ScriptableObject và Prefab đã được tạo trong thư mục Assets/_Project/Resources!", "OK");
        }

        private static void CreateSingleCharacter(
            string charId, string charName, ElementType element, CharacterRole role, Color themeColor,
            float maxHP, float atk, float def, float speed, float critRate, float critDmg)
        {
            string animFolder = "Assets/_Project/Resources/Animators";
            string skillsFolder = "Assets/_Project/Resources/Skills";
            string charDataFolder = "Assets/_Project/Resources/Characters";
            string prefabFolder = "Assets/_Project/Resources/Prefabs/Characters";

            CreateFolderIfNotExistStatic("Assets/_Project/Resources");
            CreateFolderIfNotExistStatic(animFolder);
            CreateFolderIfNotExistStatic(skillsFolder);
            CreateFolderIfNotExistStatic(charDataFolder);
            CreateFolderIfNotExistStatic("Assets/_Project/Resources/Prefabs");
            CreateFolderIfNotExistStatic(prefabFolder);

            // 1. Tạo Animator Controller
            string animPath = $"{animFolder}/{charName}_Animator.controller";
            var animatorController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(animPath);
            
            // Thêm các state hoạt ảnh
            var layer = animatorController.layers[0];
            var stateMachine = layer.stateMachine;
            stateMachine.AddState("Idle");
            stateMachine.AddState("Run");
            stateMachine.AddState("Attack1");
            stateMachine.AddState("Attack2");
            stateMachine.AddState("Ultimate");
            stateMachine.AddState("Defend");
            stateMachine.AddState("Hit");
            stateMachine.AddState("Die");

            // 2. Tạo SkillData
            SkillData basicSkill = CreateSkillAssetStatic($"{charName}_SkillBasic", $"{charId}_basic", "Tấn Công Thường", "Gây sát thương thuộc tính chuẩn lên một mục tiêu.", SkillType.BASIC, 0, 1.0f, TargetType.SINGLE, 0f, 10f, null, null, skillsFolder);
            SkillData specialSkill = CreateSkillAssetStatic($"{charName}_SkillSpecial", $"{charId}_special", "Kỹ Năng Đặc Biệt", "Bộc phát kỹ năng gây sát thương lớn và tạo hiệu ứng đặc biệt lên mục tiêu.", SkillType.SPECIAL, 2, 1.5f, TargetType.SINGLE, 0f, 15f, null, null, skillsFolder);
            SkillData ultimateSkill = CreateSkillAssetStatic($"{charName}_SkillUltimate", $"{charId}_ultimate", "Chiêu Cuối (Ultimate)", "Chiêu cuối hủy diệt gây sát thương diện rộng cực lớn.", SkillType.ULTIMATE, 0, 2.2f, TargetType.AOE, 100f, 0f, null, null, skillsFolder);

            // 3. Dữ liệu CharacterData
            CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.characterId = charId;
            charData.characterName = charName;
            charData.element = element;
            charData.role = role;
            charData.themeColor = themeColor;
            charData.baseMaxHP = maxHP;
            charData.baseATK = atk;
            charData.baseDEF = def;
            charData.baseSpeed = speed;
            charData.baseCritRate = critRate;
            charData.baseCritDMG = critDmg;
            charData.isRecollectionUnlocked = true;
            
            charData.skillBasic = basicSkill;
            charData.skillSpecial = specialSkill;
            charData.skillUltimate = ultimateSkill;

            string charDataPath = $"{charDataFolder}/{charName}_Data.asset";
            AssetDatabase.CreateAsset(charData, charDataPath);

            // 4. Tạo Prefab nhân vật hoàn chỉnh
            GameObject tempGO = new GameObject($"Ally_{charName}");
            CombatCharacter cc = tempGO.AddComponent<CombatCharacter>();
            cc.isAlly = true;
            cc.characterData = charData;

            GameObject modelRootGO = new GameObject("ModelRoot");
            modelRootGO.transform.SetParent(tempGO.transform);
            cc.modelRoot = modelRootGO.transform;

            Animator animator = modelRootGO.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            string prefabPath = $"{prefabFolder}/{charName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
            DestroyImmediate(tempGO);

            Debug.Log($"[CharacterBuilder] Đã tạo thành công nhân vật: {charName}");
        }

        private static void CreateFolderIfNotExistStatic(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static SkillData CreateSkillAssetStatic(string assetName, string skillId, string skillName, string desc, SkillType type, int cd, float dmgMult, TargetType target, float cost, float generated, AnimationClip clip, GameObject vfx, string folder)
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
            skill.skillClip = clip;
            skill.skillImpactVFX = vfx;

            string path = $"{folder}/{assetName}.asset";
            AssetDatabase.CreateAsset(skill, path);
            return skill;
        }
    }
}
