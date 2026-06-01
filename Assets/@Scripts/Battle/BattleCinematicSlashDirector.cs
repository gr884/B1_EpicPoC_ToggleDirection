using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BattleCinematicSlashDirector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _focusTarget;
    [SerializeField] private Canvas[] _canvasesToDisable;
    [SerializeField] private SpriteRenderer _playerSpriteRenderer;
    [SerializeField] private Animator _playerAnimator;

    [Header("Setup")]
    [SerializeField] private float _setupDuration = 0.45f;
    [SerializeField] private float _targetYaw = -80f;
    [SerializeField] private float _targetFov = 30f;

    [Header("Player Dash")]
    [SerializeField] private float _playerTargetWorldX = 9.5f;
    [SerializeField] private float _playerDashDuration = 0.18f;
    [SerializeField] private Sprite _playerSpriteBeforeDash;
    [SerializeField] private Sprite _playerSpriteAfterDash;

    [Header("Enemy Hit")]
    [SerializeField] private float _hitDelay = 1f;
    [SerializeField] private float _hitLoopDuration = 3f;
    [SerializeField] private float _hitRepeatInterval = 0.34f;
    [SerializeField] private float _enemyShakeAmount = 0.12f;
    [SerializeField] private Color _enemyHitTint = new(1f, 0.15f, 0.12f, 1f);

    [Header("Slash Line")]
    [SerializeField] private Material _lineMaterial;
    [SerializeField] private float _lineWidth = 0.08f;
    [SerializeField] private float _lineExtraLength = 1.5f;
    [SerializeField] private float _lineYOffset = 0.15f;
    [SerializeField] private float _lineHoldDuration = 3f;
    [SerializeField] private float _lineFadeDuration = 0.25f;
    [SerializeField] private int _lineSortingOrder = 100;

    private Coroutine _routine;
    private CinemachinePositionComposer _positionComposer;
    private BattleActorMotionTarget _playerMotionTarget;
    private BattleActorMotionTarget _enemyMotionTarget;
    private SpumEnemyMotionPlayer _enemyMotionPlayer;
    private Transform _playerVisual;
    private Transform _enemyVisual;
    private LineRenderer _lineRenderer;
    private Material _runtimeLineMaterial;
    private bool _hasSnapshot;

    private CameraSnapshot _cameraSnapshot;
    private CinemachineSnapshot _cinemachineSnapshot;
    private ComposerSnapshot _composerSnapshot;
    private TransformSnapshot _focusSnapshot;
    private TransformSnapshot _playerSnapshot;
    private TransformSnapshot _enemySnapshot;
    private CanvasSnapshot[] _canvasSnapshots;
    private PlayerVisualSnapshot _playerVisualSnapshot;
    private SpriteColorSnapshot[] _enemyColorSnapshots;

    public void Play()
    {
        StopAndRestore();

        if (!ResolveReferences())
            return;

        CaptureSnapshot();
        _routine = StartCoroutine(PlayRoutine());
    }

    public void StopAndRestore()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        RestoreSnapshot();
        DestroySlashLine();
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    private bool ResolveReferences()
    {
        if (_cinemachineCamera == null)
            _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_focusTarget == null)
        {
            GameObject focusObject = GameObject.Find("BattleCinematicFocusTarget");
            if (focusObject != null)
                _focusTarget = focusObject.transform;
        }

        BattleManager battleManager = BattleManager.Instance;
        Player player = battleManager != null ? battleManager.Player : null;
        Enemy enemy = battleManager != null ? battleManager.Enemy : null;

        _playerMotionTarget = player != null ? player.MotionTarget : null;
        _enemyMotionTarget = enemy != null ? enemy.MotionTarget : null;
        _enemyMotionPlayer = enemy != null ? enemy.MotionPlayer : null;
        _playerVisual = _playerMotionTarget != null ? _playerMotionTarget.VisualRoot : null;
        _enemyVisual = _enemyMotionTarget != null ? _enemyMotionTarget.VisualRoot : null;

        if (_canvasesToDisable == null || _canvasesToDisable.Length == 0)
            _canvasesToDisable = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (_playerVisual != null)
        {
            if (_playerSpriteRenderer == null)
                _playerSpriteRenderer = _playerVisual.GetComponentInChildren<SpriteRenderer>(true);

            if (_playerAnimator == null)
                _playerAnimator = _playerVisual.GetComponentInChildren<Animator>(true);
        }

        if (_cinemachineCamera != null)
            _positionComposer = _cinemachineCamera.GetComponent<CinemachinePositionComposer>();

        bool valid = _cinemachineCamera != null
            && _mainCamera != null
            && _focusTarget != null
            && _playerVisual != null
            && _enemyVisual != null;

        if (!valid)
            Debug.LogWarning("[BattleCinematicSlashDirector] Missing camera, focus target, player visual, or enemy visual.");

        return valid;
    }

    private void CaptureSnapshot()
    {
        _cameraSnapshot = new CameraSnapshot(_mainCamera);
        _cinemachineSnapshot = new CinemachineSnapshot(_cinemachineCamera);
        _composerSnapshot = new ComposerSnapshot(_positionComposer);
        _focusSnapshot = new TransformSnapshot(_focusTarget);
        _playerSnapshot = new TransformSnapshot(_playerVisual);
        _enemySnapshot = new TransformSnapshot(_enemyVisual);
        _canvasSnapshots = CaptureCanvasSnapshots(_canvasesToDisable);
        _playerVisualSnapshot = new PlayerVisualSnapshot(_playerSpriteRenderer, _playerAnimator);
        _enemyColorSnapshots = CaptureSpriteColors(_enemyVisual);
        _hasSnapshot = true;
    }

    private IEnumerator PlayRoutine()
    {
        DisableCanvases();
        FreezePlayerAnimation();
        PrepareSlashLine();

        yield return SetupCinematicRoutine();
        yield return PlayerDashRoutine();

        if (_hitDelay > 0f)
            yield return new WaitForSeconds(_hitDelay);

        yield return EnemyHitRoutine();

        float remainingLineHold = _lineHoldDuration - _hitDelay - _hitLoopDuration;
        if (remainingLineHold > 0f)
            yield return new WaitForSeconds(remainingLineHold);

        if (_lineFadeDuration > 0f)
            yield return FadeSlashLineRoutine();

        _routine = null;
        RestoreSnapshot();
        DestroySlashLine();
    }

    private IEnumerator SetupCinematicRoutine()
    {
        Transform playerPoint = _playerMotionTarget.AttackPoint;
        Transform enemyPoint = _enemyMotionTarget.AttackPoint;
        Vector3 focusStart = _focusTarget.position;
        Vector3 focusEnd = (playerPoint.position + enemyPoint.position) * 0.5f;

        Quaternion cameraStartRotation = _cinemachineCamera.transform.rotation;
        Quaternion playerStartRotation = _playerVisual.rotation;
        Quaternion enemyStartRotation = _enemyVisual.rotation;

        LensSettings startLens = _cinemachineCamera.Lens;
        _cinemachineCamera.Follow = _focusTarget;
        _cinemachineCamera.LookAt = _focusTarget;

        if (_positionComposer != null)
        {
            _positionComposer.Damping = Vector3.zero;
            _positionComposer.CenterOnActivate = true;
        }

        float duration = Mathf.Max(0.01f, _setupDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            ApplySetupFrame(t, focusStart, focusEnd, cameraStartRotation, playerStartRotation, enemyStartRotation, startLens);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplySetupFrame(1f, focusStart, focusEnd, cameraStartRotation, playerStartRotation, enemyStartRotation, startLens);
    }

    private void ApplySetupFrame(
        float t,
        Vector3 focusStart,
        Vector3 focusEnd,
        Quaternion cameraStartRotation,
        Quaternion playerStartRotation,
        Quaternion enemyStartRotation,
        LensSettings startLens)
    {
        _focusTarget.position = Vector3.Lerp(focusStart, focusEnd, t);
        _cinemachineCamera.transform.rotation = LerpYaw(cameraStartRotation, _targetYaw, t);
        _playerVisual.rotation = LerpYaw(playerStartRotation, _targetYaw, t);
        _enemyVisual.rotation = LerpYaw(enemyStartRotation, _targetYaw, t);

        LensSettings lens = _cinemachineCamera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Perspective;
        lens.FieldOfView = Mathf.Lerp(startLens.FieldOfView, _targetFov, t);
        _cinemachineCamera.Lens = lens;
    }

    private IEnumerator PlayerDashRoutine()
    {
        Vector3 start = _playerVisual.localPosition;
        Vector3 end = new(_playerTargetWorldX, start.y, start.z);
        ConfigureSlashLine(LocalToWorld(_playerVisual, start), LocalToWorld(_playerVisual, end));
        ApplyPlayerSprite(_playerSpriteBeforeDash);

        float duration = Mathf.Max(0.01f, _playerDashDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _playerVisual.localPosition = Vector3.Lerp(start, end, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _playerVisual.localPosition = end;
        ApplyPlayerSprite(_playerSpriteAfterDash);
    }

    private IEnumerator EnemyHitRoutine()
    {
        Vector3 enemyBasePosition = _enemyVisual.position;
        float elapsed = 0f;
        float nextHitTime = 0f;
        float duration = Mathf.Max(0f, _hitLoopDuration);

        while (elapsed < duration)
        {
            if (elapsed >= nextHitTime)
            {
                _enemyMotionPlayer?.PlayHit();
                nextHitTime = elapsed + Mathf.Max(0.05f, _hitRepeatInterval);
            }

            float pulse = Mathf.PingPong(elapsed * 8f, 1f);
            ApplyEnemyTint(pulse);

            Vector2 shake = Random.insideUnitCircle * _enemyShakeAmount;
            _enemyVisual.position = enemyBasePosition + new Vector3(shake.x, shake.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _enemyVisual.position = enemyBasePosition;
        RestoreEnemyColors();
    }

    private void PrepareSlashLine()
    {
        DestroySlashLine();

        GameObject lineObject = new("BattleCinematicSlashLine");
        _lineRenderer = lineObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.numCapVertices = 4;
        _lineRenderer.sortingLayerName = "Default";
        _lineRenderer.sortingOrder = _lineSortingOrder;
        _lineRenderer.material = GetLineMaterial();
        SetLineAlpha(0f);
        lineObject.SetActive(false);
    }

    private void ConfigureSlashLine(Vector3 dashStart, Vector3 dashEnd)
    {
        if (_lineRenderer == null) return;

        float minX = Mathf.Min(dashStart.x, dashEnd.x) - _lineExtraLength;
        float maxX = Mathf.Max(dashStart.x, dashEnd.x) + _lineExtraLength;
        float y = dashStart.y + _lineYOffset;
        float z = dashStart.z;

        _lineRenderer.SetPosition(0, new Vector3(minX, y, z));
        _lineRenderer.SetPosition(1, new Vector3(maxX, y, z));
        _lineRenderer.gameObject.SetActive(true);
        SetLineAlpha(1f);
    }

    private IEnumerator FadeSlashLineRoutine()
    {
        if (_lineRenderer == null)
            yield break;

        float duration = Mathf.Max(0.01f, _lineFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            SetLineAlpha(alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetLineAlpha(0f);
    }

    private Material GetLineMaterial()
    {
        if (_lineMaterial != null)
            return _lineMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Hidden/Internal-Colored");

        if (shader == null)
            return null;

        _runtimeLineMaterial = new Material(shader);
        return _runtimeLineMaterial;
    }

    private void SetLineAlpha(float alpha)
    {
        if (_lineRenderer == null) return;

        Color color = new(1f, 1f, 1f, Mathf.Clamp01(alpha));
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }

    private void ApplyEnemyTint(float t)
    {
        if (_enemyColorSnapshots == null) return;

        foreach (SpriteColorSnapshot snapshot in _enemyColorSnapshots)
        {
            if (snapshot.Renderer == null) continue;
            snapshot.Renderer.color = Color.Lerp(snapshot.Color, _enemyHitTint, t);
        }
    }

    private void RestoreEnemyColors()
    {
        if (_enemyColorSnapshots == null) return;

        foreach (SpriteColorSnapshot snapshot in _enemyColorSnapshots)
        {
            if (snapshot.Renderer != null)
                snapshot.Renderer.color = snapshot.Color;
        }
    }

    private void RestoreSnapshot()
    {
        if (!_hasSnapshot)
            return;

        _cameraSnapshot.Restore();
        _cinemachineSnapshot.Restore();
        _composerSnapshot.Restore();
        _focusSnapshot.Restore();
        _playerSnapshot.Restore();
        _enemySnapshot.Restore();
        RestoreCanvases();
        _playerVisualSnapshot.Restore();
        RestoreEnemyColors();

        _hasSnapshot = false;
    }

    private void DisableCanvases()
    {
        if (_canvasesToDisable == null) return;

        foreach (Canvas canvas in _canvasesToDisable)
        {
            if (canvas != null)
                canvas.gameObject.SetActive(false);
        }
    }

    private void RestoreCanvases()
    {
        if (_canvasSnapshots == null) return;

        foreach (CanvasSnapshot snapshot in _canvasSnapshots)
            snapshot.Restore();
    }

    private void FreezePlayerAnimation()
    {
        if (_playerAnimator != null)
            _playerAnimator.enabled = false;
    }

    private void ApplyPlayerSprite(Sprite sprite)
    {
        if (_playerSpriteRenderer != null && sprite != null)
            _playerSpriteRenderer.sprite = sprite;
    }

    private void DestroySlashLine()
    {
        if (_lineRenderer != null)
        {
            Destroy(_lineRenderer.gameObject);
            _lineRenderer = null;
        }

        if (_runtimeLineMaterial != null)
        {
            Destroy(_runtimeLineMaterial);
            _runtimeLineMaterial = null;
        }
    }

    private static Quaternion LerpYaw(Quaternion startRotation, float targetYaw, float t)
    {
        Vector3 euler = startRotation.eulerAngles;
        euler.y = Mathf.LerpAngle(euler.y, targetYaw, t);
        return Quaternion.Euler(euler);
    }

    private static Vector3 LocalToWorld(Transform transform, Vector3 localPosition)
    {
        return transform.parent != null ? transform.parent.TransformPoint(localPosition) : localPosition;
    }

    private static SpriteColorSnapshot[] CaptureSpriteColors(Transform root)
    {
        if (root == null)
            return System.Array.Empty<SpriteColorSnapshot>();

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteColorSnapshot[] snapshots = new SpriteColorSnapshot[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            snapshots[i] = new SpriteColorSnapshot(renderers[i]);

        return snapshots;
    }

    private static CanvasSnapshot[] CaptureCanvasSnapshots(Canvas[] canvases)
    {
        if (canvases == null)
            return System.Array.Empty<CanvasSnapshot>();

        CanvasSnapshot[] snapshots = new CanvasSnapshot[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
            snapshots[i] = new CanvasSnapshot(canvases[i]);

        return snapshots;
    }

    private readonly struct CameraSnapshot
    {
        private readonly Camera _camera;
        private readonly bool _orthographic;
        private readonly float _fieldOfView;
        private readonly float _orthographicSize;
        private readonly TransformSnapshot _transform;

        public CameraSnapshot(Camera camera)
        {
            _camera = camera;
            _orthographic = camera != null && camera.orthographic;
            _fieldOfView = camera != null ? camera.fieldOfView : 60f;
            _orthographicSize = camera != null ? camera.orthographicSize : 5f;
            _transform = new TransformSnapshot(camera != null ? camera.transform : null);
        }

        public void Restore()
        {
            if (_camera == null) return;

            _camera.orthographic = _orthographic;
            _camera.fieldOfView = _fieldOfView;
            _camera.orthographicSize = _orthographicSize;
            _transform.Restore();
        }
    }

    private readonly struct CinemachineSnapshot
    {
        private readonly CinemachineCamera _camera;
        private readonly LensSettings _lens;
        private readonly CameraTarget _target;
        private readonly TransformSnapshot _transform;

        public CinemachineSnapshot(CinemachineCamera camera)
        {
            _camera = camera;
            _lens = camera != null ? camera.Lens : default;
            _target = camera != null ? camera.Target : default;
            _transform = new TransformSnapshot(camera != null ? camera.transform : null);
        }

        public void Restore()
        {
            if (_camera == null) return;

            _camera.Lens = _lens;
            _camera.Target = _target;
            _transform.Restore();
        }
    }

    private readonly struct ComposerSnapshot
    {
        private readonly CinemachinePositionComposer _composer;
        private readonly float _cameraDistance;
        private readonly float _deadZoneDepth;
        private readonly Vector3 _targetOffset;
        private readonly Vector3 _damping;
        private readonly bool _centerOnActivate;

        public ComposerSnapshot(CinemachinePositionComposer composer)
        {
            _composer = composer;
            _cameraDistance = composer != null ? composer.CameraDistance : 10f;
            _deadZoneDepth = composer != null ? composer.DeadZoneDepth : 0f;
            _targetOffset = composer != null ? composer.TargetOffset : Vector3.zero;
            _damping = composer != null ? composer.Damping : Vector3.one;
            _centerOnActivate = composer == null || composer.CenterOnActivate;
        }

        public void Restore()
        {
            if (_composer == null) return;

            _composer.CameraDistance = _cameraDistance;
            _composer.DeadZoneDepth = _deadZoneDepth;
            _composer.TargetOffset = _targetOffset;
            _composer.Damping = _damping;
            _composer.CenterOnActivate = _centerOnActivate;
        }
    }

    private readonly struct TransformSnapshot
    {
        private readonly Transform _transform;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly Vector3 _scale;

        public TransformSnapshot(Transform transform)
        {
            _transform = transform;
            _position = transform != null ? transform.position : Vector3.zero;
            _rotation = transform != null ? transform.rotation : Quaternion.identity;
            _scale = transform != null ? transform.localScale : Vector3.one;
        }

        public void Restore()
        {
            if (_transform == null) return;

            _transform.position = _position;
            _transform.rotation = _rotation;
            _transform.localScale = _scale;
        }
    }

    private readonly struct CanvasSnapshot
    {
        private readonly GameObject _gameObject;
        private readonly bool _activeSelf;

        public CanvasSnapshot(Canvas canvas)
        {
            _gameObject = canvas != null ? canvas.gameObject : null;
            _activeSelf = _gameObject != null && _gameObject.activeSelf;
        }

        public void Restore()
        {
            if (_gameObject != null)
                _gameObject.SetActive(_activeSelf);
        }
    }

    private readonly struct PlayerVisualSnapshot
    {
        private readonly SpriteRenderer _spriteRenderer;
        private readonly Sprite _sprite;
        private readonly Animator _animator;
        private readonly bool _animatorEnabled;

        public PlayerVisualSnapshot(SpriteRenderer spriteRenderer, Animator animator)
        {
            _spriteRenderer = spriteRenderer;
            _sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            _animator = animator;
            _animatorEnabled = animator != null && animator.enabled;
        }

        public void Restore()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = _sprite;

            if (_animator != null)
                _animator.enabled = _animatorEnabled;
        }
    }

    private readonly struct SpriteColorSnapshot
    {
        public readonly SpriteRenderer Renderer;
        public readonly Color Color;

        public SpriteColorSnapshot(SpriteRenderer renderer)
        {
            Renderer = renderer;
            Color = renderer != null ? renderer.color : Color.white;
        }
    }
}
