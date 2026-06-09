using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIForwardMotionEffect : MonoBehaviour
{
    [System.Serializable]
    private struct ForwardMotionStep
    {
        public float y;
        public float z;
        public float moveDuration;
    }

    [Header("Target")]
    [SerializeField] private RectTransform targetImage;
    [SerializeField] private Material blurMaterial;

    [Header("Motion")]
    [SerializeField] private float startScale = 1.0f;
    [SerializeField] private float endScale = 1.25f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private ForwardMotionStep step1 = new() { y = 70f, z = -100f, moveDuration = 0.3f };
    [SerializeField] private ForwardMotionStep step2 = new() { y = 120f, z = -150f, moveDuration = 0.3f };
    [SerializeField] private ForwardMotionStep step3 = new() { y = 30f, z = -225f, moveDuration = 0.3f };
    [SerializeField] private ForwardMotionStep step4 = new() { y = 70f, z = -300f, moveDuration = 0.3f };

    [Header("Blur")]
    [SerializeField] private float maxBlurStrength = 0.035f;

    [Header("Transition")]
    [SerializeField] private CanvasGroup fallbackGroup;
    [SerializeField] private float transitionFadeDuration = 0.5f;

    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalPosition;
    private CanvasGroup movingGroup;
    private Tween motionTween;

    private void Awake()
    {
        if (targetImage != null)
        {
            originalAnchoredPosition = targetImage.anchoredPosition;
            originalLocalPosition = targetImage.localPosition;
        }

        ResolveMovingGroup();
        ResetFadeGroups();
        SetBlur(0f);
    }

    void OnEnable()
    {
        PlayForwardMotion();   
    }
    public void PlayForwardMotion()
    {
        motionTween?.Kill();

        motionTween = ForwardRoutine();
    }

    public void StopForwardMotion()
    {
        motionTween?.Kill();
        motionTween = null;

        if (targetImage != null)
        {
            targetImage.localScale = Vector3.one * startScale;
            targetImage.anchoredPosition = originalAnchoredPosition;
            targetImage.localPosition = originalLocalPosition;
        }

        ResetFadeGroups();
        SetBlur(0f);
    }

    private Tween ForwardRoutine()
    {
        if (targetImage == null)
            return null;

        Vector3 startPosition = originalLocalPosition;
        Vector3 firstPosition = BuildStepPosition(startPosition, step1);
        Vector3 secondPosition = BuildStepPosition(startPosition, step2);
        Vector3 thirdPosition = BuildStepPosition(startPosition, step3);
        Vector3 fourthPosition = BuildStepPosition(startPosition, step4);

        targetImage.localScale = Vector3.one * startScale;
        targetImage.localPosition = startPosition;
        ResetFadeGroups();
        SetBlur(0f);

        Tween step4Tween = CreateForwardSegment(thirdPosition, fourthPosition, 0.75f, 1f, step4.moveDuration);
        Tween transitionTween = CreateTransitionFade();

        Sequence sequence = DOTween.Sequence()
            .SetEase(Ease.Linear)
            .Append(CreateForwardSegment(startPosition, firstPosition, 0f, 0.25f, step1.moveDuration))
            .Append(CreateForwardSegment(firstPosition, secondPosition, 0.25f, 0.5f, step2.moveDuration))
            .Append(CreateForwardSegment(secondPosition, thirdPosition, 0.5f, 0.75f, step3.moveDuration))
            .Append(step4Tween)
            .Join(transitionTween)
            .OnComplete(() =>
            {
                targetImage.localScale = Vector3.one * startScale;
                targetImage.anchoredPosition = originalAnchoredPosition;
                targetImage.localPosition = originalLocalPosition;
                ResetFadeGroups();
                SetBlur(0f);
                motionTween = null;
            });

        return sequence;
    }

    private Tween CreateForwardSegment(Vector3 startPosition, Vector3 endPosition, float totalStartT, float totalEndT, float moveDuration)
    {
        return DOTween.To(() => 0f, value =>
        {
            float t = Mathf.Clamp01(value);
            float totalT = Mathf.Lerp(totalStartT, totalEndT, t);
            float curved = zoomCurve.Evaluate(totalT);

            float scale = Mathf.Lerp(startScale, endScale, curved);
            targetImage.localScale = Vector3.one * scale;

            Vector3 basePosition = Vector3.LerpUnclamped(startPosition, endPosition, t);
            targetImage.localPosition = basePosition;

            float blur = Mathf.Sin(t * Mathf.PI) * maxBlurStrength;
            SetBlur(blur);
        }, 1f, Mathf.Max(0.01f, moveDuration))
            .SetEase(Ease.Linear);
    }

    private Tween CreateTransitionFade()
    {
        Sequence sequence = DOTween.Sequence();

        ResolveMovingGroup();

        float fadeDuration = Mathf.Max(0.01f, transitionFadeDuration);

        sequence.OnStart(() =>
        {
            if (fallbackGroup != null)
            {
                fallbackGroup.gameObject.SetActive(true);
                fallbackGroup.alpha = 0f;
            }
        });

        if (movingGroup != null)
            sequence.Join(movingGroup.DOFade(0f, fadeDuration));

        if (fallbackGroup != null)
            sequence.Join(fallbackGroup.DOFade(1f, fadeDuration));

        return sequence;
    }

    private static Vector3 BuildStepPosition(Vector3 basePosition, ForwardMotionStep step)
    {
        return new Vector3(basePosition.x, step.y, step.z);
    }

    private void ResolveMovingGroup()
    {
        if (targetImage == null)
            return;

        if (movingGroup == null && !targetImage.TryGetComponent(out movingGroup))
            movingGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
    }

    private void ResetFadeGroups()
    {
        if (movingGroup != null)
            movingGroup.alpha = 1f;

        if (fallbackGroup != null)
        {
            fallbackGroup.alpha = 0f;
            fallbackGroup.gameObject.SetActive(false);
        }
    }

    private void SetBlur(float value)
    {
        if (blurMaterial != null)
            blurMaterial.SetFloat("_BlurStrength", value);
    }
}
