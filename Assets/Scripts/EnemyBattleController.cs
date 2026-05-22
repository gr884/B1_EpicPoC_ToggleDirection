using TMPro;
using UnityEngine;

public class EnemyBattleController : MonoBehaviour
{
    [Header("Enemy Data")]
    [SerializeField] private EnemyDataSO enemyData;

    [Header("Runtime Components")]
    [SerializeField] private BattleActorView actorView;
    [SerializeField] private TMP_Text hpText;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool createMissingVisual = true;
    [SerializeField] private bool createMissingHpText = true;
    [SerializeField] private Vector3 hpTextLocalPosition = new(0f, 1.3f, 0f);
    [SerializeField] private int sortingOrder = 10;

    [Header("Health Bar")]
    [SerializeField] private bool createMissingHealthBar = true;
    [SerializeField] private Sprite hpBarSprite;
    [SerializeField] private string hpBarResourcePath = "EnemyHealthBarSprite";
    [SerializeField] private Transform hpBarFill;
    [SerializeField] private Vector3 hpBarLocalPosition = new(0f, 1.05f, 0f);
    [SerializeField] private Vector2 hpBarSize = new(10f, 1f);
    [SerializeField] private Color hpBarBackgroundColor = new(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color hpBarFillColor = new(0.85f, 0.12f, 0.12f, 1f);

    private Sprite generatedHealthBarSprite;

    public EnemyDataSO EnemyData => enemyData;
    public BattleActorView ActorView => actorView;

    private void Awake()
    {
        ResolveRefs();
    }

    private void OnDestroy()
    {
        if (generatedHealthBarSprite != null)
        {
            Destroy(generatedHealthBarSprite);
        }
    }

    public void Initialize(EnemyDataSO data)
    {
        enemyData = data;
        ResolveRefs();

        if (enemyData == null)
        {
            Debug.LogError("EnemyBattleController: Missing EnemyDataSO.");
            return;
        }

        EnsureVisual();
        EnsureHpText();
        EnsureHealthBar();

        if (actorView != null)
        {
            actorView.Configure(enemyData.displayName, enemyData.maxHp, hpText);
            actorView.SetHealthBarFill(hpBarFill);
            actorView.CacheBasePose();
            actorView.ResetHp();
        }

        Debug.Log($"EnemyBattleController: Initialize enemy {enemyData.displayName}. hp={enemyData.maxHp}");
    }

    private void ResolveRefs()
    {
        if (actorView == null)
        {
            actorView = GetComponent<BattleActorView>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (hpText == null)
        {
            hpText = GetComponentInChildren<TMP_Text>();
        }

        if (hpBarFill == null)
        {
            Transform foundFill = transform.Find("HealthBar/Fill");
            hpBarFill = foundFill;
        }

        if (hpBarSprite == null && !string.IsNullOrWhiteSpace(hpBarResourcePath))
        {
            hpBarSprite = Resources.Load<Sprite>(hpBarResourcePath);
        }
    }

    private void EnsureVisual()
    {
        if (spriteRenderer == null && createMissingVisual)
        {
            GameObject visualObject = new("Visual", typeof(SpriteRenderer));
            visualObject.transform.SetParent(transform, false);
            spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = enemyData.sprite;
        spriteRenderer.sortingOrder = sortingOrder;
        FitSpriteToWorldSize(spriteRenderer, enemyData.worldSize);
    }

    private void EnsureHpText()
    {
        if (hpText != null || !createMissingHpText)
        {
            return;
        }

        GameObject textObject = new("HpText", typeof(TextMeshPro));
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = hpTextLocalPosition;

        hpText = textObject.GetComponent<TMP_Text>();
        hpText.fontSize = 3f;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = Color.white;
        hpText.enableAutoSizing = false;
    }

    private void EnsureHealthBar()
    {
        if (hpBarFill != null || !createMissingHealthBar)
        {
            return;
        }

        GameObject rootObject = new("HealthBar");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = hpBarLocalPosition;

        Sprite barSprite = hpBarSprite != null ? hpBarSprite : GetHealthBarSprite();

        GameObject backgroundObject = new("Background", typeof(SpriteRenderer));
        backgroundObject.transform.SetParent(rootObject.transform, false);
        backgroundObject.transform.localScale = new Vector3(hpBarSize.x, hpBarSize.y, 1f);

        SpriteRenderer background = backgroundObject.GetComponent<SpriteRenderer>();
        background.sprite = barSprite;
        background.color = hpBarBackgroundColor;
        background.sortingOrder = sortingOrder + 1;

        GameObject fillObject = new("Fill", typeof(SpriteRenderer));
        fillObject.transform.SetParent(rootObject.transform, false);
        fillObject.transform.localScale = new Vector3(hpBarSize.x * 0.94f, hpBarSize.y * 0.55f, 1f);

        SpriteRenderer fill = fillObject.GetComponent<SpriteRenderer>();
        fill.sprite = barSprite;
        fill.color = hpBarFillColor;
        fill.sortingOrder = sortingOrder + 2;

        hpBarFill = fillObject.transform;
    }

    private Sprite GetHealthBarSprite()
    {
        if (generatedHealthBarSprite != null)
        {
            return generatedHealthBarSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        generatedHealthBarSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        generatedHealthBarSprite.name = "EnemyHealthBarSprite";
        return generatedHealthBarSprite;
    }

    private static void FitSpriteToWorldSize(SpriteRenderer targetRenderer, Vector2 targetSize)
    {
        if (targetRenderer == null || targetRenderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = targetRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float targetWidth = Mathf.Max(0.01f, targetSize.x);
        float targetHeight = Mathf.Max(0.01f, targetSize.y);
        float scale = Mathf.Min(targetWidth / spriteSize.x, targetHeight / spriteSize.y);
        targetRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
