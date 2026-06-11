using UnityEngine;
using UnityEngine.EventSystems;

namespace RPG.Combat
{
    /// <summary>
    /// Component gắn vào các icon buff để phát hiện sự kiện rê chuột và gửi yêu cầu hiển thị Tooltip lên UIManager.
    /// </summary>
    public class BuffTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
    {
        private ActiveEffect activeEffect;

        /// <summary>
        /// Khởi tạo dữ liệu hiệu ứng cho trigger này.
        /// </summary>
        public void Initialize(ActiveEffect effect)
        {
            activeEffect = effect;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            ShowTooltip(eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ShowTooltip(eventData.position);
        }

        private void ShowTooltip(Vector2 screenPos)
        {
            if (UIManager.Instance != null && activeEffect != null && activeEffect.data != null)
            {
                string desc = $"{activeEffect.data.description}\n<color=yellow>Thời gian: {activeEffect.turnsRemaining} lượt</color>";
                UIManager.Instance.ShowTooltip(activeEffect.data.effectName, desc, activeEffect.data.effectColor, screenPos);
            }
        }

        private void HideTooltip()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideTooltip();
            }
        }

        private void OnDisable()
        {
            // Tránh việc Tooltip bị kẹt nếu icon bị hủy hoặc tắt đột ngột
            HideTooltip();
        }

        private void OnDestroy()
        {
            HideTooltip();
        }
    }
}
