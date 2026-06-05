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

            // Tạo các ScriptableObjects dữ liệu cho 4 Allies và 4 Enemies mặc định phòng hờ
            CharacterData ally1 = CreateAllyData("Fire Warrior", ElementType.Fire, new Color(0.9f, 0.2f, 0.1f), 800f, 120f, 60f, 110f, 0.20f, 1.50f);
            CharacterData ally2 = CreateAllyData("Ice Mage", ElementType.Ice, new Color(0.2f, 0.6f, 0.9f), 600f, 100f, 40f, 95f, 0.10f, 1.60f);
            CharacterData ally3 = CreateAllyData("Storm Rogue", ElementType.Lightning, new Color(0.9f, 0.8f, 0.1f), 700f, 140f, 50f, 125f, 0.30f, 1.80f);
            CharacterData ally4 = CreateAllyData("Nature Druid", ElementType.Nature, new Color(0.2f, 0.8f, 0.3f), 1000f, 80f, 70f, 100f, 0.05f, 1.20f);

            CharacterData enemy1 = CreateEnemyData("Fire Slime", ElementType.Fire, new Color(0.8f, 0.1f, 0.1f), 700f, 90f, 40f, 90f);
            CharacterData enemy2 = CreateEnemyData("Ice Sentinel", ElementType.Ice, new Color(0.1f, 0.7f, 0.8f), 900f, 80f, 75f, 80f);
            CharacterData enemy3 = CreateEnemyData("Lightning Imp", ElementType.Lightning, new Color(0.7f, 0.1f, 0.8f), 600f, 110f, 45f, 120f);
            CharacterData enemy4 = CreateEnemyData("Forest Spider", ElementType.Nature, new Color(0.1f, 0.5f, 0.2f), 800f, 95f, 50f, 100f);

            CharacterData[] allyDatas = new CharacterData[] { ally1, ally2, ally3, ally4 };
            CharacterData[] enemyDatas = new CharacterData[] { enemy1, enemy2, enemy3, enemy4 };

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

                    if (cc.isAlly) allies.Add(cc);
                    else enemies.Add(cc);
                }
            }
            else
            {
                // 4. Nếu hoàn toàn trống trơ, tiến hành tạo Capsule/Cylinder procedural dự phòng
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
                    go.transform.rotation = Quaternion.identity;

                    CombatCharacter cc = go.AddComponent<CombatCharacter>();
                    cc.Initialize(allyDatas[i], true);
                    allies.Add(cc);
                }

                for (int i = 0; i < 4; i++)
                {
                    GameObject go = new GameObject("Enemy_" + enemyDatas[i].characterName);
                    go.transform.position = enemyPositions[i];
                    go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                    CombatCharacter cc = go.AddComponent<CombatCharacter>();
                    cc.Initialize(enemyDatas[i], false);
                    enemies.Add(cc);
                }
            }

            // Kích hoạt trận đấu
            CombatManager.Instance.StartCombat(allies, enemies, weaknessDb);
        }

        #region Sinh Dữ Liệu Nhân Vật bằng C# (In-Memory)

        private CharacterData CreateAllyData(string name, ElementType element, Color color, float hp, float atk, float def, float speed, float critRate, float critDmg)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = "ally_" + name.ToLower().Replace(" ", "_");
            data.characterName = name;
            data.element = element;
            data.themeColor = color;

            data.baseMaxHP = hp;
            data.baseATK = atk;
            data.baseDEF = def;
            data.baseSpeed = speed;
            data.baseCritRate = critRate;
            data.baseCritDMG = critDmg;

            // Tạo các kỹ năng đặc thù
            data.skillBasic = CreateBasicAttack(data.characterId, element);
            data.skillSpecial = CreateSpecialSkill(data.characterId, element);
            data.skillUltimate = CreateUltimateSkill(data.characterId, element);

            return data;
        }

        private CharacterData CreateEnemyData(string name, ElementType element, Color color, float hp, float atk, float def, float speed)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = "enemy_" + name.ToLower().Replace(" ", "_");
            data.characterName = name;
            data.element = element;
            data.themeColor = color;

            data.baseMaxHP = hp;
            data.baseATK = atk;
            data.baseDEF = def;
            data.baseSpeed = speed;
            data.baseCritRate = 0.05f;
            data.baseCritDMG = 1.50f;

            // Kẻ địch chỉ cần Basic Attack và Special
            data.skillBasic = CreateBasicAttack(data.characterId, element);
            data.skillSpecial = CreateSpecialSkill(data.characterId, element);
            
            // Chiêu cuối (dành cho kẻ địch boss hoặc cấu hình mặc định)
            data.skillUltimate = CreateUltimateSkill(data.characterId, element);

            return data;
        }

        private SkillData CreateBasicAttack(string charId, ElementType element)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = charId + "_basic";
            skill.skillName = "Tấn Công Thường";
            skill.description = "Gây sát thương vật lý/thuộc tính chuẩn lên một mục tiêu.";
            skill.skillType = SkillType.BASIC;
            skill.cooldown = 0;
            skill.damageMultiplier = 1.0f;
            skill.targetType = TargetType.SINGLE;
            skill.energyCost = 0f;
            skill.energyGenerated = 10f; // Hồi 10 năng lượng
            skill.skillColor = Color.white;
            return skill;
        }

        private SkillData CreateSpecialSkill(string charId, ElementType element)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = charId + "_special";
            skill.skillType = SkillType.SPECIAL;
            skill.cooldown = 2;
            skill.energyCost = 0f;
            skill.energyGenerated = 15f; // Hồi 15 năng lượng

            // Tùy theo thuộc tính tạo hiệu ứng đặc thù
            switch (element)
            {
                case ElementType.Fire:
                    skill.skillName = "Hỏa Long Tiễn";
                    skill.description = "Bắn mũi tên lửa gây sát thương lớn và đặt hiệu ứng Burn (thiêu đốt) gây DOT 40% ATK trong 3 lượt.";
                    skill.damageMultiplier = 1.5f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.red;

                    EffectData burn = ScriptableObject.CreateInstance<EffectData>();
                    burn.effectId = "debuff_burn";
                    burn.effectName = "Thiêu Đốt";
                    burn.effectType = EffectType.DAMAGE_OVER_TIME;
                    burn.duration = 3;
                    burn.modifierValue = 0.4f; // Gây sát thương DOT = 40% ATK người ra chiêu
                    burn.effectColor = Color.red;
                    skill.effects.Add(burn);
                    break;

                case ElementType.Ice:
                    skill.skillName = "Sương Băng Chậm Chạp";
                    skill.description = "Phủ luồng không khí lạnh gây sát thương diện rộng và giảm 30% tốc độ của toàn bộ kẻ địch trong 2 lượt.";
                    skill.damageMultiplier = 1.1f;
                    skill.targetType = TargetType.AOE;
                    skill.skillColor = Color.cyan;

                    EffectData slow = ScriptableObject.CreateInstance<EffectData>();
                    slow.effectId = "debuff_slow";
                    slow.effectName = "Giảm Tốc";
                    slow.effectType = EffectType.SPEED_CHANGE;
                    slow.duration = 2;
                    slow.modifierValue = -0.3f; // Giảm 30% Speed
                    slow.effectColor = Color.cyan;
                    skill.effects.Add(slow);
                    break;

                case ElementType.Lightning:
                    skill.skillName = "Tia Sét Quá Tải";
                    skill.description = "Bộc phát luồng điện cực mạnh gây sát thương đơn mục tiêu x1.6 ATK.";
                    skill.damageMultiplier = 1.6f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.yellow;
                    break;

                case ElementType.Nature:
                    skill.skillName = "Phục Hồi Sinh Mệnh";
                    skill.description = "Niệm chú bảo vệ thiên nhiên hồi 150 HP cho một đồng đội.";
                    skill.damageMultiplier = 0.0f; // Không gây sát thương
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.green;

                    // Do cơ chế heal ta sẽ thực hiện trực tiếp trong skill hoặc dùng buff HP regen
                    // Để đơn giản, ta gán hiệu ứng Regen hồi máu dần hoặc hồi máu tức thì bằng hệ số ATK_BUFF đặc biệt.
                    // Hãy hồi máu tức thì trong Skill bằng một buff hồi máu (ta có thể xử lý trong DamageCalculator hoặc trong logic).
                    // Ở đây ta tạo một buff HP_REGEN:
                    EffectData regen = ScriptableObject.CreateInstance<EffectData>();
                    regen.effectId = "buff_regen";
                    regen.effectName = "Hồi Máu";
                    regen.effectType = EffectType.DAMAGE_OVER_TIME; // Dùng DOT nhưng giá trị âm để thành hồi máu!
                    regen.duration = 2;
                    regen.modifierValue = -0.4f; // DOT âm = Hồi HP bằng 40% ATK mỗi lượt!
                    regen.effectColor = Color.green;
                    skill.effects.Add(regen);
                    break;

                default:
                    skill.skillName = "Thiết Giáp Kích";
                    skill.description = "Tấn công vật lý nặng nề x1.4 sát thương.";
                    skill.damageMultiplier = 1.4f;
                    skill.targetType = TargetType.SINGLE;
                    skill.skillColor = Color.gray;
                    break;
            }

            return skill;
        }

        private SkillData CreateUltimateSkill(string charId, ElementType element)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = charId + "_ultimate";
            skill.skillType = SkillType.ULTIMATE;
            skill.cooldown = 0; // Chiêu cuối không có cooldown, dùng năng lượng
            skill.energyCost = 100f;
            skill.energyGenerated = 0f;

            switch (element)
            {
                case ElementType.Fire:
                    skill.skillName = "Hỏa Tiễn Hủy Diệt";
                    skill.description = "Thiêu cháy toàn bộ chiến trường. Gây sát thương diện rộng x2.2 ATK lên tất cả kẻ địch.";
                    skill.damageMultiplier = 2.2f;
                    skill.targetType = TargetType.AOE;
                    skill.skillColor = Color.red;
                    break;

                case ElementType.Ice:
                    skill.skillName = "Tuyệt Đối Băng Phong";
                    skill.description = "Đóng băng vĩnh cửu một mục tiêu. Gây sát thương x1.8 ATK và đặt trạng thái ĐÓNG BĂNG (Freeze) khiến mục tiêu không thể hành động trong 1 lượt.";
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
                    skill.description = "Trừng phạt sấm sét mục tiêu mạnh mẽ nhất. Gây sát thương cực lớn x2.8 ATK lên một kẻ địch và có 50% làm CHOÁNG (Stun) chúng trong 1 lượt.";
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
                    skill.description = "Hồi sinh toàn bộ năng lượng sống của rừng. Hồi máu nhẹ và tăng 30% ATK cho toàn bộ đồng đội trong 3 lượt.";
                    skill.damageMultiplier = 0.0f; // Kỹ năng hỗ trợ
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

        #endregion
    }
}
