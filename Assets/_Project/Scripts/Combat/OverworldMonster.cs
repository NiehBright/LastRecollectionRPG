using System.Collections.Generic;
using UnityEngine;
using BLINK.Controller;

namespace RPG.Combat
{
    public class OverworldMonster : MonoBehaviour
    {
        [Header("Thông tin quái vật")]
        [Tooltip("ID duy nhất để lưu trạng thái biến mất của quái sau khi bị đánh bại")]
        public string uniqueId = "monster_01";
        
        [Tooltip("Đội hình quái sẽ xuất hiện trong trận đấu (để trống sẽ tự động dùng quái mặc định)")]
        public List<CharacterData> enemyTeam = new List<CharacterData>();
        
        [Tooltip("Khoảng cách bắt đầu chạm trán và mở UI chọn nhân vật")]
        public float detectionRadius = 2.2f;

        private bool triggered = false;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId) || uniqueId == "monster_01")
            {
                uniqueId = "monster_" + System.Guid.NewGuid().ToString().Substring(0, 8) + "_" + gameObject.name.Replace(" ", "_");
            }
        }
#endif

        private void Start()
        {
            // Kiểm tra xem quái vật này đã bị tiêu diệt ở lượt trước chưa
            if (CombatTeamManager.IsEnteringFromOverworld && 
                !string.IsNullOrEmpty(uniqueId) && 
                uniqueId == CombatTeamManager.MonsterToDestroyId && 
                CombatTeamManager.CombatResult == CombatResultType.WIN)
            {
                Debug.Log($"[OverworldMonster] Quái vật '{uniqueId}' đã bị tiêu diệt ở trận trước. Tiến hành tự hủy.");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Đã chuyển cơ chế kích hoạt sang đòn đánh cận chiến (CombatController.ApplyHit)
        }

        private void TriggerEncounter(TopDownWASDController player)
        {
            Debug.Log($"[OverworldMonster] Chạm trán người chơi! Khóa di chuyển và mở UI Chọn Đội Hình.");
            
            // Khóa di chuyển của người chơi
            player.movementEnabled = false;
            player.cameraEnabled = false;
            
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Horizontal", 0f);
                anim.SetFloat("Vertical", 0f);
            }

            // Kích hoạt UI chọn đội hình
            if (CombatTeamSelectionUI.Instance != null)
            {
                CombatTeamSelectionUI.Instance.OpenUI(this);
            }
            else
            {
                // Nếu chưa có, tự tạo GameObject giữ script này
                GameObject selectionUIGO = new GameObject("[CombatTeamSelectionUI]");
                CombatTeamSelectionUI ui = selectionUIGO.AddComponent<CombatTeamSelectionUI>();
                ui.OpenUI(this);
            }
        }

        /// <summary>
        /// Tạo đội hình kẻ địch mặc định cho quái vật này nếu không được cấu hình sẵn
        /// </summary>
        public List<CharacterData> GetDefaultEnemyTeam()
        {
            List<CharacterData> list = new List<CharacterData>();

            // Slime hỏa
            CharacterData e1 = ScriptableObject.CreateInstance<CharacterData>();
            e1.characterId = "enemy_fire_slime_" + uniqueId;
            e1.characterName = "Fire Slime";
            e1.element = ElementType.Fire;
            e1.themeColor = new Color(0.8f, 0.1f, 0.1f);
            e1.baseMaxHP = 700f;
            e1.baseATK = 90f;
            e1.baseDEF = 40f;
            e1.baseSpeed = 90f;
            e1.skillBasic = CreateBasicAttack(e1.characterId);
            list.Add(e1);

            // Sentinel băng
            CharacterData e2 = ScriptableObject.CreateInstance<CharacterData>();
            e2.characterId = "enemy_ice_sentinel_" + uniqueId;
            e2.characterName = "Ice Sentinel";
            e2.element = ElementType.Ice;
            e2.themeColor = new Color(0.1f, 0.7f, 0.8f);
            e2.baseMaxHP = 900f;
            e2.baseATK = 80f;
            e2.baseDEF = 75f;
            e2.baseSpeed = 80f;
            e2.skillBasic = CreateBasicAttack(e2.characterId);
            list.Add(e2);

            return list;
        }

        private SkillData CreateBasicAttack(string charId)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = charId + "_basic";
            skill.skillName = "Tấn Công Thường";
            skill.skillType = SkillType.BASIC;
            skill.cooldown = 0;
            skill.damageMultiplier = 1.0f;
            skill.targetType = TargetType.SINGLE;
            skill.energyCost = 0f;
            skill.energyGenerated = 10f;
            skill.skillColor = Color.white;
            return skill;
        }

        // Vẽ bán kính phát hiện trong Editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
