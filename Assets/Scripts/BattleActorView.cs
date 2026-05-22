using TMPro;
using UnityEngine;

public class BattleActorView : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private int maxHp = 20;
    [SerializeField] private string label = "Actor";
    [SerializeField] private float hitMoveAmount = 12f;
    [SerializeField] private float feedbackDuration = 0.16f;

    private int currentHp;
    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;

    public int CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseAnchoredPosition = rectTransform.anchoredPosition;
        }

        if (hpText == null)
        {
            hpText = GetComponentInChildren<TMP_Text>();
        }
    }

    public void Configure(string actorLabel, int actorMaxHp, TMP_Text actorHpText)
    {
        label = actorLabel;
        maxHp = Mathf.Max(1, actorMaxHp);
        if (actorHpText != null)
        {
            hpText = actorHpText;
        }
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
    }

    private System.Collections.IEnumerator HitFeedback()
    {
        if (rectTransform == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < feedbackDuration)
        {
            float t = elapsed / feedbackDuration;
            float offset = Mathf.Sin(t * Mathf.PI * 4f) * hitMoveAmount * (1f - t);
            rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(offset, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = baseAnchoredPosition;
    }
}
