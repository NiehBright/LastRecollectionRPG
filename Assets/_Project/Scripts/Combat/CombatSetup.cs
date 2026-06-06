using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public class CombatSetup : MonoBehaviour
    {
        [Header("Cơ sở dữ liệu tương khắc")]
        private ElementWeaknessDatabase weaknessDb;

        private void Start()
        {
            // 1. Tạo môi trường 3D Arena
            Setup3DArena();

            // 2. Tạo hoặc cấu hình các Managers cốt lõi
            SetupManagers();

            // 3. Khởi tạo Ma trận thuộc tính tương khắc
            SetupWeaknessDatabase();

            // 4. Tạo nhân vật và bắt đầu trận đấu
            SpawnAndStartCombat();
        }

        private void Setup3DArena()
        {
            // Cấu hình Camera Chính
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 6.5f, -12f);
                mainCam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
                mainCam.backgroundColor = new Color(0.1f, 0.12f, 0.15f); // Màu nền tối sang trọng
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }

            // Tạo Ánh sáng Directional Light nếu chưa có
            Light dirLight = FindAnyObjectByType<Light>();
            if (dirLight == null || dirLight.type != LightType.Directional)
            {
                GameObject lightGO = new GameObject("DirectionalLight");
                Light light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1.2f;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Tạo Mặt đất (Ground Plane)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattleGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            
            Renderer r = ground.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.15f, 0.18f, 0.22f); // Màu xám xanh tối
            r.material = mat;
        }

        private void SetupManagers()
        {
            GameObject managersGO = GameObject.Find("CombatManagers");
            if (managersGO == null)
            {
                managersGO = new GameObject("CombatManagers");
            }

            // Thêm các component quản lý nếu chưa có
            if (CombatManager.Instance == null) managersGO.AddComponent<CombatManager>();
            if (EffectManager.Instance == null) managersGO.AddComponent<EffectManager>();
            if (ProceduralVFX.Instance == null) managersGO.AddComponent<ProceduralVFX>();
            if (FloatingText.Instance == null) managersGO.AddComponent<FloatingText>();
            if (UIManager.Instance == null) managersGO.AddComponent<UIManager>();
        }

        private void SetupWeaknessDatabase()
        {
            weaknessDb = ScriptableObject.CreateInstance<ElementWeaknessDatabase>();
            weaknessDb.InitializeDefaults();
        }

        private void SpawnAndStartCombat()
        {
            List<CombatCharacter> allies = new List<CombatCharacter>();
            List<CombatCharacter> enemies = new List<CombatCharacter>();

            // Tạo các ScriptableObjects dữ liệu cho 4 Allies và 4 Enemies mặc định phòng hờ theo cấu trúc vai trò mới
            CharacterData ally1 = CreateAllyData("Kazuko", ElementType.Fire, CharacterRole.VANGUARD, new Color(0.9f, 0.2f, 0.1f), 700f, 150f, 40f, 110f, 0.30f, 1.80f);
            CharacterData ally2 = CreateAllyData("Hoshi", ElementType.Ice, CharacterRole.BASTION, new Color(0.2f, 0.6f, 0.9f), 1000f, 80f, 80f, 90f, 0.05f, 1.40f);
            CharacterData ally3 = CreateAllyData("Rin", ElementType.Lightning, CharacterRole.ECHO, new Color(0.9f, 0.8f, 0.1f), 650f, 90f, 45f, 130f, 0.15f, 1.60f);
            CharacterData ally4 = CreateAllyData("Mei", ElementType.Nature, CharacterRole.WARDEN, new Color(0.2f, 0.8f, 0.3f), 900f, 80f, 60f, 100f, 0.10f, 1.50f);

            CharacterData enemy1 = CreateEnemyData("Fire Slime", ElementType.Fire, new Color(0.8f, 0.1f, 0.1f), 700f, 90f, 40f, 90f);
            CharacterData enemy2 = CreateEnemyData("Ice Sentinel", ElementType.Ice, new Color(0.1f, 0.7f, 0.8f), 900f, 80f, 75f, 80f);
            CharacterData enemy3 = CreateEnemyData("Lightning Imp", ElementType.Lightning, new Color(0.7f, 0.1f, 0.8f), 600f, 110f, 45f, 120f);
            CharacterData enemy4 = CreateEnemyData("Forest Spider", ElementType.Nature, new Color(0.1f, 0.5f, 0.2f), 800f, 95f, 50f, 100f);

            CharacterData[] allyDatas = new CharacterData[] { ally1, ally2, ally3, ally4 };
            CharacterData[] enemyDatas = new CharacterData[] { enemy1, enemy2, enemy3, enemy4 };

            if (CombatTeamManager.IsEnteringFromOverworld)
            {
                // Xóa các quái/nhân vật mặc định có sẵn trong Scene đấu trường để tránh bị thừa
                CombatCharacter[] presetChars = FindObjectsByType<CombatCharacter>(FindObjectsSortMode.None);
                foreach (var pc in presetChars)
                {
                    Destroy(pc.gameObject);
                }

                // Sinh đội hình Đồng minh động
                int allyCount = CombatTeamManager.SelectedAllies.Count;
                Vector3[] allyPositions = GetDynamicPositions(allyCount, true);
                for (int i = 0; i < allyCount; i++)
                {
                    CharacterData data = CombatTeamManager.SelectedAllies[i];
                    GameObject prefab = null;
                    if (CharacterMenuManager.Instance != null && CharacterMenuManager.Instance.characters != null)
                    {
                        var menuChar = CharacterMenuManager.Instance.characters.Find(c => c.characterName == data.characterName);
                        if (menuChar != null) prefab = menuChar.modelPrefab;
                    }
                    if (prefab == null) prefab = Resources.Load<GameObject>($"Prefabs/Characters/{data.characterName}");
                    if (prefab == null) prefab = Resources.Load<GameObject>($"Prefabs/{data.characterName}");

                    GameObject go;
                    if (prefab != null)
                    {
                        go = Instantiate(prefab, allyPositions[i], Quaternion.identity);
                    }
                    else
                    {
                        go = new GameObject("Ally_" + data.characterName);
                        go.transform.position = allyPositions[i];
                    }

                    CleanOverworldComponents(go);
                    SetCombatRotation(go, true);

                    CombatCharacter cc = go.GetComponent<CombatCharacter>();
                    if (cc == null) cc = go.AddComponent<CombatCharacter>();
                    cc.Initialize(data, true);
                    allies.Add(cc);
                }

                // Sinh đội hình Kẻ địch động
                int enemyCount = CombatTeamManager.SelectedEnemies.Count;
                Vector3[] enemyPositions = GetDynamicPositions(enemyCount, false);
                for (int i = 0; i < enemyCount; i++)
                {
                    CharacterData data = CombatTeamManager.SelectedEnemies[i];
                    GameObject prefab = Resources.Load<GameObject>($"Prefabs/Characters/{data.characterName}");
                    if (prefab == null) prefab = Resources.Load<GameObject>($"Prefabs/{data.characterName}");

                    GameObject go;
                    if (prefab != null)
                    {
                        go = Instantiate(prefab, enemyPositions[i], Quaternion.identity);
                    }
                    else
                    {
                        go = new GameObject("Enemy_" + data.characterName);
                        go.transform.position = enemyPositions[i];
                    }

                    CleanOverworldComponents(go);
                    SetCombatRotation(go, false);

                    CombatCharacter cc = go.GetComponent<CombatCharacter>();
                    if (cc == null) cc = go.AddComponent<CombatCharacter>();
                    cc.Initialize(data, false);
                    enemies.Add(cc);
                }
            }
            else
            {
                // 1. Quét tìm các CombatCharacter có sẵn trên Hierarchy
                CombatCharacter[] existingChars = FindObjectsByType<CombatCharacter>(FindObjectsSortMode.None);
                
                // 2. Tìm thêm các GameObject có tên chứa 'Ally_' hoặc 'Enemy_' nhưng thiếu script để tự động gán
                if (existingChars == null || existingChars.Length == 0)
                {
                    var allGOs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    List<CombatCharacter> foundList = new List<CombatCharacter>();
                    foreach (var go in allGOs)
                    {
                        if (go.transform.parent == null) // Chỉ quét ở root Hierarchy để tránh lấy các model con bên trong
                        {
                            if (go.name.StartsWith("Ally_") || go.name.StartsWith("Enemy_"))
                            {
                                CombatCharacter cc = go.AddComponent<CombatCharacter>();
                                cc.isAlly = go.name.StartsWith("Ally_");
                                foundList.Add(cc);
                            }
                        }
                    }
                    if (foundList.Count > 0)
                    {
                        existingChars = foundList.ToArray();
                    }
                }

                // 3. Nếu tìm thấy nhân vật có sẵn, tiến hành liên kết và sử dụng
                if (existingChars != null && existingChars.Length > 0)
                {
                    Debug.Log($"[CombatSetup] Tìm thấy {existingChars.Length} nhân vật có sẵn trên scene. Sử dụng chúng cho trận đấu.");
                    int allyIndex = 0;
                    int enemyIndex = 0;

                    foreach (var cc in existingChars)
                    {
                        if (cc.characterData == null)
                        {
                            if (cc.isAlly)
                            {
                                cc.characterData = allyDatas[Mathf.Min(allyIndex, allyDatas.Length - 1)];
                                allyIndex++;
                            }
                            else
                            {
                                cc.characterData = enemyDatas[Mathf.Min(enemyIndex, enemyDatas.Length - 1)];
                                enemyIndex++;
                            }
                        }

                        cc.Initialize(cc.characterData, cc.isAlly);
                        CleanOverworldComponents(cc.gameObject);
                        SetCombatRotation(cc.gameObject, cc.isAlly);

                        if (cc.isAlly) allies.Add(cc);
                        else enemies.Add(cc);
                    }
                }
                else
                {
                    // 4. Nếu hoàn toàn trống trơ, tiến hành tạo Capsule/Cylinder dự phòng
                    Debug.Log("[CombatSetup] Scene trống trơn. Tạo các Capsule/Cylinder dự phòng bằng code...");
                    
                    Vector3[] allyPositions = new Vector3[]
                    {
                        new Vector3(-4.5f, 0f, -4f),
                        new Vector3(-1.5f, 0f, -4.5f),
                        new Vector3(1.5f, 0f, -4.5f),
                        new Vector3(4.5f, 0f, -4f)
                    };

                    Vector3[] enemyPositions = new Vector3[]
                    {
                        new Vector3(-4.5f, 0f, 4f),
                        new Vector3(-1.5f, 0f, 4.5f),
                        new Vector3(1.5f, 0f, 4.5f),
                        new Vector3(4.5f, 0f, 4f)
                    };

                    for (int i = 0; i < 4; i++)
                    {
                        GameObject go = new GameObject("Ally_" + allyDatas[i].characterName);
                        go.transform.position = allyPositions[i];
                        CleanOverworldComponents(go);
                        SetCombatRotation(go, true);

                        CombatCharacter cc = go.AddComponent<CombatCharacter>();
                        cc.Initialize(allyDatas[i], true);
                        allies.Add(cc);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        GameObject go = new GameObject("Enemy_" + enemyDatas[i].characterName);
                        go.transform.position = enemyPositions[i];
                        CleanOverworldComponents(go);
                        SetCombatRotation(go, false);

                        CombatCharacter cc = go.AddComponent<CombatCharacter>();
                        cc.Initialize(enemyDatas[i], false);
                        enemies.Add(cc);
                    }
                }
            }

            // Kích hoạt trận đấu
            CombatManager.Instance.StartCombat(allies, enemies, weaknessDb);
        }

        private void CleanOverworldComponents(GameObject go)
        {
            if (go == null) return;

            // Vô hiệu hóa ngay lập tức để tránh lỗi di chuyển hay trọng lực kéo sập model xuống
            var wasd = go.GetComponent<BLINK.Controller.TopDownWASDController>();
            if (wasd != null) wasd.enabled = false;

            var combatCtrl = go.GetComponent<BLINK.Controller.CombatController>();
            if (combatCtrl != null) combatCtrl.enabled = false;

            var cc = go.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Hủy các component overworld này khỏi gameobject
            if (wasd != null) Destroy(wasd);
            if (combatCtrl != null) Destroy(combatCtrl);
            if (cc != null) Destroy(cc);
            if (rb != null) Destroy(rb);
            if (col != null) Destroy(col);
        }

        private void SetCombatRotation(GameObject go, bool isAlly)
        {
            if (go == null) return;

            // Allies hướng lên trên (+Z: xoay 0 Y), Enemies hướng xuống dưới (-Z: xoay 180 Y)
            float targetY = isAlly ? 0f : 180f;

            // Kiểm tra xem model có bị xoay ngược 180 độ sẵn không (ví dụ ModelRoot xoay ngược)
            Transform modelRoot = go.transform.Find("ModelRoot");
            if (modelRoot == null && go.transform.childCount > 0)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    Transform child = go.transform.GetChild(i);
                    if (child.name.ToLower().Contains("model") || child.name.ToLower().Contains("mesh") || child.name.ToLower().Contains("root"))
                    {
                        modelRoot = child;
                        break;
                    }
                }
            }

            if (modelRoot != null)
            {
                // Nếu modelRoot có rotation ban đầu bị xoay (ví dụ localEulerAngles.y khoảng 180), ta có thể bù trừ
                float localY = modelRoot.localEulerAngles.y;
                if (Mathf.Approximately(localY, 180f) || (localY > 170f && localY < 190f))
                {
                    // Model con bị xoay ngược 180 độ, ta bù lại bằng cách xoay root ngược 180 độ so với hướng gốc
                    go.transform.rotation = Quaternion.Euler(0f, targetY + 180f, 0f);
                    return;
                }
            }

            go.transform.rotation = Quaternion.Euler(0f, targetY, 0f);
        }

        #region Sinh Dữ Liệu Nhân Vật bằng C# (In-Memory)

        private CharacterData CreateAllyData(string name, ElementType element, CharacterRole role, Color color, float hp, float atk, float def, float speed, float critRate, float critDmg)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = "ally_" + name.ToLower().Replace(" ", "_");
            data.characterName = name;
            data.element = element;
            data.role = role;
            data.themeColor = color;
            data.isRecollectionUnlocked = true;

            data.baseMaxHP = hp;
            data.baseATK = atk;
            data.baseDEF = def;
            data.baseSpeed = speed;
            data.baseCritRate = critRate;
            data.baseCritDMG = critDmg;

            // Tạo các kỹ năng theo vai trò
            data.skillBasic = CreateBasicAttackForRole(data.characterId, role, element);
            data.skillSpecial = CreateSpecialSkillForRole(data.characterId, role, element);
            data.skillUltimate = CreateUltimateSkillForRole(data.characterId, role, element);

            return data;
        }

        private CharacterData CreateEnemyData(string name, ElementType element, Color color, float hp, float atk, float def, float speed)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = "enemy_" + name.ToLower().Replace(" ", "_");
            data.characterName = name;
            data.element = element;
            data.role = CharacterRole.VANGUARD; // Kẻ địch mặc định là Vanguard
            data.themeColor = color;
            data.isRecollectionUnlocked = false;

            data.baseMaxHP = hp;
            data.baseATK = atk;
            data.baseDEF = def;
            data.baseSpeed = speed;
            data.baseCritRate = 0.05f;
            data.baseCritDMG = 1.50f;

            // Kẻ địch dùng kỹ năng Vanguard cơ bản
            data.skillBasic = CreateBasicAttackForRole(data.characterId, CharacterRole.VANGUARD, element);
            data.skillSpecial = CreateSpecialSkillForRole(data.characterId, CharacterRole.VANGUARD, element);
            data.skillUltimate = CreateUltimateSkillForRole(data.characterId, CharacterRole.VANGUARD, element);

            return data;
        }

        private SkillData CreateBasicAttackForRole(string charId, CharacterRole role, ElementType element)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = charId + "_basic";
            skill.skillType = SkillType.BASIC;
            skill.cooldown = 0;
            skill.damageMultiplier = 1.0f;
            skill.targetType = TargetType.SINGLE;
            skill.energyCost = 0f;
            skill.energyGenerated = 10f;
            skill.skillColor = Color.white;

            switch (role)
            {
                case CharacterRole.BASTION:
                    skill.skillName = "Khiên Kích Provoke";
                    skill.description = "Tấn công bằng khiên gây sát thương và khiêu khích đối thủ.";
                    skill.damageMultiplier = 0.8f;
                    break;
                case CharacterRole.VANGUARD:
                    skill.skillName = "Chém Nhược Điểm";
                    skill.description = "Chém mạnh vào yếu điểm của kẻ địch.";
                    skill.damageMultiplier = 1.2f;
                    break;
                case CharacterRole.ECHO:
                    skill.skillName = "Sét Cộng Hưởng";
                    skill.description = "Tấn công tia sét có khả năng truyền dẫn cộng hưởng.";
                    skill.damageMultiplier = 0.9f;
                    break;
                case CharacterRole.WARDEN:
                    skill.skillName = "Cành Lá Trị Liệu";
                    skill.description = "Đòn đánh nhẹ hồi HP nhỏ cho đồng đội ít máu nhất.";
                    skill.damageMultiplier = 0.6f;
                    break;
                case CharacterRole.PHANTOM:
                    skill.skillName = "Tấn Công Suy Yếu";
                    skill.description = "Tấn công nhanh làm suy yếu phòng tuyến kẻ địch.";
                    skill.damageMultiplier = 0.9f;
                    break;
            }

            return skill;
        }

        private SkillData CreateSpecialSkillForRole(string charId, CharacterRole role, ElementType element)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = charId + "_special";
            skill.skillType = SkillType.SPECIAL;
            skill.cooldown = 2;
            skill.energyCost = 0f;
            skill.energyGenerated = 15f;

            switch (role)
            {
                case CharacterRole.BASTION:
                    skill.skillName = "Fortress Stance";
                    skill.description = "Bật thế phòng thủ vững chắc, tạo lá chắn giảm 40% sát thương nhận vào trong 2 lượt.";
                    skill.damageMultiplier = 0f;
                    skill.targetType = TargetType.ALL_ALLIES;
                    skill.skillColor = Color.blue;
                    
                    EffectData fort = ScriptableObject.CreateInstance<EffectData>();
                    fort.effectId = "fortress_buff";
                    fort.effectName = "Fortress Shield";
                    fort.effectType = EffectType.DEF_BUFF;
                    fort.duration = 2;
                    fort.modifierValue = 0.40f; // DEF +40%
                    fort.effectColor = Color.blue;
                    skill.effects.Add(fort);
                    break;

                case CharacterRole.VANGUARD:
                    skill.skillName = "Breach Armor";
                    skill.description = "Đòn đâm phá giáp, gây sát thương lớn bỏ qua 30% DEF kẻ địch.";
                    skill.damageMultiplier = 1.6f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.red;
                    
                    if (element == ElementType.Fire)
                    {
                        EffectData burn = ScriptableObject.CreateInstance<EffectData>();
                        burn.effectId = "debuff_burn";
                        burn.effectName = "Thiêu Đốt";
                        burn.effectType = EffectType.BURN;
                        burn.duration = 3;
                        burn.modifierValue = 0.4f;
                        burn.effectColor = Color.red;
                        skill.effects.Add(burn);
                    }
                    break;

                case CharacterRole.ECHO:
                    skill.skillName = "Chain Catalyst";
                    skill.description = "Sét lan truyền qua 3 mục tiêu ngẫu nhiên gây sát thương liên tiếp.";
                    skill.damageMultiplier = 1.2f;
                    skill.targetType = TargetType.AOE;
                    skill.skillColor = Color.yellow;
                    break;

                case CharacterRole.WARDEN:
                    skill.skillName = "Bloom";
                    skill.description = "Hồi HP mạnh cho 1 đồng đội và giải trừ 1 hiệu ứng xấu.";
                    skill.damageMultiplier = 0f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.green;
                    skill.cooldown = 3;

                    EffectData heal = ScriptableObject.CreateInstance<EffectData>();
                    heal.effectId = "heal_bloom";
                    heal.effectName = "Bloom Heal";
                    heal.effectType = EffectType.DAMAGE_OVER_TIME; // DOT âm = Heal
                    heal.duration = 2;
                    heal.modifierValue = -0.5f; // Hồi 50% ATK mỗi lượt
                    heal.effectColor = Color.green;
                    skill.effects.Add(heal);
                    break;

                case CharacterRole.PHANTOM:
                    skill.skillName = "Unravel Defenses";
                    skill.description = "Tấn công làm suy yếu kẻ địch, giảm 20% ATK trong 2 lượt.";
                    skill.damageMultiplier = 1.2f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.magenta;

                    EffectData weak = ScriptableObject.CreateInstance<EffectData>();
                    weak.effectId = "debuff_weaken";
                    weak.effectName = "Suy Yếu ATK";
                    weak.effectType = EffectType.ATK_BUFF; // Buff ATK âm = Giảm ATK
                    weak.duration = 2;
                    weak.modifierValue = -0.2f;
                    weak.effectColor = Color.magenta;
                    skill.effects.Add(weak);
                    break;
            }

            return skill;
        }

        private SkillData CreateUltimateSkillForRole(string charId, CharacterRole role, ElementType element)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = charId + "_ultimate";
            skill.skillName = "Tuyệt Kỹ " + role.ToString();
            skill.description = "Giải phóng năng lượng bộc phá sức mạnh tối đa.";
            skill.skillType = SkillType.ULTIMATE;
            skill.cooldown = 0;
            skill.damageMultiplier = 2.2f;
            skill.targetType = TargetType.AOE;
            skill.energyCost = 100f;
            skill.energyGenerated = 0f;

            switch (element)
            {
                case ElementType.Fire:
                    skill.skillName = "Hỏa Tiễn Hủy Diệt";
                    skill.description = "Thiêu cháy toàn bộ chiến trường. Gây sát thương diện rộng x2.2 ATK lên tất cả kẻ địch.";
                    skill.skillColor = Color.red;
                    break;

                case ElementType.Ice:
                    skill.skillName = "Tuyệt Đối Băng Phong";
                    skill.description = "Đóng băng vĩnh cửu một mục tiêu. Gây sát thương x1.8 ATK và đặt trạng thái ĐÓNG BĂNG (Freeze) 1 lượt.";
                    skill.damageMultiplier = 1.8f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.blue;

                    EffectData freeze = ScriptableObject.CreateInstance<EffectData>();
                    freeze.effectId = "debuff_freeze";
                    freeze.effectName = "Đóng Băng";
                    freeze.effectType = EffectType.FREEZE;
                    freeze.duration = 1;
                    freeze.modifierValue = 0f;
                    freeze.effectColor = Color.blue;
                    skill.effects.Add(freeze);
                    break;

                case ElementType.Lightning:
                    skill.skillName = "Thiên Lôi Triệu Hồi";
                    skill.description = "Trừng phạt sấm sét gây x2.8 sát thương lên một kẻ địch và có 50% làm CHOÁNG (Stun) 1 lượt.";
                    skill.damageMultiplier = 2.8f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.yellow;

                    EffectData stun = ScriptableObject.CreateInstance<EffectData>();
                    stun.effectId = "debuff_stun";
                    stun.effectName = "Choáng";
                    stun.effectType = EffectType.STUN;
                    stun.duration = 1;
                    stun.modifierValue = 0f;
                    stun.effectColor = Color.yellow;
                    skill.effects.Add(stun);
                    break;

                case ElementType.Nature:
                    skill.skillName = "Rừng Già Trỗi Dậy";
                    skill.description = "Hồi sinh năng lượng sống. Hồi 10% HP và tăng 30% ATK cho toàn bộ đồng đội trong 3 lượt.";
                    skill.damageMultiplier = 0.0f;
                    skill.targetType = TargetType.ALL_ALLIES;
                    skill.skillColor = Color.green;

                    EffectData atkBuff = ScriptableObject.CreateInstance<EffectData>();
                    atkBuff.effectId = "buff_atk";
                    atkBuff.effectName = "Tăng ATK";
                    atkBuff.effectType = EffectType.ATK_BUFF;
                    atkBuff.duration = 3;
                    atkBuff.modifierValue = 0.3f; // Tăng 30% ATK
                    atkBuff.effectColor = Color.green;
                    skill.effects.Add(atkBuff);
                    break;

                default:
                    skill.skillName = "Chấn Động Địa Cầu";
                    skill.description = "Giã một đòn trời giáng gây x2.5 sát thương đơn mục tiêu.";
                    skill.damageMultiplier = 2.5f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.gray;
                    break;
            }

            return skill;
        }

        private Vector3[] GetDynamicPositions(int count, bool isAllySide)
        {
            float z = isAllySide ? -4.5f : 4.5f;
            float zOffset = isAllySide ? 0.5f : -0.5f; // Đẩy các vị trí ngoài lùi lại một chút
            Vector3[] pos = new Vector3[count];

            if (count == 1)
            {
                pos[0] = new Vector3(0f, 0f, z);
            }
            else if (count == 2)
            {
                pos[0] = new Vector3(-1.5f, 0f, z);
                pos[1] = new Vector3(1.5f, 0f, z);
            }
            else if (count == 3)
            {
                pos[0] = new Vector3(-3.0f, 0f, z - zOffset);
                pos[1] = new Vector3(0f, 0f, z);
                pos[2] = new Vector3(3.0f, 0f, z - zOffset);
            }
            else
            {
                // Mặc định hoặc 4 trở lên
                pos[0] = new Vector3(-4.5f, 0f, z - zOffset);
                pos[1] = new Vector3(-1.5f, 0f, z);
                pos[2] = new Vector3(1.5f, 0f, z);
                pos[3] = new Vector3(4.5f, 0f, z - zOffset);
                for (int i = 4; i < count; i++)
                {
                    pos[i] = new Vector3(1.5f * (i - 1.5f), 0f, z - zOffset * 2);
                }
            }
            return pos;
        }

        #endregion
    }
}
