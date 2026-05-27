using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Simple floating damage popup using built-in TextMesh
// Spawns a temporary 3D text at a world position and fades/moves up.
namespace BLINK.Controller
{
    public class DamagePopup : MonoBehaviour
    {
        [Header("Popup Visual")]
        public TMP_Text textComponent;
        public float lifetime = 1.0f;
        public float riseDistance = 0.8f;

        static DamagePopup _poolPrefab;
        static readonly Queue<DamagePopup> Pool = new Queue<DamagePopup>();

        Color _initialColor = Color.yellow;
        Coroutine _routine;

        void Awake()
        {
            if (textComponent == null)
                textComponent = GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                _initialColor = textComponent.color;
        }

        void OnEnable()
        {
            // no-op: started manually by Play() so pooled objects can reset safely
        }

        public void SetText(string text)
        {
            if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = text;
        }

        public static void SpawnFromPrefab(GameObject prefab, Vector3 worldPosition, string attackName, int damage)
        {
            if (prefab == null)
            {
                Spawn(worldPosition, attackName, damage);
                return;
            }

            DamagePopup prefabPopup = prefab.GetComponent<DamagePopup>();
            if (prefabPopup == null)
            {
                Spawn(worldPosition, attackName, damage);
                return;
            }

            if (_poolPrefab != prefabPopup)
            {
                Pool.Clear();
                _poolPrefab = prefabPopup;
            }

            DamagePopup popup = Pool.Count > 0 ? Pool.Dequeue() : Instantiate(_poolPrefab);
            popup.gameObject.SetActive(true);
            popup.transform.position = worldPosition + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));
            popup.SetText(attackName + "\n" + damage);
            popup.Play();
        }

        public static void Spawn(Vector3 worldPosition, string attackName, int damage)
        {
            GameObject go = new GameObject("DamagePopup");
            go.transform.position = worldPosition + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));

            var textObj = new GameObject("TMP");
            textObj.transform.SetParent(go.transform, false);
            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;

            var popup = go.AddComponent<DamagePopup>();
            popup.textComponent = tmp;
            popup.SetText(attackName + "\n" + damage);
            popup.Play();
        }

        void Play()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(AnimateAndRecycle());
        }

        IEnumerator AnimateAndRecycle()
        {
            float t = 0f;
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * riseDistance;

            if (textComponent != null)
            {
                _initialColor = textComponent.color;
            }

            while (t < lifetime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / lifetime);
                transform.position = Vector3.Lerp(start, end, p);
                if (textComponent != null)
                {
                    textComponent.color = Color.Lerp(_initialColor, new Color(_initialColor.r, _initialColor.g, _initialColor.b, 0f), p);
                }
                yield return null;
            }

            if (_poolPrefab != null && _poolPrefab != this)
            {
                gameObject.SetActive(false);
                Pool.Enqueue(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

