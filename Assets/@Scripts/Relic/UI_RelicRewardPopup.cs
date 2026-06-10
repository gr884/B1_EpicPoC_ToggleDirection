using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_RelicRewardPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Transform _relicContainer;
    [SerializeField] private RelicView _relicOptionPrefab;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private RelicPlacementController _placementController;

    private readonly List<RelicView> _displayedRelics = new();
    private readonly List<RelicData> _currentCandidates = new();
    private RelicData _selectedRelic;
    private Action _onClosed;

    private void Awake()
    {
        _confirmButton?.onClick.AddListener(HandleConfirm);
        SetVisible(false);
    }

    public void ShowRelicReward(IReadOnlyList<RelicData> candidates, Action onClosed)
    {
        ClearRelics();
        _currentCandidates.Clear();
        _onClosed = onClosed;
        _selectedRelic = null;

        if (candidates != null)
        {
            foreach (RelicData relic in candidates)
                if (relic != null)
                    _currentCandidates.Add(relic);
        }

        foreach (RelicData relic in _currentCandidates)
            SpawnRelicOption(relic);

        if (_confirmButton != null)
            _confirmButton.interactable = false;
        SetVisible(true);
    }

    private void SpawnRelicOption(RelicData data)
    {
        if (_relicOptionPrefab == null || _relicContainer == null) return;

        RelicInstance previewInstance = new(data);
        RelicView view = Instantiate(_relicOptionPrefab, _relicContainer);
        view.Initialize(previewInstance, null);

        Button button = view.GetComponent<Button>();
        if (button == null)
            button = view.gameObject.AddComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => HandleRelicClicked(data, view));

        _displayedRelics.Add(view);
    }

    private void HandleRelicClicked(RelicData data, RelicView view)
    {
        _selectedRelic = data;
        if (_confirmButton != null)
            _confirmButton.interactable = true;

        foreach (RelicView relicView in _displayedRelics)
            relicView.SetSelected(relicView == view);
    }

    private void HandleConfirm()
    {
        if (_selectedRelic == null) return;
        RelicData selectedRelic = _selectedRelic;

        SetVisible(false);
        ClearRelics();

        RelicPlacementController controller = ResolvePlacementController();
        if (controller == null)
        {
            Close();
            return;
        }

        controller.BeginPlacement(
            selectedRelic,
            Close,
            ReopenForSelection);
    }

    private void ReopenForSelection()
    {
        ShowRelicReward(new List<RelicData>(_currentCandidates), _onClosed);
    }

    private void Close()
    {
        ClearRelics();
        SetVisible(false);
        Action callback = _onClosed;
        _onClosed = null;
        callback?.Invoke();
    }

    private void ClearRelics()
    {
        foreach (RelicView relic in _displayedRelics)
            if (relic != null)
                Destroy(relic.gameObject);

        _displayedRelics.Clear();
        _selectedRelic = null;
    }

    private RelicPlacementController ResolvePlacementController()
    {
        if (_placementController != null)
            return _placementController;

        _placementController = RelicPlacementController.Instance != null
            ? RelicPlacementController.Instance
            : FindFirstObjectByType<RelicPlacementController>(FindObjectsInactive.Include);
        return _placementController;
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
