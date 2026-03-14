using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapUIManager : MonoBehaviour
{
    public static MapUIManager Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Transform nodeContainer;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button deckViewButton;

    [Header("노드 아이콘")]
    [SerializeField] private Sprite battleIcon;
    [SerializeField] private Sprite eliteIcon;
    [SerializeField] private Sprite eventIcon;
    [SerializeField] private Sprite shopIcon;
    [SerializeField] private Sprite restIcon;
    [SerializeField] private Sprite bossIcon;

    [Header("노드 색상")]
    [SerializeField]
    private Color accessibleColor
        = Color.white;
    [SerializeField]
    private Color clearedColor
        = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField]
    private Color inaccessibleColor
        = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField]
    private Color currentNodeColor
        = Color.yellow;

    private Dictionary<string, GameObject> nodeObjects
        = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mapPanel.SetActive(false);
    }

    void Start()
    {
        if (deckViewButton != null)
            deckViewButton.onClick.AddListener(
                () => DeckViewer.Instance.OpenInMap());
    }

    // ─────────────────────────────────────
    // 맵 표시 / 숨기기
    // ─────────────────────────────────────
    public void DrawMap(
        List<List<MapNodeData>> mapLayers,
        MapNodeData currentNode)
    {
        mapPanel.SetActive(true);
        ClearMap();

        // Content 높이 설정
        RectTransform contentRect =
            nodeContainer.GetComponent<RectTransform>();

        float ySpacing = 200f;
        float padding = 100f;
        float totalHeight =
            (mapLayers.Count - 1) * ySpacing + padding * 2f;

        contentRect.sizeDelta =
            new Vector2(contentRect.sizeDelta.x, totalHeight);

        // 노드 생성
        foreach (List<MapNodeData> layer in mapLayers)
            foreach (MapNodeData node in layer)
                CreateNodeObject(node, currentNode);

        // 연결선 생성
        foreach (List<MapNodeData> layer in mapLayers)
        {
            foreach (MapNodeData node in layer)
            {
                foreach (string nextNodeId in node.nextNodeIds)
                {
                    if (!nodeObjects.ContainsKey(node.nodeId)
                        || !nodeObjects.ContainsKey(nextNodeId))
                        continue;

                    if (linePrefab == null) continue;

                    Vector2 startPos = nodeObjects[node.nodeId]
                        .GetComponent<RectTransform>().localPosition;
                    Vector2 endPos = nodeObjects[nextNodeId]
                        .GetComponent<RectTransform>().localPosition;

                    DrawLine(startPos, endPos);
                }
            }
        }

        StartCoroutine(ScrollToBottom());
    }

    public void HideMap()
    {
        mapPanel.SetActive(false);
    }

    // ─────────────────────────────────────
    // 노드 오브젝트 생성
    // ─────────────────────────────────────

    void CreateNodeObject(
    MapNodeData node,
    MapNodeData currentNode)
    {
        if (nodePrefab == null) return;

        GameObject nodeGO =
            Instantiate(nodePrefab, nodeContainer);
        RectTransform rect =
            nodeGO.GetComponent<RectTransform>();

        float ySpacing = 200f;
        int totalLayers = 10;

        float totalHeight = (totalLayers - 1) * ySpacing;
        float yPos = -totalHeight / 2f
            + node.layerIndex * ySpacing;

        rect.localPosition = new Vector3(
            node.position.x, yPos, 0);

        // 아이콘
        Image nodeImage = nodeGO.GetComponent<Image>();
        if (nodeImage != null)
        {
            nodeImage.sprite = GetNodeIcon(node.nodeType);

            if (currentNode != null
                && node.nodeId == currentNode.nodeId)
                nodeImage.color = currentNodeColor;
            else if (node.isCleared)
                nodeImage.color = clearedColor;
            else if (node.isAccessible)
                nodeImage.color = accessibleColor;
            else
                nodeImage.color = inaccessibleColor;
        }

        // 버튼
        Button nodeButton = nodeGO.GetComponent<Button>();
        if (nodeButton != null)
        {
            MapNodeData capturedNode = node;
            nodeButton.onClick.AddListener(
                () => MapManager.Instance
                    .OnNodeSelected(capturedNode));
            nodeButton.interactable =
                node.isAccessible && !node.isCleared;
        }

        // 텍스트
        TextMeshProUGUI nodeText =
            nodeGO.GetComponentInChildren<TextMeshProUGUI>();
        if (nodeText != null)
            nodeText.text = GetNodeLabel(node.nodeType);

        nodeObjects[node.nodeId] = nodeGO;
    }

    // ─────────────────────────────────────
    // 연결선 그리기
    // ─────────────────────────────────────

    void DrawLine(Vector2 startPos, Vector2 endPos)
    {
        GameObject lineGO = Instantiate(linePrefab, nodeContainer);
        RectTransform lineRect = lineGO.GetComponent<RectTransform>();

        // 중간 지점
        lineRect.localPosition = (startPos + endPos) / 2f;

        // 선 길이
        float distance = Vector2.Distance(startPos, endPos);
        lineRect.sizeDelta = new Vector2(distance, 3f);

        // 선 각도
        Vector2 direction = endPos - startPos;
        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // ─────────────────────────────────────
    // 스크롤
    // ─────────────────────────────────────

    IEnumerator ScrollToBottom()
    {
        // 2프레임 대기
        // → Content 크기 계산 완료 후 스크롤
        yield return null;
        yield return null;

        scrollRect.verticalNormalizedPosition = 0f;
    }

    // ─────────────────────────────────────
    // 맵 초기화
    // ─────────────────────────────────────

    void ClearMap()
    {
        // nodeObjects로 생성된 노드 삭제
        foreach (GameObject nodeGO in nodeObjects.Values)
        {
            if (nodeGO != null)
                Destroy(nodeGO);
        }
        nodeObjects.Clear();

        // 연결선 삭제
        // → nodePrefab이 아닌 linePrefab으로
        //   생성된 오브젝트 삭제
        foreach (Transform child in nodeContainer)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    // ─────────────────────────────────────
    // 노드 아이콘 / 라벨
    // ─────────────────────────────────────

    Sprite GetNodeIcon(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return battleIcon;
            case NodeType.EliteBattle: return eliteIcon;
            case NodeType.Event: return eventIcon;
            case NodeType.Shop: return shopIcon;
            case NodeType.Rest: return restIcon;
            case NodeType.Boss: return bossIcon;
            default: return battleIcon;
        }
    }

    string GetNodeLabel(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return "전투";
            case NodeType.EliteBattle: return "강적";
            case NodeType.Event: return "?";
            case NodeType.Shop: return "상점";
            case NodeType.Rest: return "휴식";
            case NodeType.Boss: return "보스";
            default: return "?";
        }
    }
}