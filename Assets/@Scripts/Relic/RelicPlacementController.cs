using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class RelicPlacementController : SingletonBehaviour<RelicPlacementController>
{
    [Header("Refs")]
    [SerializeField] private UI_RelicPlacementConfirmPopup _confirmPopup;
    [SerializeField] private InputActionReference _rotateAction;

    private RelicData _pendingRelic;
    private GridSlot _hoveredSlot;
    private int _rotationSteps;
    private Action _onConfirmed;
    private Action _onCancelledToSelection;

    public bool IsPlacing => _pendingRelic != null;

    private void OnEnable()
    {
        if (_rotateAction != null && _rotateAction.action != null)
        {
            _rotateAction.action.performed += HandleRotatePerformed;
            _rotateAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (_rotateAction != null && _rotateAction.action != null)
        {
            _rotateAction.action.performed -= HandleRotatePerformed;
            _rotateAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!IsPlacing) return;

        if (_rotateAction == null && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RotatePendingRelic();
    }

    public void BeginPlacement(RelicData relic, Action onConfirmed, Action onCancelledToSelection)
    {
        if (relic == null) return;

        _pendingRelic = relic;
        _rotationSteps = 0;
        _hoveredSlot = null;
        _onConfirmed = onConfirmed;
        _onCancelledToSelection = onCancelledToSelection;

        GridManager.Instance?.ClearPendingDirectionalImpact();
    }

    public void HandleSlotHovered(GridSlot slot)
    {
        if (!IsPlacing || slot == _hoveredSlot) return;

        _hoveredSlot = slot;
        RefreshPreview();
    }

    public void HandleSlotClicked(GridSlot slot)
    {
        if (!IsPlacing || slot == null || _pendingRelic == null) return;
        if (RelicManager.Instance == null) return;
        if (!RelicManager.Instance.CanPlaceRelic(_pendingRelic, slot.Position, _rotationSteps)) return;

        _hoveredSlot = slot;
        RefreshPreview();

        if (_confirmPopup != null)
        {
            _confirmPopup.Show(
                _pendingRelic,
                ConfirmPlacement,
                CancelToSelection);
        }
        else
        {
            ConfirmPlacement();
        }
    }

    public void CancelToSelection()
    {
        GridManager.Instance?.ClearPendingDirectionalImpact();
        _confirmPopup?.Hide();

        _pendingRelic = null;
        _hoveredSlot = null;
        _rotationSteps = 0;

        Action callback = _onCancelledToSelection;
        _onConfirmed = null;
        _onCancelledToSelection = null;
        callback?.Invoke();
    }

    private void ConfirmPlacement()
    {
        if (_pendingRelic == null || _hoveredSlot == null || RelicManager.Instance == null)
            return;

        RelicView placed = RelicManager.Instance.PlaceRelic(_pendingRelic, _hoveredSlot.Position, _rotationSteps);
        if (placed == null) return;

        GridManager.Instance?.ClearPendingDirectionalImpact();
        _confirmPopup?.Hide();

        _pendingRelic = null;
        _hoveredSlot = null;
        _rotationSteps = 0;

        Action callback = _onConfirmed;
        _onConfirmed = null;
        _onCancelledToSelection = null;
        callback?.Invoke();
    }

    private void HandleRotatePerformed(InputAction.CallbackContext context)
    {
        if (context.performed)
            RotatePendingRelic();
    }

    private void RotatePendingRelic()
    {
        if (!IsPlacing) return;

        _rotationSteps = (_rotationSteps + 1) % 4;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_pendingRelic == null || _hoveredSlot == null || GridManager.Instance == null)
        {
            GridManager.Instance?.ClearPendingDirectionalImpact();
            return;
        }

        GridManager.Instance.ShowPendingRelicPlacement(_pendingRelic, _hoveredSlot, _rotationSteps);
    }
}
