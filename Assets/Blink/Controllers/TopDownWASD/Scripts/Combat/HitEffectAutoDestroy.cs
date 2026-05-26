using UnityEngine;

namespace BLINK.Controller
{
    // Lightweight test hit effect: scales and fades quickly, then destroys itself.
    public class HitEffectAutoDestroy : MonoBehaviour
    {
        public float lifetime = 0.25f;
        public float endScale = 0.02f;

        Vector3 _startScale;
        Renderer _renderer;
        Color _startColor = Color.white;
        float _elapsed;

        void Awake()
        {
            _startScale = transform.localScale;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null && _renderer.material != null)
            {
                _startColor = _renderer.material.color;
                _renderer.material.color = Color.yellow;
            }
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, lifetime));
            transform.localScale = Vector3.Lerp(_startScale, Vector3.one * endScale, t);

            if (_renderer != null && _renderer.material != null)
            {
                Color c = Color.Lerp(Color.yellow, new Color(_startColor.r, _startColor.g, _startColor.b, 0f), t);
                _renderer.material.color = c;
            }

            if (_elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}

