using UnityEngine;
using UnityEngine.EventSystems;

namespace RPG.Combat
{
    /// <summary>
    /// Cho phép người chơi nhấn giữ chuột và kéo trên vùng UI hiển thị để xoay model 3D của nhân vật.
    /// </summary>
    public class ShowroomRotator : MonoBehaviour, IDragHandler
    {
        [Tooltip("Model 3D trong Showroom cần xoay.")]
        public Transform targetModel;

        [Tooltip("Tốc độ xoay model.")]
        public float rotationSpeed = 0.4f;

        public void OnDrag(PointerEventData eventData)
        {
            if (targetModel != null)
            {
                // Xoay model quanh trục Y (trục thẳng đứng) dựa trên di chuyển chuột theo phương ngang delta.x
                targetModel.Rotate(Vector3.up, -eventData.delta.x * rotationSpeed, Space.World);
            }
        }
    }
}
