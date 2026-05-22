using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private GameObject failPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button exitButton;

    private GameManager gameManager;
    private readonly List<Button> boundRestartButtons = new();

    public void Initialize(GameManager owner)
    {
        gameManager = owner;

        AutoBindRefs();
        BindButtons();
        HideResult();
    }

    public void ShowClear()
    {
        bool isLast = gameManager != null && gameManager.IsLastLevel();

        if (clearPanel != null)
        {
            clearPanel.SetActive(true);
        }

        if (failPanel != null)
        {
            failPanel.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!isLast);
        }

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(isLast);
        }
    }

    public void ShowFail()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }

        if (failPanel != null)
        {
            failPanel.SetActive(true);
        }
    }

    public void HideResult()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }

        if (failPanel != null)
        {
            failPanel.SetActive(false);
        }

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
        }
    }

    private void AutoBindRefs()
    {
        if (clearPanel == null)
        {
            clearPanel = FindSceneObjectByName("ClearPanel");
        }

        if (failPanel == null)
        {
            failPanel = FindSceneObjectByName("FailPanel");
        }

        if (restartButton == null)
        {
            GameObject firstRestart = FindSceneObjectByName("RestartButton");
            if (firstRestart != null)
            {
                restartButton = firstRestart.GetComponent<Button>();
            }
        }

        if (nextButton == null)
        {
            GameObject nextObj = FindSceneObjectByName("NextButton");
            if (nextObj != null)
            {
                nextButton = nextObj.GetComponent<Button>();
            }
        }

        if (undoButton == null)
        {
            GameObject undoObj = FindSceneObjectByName("UndoButton");
            if (undoObj != null)
            {
                undoButton = undoObj.GetComponent<Button>();
            }
        }

        if (endTurnButton == null)
        {
            GameObject endTurnObj = FindSceneObjectByName("EndTurnButton");
            if (endTurnObj != null)
            {
                endTurnButton = endTurnObj.GetComponent<Button>();
            }
        }

        if (exitButton == null)
        {
            GameObject exitObj = FindSceneObjectByName("ExitButton");
            if (exitObj != null)
            {
                exitButton = exitObj.GetComponent<Button>();
            }
        }
    }

    private void BindButtons()
    {
        foreach (Button btn in boundRestartButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(gameManager.RestartCurrentLevel);
            }
        }
        boundRestartButtons.Clear();

        if (restartButton != null)
        {
            RegisterRestartButton(restartButton);
        }

        RegisterRestartButtonsInParent(clearPanel);
        RegisterRestartButtonsInParent(failPanel);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(gameManager.LoadNextLevel);
        }

        if (undoButton != null)
        {
            undoButton.onClick.RemoveAllListeners();
            undoButton.onClick.AddListener(gameManager.UndoLastMove);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(gameManager.EndTurn);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(gameManager.ExitGame);
        }
    }

    private void RegisterRestartButtonsInParent(GameObject parent)
    {
        if (parent == null)
        {
            return;
        }

        Button[] buttons = parent.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn == null || btn.name != "RestartButton")
            {
                continue;
            }

            RegisterRestartButton(btn);
        }
    }

    private void RegisterRestartButton(Button btn)
    {
        if (btn == null || boundRestartButtons.Contains(btn))
        {
            return;
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(gameManager.RestartCurrentLevel);
        boundRestartButtons.Add(btn);
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null)
            {
                continue;
            }

            if (!go.scene.IsValid())
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
