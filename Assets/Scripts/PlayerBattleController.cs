using TMPro;
using UnityEngine;

public class PlayerBattleController : MonoBehaviour
{
    [Header("Player Data")]
    [SerializeField] private string displayName = "Player";
    [Min(1)]
    [SerializeField] private int maxHp = 20;

    [Header("Runtime Components")]
    [SerializeField] private BattleActorView actorView;
    [SerializeField] private TMP_Text hpText;

    [Header("Deck")]
    [SerializeField] private UserCardPool cardPool;

    [Header("Visual")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Vector2 worldSize = new(2f, 2f);
    [SerializeField] private Color fallbackColor = new(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private bool createMissingVisual = true;
    [SerializeField] private bool createMissingHpText = true;
    [SerializeField] private Vector3 hpTextLocalPosition = new(0f, 1.3f, 0f);

    private Sprite generatedFallbackSprite;

    public BattleActorView ActorView => actorView;
    public UserCardPool CardPool => cardPool;

    private void Awake()
    {
        ResolveRefs();
    }

    private void OnDestroy()
    {
        if (generatedFallbackSprite != null)
        {
            Destroy(generatedFallbackSprite);
        }
    }

    public void InitializeForBattle()
    {
        ResolveRefs();
        EnsureVisual();
        EnsureHpText();

        if (actorView != null)
        {
            actorView.Configure(displayName, maxHp, hpText);
            actorView.CacheBasePose();
            actorView.ResetHp();
        }

        Debug.Log($"PlayerBattleController: Initialize player. hp={maxHp}, deck={(cardPool != null ? cardPool.DrawCount : 0)} draw.");
    }

    private void ResolveRefs()
    {
        if (actorView == null)
        {
            actorView = GetComponent<BattleActorView>();
        }

        if (cardPool == null)
        {
            cardPool = GetComponent<UserCardPool>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (hpText == null)
        {
            hpText = GetComponentInChildren<TMP_Text>();
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

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
        else if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetFallbackSprite();
        }

        spriteRenderer.color = fallbackColor;
        spriteRenderer.sortingOrder = 9;
        FitSpriteToWorldSize(spriteRenderer, worldSize);
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

    private Sprite GetFallbackSprite()
    {
        if (generatedFallbackSprite != null)
        {
            return generatedFallbackSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        generatedFallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        generatedFallbackSprite.name = "PlayerFallbackSprite";
        return generatedFallbackSprite;
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
