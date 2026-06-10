using System;
using UnityEngine;

public enum MainMenuCardDirection
{
    None,
    Up,
    UpRight,
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
    UpLeft
}

public enum MainMenuCardAction
{
    None,
    GameStart,
    Options,
    Credits
}

[Serializable]
public class MainMenuFixedCardSeed
{
    [Header("표시")]
    public string displayName = "게임 시작";
    public Sprite icon;

    [Header("위치/동작")]
    public Vector2Int position = new(1, 2);
    public MainMenuCardAction action = MainMenuCardAction.GameStart;
}

[Serializable]
public class MainMenuHandCardSeed
{
    [Header("표시")]
    public string displayName = "위쪽";
    public Sprite icon;

    [Header("방향")]
    public MainMenuCardDirection direction = MainMenuCardDirection.Up;
}
