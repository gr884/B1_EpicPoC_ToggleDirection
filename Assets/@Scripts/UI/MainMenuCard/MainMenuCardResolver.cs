using System.Collections;
using UnityEngine;

public class MainMenuCardResolver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MainMenuGridController _grid;
    [SerializeField] private MainMenuHandController _hand;
    [SerializeField] private UI_MainMenu _mainMenu;

    [Header("Feedback")]
    [SerializeField, Min(0f)] private float _feedbackDuration = 0.25f;

    private bool _isResolving;
    private bool _subscribed;

    public void Initialize()
    {
        ResolveRefs();
        Subscribe();
    }

    private void OnEnable()
    {
        ResolveRefs();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveRefs()
    {
        if (_grid == null)
            _grid = GetComponent<MainMenuGridController>();
        if (_hand == null)
            _hand = GetComponent<MainMenuHandController>();
        if (_mainMenu == null)
            _mainMenu = GetComponentInParent<UI_MainMenu>();
    }

    private void Subscribe()
    {
        if (_subscribed || _grid == null) return;

        _grid.OnHandCardPlaced += ResolvePlacedCard;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _grid == null) return;

        _grid.OnHandCardPlaced -= ResolvePlacedCard;
        _subscribed = false;
    }

    private void ResolvePlacedCard(MainMenuCardView placedCard)
    {
        if (placedCard == null) return;
        StartCoroutine(ResolvePlacedCardRoutine(placedCard));
    }

    private IEnumerator ResolvePlacedCardRoutine(MainMenuCardView placedCard)
    {
        if (_isResolving)
        {
            ReturnPlacedCard(placedCard);
            yield break;
        }

        _isResolving = true;

        MainMenuGridSlot origin = placedCard.CurrentSlot;
        MainMenuGridSlot targetSlot = _grid != null ? _grid.GetNeighbor(origin, placedCard.Direction) : null;
        MainMenuCardView targetCard = targetSlot != null ? targetSlot.OccupiedCard : null;

        if (targetCard != null && targetCard.IsFixed)
        {
            targetCard.SetActivated(true);
            yield return targetCard.PlayActivationFeedback(_feedbackDuration);

            if (targetCard.Action == MainMenuCardAction.GameStart)
            {
                _mainMenu?.StartGameFromMenuCard();
                _isResolving = false;
                yield break;
            }

            Debug.Log($"[MainMenuCardResolver] 아직 구현되지 않은 메뉴 카드: {targetCard.Action}");
            targetCard.SetActivated(false);
        }

        ReturnPlacedCard(placedCard);
        _isResolving = false;
    }

    private void ReturnPlacedCard(MainMenuCardView card)
    {
        if (card == null) return;

        MainMenuGridSlot slot = card.CurrentSlot;
        if (slot != null && slot.OccupiedCard == card)
            slot.ClearCard();

        _hand?.ReturnCard(card);
    }
}
