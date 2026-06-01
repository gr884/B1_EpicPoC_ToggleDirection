using DG.Tweening;
using UnityEngine;

public class UI_BounceArrow : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _amplitude = 15f;
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private Ease _ease = Ease.InOutSine;

    private RectTransform _rectTransform;
    private Vector2 _basePosition;
    private Tween _tween;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_rectTransform == null) return;
        _basePosition = _rectTransform.anchoredPosition;

        _tween = _rectTransform
            .DOAnchorPosY(_basePosition.y - _amplitude, _duration)
            .SetEase(_ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        _tween?.Kill();
        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _basePosition;
    }
}