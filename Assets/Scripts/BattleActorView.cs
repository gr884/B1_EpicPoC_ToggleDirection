using TMPro;
using UnityEngine;

public class BattleActorView : MonoBehaviour
{
    [Header("View Binding")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Transform hpFillTransform;

    [HideInInspector]
    [SerializeField] private int maxHp = 20;

    [HideInInspector]
    [SerializeField] private string label = "Actor";

    [Header("Feedback")]
    [SerializeField] private float hitMoveAmount = 12f;
    [SerializeField] private float feedbackDuration = 0.16f;

    private int currentHp;
    private RectTransform rectTransform;
    private Transform cachedTransform;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseLocalPosition;
    private Vector3 baseHpFillScale = Vector3.one;

    public int CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0;

    private void Awake()
    {
        cachedTransform = transform;
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseAnchoredPosition = rectTransform.anchoredPosition;
        }
        baseLocalPosition = cachedTransform.localPosition;

        if (hpText == null)
        {
            hpText = GetComponentInChildren<TMP_Text>();
        }

        CacheHealthBarBaseScale();
    }

    public void Configure(string actorLabel, int actorMaxHp, TMP_Text actorHpText)
    {
        label = actorLabel;
        maxHp = Mathf.Max(1, actorMaxHp);
        if (actorHpText != null)
        {
            hpText = actorHpText;
        }

        CacheBasePose();
        CacheHealthBarBaseScale();
    }

    public void SetHealthBarFill(Transform fillTransform)
    {
        hpFillTransform = fillTransform;
        CacheHealthBarBaseScale();
        Refresh();
    }

    public void CacheBasePose()
    {
        if (rectTransform != null)
        {
            baseAnchoredPosition = rectTransform.anchoredPosition;
        }

        if (cachedTransform == null)
        {
            cachedTransform = transform;
        }

        baseLocalPosition = cachedTransform.localPosition;
    }

    public void ResetHp()
    {
        currentHp = Mathf.Max(1, maxHp);
        Refresh();
    }

    public void TakeDamage(int amount)
    {
        int damage = Mathf.Max(0, amount);
        currentHp = Mathf.Max(0, currentHp - damage);
        Refresh();

        if (damage > 0)
        {
            StopAllCoroutines();
            StartCoroutine(HitFeedback());
        }

        Debug.Log($"{label}: TakeDamage {damage}. hp={currentHp}/{maxHp}");
    }

    private void Refresh()
    {
        if (hpText != null)
        {
            hpText.text = $"{label} HP {currentHp}/{maxHp}";
        }

        if (hpFillTransform != null)
        {
            float normalizedHp = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            hpFillTransform.localScale = new Vector3(baseHpFillScale.x * normalizedHp, baseHpFillScale.y, baseHpFillScale.z);
        }
    }

    private void CacheHealthBarBaseScale()
    {
        if (hpFillTransform != null)
        {
            baseHpFillScale = hpFillTransform.localScale;
        }
    }

    private System.Collections.IEnumerator HitFeedback()
    {
        if (rectTransform == null && cachedTransform == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < feedbackDuration)
        {
            float t = elapsed / feedbackDuration;
            float offset = Mathf.Sin(t * Mathf.PI * 4f) * hitMoveAmount * (1f - t);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(offset, 0f);
            }
            else
            {
                cachedTransform.localPosition = baseLocalPosition + new Vector3(offset * 0.01f, 0f, 0f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = baseAnchoredPosition;
        }
        else
        {
            cachedTransform.localPosition = baseLocalPosition;
        }
    }
}
