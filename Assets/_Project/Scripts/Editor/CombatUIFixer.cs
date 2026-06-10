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

            bool isModified = false;

            // 0. CẬP NHẬT TURN QUEUE PANEL THÀNH DẠNG DỌC BÊN TRÁI
            if (refs.turnQueuePanel != null)
            {
                RectTransform turnRect = refs.turnQueuePanel.GetComponent<RectTransform>();
                if (turnRect != null)
                {
                    turnRect.anchorMin = new Vector2(0f, 1f);
                    turnRect.anchorMax = new Vector2(0f, 1f);
                    turnRect.pivot = new Vector2(0f, 1f);
                    turnRect.anchoredPosition = new Vector2(20f, -20f);
                    turnRect.sizeDelta = new Vector2(90f, 450f);

                    // Thay HorizontalLayoutGroup bằng VerticalLayoutGroup
                    HorizontalLayoutGroup hLayout = refs.turnQueuePanel.GetComponent<HorizontalLayoutGroup>();
                    if (hLayout != null)
                    {
                        Object.DestroyImmediate(hLayout, true);
                    }

                    VerticalLayoutGroup vLayout = refs.turnQueuePanel.GetComponent<VerticalLayoutGroup>();
                    if (vLayout == null)
                    {
                        vLayout = refs.turnQueuePanel.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    vLayout.childAlignment = TextAnchor.UpperCenter;
                    vLayout.spacing = 8f;
                    vLayout.childControlHeight = false;
                    vLayout.childControlWidth = false;
                    vLayout.childForceExpandHeight = false;
                    vLayout.childForceExpandWidth = false;

                    isModified = true;
                    Debug.Log("[CombatUIFixer] Đã chuyển đổi TurnQueuePanel sang hàng dọc bên trái.");
                }
            }

            // 1. TÍCH HỢP RECOLLECTION BANNER
            Transform existingBanner = prefabRoot.transform.Find("RecollectionBannerUI");
            if (existingBanner != null)
            {
                if (refs.recollectionBannerPanel == null)
                {
                    refs.recollectionBannerPanel = existingBanner;
                    isModified = true;
                    Debug.Log("[CombatUIFixer] Đã cập nhật tham chiếu recollectionBannerPanel cho Prefab.");
                }
            }
            else
            {
                Debug.Log("[CombatUIFixer] Bắt đầu tích hợp Recollection Banner vào Prefab CombatUI...");
                
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

                refs.recollectionBannerPanel = bannerGO.transform;
                bannerGO.SetActive(false);
                isModified = true;
                Debug.Log("[CombatUIFixer] Đã tích hợp thành công Recollection Banner vào Prefab.");
            }

            // 2. TÍCH HỢP ENEMY TARGET HUD
            // Để đảm bảo thiết kế mới được tích hợp sạch sẽ, ta chủ động xóa EnemyTargetHUDPanel cũ
            Transform oldHUD = prefabRoot.transform.Find("EnemyTargetHUDPanel");
            if (oldHUD != null)
            {
                Object.DestroyImmediate(oldHUD.gameObject, true);
                refs.enemyTargetHUDPanel = null;
                refs.enemyTargetCardTemplate = null;
            }

            Debug.Log("[CombatUIFixer] Bắt đầu tích hợp Enemy Target HUD đa mục tiêu mới...");
            
            GameObject hudGO = new GameObject("EnemyTargetHUDPanel");
            hudGO.transform.SetParent(prefabRoot.transform, false);

            RectTransform hudRect = hudGO.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(1f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(1f, 1f);
            hudRect.anchoredPosition = new Vector2(-20f, -20f);
            hudRect.sizeDelta = new Vector2(550f, 100f);
            hudRect.localScale = Vector3.one;

            HorizontalLayoutGroup containerLayout = hudGO.AddComponent<HorizontalLayoutGroup>();
            containerLayout.childAlignment = TextAnchor.UpperRight;
            containerLayout.spacing = 8f;
            containerLayout.childControlHeight = false;
            containerLayout.childControlWidth = false;
            containerLayout.childForceExpandHeight = false;
            containerLayout.childForceExpandWidth = false;

            // Tạo Template thẻ HUD của từng quái vật (Kích thước: 120 x 85)
            GameObject templateGO = new GameObject("EnemyTargetCardTemplate");
            templateGO.transform.SetParent(hudGO.transform, false);

            RectTransform templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.sizeDelta = new Vector2(120f, 85f);
            templateRect.localScale = Vector3.one;

            Image cardBg = templateGO.AddComponent<Image>();
            cardBg.color = new Color(0.05f, 0.05f, 0.07f, 0.9f);

            Outline cardOutline = templateGO.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.7f, 0.1f, 0.1f, 0.8f);
            cardOutline.effectDistance = new Vector2(1f, -1f);

            VerticalLayoutGroup cardLayout = templateGO.AddComponent<VerticalLayoutGroup>();
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.padding = new RectOffset(6, 6, 5, 5);
            cardLayout.spacing = 3f;
            cardLayout.childControlHeight = false;
            cardLayout.childControlWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = false;

            // 2.1 NameText
            GameObject nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(templateGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(108f, 14f);
            Text nameTxt = nameGO.AddComponent<Text>();
            nameTxt.text = "Enemy Name";
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize = 9;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.color = Color.white;

            // 2.2 HPBar_Bg
            GameObject hpBgGO = new GameObject("HPBar_Bg");
            hpBgGO.transform.SetParent(templateGO.transform, false);
            RectTransform hpBgRect = hpBgGO.AddComponent<RectTransform>();
            hpBgRect.sizeDelta = new Vector2(108f, 10f);
            Image hpBgImg = hpBgGO.AddComponent<Image>();
            hpBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // HPBar_Fill
            GameObject hpFillGO = new GameObject("HPBar_Fill");
            hpFillGO.transform.SetParent(hpBgGO.transform, false);
            RectTransform hpFillRect = hpFillGO.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            Image hpFillImg = hpFillGO.AddComponent<Image>();
            hpFillImg.color = new Color(0.9f, 0.1f, 0.1f);

            // HPText
            GameObject hpTextGO = new GameObject("HPText");
            hpTextGO.transform.SetParent(hpBgGO.transform, false);
            RectTransform hpTextRect = hpTextGO.AddComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;
            Text hpTxt = hpTextGO.AddComponent<Text>();
            hpTxt.text = "HP: 100/100";
            hpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpTxt.fontSize = 7;
            hpTxt.alignment = TextAnchor.MiddleCenter;
            hpTxt.color = Color.white;

            Outline hpOutline = hpTextGO.AddComponent<Outline>();
            hpOutline.effectColor = Color.black;
            hpOutline.effectDistance = new Vector2(1f, -1f);

            // 2.3 MPBar_Bg (Mana/Energy)
            GameObject mpBgGO = new GameObject("MPBar_Bg");
            mpBgGO.transform.SetParent(templateGO.transform, false);
            RectTransform mpBgRect = mpBgGO.AddComponent<RectTransform>();
            mpBgRect.sizeDelta = new Vector2(108f, 10f);
            Image mpBgImg = mpBgGO.AddComponent<Image>();
            mpBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // MPBar_Fill
            GameObject mpFillGO = new GameObject("MPBar_Fill");
            mpFillGO.transform.SetParent(mpBgGO.transform, false);
            RectTransform mpFillRect = mpFillGO.AddComponent<RectTransform>();
            mpFillRect.anchorMin = Vector2.zero;
            mpFillRect.anchorMax = Vector2.one;
            mpFillRect.offsetMin = Vector2.zero;
            mpFillRect.offsetMax = Vector2.zero;
            Image mpFillImg = mpFillGO.AddComponent<Image>();
            mpFillImg.color = new Color(0.1f, 0.5f, 0.9f); // Màu xanh dương mana

            // MPText
            GameObject mpTextGO = new GameObject("MPText");
            mpTextGO.transform.SetParent(mpBgGO.transform, false);
            RectTransform mpTextRect = mpTextGO.AddComponent<RectTransform>();
            mpTextRect.anchorMin = Vector2.zero;
            mpTextRect.anchorMax = Vector2.one;
            mpTextRect.offsetMin = Vector2.zero;
            mpTextRect.offsetMax = Vector2.zero;
            Text mpTxt = mpTextGO.AddComponent<Text>();
            mpTxt.text = "MP: 50/100";
            mpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mpTxt.fontSize = 7;
            mpTxt.alignment = TextAnchor.MiddleCenter;
            mpTxt.color = Color.white;

            Outline mpOutline = mpTextGO.AddComponent<Outline>();
            mpOutline.effectColor = Color.black;
            mpOutline.effectDistance = new Vector2(1f, -1f);

            // 2.4 BuffContainer
            GameObject buffsGO = new GameObject("BuffContainer");
            buffsGO.transform.SetParent(templateGO.transform, false);
            RectTransform buffsRect = buffsGO.AddComponent<RectTransform>();
            buffsRect.sizeDelta = new Vector2(108f, 14f);

            HorizontalLayoutGroup buffsLayout = buffsGO.AddComponent<HorizontalLayoutGroup>();
            buffsLayout.childAlignment = TextAnchor.MiddleLeft;
            buffsLayout.spacing = 2f;
            buffsLayout.childControlHeight = false;
            buffsLayout.childControlWidth = false;

            refs.enemyTargetHUDPanel = hudGO.transform;
            refs.enemyTargetCardTemplate = templateGO;
            
            templateGO.SetActive(false);
            hudGO.SetActive(false);
            
            isModified = true;
            Debug.Log("[CombatUIFixer] Đã tích hợp thành công Enemy Target HUD đa mục tiêu mới.");

            if (isModified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log("[CombatUIFixer] Đã lưu thành công các thay đổi của Prefab!");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
