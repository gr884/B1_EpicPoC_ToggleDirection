using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Tooltip : SingletonBehaviour<UI_Tooltip>
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("Settings")]
    [SerializeField] private Vector2 _offset = new Vector2(10f, -10f);

    private RectTransform _rectTransform;
    private Canvas _rootCanvas;

    protected override void Awake()
    {
        base.Awake();
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        Hide();
    }

    public void Show(CardData data, Vector2 screenPosition)
    {
        if (data == null) return;

        _iconImage.sprite = data.icon;
        _iconImage.enabled = data.icon != null;
        _nameText.text = data.displayName;
        _descriptionText.text = data.description;

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
        if (_canvasGroup.alpha > 0f)
            UpdatePosition(Input.mousePosition);
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        if (_rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            screenPosition,
            _rootCanvas.worldCamera,
            out Vector2 localPoint);

        _rectTransform.anchoredPosition = localPoint + _offset;
    }
}