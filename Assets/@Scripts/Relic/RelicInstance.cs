using UnityEngine;

public class RelicInstance
{
    private static int s_nextInstanceId = 1;

    public RelicInstance(RelicData sourceData)
    {
        InstanceId = s_nextInstanceId++;
        SourceData = sourceData;
    }

    public int InstanceId { get; }
    public RelicData SourceData { get; }
    public Vector2Int GridOrigin { get; private set; }
    public int RotationSteps { get; private set; }
    public bool IsActivated { get; private set; }

    public void SetPlacement(Vector2Int origin, int rotationSteps)
    {
        GridOrigin = origin;
        RotationSteps = rotationSteps % 4;
        if (RotationSteps < 0) RotationSteps += 4;
    }

    public void SetActivated(bool activated)
    {
        IsActivated = activated;
    }
}
