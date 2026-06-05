using UnityEngine;
using UnityEngine.UI;

namespace RPG.Combat
{
    public class CombatUIReferences : MonoBehaviour
    {
        [Header("UI Panels")]
        public Transform turnQueuePanel;
        public Transform partyPanel;
        public Transform actionPanel;
        public Transform descriptionPanel;
        public Transform targetSelectionPanel;
        public Transform endScreenPanel;

        [Header("Texts")]
        public Text descriptionText;
        public Text endScreenText;

        [Header("Action Buttons")]
        public Button basicButton;
        public Button specialButton;
        public Button ultimateButton;
        public Button defendButton;

        [Header("Button Labels")]
        public Text basicText;
        public Text specialText;
        public Text ultimateText;

        [Header("End Screen Buttons")]
        public Button restartButton;
    }
}
