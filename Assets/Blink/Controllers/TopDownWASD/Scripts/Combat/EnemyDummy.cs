using UnityEngine;
using UnityEngine.UI;

// Simple enemy component for testing attacks.
// Attach to a cube (tag it as "Enemy" in the inspector).
// Creates a world-space healthbar automatically if none is provided.
namespace BLINK.Controller
{
    public class EnemyDummy : MonoBehaviour
    {
    public float maxHealth = 1000f;
    public float currentHealth;

    // optional healthbar (child Canvas with an Image named "Fill")
    public Image healthFillImage;

    void Awake()
    {
        currentHealth = maxHealth;
        if (healthFillImage == null)
        {
            CreateSimpleHealthBar();
        }
        UpdateHealthBar();
    }

    void CreateSimpleHealthBar()
    {
        // create a small world-space canvas above the enemy
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = Vector3.up * 1.6f;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.scaleFactor = 100f;
        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 12);

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.black;
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.red;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;

        healthFillImage = fillImg;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);
        UpdateHealthBar();
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

        void Die()
        {
            // simple: destroy or disable
            gameObject.SetActive(false);
        }
    }
}

