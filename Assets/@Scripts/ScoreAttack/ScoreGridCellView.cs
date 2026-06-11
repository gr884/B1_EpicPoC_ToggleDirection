using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScoreGridCellView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Image _image;
    private Color _baseColor;
    private Action<ScoreGridCellView> _clicked;
    private Action<ScoreGridCellView, bool> _hovered;
    private Image _signalPreview;

    public Vector2Int Position { get; private set; }

    public void Initialize(
        Vector2Int position,
        Color baseColor,
        Action<ScoreGridCellView> clicked,
        Action<ScoreGridCellView, bool> hovered)
    {
        Position = position;
        _baseColor = baseColor;
        _clicked = clicked;
        _hovered = hovered;
        _image = GetComponent<Image>();
        if (_image != null)
            _image.color = baseColor;
    }

    public void SetPreview(bool visible, bool valid)
    {
        if (_image == null) return;
        _image.color = visible
            ? (valid ? new Color(0.2f, 1f, 0.55f, 0.55f) : new Color(1f, 0.2f, 0.2f, 0.55f))
            : _baseColor;
    }

    public void SetSignalPreview(bool visible, Color color)
    {
        if (_signalPreview == null)
            _signalPreview = CreateSignalPreview();
        _signalPreview.gameObject.SetActive(visible);
        if (visible)
            _signalPreview.color = color;
    }

    private Image CreateSignalPreview()
    {
        GameObject obj = new("SignalPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(20f, 20f);
        Image image = obj.GetComponent<Image>();
        image.raycastTarget = false;
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2f, -2f);
        obj.SetActive(false);
        return image;
    }

    public void OnPointerClick(PointerEventData eventData) => _clicked?.Invoke(this);
    public void OnPointerEnter(PointerEventData eventData) => _hovered?.Invoke(this, true);
    public void OnPointerExit(PointerEventData eventData) => _hovered?.Invoke(this, false);
}
