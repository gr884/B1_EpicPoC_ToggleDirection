using TMPro;
using UnityEngine;

public class UI_IntentTooltip : SingletonBehaviour<UI_IntentTooltip>
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("Settings")]
    [SerializeField] private Vector2 _offset = new Vector2(16f, -16f);

    private RectTransform _rectTransform;
    private Canvas _rootCanvas;
    private RectTransform _canvasRectTransform;

    protected override void Awake()
    {
        base.Awake();
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        _canvasRectTransform = _rootCanvas != null ? _rootCanvas.transform as RectTransform : null;
        _rectTransform.pivot = new Vector2(0f, 1f);
        Hide();
    }

    public void Show(string title, string description, Vector2 screenPosition)
    {
        _titleText.text = title;
        _descriptionText.text = description;

        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = false;

        UpdatePosition(screenPosition);
    }

    public void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (_canvasGroup.alpha <= 0f) return;
        UpdatePosition(Input.mousePosition);
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        if (_rootCanvas == null || _rectTransform == null || _canvasRectTransform == null) return;

        Camera uiCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform, screenPosition, uiCamera, out Vector2 localPoint)) return;

        Vector2 targetPosition = localPoint + _offset;

        float tooltipWidth = _rectTransform.rect.width;
        float tooltipHeight = _rectTransform.rect.height;
        Rect canvasRect = _canvasRectTransform.rect;

        if (targetPosition.x + tooltipWidth > canvasRect.xMax)
            targetPosition.x = localPoint.x - Mathf.Abs(_offset.x) - tooltipWidth;

        if (targetPosition.y - tooltipHeight < canvasRect.yMin)
            targetPosition.y = localPoint.y + Mathf.Abs(_offset.y) + tooltipHeight;

        _rectTransform.anchoredPosition = targetPosition;
    }
}