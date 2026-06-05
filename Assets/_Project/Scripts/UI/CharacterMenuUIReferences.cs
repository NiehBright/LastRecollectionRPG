using UnityEngine;
using UnityEngine.UI;

namespace RPG.Combat
{
    public class CharacterMenuUIReferences : MonoBehaviour
    {
        [Header("UI Panels")]
        public Transform characterListParent; // ScrollRect content hoặc Panel chứa thẻ nhân vật bên trái
        public Transform statsPanel;          // Bảng chỉ số bên phải
        public Transform skillListParent;     // Nơi chứa các kỹ năng bên phải

        [Header("Text Fields")]
        public Text characterNameText;
        public Text characterElementText;
        public Text characterStatsText;       // Chứa text chỉ số HP, ATK, DEF, Speed, Crit...

        [Header("Model Preview")]
        public RawImage modelRenderImage;     // RawImage hiển thị Render Texture của Showroom

        [Header("Buttons")]
        public Button closeButton;
    }
}
