using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public enum CombatResultType
    {
        NONE,
        WIN,
        LOSE
    }

    public static class CombatTeamManager
    {
        // Danh sách nhân vật người chơi đã chọn để chiến đấu
        public static List<CharacterData> SelectedAllies = new List<CharacterData>();
        
        // Danh sách kẻ địch của quái vật vừa chạm
        public static List<CharacterData> SelectedEnemies = new List<CharacterData>();
        
        // Định danh duy nhất của quái vật ở Overworld để xóa đi nếu thắng trận
        public static string MonsterToDestroyId = "";
        
        // Cờ kiểm tra xem có phải bắt đầu trận đấu từ Overworld hay không
        public static bool IsEnteringFromOverworld = false;
        
        // Kết quả của trận đấu vừa rồi
        public static CombatResultType CombatResult = CombatResultType.NONE;

        public static void Clear()
        {
            SelectedAllies.Clear();
            SelectedEnemies.Clear();
            MonsterToDestroyId = "";
            IsEnteringFromOverworld = false;
            CombatResult = CombatResultType.NONE;
        }
    }
}
