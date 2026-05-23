using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color enemyActiveColor = new Color(0.2f, 0.55f, 1f, 1f);
    [SerializeField] private Color enemyInactiveColor = new Color(1f, 0.55f, 0.55f, 1f);

    public CardData Data { get; private set; }
    public CardTeam Team { get; private set; } = CardTeam.Ally;
    public bool IsAlly => Team == CardTeam.Ally;
    public bool IsEnemy => Team == CardTeam.Enemy;
    public bool IsActivated { get; private set; }
    public bool IsPlacedOnBoard { get; private set; }
    public BoardSlot CurrentSlot { get; private set; }

    private RectTransform rectTransform;
    private readonly List<Image> extraIconLayers = new();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Reset()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
    }

    public void Initialize(CardData data, CardTeam team, bool startsActivated = false)
    {
        Data = data;
        Team = team;
        IsActivated = startsActivated;
        CurrentSlot = null;
        IsPlacedOnBoard = false;

        ApplyIconVisual();

        if (titleText != null)
        {
            titleText.text = data != null ? data.displayName : "Card";
            titleText.alignment = TextAlignmentOptions.TopLeft;
        }

        RefreshVisual();
    }

    public void SetPlaced(BoardSlot slot)
    {
        CurrentSlot = slot;
        IsPlacedOnBoard = slot != null;
    }

    public void SetActivated(bool activated)
    {
        IsActivated = activated;
        RefreshVisual();
    }

    public void SetDraggable(bool draggable)
    {
        CardDragHandler dragHandler = GetComponent<CardDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.enabled = draggable && !IsEnemy;
        }
    }

    private void RefreshVisual()
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (IsEnemy)
        {
            backgroundImage.color = IsActivated ? enemyActiveColor : enemyInactiveColor;
        }
        else
        {
            backgroundImage.color = IsActivated ? activeColor : inactiveColor;
        }
    }

    private void ApplyIconVisual()
    {
        if (iconImage == null)
        {
            return;
        }

        if (TryApplyLayeredIcons())
        {
            return;
        }

        iconImage.sprite = Data != null ? Data.icon : null;
        iconImage.enabled = iconImage.sprite != null;
        SetExtraLayerVisibleFrom(0, false);
    }

    private bool TryApplyLayeredIcons()
    {
        CardDirectionData dirData = Data != null ? Data.directionData : null;
        if (dirData == null || dirData.layeredIcons == null || dirData.layeredIcons.Count == 0)
        {
            return false;
        }

        int baseSiblingIndex = iconImage.transform.GetSiblingIndex();
        for (int i = 0; i < dirData.layeredIcons.Count; i++)
        {
            Image layer = GetOrCreateLayerImage(i);
            if (layer == null)
            {
                continue;
            }

            Sprite sprite = dirData.layeredIcons[i];
            layer.sprite = sprite;
            layer.enabled = sprite != null;
            layer.preserveAspect = iconImage.preserveAspect;
            layer.rectTransform.SetSiblingIndex(baseSiblingIndex + i);
        }

        SetExtraLayerVisibleFrom(dirData.layeredIcons.Count - 1, false);
        return true;
    }

    private Image GetOrCreateLayerImage(int layerIndex)
    {
        if (layerIndex <= 0)
        {
            return iconImage;
        }

        int extraIndex = layerIndex - 1;
        while (extraIconLayers.Count <= extraIndex)
        {
            Image created = CreateExtraLayerImage(extraIconLayers.Count + 1);
            if (created == null)
            {
                return null;
            }

            extraIconLayers.Add(created);
        }

        return extraIconLayers[extraIndex];
    }

    private Image CreateExtraLayerImage(int order)
    {
        if (iconImage == null)
        {
            return null;
        }

        GameObject layerObject = new($"DirectionLayer_{order}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform layerRect = layerObject.GetComponent<RectTransform>();
        RectTransform iconRect = iconImage.rectTransform;
        layerRect.SetParent(iconRect.parent, false);
        layerRect.anchorMin = iconRect.anchorMin;
        layerRect.anchorMax = iconRect.anchorMax;
        layerRect.pivot = iconRect.pivot;
        layerRect.anchoredPosition = iconRect.anchoredPosition;
        layerRect.sizeDelta = iconRect.sizeDelta;
        layerRect.localScale = Vector3.one;

        Image layerImage = layerObject.GetComponent<Image>();
        layerImage.color = iconImage.color;
        layerImage.material = iconImage.material;
        layerImage.type = iconImage.type;
        layerImage.preserveAspect = iconImage.preserveAspect;
        layerImage.raycastTarget = iconImage.raycastTarget;
        layerImage.maskable = iconImage.maskable;

        return layerImage;
    }

    private void SetExtraLayerVisibleFrom(int usedLayerIndex, bool visible)
    {
        int firstExtraToApply = Mathf.Max(usedLayerIndex, 0);
        for (int i = firstExtraToApply; i < extraIconLayers.Count; i++)
        {
            Image layer = extraIconLayers[i];
            if (layer != null)
            {
                layer.enabled = visible;
            }
        }
    }

    public IEnumerator PlayActivationFeedback(float duration)
    {
        if (rectTransform == null)
        {
            yield break;
        }

        Vector3 baseScale = Vector3.one;
        Quaternion baseRotation = Quaternion.identity;
        float angle = Random.value > 0.5f ? 10f : -10f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float wiggle = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t);
            float punch = Mathf.Sin(t * Mathf.PI);

            rectTransform.localRotation = Quaternion.Euler(0f, 0f, wiggle * angle);
            rectTransform.localScale = new Vector3(1f + 0.08f * punch, 1f - 0.08f * punch, 1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.localRotation = baseRotation;
        rectTransform.localScale = baseScale;
    }
}
