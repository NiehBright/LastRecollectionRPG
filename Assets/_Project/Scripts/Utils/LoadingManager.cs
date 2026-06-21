using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RPG.Combat
{
    public class LoadingManager : MonoBehaviour
    {
        private static LoadingManager instance;

        public static LoadingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("LoadingManager");
                    instance = go.AddComponent<LoadingManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Canvas loadingCanvas;
        private CanvasGroup canvasGroup;
        private Image progressBarFill;
        private Text progressText;
        private Text tipText;
        private Image spinnerImage;
        private bool isTransitioning = false;

        private static Sprite cachedSpinnerSprite;
        private static Sprite cachedTrackSprite;

        private string[] gameplayTips = new string[]
        {
            "Mẹo: Kích hoạt Recollection của Chỉ Huy đúng lúc để tăng 1.5x đến 2x sát thương cho cả đội!",
            "Mẹo: Khi thanh Nộ dùng chung đạt 100%, hãy bấm để kích hoạt Ultimate của đồng minh và cắt lượt đi của đối thủ!",
            "Mẹo: Parry QTE thành công giúp chặn đứng 100% sát thương từ kẻ địch và lập tức phản công chớp nhoáng!",
            "Mẹo: Thất bại khi Parry QTE sẽ khiến bạn phải chịu thêm 1.5x hoặc 2.0x (nếu khắc hệ) sát thương!",
            "Mẹo: Hãy chú ý hệ nguyên tố Ether khắc chế tất cả các hệ nguyên tố khác và không bị khắc chế ngược!",
            "Mẹo: Tiêu diệt kẻ địch bằng đòn bạo kích (Crit Kill) giúp tích lũy thêm 18% thanh năng lượng Recollection!",
            "Mẹo: Đồng minh hàng trước nếu phòng thủ sẽ được hồi thêm 10% năng lượng chiêu cuối.",
            "Mẹo: Một số kỹ năng có cự ly đánh xa (Ranged) sẽ không cần chạy đến vị trí mục tiêu.",
            "Mẹo: Bạn có thể click chuột trái trực tiếp lên quái vật để xem nhanh thông tin HP, MP và hiệu ứng của chúng."
        };

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (isTransitioning && spinnerImage != null)
            {
                spinnerImage.rectTransform.Rotate(0f, 0f, -250f * Time.unscaledDeltaTime);
            }
        }

        /// <summary>
        /// Chuyển cảnh mượt mà kèm theo màn hình loading.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isTransitioning) return;
            StartCoroutine(CoLoadScene(sceneName));
        }

        private IEnumerator CoLoadScene(string sceneName)
        {
            isTransitioning = true;

            // Khôi phục Time.timeScale về 1 để tránh lỗi chuyển cảnh khi thời gian đang dừng
            Time.timeScale = 1.0f;

            // Tạo UI Loading nếu chưa có
            if (loadingCanvas == null)
            {
                CreateLoadingUI();
            }

            // Chọn mẹo ngẫu nhiên
            if (tipText != null && gameplayTips.Length > 0)
            {
                tipText.text = gameplayTips[UnityEngine.Random.Range(0, gameplayTips.Length)];
            }

            if (progressBarFill != null)
            {
                progressBarFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            }
            if (progressText != null)
            {
                progressText.text = "Đang tải: 0%";
            }

            loadingCanvas.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            // 1. Fade In màn hình loading
            float fadeTime = 0.3f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(0.2f); // Tạo độ trễ ngắn cho mượt mà thị giác

            // 2. Tải cảnh bất đồng bộ
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float currentProgress = 0f;
            while (currentProgress < 1.0f)
            {
                // Mặc định AsyncOperation.progress chạy từ 0 đến 0.9 khi load xong (allowSceneActivation = false)
                float targetProgress = Mathf.Clamp01(op.progress / 0.9f);
                
                // Lerp tăng tiến độ mượt mà hơn
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.unscaledDeltaTime * 1.2f);
                
                if (progressBarFill != null)
                {
                    progressBarFill.rectTransform.anchorMax = new Vector2(currentProgress, 1f);
                }
                if (progressText != null)
                {
                    progressText.text = $"Đang tải: {Mathf.RoundToInt(currentProgress * 100f)}%";
                }

                if (currentProgress >= 0.999f && op.progress >= 0.9f)
                {
                    currentProgress = 1.0f;
                    if (progressBarFill != null) progressBarFill.rectTransform.anchorMax = new Vector2(1f, 1f);
                    if (progressText != null) progressText.text = "Hoàn tất! Đang vào game...";
                    break;
                }

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f); // Chờ hiển thị 100% trong chốc lát

            // 3. Kích hoạt cảnh mới
            op.allowSceneActivation = true;
            while (!op.isDone)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // 4. Fade Out màn hình loading
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeTime));
                yield return null;
            }
            canvasGroup.alpha = 0f;
            loadingCanvas.gameObject.SetActive(false);

            isTransitioning = false;
        }

        private void CreateLoadingUI()
        {
            GameObject canvasGO = new GameObject("LoadingUI_Canvas");
            loadingCanvas = canvasGO.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 9999; // Hiển thị đè lên tất cả UI khác

            canvasGO.AddComponent<CanvasScaler>();
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            DontDestroyOnLoad(canvasGO);

            // Panel nền đen huyền ảo / Gradient
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.04f, 0.06f, 1f); // Dark violet huyền bí

            // Centered Spinner (Vòng xoay loading)
            GameObject spinnerGO = new GameObject("Spinner");
            spinnerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform spinnerRect = spinnerGO.AddComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRect.pivot = new Vector2(0.5f, 0.5f);
            spinnerRect.anchoredPosition = new Vector2(0f, 50f);
            spinnerRect.sizeDelta = new Vector2(80f, 80f);

            spinnerImage = spinnerGO.AddComponent<Image>();
            spinnerImage.sprite = GetDefaultSpinnerSprite();
            spinnerImage.color = new Color(0.65f, 0.25f, 0.95f); // Cosmic Purple

            // Text tiêu đề ở trung tâm
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(canvasGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, -30f);
            titleRect.sizeDelta = new Vector2(400f, 40f);

            Text titleText = titleGO.AddComponent<Text>();
            titleText.text = "LAST RECOLLECTION";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            Outline titleOutline = titleGO.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            titleOutline.effectDistance = new Vector2(2f, -2f);

            // Container chứa Progress Bar
            GameObject barContainerGO = new GameObject("ProgressBarContainer");
            barContainerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform barContainerRect = barContainerGO.AddComponent<RectTransform>();
            barContainerRect.anchorMin = new Vector2(0.5f, 0.35f);
            barContainerRect.anchorMax = new Vector2(0.5f, 0.35f);
            barContainerRect.pivot = new Vector2(0.5f, 0.5f);
            barContainerRect.anchoredPosition = Vector2.zero;
            barContainerRect.sizeDelta = new Vector2(600f, 10f);

            Image trackImg = barContainerGO.AddComponent<Image>();
            trackImg.sprite = GetDefaultTrackSprite();
            trackImg.color = new Color(0.12f, 0.12f, 0.18f, 1f); // Nền xám đen

            // Fill của Progress Bar
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barContainerGO.transform, false);
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f); // Tiến độ ban đầu là 0
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 0f); // Auto stretching

            progressBarFill = fillGO.AddComponent<Image>();
            progressBarFill.sprite = GetDefaultTrackSprite();
            progressBarFill.color = new Color(0.15f, 0.85f, 0.95f); // Neon Cyan

            // Glow effect dưới thanh Progress Bar
            GameObject glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(fillRect.transform, false);
            glowGO.transform.SetAsFirstSibling();
            RectTransform glowRect = glowGO.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.sizeDelta = new Vector2(10f, 10f); // Phình ra hai bên
            Image glowImg = glowGO.AddComponent<Image>();
            glowImg.sprite = GetDefaultTrackSprite();
            glowImg.color = new Color(0.15f, 0.85f, 0.95f, 0.35f);

            // Progress Text (e.g. 50%)
            GameObject progressTextGO = new GameObject("ProgressText");
            progressTextGO.transform.SetParent(canvasGO.transform, false);
            RectTransform progressTextRect = progressTextGO.AddComponent<RectTransform>();
            progressTextRect.anchorMin = new Vector2(0.5f, 0.35f);
            progressTextRect.anchorMax = new Vector2(0.5f, 0.35f);
            progressTextRect.pivot = new Vector2(0.5f, 0.5f);
            progressTextRect.anchoredPosition = new Vector2(0f, 25f);
            progressTextRect.sizeDelta = new Vector2(200f, 30f);

            progressText = progressTextGO.AddComponent<Text>();
            progressText.text = "Đang tải: 0%";
            progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            progressText.fontSize = 15;
            progressText.fontStyle = FontStyle.Bold;
            progressText.alignment = TextAnchor.MiddleCenter;
            progressText.color = Color.white;

            Outline progressOutline = progressTextGO.AddComponent<Outline>();
            progressOutline.effectColor = Color.black;
            progressOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Bảng hiện gameplay tips ở dưới cùng
            GameObject tipPanelGO = new GameObject("TipPanel");
            tipPanelGO.transform.SetParent(canvasGO.transform, false);
            RectTransform tipPanelRect = tipPanelGO.AddComponent<RectTransform>();
            tipPanelRect.anchorMin = new Vector2(0.5f, 0.15f);
            tipPanelRect.anchorMax = new Vector2(0.5f, 0.15f);
            tipPanelRect.pivot = new Vector2(0.5f, 0.5f);
            tipPanelRect.anchoredPosition = Vector2.zero;
            tipPanelRect.sizeDelta = new Vector2(800f, 60f);

            tipText = tipPanelGO.AddComponent<Text>();
            tipText.text = "";
            tipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tipText.fontSize = 13;
            tipText.fontStyle = FontStyle.Italic;
            tipText.alignment = TextAnchor.MiddleCenter;
            tipText.color = new Color(0.85f, 0.85f, 0.9f, 1f);

            Outline tipOutline = tipPanelGO.AddComponent<Outline>();
            tipOutline.effectColor = Color.black;
            tipOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private Sprite GetDefaultSpinnerSprite()
        {
            if (cachedSpinnerSprite != null) return cachedSpinnerSprite;
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float outerRadius = size / 2f - 4f;
            float innerRadius = outerRadius - 10f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= innerRadius && dist <= outerRadius)
                    {
                        // Tạo hiệu ứng quét mờ dần (gradient alpha sweep)
                        float angle = Mathf.Atan2(dy, dx); // range -PI to PI
                        float t = (angle + Mathf.PI) / (2f * Mathf.PI); // map to 0..1
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, t));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            cachedSpinnerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedSpinnerSprite;
        }

        private Sprite GetDefaultTrackSprite()
        {
            if (cachedTrackSprite != null) return cachedTrackSprite;
            int width = 32;
            int height = 32;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Tạo sprite hình hộp bo góc tròn nhẹ
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();
            cachedTrackSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            return cachedTrackSprite;
        }
    }
}
