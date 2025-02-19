using UnityEngine;

public sealed class TPManager : SingletonBase<TPManager>
{
    // index == one of the points linked in the scenedata
    public bool TryTPToPoint(int index)
    {
        SceneData sceneData = GameManager.Instance.SceneData;
        PlayerN player = sceneData.LocalPlayer;
        if (!player || index >= sceneData.TPPoints.Count) return false;

        
        Transform point = sceneData.TPPoints[index];

        player.transform.SetPositionAndRotation(point.position, point.rotation);
        Physics.SyncTransforms();

        return true;
    }
}