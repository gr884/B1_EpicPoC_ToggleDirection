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
    [SerializeField] private ForwardMotionStep step1 = new() { y = 70f, z = 0f, moveDuration = 0.5f };
    [SerializeField] private ForwardMotionStep step2 = new() { y = 150f, z = -150f, moveDuration = 0.5f };
    [SerializeField] private ForwardMotionStep step3 = new() { y = 70f, z = -150f, moveDuration = 0.5f };
    [SerializeField] private ForwardMotionStep step4 = new() { y = 150f, z = -250f, moveDuration = 0.5f };

    [Header("Blur")]
    [SerializeField] private float maxBlurStrength = 0.035f;

    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalPosition;
    private Tween motionTween;

    private void Awake()
    {
        if (targetImage != null)
        {
            originalAnchoredPosition = targetImage.anchoredPosition;
            originalLocalPosition = targetImage.localPosition;
        }

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
        SetBlur(0f);

        Sequence sequence = DOTween.Sequence()
            .SetEase(Ease.Linear)
            .Append(CreateForwardSegment(startPosition, firstPosition, 0f, 0.25f, step1.moveDuration))
            .Append(CreateForwardSegment(firstPosition, secondPosition, 0.25f, 0.5f, step2.moveDuration))
            .Append(CreateForwardSegment(secondPosition, thirdPosition, 0.5f, 0.75f, step3.moveDuration))
            .Append(CreateForwardSegment(thirdPosition, fourthPosition, 0.75f, 1f, step4.moveDuration))
            .OnComplete(() =>
            {
                targetImage.localScale = Vector3.one * endScale;
                targetImage.localPosition = fourthPosition;
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

    private static Vector3 BuildStepPosition(Vector3 basePosition, ForwardMotionStep step)
    {
        return new Vector3(basePosition.x, step.y, step.z);
    }

    private void SetBlur(float value)
    {
        if (blurMaterial != null)
            blurMaterial.SetFloat("_BlurStrength", value);
    }
}
