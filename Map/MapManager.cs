using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("연결")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private MapUIManager mapUIManager;

    // 현재 맵 데이터
    private List<List<MapNodeData>> currentMap;

    // 현재 위치한 노드
    private MapNodeData currentNode;

    // 노드 ID로 빠르게 찾기 위한 딕셔너리
    private Dictionary<string, MapNodeData> nodeDict
        = new Dictionary<string, MapNodeData>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ─────────────────────────────────────
    // 맵 생성
    // ─────────────────────────────────────

    // → 생성 후 GameManager에 저장
    public void GenerateNewMap()
    {
        currentMap = mapGenerator.GenerateMap();

        nodeDict.Clear();
        foreach (List<MapNodeData> layer in currentMap)
        {
            foreach (MapNodeData node in layer)
            {
                nodeDict[node.nodeId] = node;
            }
        }

        if (currentMap.Count > 0)
        {
            foreach (MapNodeData node in currentMap[0])
                node.isAccessible = true;
        }

        // GameManager 없으면 저장 건너뜀
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveMapData(currentMap, currentNode);
        }
        else
        {
            Debug.Log("GameManager 없음 → 저장 스킵 (테스트 모드)");
        }

        Debug.Log("새 맵 생성 완료");
    }

    // ─────────────────────────────────────
    // 맵 표시
    // ─────────────────────────────────────

    public void ShowMap()
    {
        if (currentMap == null)
        {
            Debug.LogWarning("맵 데이터 없음 → GenerateNewMap 먼저 호출 필요");
            return;
        }

        mapUIManager.DrawMap(currentMap, currentNode);
    }

    // ─────────────────────────────────────
    // 노드 선택
    // ─────────────────────────────────────

    public void OnNodeSelected(MapNodeData node)
    {
        if (!node.isAccessible || node.isCleared)
        {
            Debug.Log("접근 불가 노드");
            return;
        }

        currentNode = node;
        Debug.Log($"노드 선택: {node.nodeType}");

        switch (node.nodeType)
        {
            case NodeType.Battle:
            case NodeType.EliteBattle:
            case NodeType.Boss:
                GameManager.Instance.OnBattleNodeEntered(node.nodeType);
                break;

            case NodeType.Rest:
                OnRestNode();
                break;

            case NodeType.Shop:
                ShopManager.Instance.OpenShop();
                break;

            case NodeType.Event:
                OnEventNode();
                break;
        }
    }

    // ─────────────────────────────────────
    // 노드 클리어
    // ─────────────────────────────────────

    public void ClearCurrentNode()
    {
        if (currentNode == null) return;

        currentNode.isCleared = true;

        // ─────────────────────────────────────
        // 같은 층의 다른 노드 비활성화
        // ─────────────────────────────────────

        // 현재 노드가 몇 번째 층인지 찾기
        int currentLayer = -1;
        for (int i = 0; i < currentMap.Count; i++)
        {
            if (currentMap[i].Contains(currentNode))
            {
                currentLayer = i;
                break;
            }
        }

        // 같은 층의 다른 노드 비활성화
        if (currentLayer >= 0)
        {
            foreach (MapNodeData node in currentMap[currentLayer])
            {
                if (node != currentNode)
                {
                    node.isAccessible = false;
                }
            }
        }

        // ─────────────────────────────────────
        // 다음 층 노드 활성화
        // ─────────────────────────────────────

        foreach (string nextNodeId in currentNode.nextNodeIds)
        {
            if (nodeDict.ContainsKey(nextNodeId))
                nodeDict[nextNodeId].isAccessible = true;
        }

        // 변경된 맵 데이터 저장
        GameManager.Instance.SaveMapData(
            currentMap, currentNode);

        Debug.Log($"노드 클리어: {currentNode.nodeId}");

        // 맵 UI 갱신
        ShowMap();
    }

    // ─────────────────────────────────────
    // 휴식 노드
    // ─────────────────────────────────────

    void OnRestNode()
    {
        if (RunDataManager.Instance == null ||
            RunDataManager.Instance.CurrentRun == null)
        {
            Debug.LogWarning("RunDataManager 없음");
            ClearCurrentNode();
            return;
        }

        RunData run = RunDataManager.Instance.CurrentRun;

        // ─────────────────────────────────────
        // HP 회복
        // ─────────────────────────────────────
        int healAmount = Mathf.RoundToInt(run.maxHp * 0.2f);
        int newHp = Mathf.Min(run.maxHp,
            run.currentHp + healAmount);

        int prevHp = run.currentHp;
        run.currentHp = newHp;

        // ─────────────────────────────────────
        // 정신력 회복
        // ─────────────────────────────────────
        int maxSanity = PlayerPrefs
            .GetInt("MaxSanity", 100);
        int newSanity = Mathf.Min(maxSanity,
            run.mentalGauge + 15);

        int prevSanity = run.mentalGauge;
        run.mentalGauge = newSanity;

        Debug.Log(
            $"휴식: 체력 {healAmount} 회복 " +
            $"({prevHp} → {newHp}) " +
            $"정신력 15 회복 " +
            $"({prevSanity} → {newSanity})");

        ClearCurrentNode();
        ShowMap();
    }

    // ─────────────────────────────────────
    // 이벤트 노드
    // ─────────────────────────────────────
    void OnEventNode()
    {
        if (EventManager.Instance == null)
        {
            Debug.LogWarning("EventManager 없음");
            return;
        }

        EventManager.Instance.StartRandomEvent();
    }

    // ─────────────────────────────────────
    // 외부에서 데이터 읽기
    // ─────────────────────────────────────

    public MapNodeData GetCurrentNode()
        => currentNode;

    public List<List<MapNodeData>> GetCurrentMap()
        => currentMap;

    public void LoadMapData(
    List<List<MapNodeData>> map,
    MapNodeData currentNode)
    {
        currentMap = map;
        this.currentNode = currentNode;

        // nodeDict 재구성
        nodeDict.Clear();
        foreach (List<MapNodeData> layer in currentMap)
        {
            foreach (MapNodeData node in layer)
            {
                nodeDict[node.nodeId] = node;
            }
        }

        Debug.Log("맵 데이터 복원 완료");
    }
}
