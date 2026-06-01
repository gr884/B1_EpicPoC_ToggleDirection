using System;

public static class CharacterMotionEvents
{
    public static event Action<CharacterMotionRequest> CardMotionRequested;
    public static bool HasCardMotionListeners => CardMotionRequested != null;

    public static void RequestCardMotion(CharacterMotionRequest request)
    {
        CardMotionRequested?.Invoke(request);
    }

    public static void Clear()
    {
        CardMotionRequested = null;
    }
}
