using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.Combat
{
    public class FloatingText : MonoBehaviour
    {
        public static FloatingText Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Tạo một nhãn text nổi bay lên và biến mất dần tại vị trí chỉ định.
        /// </summary>
        public void SpawnText(Vector3 position, string text, Color color, float scale = 1.0f)
        {
            GameObject textGO = new GameObject("FloatingText_Instance");
            textGO.transform.position = position;

            // Thiết lập Canvas thế giới
            Canvas canvas = textGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = textGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 80f);
            rect.localScale = new Vector3(0.025f, 0.025f, 0.025f); // Expanded scale for high visibility
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;

            // Thêm Billboard để chữ luôn hướng về camera
            textGO.AddComponent<BillboardHUD>();

            // Tạo component Text
            GameObject subTextGO = new GameObject("Text");
            subTextGO.transform.SetParent(textGO.transform);
            
            RectTransform txtRect = subTextGO.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            txtRect.localPosition = Vector3.zero;
            txtRect.localScale = Vector3.one;
            txtRect.localRotation = Quaternion.identity;

            Text uiText = subTextGO.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = (int)(48 * scale); // Increased font size for dramatic impact
            uiText.fontStyle = FontStyle.Bold;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = color;

            // Thêm hiệu ứng Outline viền đen cho chữ rõ ràng hơn
            Outline outline = subTextGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3f, -3f);

            // Chạy Coroutine bay lên và mờ dần
            StartCoroutine(CoAnimateFloatingText(textGO, uiText, outline));
        }

        private IEnumerator CoAnimateFloatingText(GameObject go, Text txt, Outline outline)
        {
            float duration = 1.2f;
            float elapsed = 0f;
            Vector3 startPos = go.transform.position;
            Color baseColor = txt.color;
            Color outlineColor = outline.effectColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Di chuyển chữ bay lên (lướt lên 1m)
                go.transform.position = new Vector3(startPos.x, startPos.y + (t * 1.5f), startPos.z);

                // Mờ dần alpha
                float alpha = Mathf.Lerp(1f, 0f, t);
                txt.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                outline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha);

                yield return null;
            }

            Destroy(go);
        }
    }
}
