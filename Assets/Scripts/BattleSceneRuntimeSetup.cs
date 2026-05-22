using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleSceneRuntimeSetup : MonoBehaviour
{
    [SerializeField] private int actorMaxHp = 20;

    private void Awake()
    {
        EnsureActors();
        EnsureButtons();
    }

    private void EnsureActors()
    {
        Transform topRoot = FindSceneObjectByName("TopBattleRoot")?.transform;
        if (topRoot == null)
        {
            return;
        }

        EnsureActor(topRoot, "PlayerActor", "Player", new Vector2(-320f, 0f), new Color(0.35f, 0.75f, 1f, 1f));
        EnsureActor(topRoot, "EnemyActor", "Enemy", new Vector2(320f, 0f), new Color(1f, 0.45f, 0.45f, 1f));
    }

    private void EnsureActor(Transform parent, string objectName, string label, Vector2 anchoredPosition, Color color)
    {
        GameObject existing = FindSceneObjectByName(objectName);
        if (existing != null)
        {
            BattleActorView existingView = existing.GetComponent<BattleActorView>();
            if (existingView != null)
            {
                existingView.Configure(label, actorMaxHp, existing.GetComponentInChildren<TMP_Text>());
            }
            return;
        }

        GameObject actor = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleActorView));
        actor.transform.SetParent(parent, false);

        RectTransform rect = actor.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 120f);
        rect.anchoredPosition = anchoredPosition;

        Image image = actor.GetComponent<Image>();
        image.color = color;

        TMP_Text hpText = CreateLabel(actor.transform, "HpText", label, Vector2.zero, new Vector2(220f, 60f), 26f);
        BattleActorView view = actor.GetComponent<BattleActorView>();
        view.Configure(label, actorMaxHp, hpText);
    }

    private void EnsureButtons()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        EnsureButton(canvas.transform, "UndoButton", "Undo", new Vector2(-170f, 70f));
        EnsureButton(canvas.transform, "EndTurnButton", "End Turn", new Vector2(-40f, 70f));
    }

    private void EnsureButton(Transform parent, string objectName, string label, Vector2 anchoredPosition)
    {
        if (FindSceneObjectByName(objectName) != null)
        {
            return;
        }

        GameObject buttonObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(120f, 44f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.14f, 0.18f, 0.92f);

        CreateLabel(buttonObject.transform, "Label", label, Vector2.zero, rect.sizeDelta, 18f);
    }

    private static TMP_Text CreateLabel(Transform parent, string objectName, string text, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        GameObject labelObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        return label;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null || !go.scene.IsValid())
            {
                continue;
            }

            if (go.name == objectName)
            {
                return go;
            }
        }

        return null;
    }
}
