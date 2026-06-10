using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Tooltip : SingletonBehaviour<UI_Tooltip>
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _nameText;
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

        // 좌상단 기준으로 마우스 따라다니게
        _rectTransform.pivot = new Vector2(0f, 1f);

        Hide();
    }

    public void Show(CardData data, Vector2 screenPosition)
    {
        if (data == null) return;

        Show(data.displayName, data.description, screenPosition);
    }

    public void Show(string displayName, string description, Vector2 screenPosition)
    {
        _nameText.text = displayName;
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
                _canvasRectTransform,
                screenPosition,
                uiCamera,
                out Vector2 localPoint)) return;

        Vector2 targetPosition = localPoint + _offset;

        // 화면 밖 벗어남 방지
        float tooltipWidth = _rectTransform.rect.width;
        float tooltipHeight = _rectTransform.rect.height;
        Rect canvasRect = _canvasRectTransform.rect;

        // 오른쪽 벗어나면 왼쪽으로
        if (targetPosition.x + tooltipWidth > canvasRect.xMax)
            targetPosition.x = localPoint.x - Mathf.Abs(_offset.x) - tooltipWidth;

        // 위쪽 벗어나면 아래로
        if (targetPosition.y - tooltipHeight < canvasRect.yMin)
            targetPosition.y = localPoint.y + Mathf.Abs(_offset.y) + tooltipHeight;

        _rectTransform.anchoredPosition = targetPosition;
    }
}
