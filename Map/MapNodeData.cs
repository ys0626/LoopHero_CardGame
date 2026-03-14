using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapNodeData
{
    public string nodeId;
    public NodeType nodeType;
    public Vector2 position;
    public List<string> nextNodeIds;
    public bool isCleared;
    public bool isAccessible;
    public int layerIndex;  // 교차 방지용 층 인덱스 추가
}

public enum NodeType
{
    Battle,
    EliteBattle,
    Event,
    Shop,
    Rest,
    Boss
}