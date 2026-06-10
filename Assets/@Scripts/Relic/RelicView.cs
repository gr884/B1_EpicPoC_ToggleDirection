using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class RelicView : MonoBehaviour, IGridChainNode, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform _rectTransform;

    [Header("Refs")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private GameObject _selectionOverlay;

    [Header("Direction Icons")]
    [SerializeField] private Image _upLeft;
    [SerializeField] private Image _up;
    [SerializeField] private Image _upRight;
    [SerializeField] private Image _left;
    [SerializeField] private Image _right;
    [SerializeField] private Image _downLeft;
    [SerializeField] private Image _down;
    [SerializeField] private Image _downRight;

    [Header("Colors")]
    [SerializeField] private Color _activeColor = Color.white;
    [SerializeField] private Color _inactiveColor = new(0.45f, 0.45f, 0.45f, 1f);

    public RelicData Data { get; private set; }
    public RelicInstance Instance { get; private set; }
    public GridSlot CurrentSlot { get; private set; }
    public bool IsActivated => Instance != null && Instance.IsActivated;
    public bool IsEnemy => false;
    public string ChainDisplayName => Data != null ? Data.displayName : name;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(RelicInstance instance, GridSlot primarySlot)
    {
        Instance = instance;
        Data = instance != null ? instance.SourceData : null;
        CurrentSlot = primarySlot;

        if (_iconImage != null)
        {
            _iconImage.sprite = Data != null ? Data.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = Data != null ? Data.displayName : "";

        SetSelected(false);
        ApplyGridLayout();
        RefreshDirectionIcons();
        RefreshVisual();
    }

    public void ApplyGridLayout()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform == null) return;
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
        _rectTransform.localRotation = Quaternion.identity;
        _rectTransform.localScale = Vector3.one;
    }

    public IEnumerable<GridDirectionRay> GetDirectionRays()
    {
        if (Data == null || Instance == null || GridManager.Instance == null)
            yield break;

        foreach (RelicDirectionRay ray in Data.GetDirectionRays(Instance.RotationSteps))
        {
            GridSlot originSlot = GridManager.Instance.GetSlot(Instance.GridOrigin + ray.LocalCell);
            if (originSlot == null) continue;
            yield return new GridDirectionRay(originSlot, ray.Direction, ray.Range);
        }
    }

    public void SetActivated(bool activated)
    {
        Instance?.SetActivated(activated);
        RefreshVisual();
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOverlay != null)
            _selectionOverlay.SetActive(selected);
    }

    public IEnumerator PlayActivationFeedback(float duration)
    {
        if (_rectTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float punch = Mathf.Sin(t * Mathf.PI);
            _rectTransform.localScale = Vector3.one * (1f + 0.08f * punch);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rectTransform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Data == null || UI_Tooltip.Instance == null) return;
        UI_Tooltip.Instance.Show(Data.displayName, Data.description, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UI_Tooltip.Instance?.Hide();
    }

    private void RefreshVisual()
    {
        if (_backgroundImage != null)
            _backgroundImage.color = IsActivated ? _activeColor : _inactiveColor;
    }

    private void RefreshDirectionIcons()
    {
        HideAllDirectionIcons();
        if (Data == null || Instance == null) return;

        foreach (RelicDirectionRay ray in Data.GetDirectionRays(Instance.RotationSteps))
        {
            Image image = GetDirectionImage(ray.Direction);
            if (image != null) image.enabled = true;
        }
    }

    private void HideAllDirectionIcons()
    {
        if (_up != null) _up.enabled = false;
        if (_upRight != null) _upRight.enabled = false;
        if (_right != null) _right.enabled = false;
        if (_downRight != null) _downRight.enabled = false;
        if (_down != null) _down.enabled = false;
        if (_downLeft != null) _downLeft.enabled = false;
        if (_left != null) _left.enabled = false;
        if (_upLeft != null) _upLeft.enabled = false;
    }

    private Image GetDirectionImage(CardDirection direction) => direction switch
    {
        CardDirection.Up => _up,
        CardDirection.UpRight => _upRight,
        CardDirection.Right => _right,
        CardDirection.DownRight => _downRight,
        CardDirection.Down => _down,
        CardDirection.DownLeft => _downLeft,
        CardDirection.Left => _left,
        CardDirection.UpLeft => _upLeft,
        _ => null
    };
}
