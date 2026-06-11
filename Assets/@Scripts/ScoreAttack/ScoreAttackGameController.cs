using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreAttackGameController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform _gridRoot;
    [SerializeField] private RectTransform _pieceRoot;
    [SerializeField] private RectTransform _handRoot;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _gameOverScoreText;
    [SerializeField] private CanvasGroup _gameOverPanel;

    [Header("Piece Pool")]
    [SerializeField] private List<ScorePieceData> _piecePool = new();
    [SerializeField, Min(0)] private int _threeCellWeight = 7;
    [SerializeField, Min(0)] private int _fourCellWeight = 3;

    [Header("Board")]
    [SerializeField, Min(1)] private int _rows = 7;
    [SerializeField, Min(1)] private int _columns = 7;
    [SerializeField] private Vector2 _cellSize = new(72f, 72f);
    [SerializeField] private Vector2 _cellSpacing = new(4f, 4f);
    [SerializeField] private Color _cellColor = new(0.08f, 0.12f, 0.16f, 0.92f);

    [Header("Hand")]
    [SerializeField, Min(1)] private int _handSize = 4;
    [SerializeField] private Vector2 _handPieceSize = new(130f, 160f);

    [Header("Piece Colors")]
    [SerializeField] private List<Color> _pieceColors = new()
    {
        new Color(0.12f, 0.82f, 1f, 1f),
        new Color(1f, 0.38f, 0.28f, 1f),
        new Color(0.35f, 1f, 0.42f, 1f),
        new Color(1f, 0.78f, 0.16f, 1f),
        new Color(0.72f, 0.38f, 1f, 1f),
        new Color(1f, 0.3f, 0.72f, 1f),
        new Color(0.2f, 0.48f, 1f, 1f),
        new Color(0.2f, 1f, 0.72f, 1f),
        new Color(1f, 0.56f, 0.38f, 1f),
        new Color(0.68f, 0.9f, 0.18f, 1f),
        new Color(0.94f, 0.22f, 1f, 1f),
        new Color(0.55f, 0.78f, 0.92f, 1f)
    };

    [Header("Scoring")]
    [SerializeField] private int _placementScore = 10;
    [SerializeField] private int _toggleScore = 10;
    [SerializeField] private int _destroyBaseScore = 100;
    [SerializeField, Min(0f)] private float _waveDelay = 0.12f;

    private readonly Dictionary<Vector2Int, ScoreGridCellView> _cells = new();
    private readonly Dictionary<Vector2Int, ScorePieceRuntime> _occupancy = new();
    private readonly List<ScorePieceRuntime> _pieces = new();
    private readonly List<ScoreHandPieceView> _hand = new();

    private ScoreHandPieceView _selectedHandPiece;
    private ScoreGridCellView _hoveredCell;
    private bool _isResolving;
    private bool _isGameOver;
    private int _score;
    private int _destroyedThisChain;

    private void Start()
    {
        if (!ValidateReferences()) return;
        BuildBoard();
        BuildInitialHand();
        SetGameOverVisible(false);
        RefreshScore();
        SelectFirstPlayablePiece();
    }

    private void Update()
    {
        if (_isResolving || _isGameOver || _selectedHandPiece == null) return;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            _selectedHandPiece.Rotate(wheel > 0f ? 1 : -1);
            RefreshPlacementPreview();
        }

        if (Input.GetMouseButtonDown(1))
            CancelSelection();
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        _isResolving = false;
        _isGameOver = false;
        _score = 0;
        _destroyedThisChain = 0;

        foreach (ScorePieceRuntime piece in _pieces)
            if (piece?.View != null)
                Destroy(piece.View.gameObject);
        _pieces.Clear();
        _occupancy.Clear();

        foreach (ScoreHandPieceView handPiece in _hand)
            if (handPiece != null)
                Destroy(handPiece.gameObject);
        _hand.Clear();
        _selectedHandPiece = null;

        SetGameOverVisible(false);
        RefreshScore();
        BuildInitialHand();
        SelectFirstPlayablePiece();
    }

    private bool ValidateReferences()
    {
        if (_gridRoot == null || _handRoot == null)
        {
            Debug.LogError("[ScoreAttack] Grid Root and Hand Root are required.", this);
            enabled = false;
            return false;
        }

        if (_piecePool == null || _piecePool.Count == 0)
        {
            Debug.LogError("[ScoreAttack] Add at least one ScorePieceData to Piece Pool.", this);
            enabled = false;
            return false;
        }

        _piecePool.RemoveAll(piece => piece == null);
        return _piecePool.Count > 0;
    }

    private void BuildBoard()
    {
        for (int i = _gridRoot.childCount - 1; i >= 0; i--)
            Destroy(_gridRoot.GetChild(i).gameObject);
        _cells.Clear();

        GridLayoutGroup layout = _gridRoot.GetComponent<GridLayoutGroup>();
        if (layout == null)
            layout = _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = _cellSize;
        layout.spacing = _cellSpacing;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = _columns;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        _gridRoot.sizeDelta = new Vector2(
            _columns * _cellSize.x + (_columns - 1) * _cellSpacing.x,
            _rows * _cellSize.y + (_rows - 1) * _cellSpacing.y);

        for (int rowFromTop = 0; rowFromTop < _rows; rowFromTop++)
        {
            for (int x = 0; x < _columns; x++)
            {
                int y = _rows - 1 - rowFromTop;
                GameObject obj = new(
                    $"Cell_{x}_{y}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(ScoreGridCellView));
                obj.transform.SetParent(_gridRoot, false);
                ScoreGridCellView cell = obj.GetComponent<ScoreGridCellView>();
                cell.Initialize(new Vector2Int(x, y), _cellColor, OnCellClicked, OnCellHovered);
                _cells[cell.Position] = cell;
            }
        }

        if (_pieceRoot == null)
            _pieceRoot = CreatePieceRoot();
        ConfigurePieceRoot();
    }

    private RectTransform CreatePieceRoot()
    {
        GameObject obj = new("ScorePieceRoot", typeof(RectTransform));
        RectTransform root = obj.GetComponent<RectTransform>();
        root.SetParent(_gridRoot.parent, false);
        root.SetSiblingIndex(_gridRoot.GetSiblingIndex() + 1);
        return root;
    }

    private void ConfigurePieceRoot()
    {
        _pieceRoot.anchorMin = _gridRoot.anchorMin;
        _pieceRoot.anchorMax = _gridRoot.anchorMax;
        _pieceRoot.pivot = _gridRoot.pivot;
        _pieceRoot.anchoredPosition = _gridRoot.anchoredPosition;
        _pieceRoot.sizeDelta = new Vector2(
            _columns * _cellSize.x + (_columns - 1) * _cellSpacing.x,
            _rows * _cellSize.y + (_rows - 1) * _cellSpacing.y);
        _pieceRoot.SetAsLastSibling();
    }

    private void BuildInitialHand()
    {
        HorizontalLayoutGroup layout = _handRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = _handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;

        while (_hand.Count < _handSize)
            AddRandomHandPiece();
        RefreshHandRootSize();
    }

    private void AddRandomHandPiece()
    {
        if (_piecePool.Count == 0) return;

        ScorePieceData data = ChooseRandomPieceData();
        GameObject obj = new(
            $"Hand_{data.name}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement),
            typeof(ScoreHandPieceView));
        obj.transform.SetParent(_handRoot, false);

        LayoutElement layout = obj.GetComponent<LayoutElement>();
        layout.preferredWidth = _handPieceSize.x;
        layout.preferredHeight = _handPieceSize.y;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = _handPieceSize;

        ScoreHandPieceView view = obj.GetComponent<ScoreHandPieceView>();
        view.Initialize(data, SelectHandPiece);
        _hand.Add(view);
        RefreshHandRootSize();
    }

    private ScorePieceData ChooseRandomPieceData()
    {
        List<ScorePieceData> threeCellPieces = new();
        List<ScorePieceData> fourCellPieces = new();

        foreach (ScorePieceData piece in _piecePool)
        {
            int occupiedCount = 0;
            foreach (Vector2Int unused in piece.GetUniqueOccupiedOffsets())
                occupiedCount++;

            if (occupiedCount == 3)
                threeCellPieces.Add(piece);
            else if (occupiedCount == 4)
                fourCellPieces.Add(piece);
        }

        int threeWeight = threeCellPieces.Count > 0 ? _threeCellWeight : 0;
        int fourWeight = fourCellPieces.Count > 0 ? _fourCellWeight : 0;
        int totalWeight = threeWeight + fourWeight;
        if (totalWeight <= 0)
            return _piecePool[Random.Range(0, _piecePool.Count)];

        List<ScorePieceData> selectedGroup =
            Random.Range(0, totalWeight) < threeWeight
                ? threeCellPieces
                : fourCellPieces;
        return selectedGroup[Random.Range(0, selectedGroup.Count)];
    }

    private void SelectHandPiece(ScoreHandPieceView piece)
    {
        if (_isResolving || _isGameOver || piece == null) return;
        _selectedHandPiece = piece;
        foreach (ScoreHandPieceView handPiece in _hand)
            handPiece.SetSelected(handPiece == piece);
        RefreshPlacementPreview();
    }

    private void CancelSelection()
    {
        _selectedHandPiece = null;
        foreach (ScoreHandPieceView handPiece in _hand)
            handPiece.SetSelected(false);
        ClearPlacementPreview();
    }

    private void OnCellClicked(ScoreGridCellView cell)
    {
        if (_isResolving || _isGameOver || _selectedHandPiece?.Data == null || cell == null)
            return;
        if (!CanPlace(_selectedHandPiece.Data, cell.Position, _selectedHandPiece.QuarterTurns))
            return;

        StartCoroutine(PlaceAndResolve(_selectedHandPiece, cell.Position));
    }

    private void OnCellHovered(ScoreGridCellView cell, bool entered)
    {
        if (entered)
            _hoveredCell = cell;
        else if (_hoveredCell == cell)
            _hoveredCell = null;
        RefreshPlacementPreview();
    }

    private IEnumerator PlaceAndResolve(ScoreHandPieceView handPiece, Vector2Int anchor)
    {
        _isResolving = true;
        ClearPlacementPreview();

        ScorePieceRuntime piece = new(
            handPiece.Data,
            anchor,
            handPiece.QuarterTurns,
            ChoosePlacementColor(),
            handPiece.SignalOffsets);
        RegisterPiece(piece);
        _score += _placementScore;
        RefreshScore();

        _hand.Remove(handPiece);
        RefreshHandRootSize();
        if (_selectedHandPiece == handPiece)
            _selectedHandPiece = null;
        Destroy(handPiece.gameObject);
        AddRandomHandPiece();

        _destroyedThisChain = 0;
        yield return ResolveSignalChain(new List<ScorePieceRuntime> { piece });

        _isResolving = false;
        if (!HasAnyPlayableHandPiece())
        {
            EndGame();
            yield break;
        }

        SelectFirstPlayablePiece();
    }

    private IEnumerator ResolveSignalChain(List<ScorePieceRuntime> emitters)
    {
        List<ScorePieceRuntime> currentWave = emitters;
        while (currentWave.Count > 0)
        {
            HashSet<ScorePieceRuntime> targets = new();
            foreach (ScorePieceRuntime emitter in currentWave)
            {
                if (emitter == null || !emitter.IsOn) continue;
                foreach (Vector2Int signalPosition in emitter.SignalPositions)
                {
                    if (_occupancy.TryGetValue(signalPosition, out ScorePieceRuntime target)
                        && target != emitter)
                        targets.Add(target);
                }
            }

            if (targets.Count == 0) yield break;
            if (_waveDelay > 0f)
                yield return new WaitForSeconds(_waveDelay);

            List<ScorePieceRuntime> nextWave = new();
            List<ScorePieceRuntime> destroyed = new();
            foreach (ScorePieceRuntime target in targets)
            {
                bool wasOn = target.IsOn;
                bool isDestroyed = target.Toggle();
                _score += _toggleScore;

                if (isDestroyed)
                {
                    destroyed.Add(target);
                }
                else
                {
                    target.View?.RefreshState();
                    if (wasOn == false && target.IsOn)
                        nextWave.Add(target);
                }
            }

            foreach (ScorePieceRuntime target in destroyed)
            {
                _destroyedThisChain++;
                _score += _destroyBaseScore * _destroyedThisChain;
                UnregisterPiece(target);
            }
            RefreshScore();
            currentWave = nextWave;
        }
    }

    private void RegisterPiece(ScorePieceRuntime piece)
    {
        _pieces.Add(piece);
        foreach (Vector2Int position in piece.OccupiedPositions)
            _occupancy[position] = piece;

        GameObject obj = new($"Piece_{piece.Data.name}", typeof(RectTransform), typeof(ScorePieceView));
        obj.transform.SetParent(_pieceRoot, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        ScorePieceView view = obj.GetComponent<ScorePieceView>();
        piece.View = view;
        view.Initialize(piece, _pieceRoot, _cellSize, _cellSpacing);
    }

    private void UnregisterPiece(ScorePieceRuntime piece)
    {
        if (piece == null) return;
        foreach (Vector2Int position in piece.OccupiedPositions)
            if (_occupancy.TryGetValue(position, out ScorePieceRuntime occupant) && occupant == piece)
                _occupancy.Remove(position);
        _pieces.Remove(piece);
        if (piece.View != null)
            Destroy(piece.View.gameObject);
    }

    private bool CanPlace(ScorePieceData data, Vector2Int anchor, int quarterTurns)
    {
        if (data == null) return false;
        foreach (Vector2Int offset in data.GetRotatedOccupiedOffsets(quarterTurns))
        {
            Vector2Int position = anchor + offset;
            if (!_cells.ContainsKey(position) || _occupancy.ContainsKey(position))
                return false;
        }
        return true;
    }

    private bool HasAnyPlacement(ScorePieceData data, out int validQuarterTurns)
    {
        for (int turns = 0; turns < 4; turns++)
        {
            foreach (Vector2Int position in _cells.Keys)
            {
                if (!CanPlace(data, position, turns)) continue;
                validQuarterTurns = turns;
                return true;
            }
        }
        validQuarterTurns = 0;
        return false;
    }

    private bool HasAnyPlayableHandPiece()
    {
        foreach (ScoreHandPieceView handPiece in _hand)
            if (handPiece != null && HasAnyPlacement(handPiece.Data, out _))
                return true;
        return false;
    }

    private void SelectFirstPlayablePiece()
    {
        foreach (ScoreHandPieceView handPiece in _hand)
        {
            if (handPiece != null && HasAnyPlacement(handPiece.Data, out int turns))
            {
                while (handPiece.QuarterTurns != turns)
                    handPiece.Rotate(1);
                SelectHandPiece(handPiece);
                return;
            }
        }
    }

    private void RefreshPlacementPreview()
    {
        ClearPlacementPreview();
        if (_hoveredCell == null || _selectedHandPiece?.Data == null) return;

        bool valid = CanPlace(
            _selectedHandPiece.Data,
            _hoveredCell.Position,
            _selectedHandPiece.QuarterTurns);
        foreach (Vector2Int offset in _selectedHandPiece.Data.GetRotatedOccupiedOffsets(_selectedHandPiece.QuarterTurns))
            if (_cells.TryGetValue(_hoveredCell.Position + offset, out ScoreGridCellView cell))
                cell.SetPreview(true, valid);
        Color previewColor = ChoosePlacementColor();
        foreach (Vector2Int signalOffset in _selectedHandPiece.SignalOffsets)
        {
            Vector2Int signal = ScorePieceData.RotateOffset(
                signalOffset,
                _selectedHandPiece.QuarterTurns);
            if (_cells.TryGetValue(_hoveredCell.Position + signal, out ScoreGridCellView cell))
                cell.SetSignalPreview(true, previewColor);
        }
    }

    private void ClearPlacementPreview()
    {
        foreach (ScoreGridCellView cell in _cells.Values)
        {
            cell.SetPreview(false, false);
            cell.SetSignalPreview(false, Color.clear);
        }
    }

    private Color ChoosePlacementColor()
    {
        if (_pieceColors == null || _pieceColors.Count == 0)
            return Color.white;

        int[] usage = new int[_pieceColors.Count];
        foreach (ScorePieceRuntime piece in _pieces)
        {
            int index = FindColorIndex(piece.Color);
            if (index >= 0)
                usage[index]++;
        }

        int leastUsedIndex = 0;
        for (int i = 0; i < usage.Length; i++)
        {
            if (usage[i] == 0)
                return _pieceColors[i];
            if (usage[i] < usage[leastUsedIndex])
                leastUsedIndex = i;
        }
        return _pieceColors[leastUsedIndex];
    }

    private void RefreshHandRootSize()
    {
        if (_handRoot == null) return;

        HorizontalLayoutGroup layout = _handRoot.GetComponent<HorizontalLayoutGroup>();
        float spacing = layout != null ? layout.spacing : 0f;
        int count = Mathf.Max(0, _hand.Count);
        float width = count > 0
            ? count * _handPieceSize.x + (count - 1) * spacing
            : 0f;

        _handRoot.pivot = new Vector2(0.5f, _handRoot.pivot.y);
        _handRoot.sizeDelta = new Vector2(width, _handPieceSize.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_handRoot);
    }

    private int FindColorIndex(Color color)
    {
        for (int i = 0; i < _pieceColors.Count; i++)
        {
            Color candidate = _pieceColors[i];
            if (Mathf.Abs(candidate.r - color.r) < 0.01f
                && Mathf.Abs(candidate.g - color.g) < 0.01f
                && Mathf.Abs(candidate.b - color.b) < 0.01f)
                return i;
        }
        return -1;
    }

    private void EndGame()
    {
        _isGameOver = true;
        _selectedHandPiece = null;
        foreach (ScoreHandPieceView handPiece in _hand)
            handPiece.SetSelected(false);
        if (_gameOverScoreText != null)
            _gameOverScoreText.text = $"Score  {_score:N0}";
        SetGameOverVisible(true);
    }

    private void RefreshScore()
    {
        if (_scoreText != null)
            _scoreText.text = $"Score  {_score:N0}";
    }

    private void SetGameOverVisible(bool visible)
    {
        if (_gameOverPanel == null) return;
        _gameOverPanel.alpha = visible ? 1f : 0f;
        _gameOverPanel.interactable = visible;
        _gameOverPanel.blocksRaycasts = visible;
    }
}
