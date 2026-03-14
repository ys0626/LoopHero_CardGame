using UnityEngine;

public class MapTest : MonoBehaviour
{
    void Start()
    {
        // 맵 생성 후 바로 표시
        MapManager.Instance.GenerateNewMap();
        MapManager.Instance.ShowMap();
    }
}
