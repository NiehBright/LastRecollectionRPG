using UnityEngine;
using UnityEngine.UI;

namespace RPG.Combat
{
    public class CombatTeamSelectionUIReferences : MonoBehaviour
    {
        [Header("Showroom References")]
        public RawImage showroomRawImage;

        [Header("Slot Information Panels")]
        public Text[] slotNameTexts = new Text[4];
        public Text[] slotElementTexts = new Text[4];
        public Button[] slotButtons = new Button[4];

        [Header("Popup Selection List Panels")]
        public GameObject characterListPanel;
        public Transform listContentContainer;

        [Header("Control Buttons")]
        public Button btnStart;
        public Button btnCancel;
    }
}
