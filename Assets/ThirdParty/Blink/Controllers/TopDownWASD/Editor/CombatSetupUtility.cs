using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.UI;
using TMPro;

// Alias to avoid conflict with Suriyun's global AnimatorController class
using EditorAnimatorController = UnityEditor.Animations.AnimatorController;

namespace Blink.Controllers.TopDownWASD.Editor
{
    // Editor utility to create prefabs and an animator controller to test the combat system.
    // Run from menu: BLINK -> Combat -> Setup Combat Assets
    public static class CombatSetupUtility
    {
    [MenuItem("BLINK/Combat/Setup Combat Assets")]
    public static void SetupCombatAssets()
    {
        string baseFolder = "Assets/Resources/Blink/Controllers/TopDownWASD/Prefabs";
        EnsureFolderExists("Assets/Resources/Blink/Controllers/TopDownWASD/Prefabs");

        AddTagIfMissing("Enemy");

        // --- Create Enemy prefab ---
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = "EnemyDummy";
        enemy.tag = "Enemy";

        // add runtime component (namespace BLINK.Controller)
        var enemyComp = enemy.AddComponent<BLINK.Controller.EnemyDummy>();
        enemyComp.maxHealth = 1000f;

        // create world-space healthbar under enemy
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(enemy.transform, false);
        canvasGO.transform.localPosition = Vector3.up * 1.6f;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        var rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 12);
        canvasGO.transform.localScale = Vector3.one * 0.01f;

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.black;
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.red;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.sizeDelta = Vector2.zero;

        // assign fill to EnemyDummy
        enemyComp.healthFillImage = fillImg;

        string enemyPath = baseFolder + "/EnemyDummy.prefab";
        PrefabUtility.SaveAsPrefabAsset(enemy, enemyPath, out bool successEnemy);
        GameObject.DestroyImmediate(enemy);

        // --- Find existing Animator Controller (preferred) or create one ---
        string controllerPath = "Assets/Resources/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller";
        EditorAnimatorController ac = AssetDatabase.LoadAssetAtPath<EditorAnimatorController>(controllerPath);
        string usedControllerPath = controllerPath;

        if (ac == null)
        {
            // fallback: try non-Resources path
            controllerPath = "Assets/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller";
            ac = AssetDatabase.LoadAssetAtPath<EditorAnimatorController>(controllerPath);
            usedControllerPath = controllerPath;
        }
        if (ac == null)
        {
            // final fallback: create a controller in Prefabs folder
            usedControllerPath = baseFolder + "/TopDownWASDAnimator.controller";
            ac = EditorAnimatorController.CreateAnimatorControllerAtPath(usedControllerPath);
        }

        // Add 4 trigger parameters (Attack1..Attack4)
        for (int i = 1; i <= 4; i++)
        {
            string pname = "Attack" + i;
            if (!HasParameter(ac, pname))
                ac.AddParameter(pname, AnimatorControllerParameterType.Trigger);
        }

        // ─────────── COMBAT LAYER SETUP ───────────
        AnimatorControllerLayer combatLayer = null;
        bool combatLayerExists = false;
        int combatLayerIndex = -1;
        
        for (int i = 0; i < ac.layers.Length; i++)
        {
            if (ac.layers[i].name == "Combat")
            {
                combatLayer = ac.layers[i];
                combatLayerExists = true;
                combatLayerIndex = i;
                break;
            }
        }
        
        if (!combatLayerExists)
        {
            ac.AddLayer("Combat");
            combatLayerIndex = ac.layers.Length - 1;
            combatLayer = ac.layers[combatLayerIndex];
        }

        // Set Combat layer default weight to 0 (controlled by CombatController at runtime)
        {
            var layers = ac.layers;
            layers[combatLayerIndex].defaultWeight = 0f;
            layers[combatLayerIndex].blendingMode = AnimatorLayerBlendingMode.Override;
            ac.layers = layers;
            combatLayer = ac.layers[combatLayerIndex];
        }

        var rootSm = combatLayer.stateMachine;
        
        // ─── Create/find Idle state ───
        AnimatorState idleState = FindStateByName(rootSm, "Idle");
        if (idleState == null)
        {
            var idleChild = rootSm.AddState("Idle");
            idleState = GetAnimatorStateFromChild(idleChild);
        }

        // ─── Create attack states with RPG-standard transitions ───
        AnimatorState[] attackStates = new AnimatorState[4];
        
        for (int i = 0; i < 4; i++)
        {
            string stateName = "Attack" + (i + 1);
            
            // Try to load the actual combo02 animation clip from FBX
            string fbxPath = $"Assets/Resources/Sword_sheath_AnimSet/Animation/Humanoid/Inplace/combo02_{i + 1}_inplace.fbx";
            AnimationClip clip = LoadClipFromFBX(fbxPath, $"combo02_{i + 1}");

            if (clip == null)
            {
                // Fallback: Create or load placeholder animation clip
                clip = new AnimationClip();
                clip.name = stateName;
                var curve = AnimationCurve.EaseInOut(0f, 0f, 0.35f, 0.12f);
                clip.SetCurve("", typeof(Transform), "localPosition.z", curve);
                string clipPath = baseFolder + "/" + stateName + ".anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) == null)
                    AssetDatabase.CreateAsset(clip, clipPath);
                else
                    clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            }

            EnsureHitAnimationEvent(clip, i);

            // Create state if not present
            AnimatorState attackState = FindStateByName(rootSm, stateName);
            if (attackState == null)
            {
                var child = rootSm.AddState(stateName);
                attackState = GetAnimatorStateFromChild(child);
            }
            if (attackState != null)
                attackState.motion = clip;

            attackStates[i] = attackState;
        }

        // ─── CLEAR all old transitions to rebuild them cleanly ───
        ClearTransitions(idleState);
        for (int i = 0; i < 4; i++)
        {
            if (attackStates[i] != null)
                ClearTransitions(attackStates[i]);
        }

        // ─── BUILD RPG-STANDARD TRANSITIONS ───
        // 
        // Pattern for RPG combo:
        //   Idle ──(Attack1 trigger)──> Attack1
        //   Attack1 ──(Attack2 trigger, no exit time)──> Attack2   (combo window)
        //   Attack1 ──(exit time 0.85, duration 0.15)──> Idle      (no combo → return)
        //   ... etc
        //   Attack4 ──(exit time 0.85, duration 0.15)──> Idle      (last hit always returns)
        //
        // Key design decisions:
        //   - Combo transitions: hasExitTime=false, transitionDuration=0.08 (snappy blend)
        //   - Return to Idle:    hasExitTime=true,  exitTime=0.85, transitionDuration=0.15

        // Idle → Attack1
        {
            var t = idleState.AddTransition(attackStates[0]);
            t.AddCondition(AnimatorConditionMode.If, 0, "Attack1");
            t.hasExitTime = false;
            t.duration = 0.05f;           // very fast entry into first attack
            t.offset = 0f;
            t.interruptionSource = TransitionInterruptionSource.None;
            t.canTransitionToSelf = false;
        }

        for (int i = 0; i < 4; i++)
        {
            var state = attackStates[i];
            if (state == null) continue;

            // Attack[i] → Attack[i+1] (combo continuation)
            if (i < 3 && attackStates[i + 1] != null)
            {
                var comboTrans = state.AddTransition(attackStates[i + 1]);
                comboTrans.AddCondition(AnimatorConditionMode.If, 0, "Attack" + (i + 2));
                comboTrans.hasExitTime = false;
                comboTrans.duration = 0.08f;            // snappy combo blend
                comboTrans.offset = 0f;
                comboTrans.interruptionSource = TransitionInterruptionSource.Source;
                comboTrans.canTransitionToSelf = false;
            }

            // Attack[i] → Idle (return when combo ends / no input)
            {
                var returnTrans = state.AddTransition(idleState);
                returnTrans.hasExitTime = true;
                returnTrans.exitTime = 0.85f;           // wait until 85% of animation
                returnTrans.duration = 0.15f;            // smooth blend back to idle
                returnTrans.offset = 0f;
                returnTrans.interruptionSource = TransitionInterruptionSource.Source;
                returnTrans.canTransitionToSelf = false;
                // No conditions → this fires purely on exit time
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- Create DamagePopup prefab ---
        GameObject popupGO = new GameObject("DamagePopupPrefab");
        var popupComp = popupGO.AddComponent<BLINK.Controller.DamagePopup>();
        GameObject textGO = new GameObject("TMP");
        textGO.transform.SetParent(popupGO.transform, false);
        var tm = textGO.AddComponent<TextMeshPro>();
        tm.text = "DAMAGE";
        tm.fontSize = 4;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = Color.yellow;
        popupComp.textComponent = tm;
        string popupPath = baseFolder + "/DamagePopup.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(popupPath) == null)
        {
            PrefabUtility.SaveAsPrefabAsset(popupGO, popupPath);
        }
        GameObject.DestroyImmediate(popupGO);

        // --- Create HitEffect prefab ---
        GameObject hitFxGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitFxGO.name = "HitEffect";
        var col = hitFxGO.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        hitFxGO.transform.localScale = Vector3.one * 0.2f;
        hitFxGO.AddComponent<BLINK.Controller.HitEffectAutoDestroy>();
        string hitFxPath = baseFolder + "/HitEffect.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(hitFxPath) == null)
        {
            PrefabUtility.SaveAsPrefabAsset(hitFxGO, hitFxPath);
        }
        Object.DestroyImmediate(hitFxGO);

        // Save the animator controller to ensure changes are persisted
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // If the user has a GameObject selected in the Editor, try to assign the created controller and popup prefab
        var selected = Selection.activeGameObject;
        if (selected != null)
        {
            var selectedAnimator = selected.GetComponent<Animator>();
            if (selectedAnimator != null)
            {
                AssetDatabase.ImportAsset(usedControllerPath);
                var controllerAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(usedControllerPath);
                if (controllerAsset != null)
                {
                    selectedAnimator.runtimeAnimatorController = controllerAsset;
                    EditorUtility.SetDirty(selectedAnimator);
                }
            }

            var combat = selected.GetComponent<BLINK.Controller.CombatController>();
            if (combat != null)
            {
                var popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(popupPath);
                if (popupPrefab != null)
                {
                    combat.damagePopupPrefab = popupPrefab;
                    EditorUtility.SetDirty(combat);
                }
                var hitFxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hitFxPath);
                if (hitFxPrefab != null)
                {
                    combat.hitEffectPrefab = hitFxPrefab;
                    EditorUtility.SetDirty(combat);
                }
            }
        }

        EditorUtility.DisplayDialog("Combat Setup", 
            "Combat system configured!\n\n" +
            "Combat layer transitions:\n" +
            "  Idle -> Attack1 (trigger, 0.05s blend)\n" +
            "  Attack1 -> Attack2 -> Attack3 -> Attack4 (trigger, 0.08s blend)\n" +
            "  Each Attack -> Idle (exit time 85%, 0.15s blend)\n\n" +
            "Prefabs in: " + baseFolder, "OK");
    }

    // Helper method to change animation clips for attack states
    [MenuItem("BLINK/Combat/Update Attack Animations")]
    public static void UpdateAttackAnimations()
    {
        // Try multiple paths
        string[] possiblePaths = {
            "Assets/Resources/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller",
            "Assets/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller"
        };

        EditorAnimatorController ac = null;
        foreach (var path in possiblePaths)
        {
            ac = AssetDatabase.LoadAssetAtPath<EditorAnimatorController>(path);
            if (ac != null) break;
        }

        if (ac == null)
        {
            EditorUtility.DisplayDialog("Error", "Cannot find TopDownWASDAnimator.controller", "OK");
            return;
        }

        // Find Combat layer
        AnimatorControllerLayer combatLayer = null;
        foreach (var layer in ac.layers)
        {
            if (layer.name == "Combat")
            {
                combatLayer = layer;
                break;
            }
        }

        if (combatLayer == null)
        {
            EditorUtility.DisplayDialog("Error", "Combat layer not found in animator controller", "OK");
            return;
        }

        var rootSm = combatLayer.stateMachine;

        // Update attack states with any new animation clips
        for (int i = 1; i <= 4; i++)
        {
            AnimatorState attackState = FindStateByName(rootSm, "Attack" + i);
            if (attackState != null)
            {
                AnimationClip clip = attackState.motion as AnimationClip;
                if (clip != null)
                {
                    EnsureHitAnimationEvent(clip, i - 1);
                    EditorUtility.SetDirty(clip);
                }
            }
        }

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "Attack animations updated!", "OK");
    }

    // ─────────── HELPER METHODS ───────────

    /// <summary>Recursively create folder path if it doesn't exist.</summary>
    static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    static bool HasParameter(EditorAnimatorController ac, string paramName)
    {
        foreach (var p in ac.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    static void ClearTransitions(AnimatorState state)
    {
        if (state == null) return;
        var transitions = state.transitions;
        for (int i = transitions.Length - 1; i >= 0; i--)
        {
            state.RemoveTransition(transitions[i]);
        }
    }

    static void AddTagIfMissing(string tag)
    {
        try
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (allAssets == null || allAssets.Length == 0)
            {
                Debug.LogWarning("Could not find TagManager.asset at ProjectSettings/TagManager.asset");
                return;
            }

            var tagManager = new SerializedObject(allAssets[0]);
            var tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null)
            {
                Debug.LogWarning("Could not find 'tags' property in TagManager");
                return;
            }

            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue == tag) { found = true; break; }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                tagManager.ApplyModifiedProperties();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to add tag '{tag}': {ex.Message}");
        }
    }

    static AnimatorState FindStateByName(AnimatorStateMachine sm, string name)
    {
        System.Array arr = sm.states as System.Array;
        if (arr == null) return null;
        for (int i = 0; i < arr.Length; i++)
        {
            var elem = arr.GetValue(i);
            AnimatorState st = GetAnimatorStateFromChild(elem);
            if (st != null && st.name == name) return st;
        }
        return null;
    }

    static AnimatorState GetAnimatorStateFromChild(object child)
    {
        if (child == null) return null;
        var type = child.GetType();
        var prop = type.GetProperty("state");
        if (prop != null)
        {
            var val = prop.GetValue(child);
            return val as AnimatorState;
        }
        if (child is AnimatorState st) return st;
        return null;
    }

    static void EnsureHitAnimationEvent(AnimationClip clip, int attackIndex)
    {
        if (clip == null) return;

        // Check if the clip is read-only (e.g. from an imported FBX)
        string path = AssetDatabase.GetAssetPath(clip);
        if (!string.IsNullOrEmpty(path) && path.ToLower().EndsWith(".fbx"))
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                var clipAnimations = importer.clipAnimations;
                if (clipAnimations == null || clipAnimations.Length == 0)
                {
                    clipAnimations = importer.defaultClipAnimations;
                }

                if (clipAnimations != null && clipAnimations.Length > 0)
                {
                    bool modified = false;
                    for (int i = 0; i < clipAnimations.Length; i++)
                    {
                        // Match either clip name or take name
                        if (clipAnimations[i].name == clip.name || clipAnimations[i].takeName == clip.name)
                        {
                            var events = clipAnimations[i].events;
                            bool exists = false;
                            if (events != null)
                            {
                                foreach (var ev in events)
                                {
                                    if (ev.functionName == "OnAttackHit")
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                            }

                            if (!exists)
                            {
                                float normalizedHitTime = 0.35f + (attackIndex * 0.05f);
                                float eventTime = normalizedHitTime * clip.length; // time in seconds
                                var newEv = new AnimationEvent
                                {
                                    functionName = "OnAttackHit",
                                    intParameter = attackIndex,
                                    time = eventTime
                                };

                                var evList = new System.Collections.Generic.List<AnimationEvent>(events ?? new AnimationEvent[0]);
                                evList.Add(newEv);
                                clipAnimations[i].events = evList.ToArray();
                                modified = true;
                            }
                        }
                    }

                    if (modified)
                    {
                        importer.clipAnimations = clipAnimations;
                        importer.SaveAndReimport();
                    }
                }
            }
        }
        else
        {
            // Standard editable clip (.anim)
            var events = AnimationUtility.GetAnimationEvents(clip);
            bool exists = false;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].functionName == "OnAttackHit")
                {
                    exists = true;
                    break;
                }
            }
            if (exists) return;

            float normalizedHitTime = 0.35f + (attackIndex * 0.05f);
            float eventTime = Mathf.Clamp(clip.length * normalizedHitTime, 0.05f, Mathf.Max(0.05f, clip.length - 0.01f));
            var evt = new AnimationEvent
            {
                functionName = "OnAttackHit",
                intParameter = attackIndex,
                time = eventTime
            };

            var newEvents = new AnimationEvent[events.Length + 1];
            for (int i = 0; i < events.Length; i++) newEvents[i] = events[i];
            newEvents[newEvents.Length - 1] = evt;
            AnimationUtility.SetAnimationEvents(clip, newEvents);
            EditorUtility.SetDirty(clip);
        }
    }

    public static AnimationClip LoadClipFromFBX(string fbxPath, string clipName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null) return null;
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && clip.name == clipName)
            {
                return clip;
            }
        }
        // Fallback: return the first animation clip that is not a preview clip
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }
}
}
