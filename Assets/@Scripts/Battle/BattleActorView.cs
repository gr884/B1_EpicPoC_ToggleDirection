using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleActorView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private GameObject _defenseRoot;
    [SerializeField] private TMP_Text _defenseText;

    [Header("Feedback")]
    [SerializeField] private float _hitMoveAmount = 12f;
    [SerializeField] private float _feedbackDuration = 0.16f;

    private int _maxHp;
    private int _currentHp;
    private string _label;
    private bool _hasDied;

    public string Label => _label;
    private RectTransform _rectTransform;
    private Vector2 _baseAnchoredPosition;

    public int CurrentHp => _currentHp;
    public bool IsDead => _currentHp <= 0;

    public event System.Action OnDied;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
            _baseAnchoredPosition = _rectTransform.anchoredPosition;
    }

    public void Setup(string label, int maxHp)
    {
        _label = label;
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;
        _hasDied = false;
        SetDefense(0);
        Refresh();
    }

    public void Setup(string label, int maxHp, int currentHp)
    {
        _label = label;
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = Mathf.Clamp(currentHp, 0, _maxHp);
        _hasDied = _currentHp <= 0;
        SetDefense(0);
        Refresh();
    }

    public void TakeDamage(int amount)
    {
        int damage = Mathf.Max(0, amount);
        _currentHp = Mathf.Max(0, _currentHp - damage);
        Refresh();

        if (damage > 0)
        {
            StopAllCoroutines();
            StartCoroutine(HitFeedback());
        }

        Debug.Log($"[BattleActorView] {_label} 데미지 {damage} → HP {_currentHp}/{_maxHp}");

        if (!_hasDied && _currentHp <= 0)
        {
            _hasDied = true;
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        _currentHp = Mathf.Min(_maxHp, _currentHp + Mathf.Max(0, amount));
        Refresh();
    }

    public void SetDefense(int defense)
    {
        if (_defenseRoot != null)
            _defenseRoot.SetActive(defense > 0);
        if (_defenseText != null)
            _defenseText.text = defense.ToString();
    }

    // ── 내부 ───────────────────────────────────────────────

    private void Refresh()
    {
        if (_hpText != null)
            _hpText.text = $"{_currentHp} / {_maxHp}";

        if (_hpSlider != null)
            _hpSlider.value = _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;
    }

    private IEnumerator HitFeedback()
    {
        if (_rectTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < _feedbackDuration)
        {
            float t = elapsed / _feedbackDuration;
            float offset = Mathf.Sin(t * Mathf.PI * 4f) * _hitMoveAmount * (1f - t);
            _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(offset, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _rectTransform.anchoredPosition = _baseAnchoredPosition;
    }
}
