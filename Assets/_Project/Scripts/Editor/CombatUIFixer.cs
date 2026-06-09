using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace RPG.Combat
{
    public static class CombatUIFixer
    {
        [MenuItem("Tools/RPG/Integrate Recollection Banner to Prefab")]
        public static void FixCombatUIPrefab()
        {
            string prefabPath = "Assets/_Project/Resources/Prefabs/CombatUI.prefab";
            
            // Load nội dung prefab ngầm không qua Scene
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning("[CombatUIFixer] Không tìm thấy file Prefab CombatUI tại: " + prefabPath);
                return;
            }

            CombatUIReferences refs = prefabRoot.GetComponent<CombatUIReferences>();
            if (refs == null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            // Kiểm tra xem đã có RecollectionBannerUI chưa
            Transform existingBanner = prefabRoot.transform.Find("RecollectionBannerUI");
            if (existingBanner != null)
            {
                // Đã có rồi, chỉ gán lại reference nếu thiếu
                if (refs.recollectionBannerPanel == null)
                {
                    refs.recollectionBannerPanel = existingBanner;
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    Debug.Log("[CombatUIFixer] Đã cập nhật tham chiếu recollectionBannerPanel cho Prefab.");
                }
                else
                {
                    Debug.Log("[CombatUIFixer] Prefab đã tích hợp sẵn Recollection Banner.");
                }
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            Debug.Log("[CombatUIFixer] Bắt đầu tích hợp Recollection Banner vào Prefab CombatUI...");

            // 1. Tạo RecollectionBannerUI làm con của Canvas chính
            GameObject bannerGO = new GameObject("RecollectionBannerUI");
            bannerGO.transform.SetParent(prefabRoot.transform, false);

            RectTransform bannerRect = bannerGO.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0f, 0.5f);
            bannerRect.anchorMax = new Vector2(1f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerRect.sizeDelta = new Vector2(0f, 150f);
            bannerRect.localScale = new Vector3(1f, 0f, 1f); // Mặc định thu hẹp Y

            CanvasGroup group = bannerGO.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Image bgImage = bannerGO.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.07f, 0.85f); // Đen mờ sang trọng

            // 2. Tạo viền Top
            GameObject borderTop = new GameObject("BorderTop");
            borderTop.transform.SetParent(bannerGO.transform, false);
            RectTransform btRect = borderTop.AddComponent<RectTransform>();
            btRect.anchorMin = new Vector2(0f, 1f);
            btRect.anchorMax = new Vector2(1f, 1f);
            btRect.pivot = new Vector2(0.5f, 1f);
            btRect.anchoredPosition = Vector2.zero;
            btRect.sizeDelta = new Vector2(0f, 3f);
            Image btImg = borderTop.AddComponent<Image>();
            btImg.color = Color.white;

            // 3. Tạo viền Bottom
            GameObject borderBottom = new GameObject("BorderBottom");
            borderBottom.transform.SetParent(bannerGO.transform, false);
            RectTransform bbRect = borderBottom.AddComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0f, 0f);
            bbRect.anchorMax = new Vector2(1f, 0f);
            bbRect.pivot = new Vector2(0.5f, 0f);
            bbRect.anchoredPosition = Vector2.zero;
            bbRect.sizeDelta = new Vector2(0f, 3f);
            Image bbImg = borderBottom.AddComponent<Image>();
            bbImg.color = Color.white;

            // 4. Tạo TitleText (RECOLLECTION)
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(bannerGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 15f); // Lệch lên trên một chút
            titleRect.sizeDelta = new Vector2(0f, 60f);

            Text titleTxt = titleGO.AddComponent<Text>();
            titleTxt.text = "RECOLLECTION";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 42;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = Color.white;
            titleTxt.fontStyle = FontStyle.Bold;

            Outline titleOutline = titleGO.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            titleOutline.effectDistance = new Vector2(2f, -2f);

            // 5. Tạo SubTitleText
            GameObject subTitleGO = new GameObject("SubTitleText");
            subTitleGO.transform.SetParent(bannerGO.transform, false);
            RectTransform subTitleRect = subTitleGO.AddComponent<RectTransform>();
            subTitleRect.anchorMin = new Vector2(0f, 0.5f);
            subTitleRect.anchorMax = new Vector2(1f, 0.5f);
            subTitleRect.pivot = new Vector2(0.5f, 0.5f);
            subTitleRect.anchoredPosition = new Vector2(0f, -25f); // Lệch xuống dưới một chút
            subTitleRect.sizeDelta = new Vector2(0f, 30f);

            Text subTitleTxt = subTitleGO.AddComponent<Text>();
            subTitleTxt.text = "— COMMANDER AWAKENING —";
            subTitleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTitleTxt.fontSize = 14;
            subTitleTxt.alignment = TextAnchor.MiddleCenter;
            subTitleTxt.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            subTitleTxt.fontStyle = FontStyle.Normal;

            Outline subOutline = subTitleGO.AddComponent<Outline>();
            subOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            subOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Gán reference vào script refs của Prefab
            refs.recollectionBannerPanel = bannerGO.transform;

            // Tắt hoạt động của banner này mặc định (chỉ mở lên khi chạy hoạt ảnh)
            bannerGO.SetActive(false);

            // Lưu thay đổi lại Prefab và đóng ngầm
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("[CombatUIFixer] Đã tích hợp thành công Recollection Banner vào Prefab CombatUI!");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
