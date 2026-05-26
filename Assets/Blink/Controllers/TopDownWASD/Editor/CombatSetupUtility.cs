using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.UI;
using TMPro;

namespace Blink.Controllers.TopDownWASD.Editor
{
    // Editor utility to create prefabs and an animator controller to test the combat system.
    // Run from menu: BLINK -> Combat -> Setup Combat Assets
    public static class CombatSetupUtility
{
    [MenuItem("BLINK/Combat/Setup Combat Assets")]
    public static void SetupCombatAssets()
    {
        string baseFolder = "Assets/Blink/Controllers/TopDownWASD/Prefabs";
        if (!AssetDatabase.IsValidFolder("Assets/Blink/Controllers/TopDownWASD"))
        {
            AssetDatabase.CreateFolder("Assets/Blink/Controllers", "TopDownWASD");
        }
        if (!AssetDatabase.IsValidFolder(baseFolder))
        {
            AssetDatabase.CreateFolder("Assets/Blink/Controllers/TopDownWASD", "Prefabs");
        }

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
        string existingControllerPath = "Assets/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller";
        UnityEditor.Animations.AnimatorController ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(existingControllerPath);
        string usedControllerPath = existingControllerPath;
        if (ac == null)
        {
            // fallback: create a controller in Prefabs folder
            usedControllerPath = baseFolder + "/TopDownWASDAnimator.controller";
            ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(usedControllerPath);
        }

        // Add 4 trigger parameters (Attack1..Attack4)
        for (int i = 1; i <= 4; i++)
        {
            string pname = "Attack" + i;
            bool found = false;
            foreach (var p in ac.parameters)
            {
                if (p.name == pname) { found = true; break; }
            }
            if (!found)
                ac.AddParameter(pname, AnimatorControllerParameterType.Trigger);
        }

        // Create a new layer for combat attacks (separate from Locomotion)
        if (ac.layers.Length <= 1)
        {
            ac.AddLayer("Combat");
        }
        
        AnimatorControllerLayer combatLayer = ac.layers[ac.layers.Length - 1];
        if (combatLayer.name != "Combat")
        {
            combatLayer.name = "Combat";
        }
        
        var rootSm = combatLayer.stateMachine;
        
        // Create Idle state first in Combat layer
        AnimatorState idleState = FindStateByName(rootSm, "Idle");
        if (idleState == null)
        {
            var idleChild = rootSm.AddState("Idle");
            idleState = GetAnimatorStateFromChild(idleChild);
        }

        // Create animation clips and setup combo chain: Idle -> Attack1 -> Attack2 -> Attack3 -> Attack4 -> Idle
        AnimatorState previousState = idleState;
        for (int i = 1; i <= 4; i++)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = "Attack" + i;
            // simple small forward motion on localPosition.z
            var curve = AnimationCurve.EaseInOut(0f, 0f, 0.35f, 0.12f);
            clip.SetCurve("", typeof(Transform), "localPosition.z", curve);
            string clipPath = baseFolder + "/Attack" + i + ".anim";
            // if the clip already exists in the project, don't overwrite
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) == null)
            {
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            }

            EnsureHitAnimationEvent(clip, i - 1);

            // add state if not already present
            AnimatorState attackState = FindStateByName(rootSm, "Attack" + i);
            if (attackState == null)
            {
                var child = rootSm.AddState("Attack" + i);
                attackState = GetAnimatorStateFromChild(child);
            }
            if (attackState != null)
                attackState.motion = clip;

            // Transition FROM previous state (or Idle) TO this attack state
            if (previousState != null && attackState != null)
            {
                bool hasTransition = false;
                foreach (var trans in previousState.transitions)
                {
                    if (trans.destinationState == attackState && TransitionHasCondition(trans, "Attack" + i))
                    {
                        hasTransition = true;
                        break;
                    }
                }
                if (!hasTransition)
                {
                    var transToAttack = previousState.AddTransition(attackState);
                    transToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack" + i);
                    transToAttack.hasExitTime = false;
                    transToAttack.duration = 0f;
                }
            }

            // Transition FROM this attack TO Idle (if attack combo ends)
            if (attackState != null && idleState != null)
            {
                bool hasIdleTransition = false;
                foreach (var trans in attackState.transitions)
                {
                    if (trans.destinationState == idleState)
                    {
                        hasIdleTransition = true;
                        break;
                    }
                }
                if (!hasIdleTransition)
                {
                    var transToIdle = attackState.AddTransition(idleState);
                    transToIdle.hasExitTime = true;
                    transToIdle.exitTime = 0.9f;
                    transToIdle.duration = 0.1f;
                }
            }

            // If not the last attack, create transition to next attack
            if (i < 4 && attackState != null)
            {
                AnimatorState nextAttackState = FindStateByName(rootSm, "Attack" + (i + 1));
                if (nextAttackState == null)
                {
                    var nextChild = rootSm.AddState("Attack" + (i + 1));
                    nextAttackState = GetAnimatorStateFromChild(nextChild);
                }
                if (nextAttackState != null)
                {
                    bool hasComboTransition = false;
                    foreach (var trans in attackState.transitions)
                    {
                        if (trans.destinationState == nextAttackState && TransitionHasCondition(trans, "Attack" + (i + 1)))
                        {
                            hasComboTransition = true;
                            break;
                        }
                    }
                    if (!hasComboTransition)
                    {
                        var comboTrans = attackState.AddTransition(nextAttackState);
                        comboTrans.AddCondition(AnimatorConditionMode.If, 0, "Attack" + (i + 1));
                        comboTrans.hasExitTime = false;
                        comboTrans.duration = 0f;
                    }
                }
            }

            previousState = attackState;
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

        // If the user has a GameObject selected in the Editor, try to assign the created controller and popup prefab
        var selected = Selection.activeGameObject;
        if (selected != null)
        {
            var selectedAnimator = selected.GetComponent<Animator>();
            if (selectedAnimator != null)
            {
                // assign the controller asset we created/modified
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

        EditorUtility.DisplayDialog("Combat Setup", "Created Enemy prefab and Animator Controller in:\n" + baseFolder, "OK");
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

    // helper: find AnimatorState by name in a state machine, compatible with different Unity versions
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
        // try property 'state' (ChildAnimatorState)
        var prop = type.GetProperty("state");
        if (prop != null)
        {
            var val = prop.GetValue(child);
            return val as AnimatorState;
        }
        // maybe the element itself is AnimatorState
        if (child is AnimatorState st) return st;
        return null;
    }

    static bool TransitionHasCondition(AnimatorStateTransition trans, string parameterName)
    {
        if (trans == null) return false;
        foreach (var condition in trans.conditions)
        {
            if (condition.parameter == parameterName)
                return true;
        }
        return false;
    }

    static void EnsureHitAnimationEvent(AnimationClip clip, int attackIndex)
    {
        if (clip == null) return;
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

        float eventTime = Mathf.Clamp(0.18f + (attackIndex * 0.03f), 0.05f, Mathf.Max(0.05f, clip.length - 0.01f));
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
}
