using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    [Header("¸Ê ¼³Á¤")]
    [SerializeField] private int totalLayers = 10;
    [SerializeField] private int minNodesPerLayer = 2;
    [SerializeField] private int maxNodesPerLayer = 5;

    [Header("³ëµå µîÀå È®·ü (0~1)")]
    [SerializeField] private float battleChance = 0.45f;
    [SerializeField] private float eliteChance = 0.15f;
    [SerializeField] private float eventChance = 0.22f;
    [SerializeField] private float shopChance = 0.12f;
    [SerializeField] private float restChance = 0.06f;

    private List<List<MapNodeData>> mapLayers
        = new List<List<MapNodeData>>();

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸Ê »ý¼º
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    public List<List<MapNodeData>> GenerateMap()
    {
        mapLayers.Clear();

        for (int layer = 0; layer < totalLayers; layer++)
        {
            List<MapNodeData> layerNodes
                = new List<MapNodeData>();

            // Ã¹ ¹øÂ° Ãþ ¡æ ÀÏ¹Ý ÀüÅõ 1°³ °íÁ¤
            if (layer == 0)
            {
                layerNodes.Add(
                    CreateNode(layer, 0, NodeType.Battle, 1));
            }
            // ¸¶Áö¸· Ãþ ¡æ º¸½º 1°³ °íÁ¤
            else if (layer == totalLayers - 1)
            {
                layerNodes.Add(
                    CreateNode(layer, 0, NodeType.Boss, 1));
            }
            // º¸½º Á÷Àü Ãþ ¡æ ÈÞ½Ä º¸Àå
            else if (layer == totalLayers - 2)
            {
                int nodeCount = Random.Range(
                    minNodesPerLayer, maxNodesPerLayer + 1);

                for (int i = 0; i < nodeCount; i++)
                {
                    // Ã¹ ¹øÂ° ³ëµå´Â ÈÞ½Ä º¸Àå
                    NodeType type = (i == 0)
                        ? NodeType.Rest
                        : GetRandomNodeType(layer);

                    layerNodes.Add(
                        CreateNode(layer, i, type, nodeCount));
                }
            }
            else
            {
                int nodeCount = Random.Range(
                    minNodesPerLayer, maxNodesPerLayer + 1);

                for (int i = 0; i < nodeCount; i++)
                {
                    NodeType type = GetRandomNodeType(layer);
                    layerNodes.Add(CreateNode(layer, i, type, nodeCount));
                }

                // 3Ãþ¸¶´Ù »óÁ¡ º¸Àå
                // layer 3, 6, 9... Áß º¸½º/ÈÞ½Ä Ãþ Á¦¿Ü
                if (layer % 3 == 0)
                {
                    bool hasShop = layerNodes.Exists(n => n.nodeType == NodeType.Shop);

                    if (!hasShop)
                    {
                        // ·£´ý ³ëµå ÇÏ³ª¸¦ »óÁ¡À¸·Î ±³Ã¼
                        int replaceIndex = Random.Range(0, layerNodes.Count);
                        layerNodes[replaceIndex].nodeType = NodeType.Shop;
                    }
                }
            }

            mapLayers.Add(layerNodes);
        }

        ConnectNodes();

        // Ã¹ ¹øÂ° Ãþ Á¢±Ù °¡´É ¼³Á¤
        foreach (MapNodeData node in mapLayers[0])
            node.isAccessible = true;

        Debug.Log(
            $"¸Ê »ý¼º ¿Ï·á: {totalLayers}Ãþ / " +
            $"ÃÑ ³ëµå {GetTotalNodeCount()}°³");

        return mapLayers;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³ëµå »ý¼º
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    MapNodeData CreateNode(
    int layer, int index,
    NodeType type, int totalInLayer)
    {
        MapNodeData node = new MapNodeData();

        node.nodeId = $"node_{layer}_{index}";
        node.nodeType = type;
        node.nextNodeIds = new List<string>();
        node.isCleared = false;
        node.isAccessible = false;
        node.layerIndex = layer;

        float xSpacing = 160f;
        float ySpacing = 200f;

        float centerOffset = (totalInLayer - 1) / 2f;
        float xPos = (index - centerOffset) * xSpacing;

        // layer 0ÀÌ ¸Ç ¾Æ·¡
        // layer 9°¡ ¸Ç À§
        float yPos = layer * ySpacing;

        node.position = new Vector2(xPos, yPos);

        return node;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³ëµå Å¸ÀÔ °áÁ¤
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    NodeType GetRandomNodeType(int layer)
    {
        // Ã¹ ¹øÂ° Ãþ ¡æ ¹«Á¶°Ç ÀüÅõ
        if (layer == 0)
            return NodeType.Battle;

        // 4Ãþ ÀÌÀü ¡æ ¿¤¸®Æ® Á¦¿Ü
        float currentEliteChance =
            (layer >= 4) ? eliteChance : 0f;

        float total = battleChance
            + currentEliteChance
            + eventChance
            + shopChance
            + restChance;

        float random = Random.value * total;
        float cumulative = 0f;

        cumulative += battleChance;
        if (random < cumulative) return NodeType.Battle;

        cumulative += currentEliteChance;
        if (random < cumulative) return NodeType.EliteBattle;

        cumulative += eventChance;
        if (random < cumulative) return NodeType.Event;

        cumulative += shopChance;
        if (random < cumulative) return NodeType.Shop;

        cumulative += restChance;
        if (random < cumulative) return NodeType.Rest;

        return NodeType.Battle;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³ëµå ¿¬°á (¼± ±³Â÷ ÃÖ¼ÒÈ­)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    void ConnectNodes()
    {
        for (int layer = 0;
            layer < mapLayers.Count - 1; layer++)
        {
            List<MapNodeData> currentLayer = mapLayers[layer];
            List<MapNodeData> nextLayer = mapLayers[layer + 1];

            // X À§Ä¡ ±âÁØÀ¸·Î Á¤·Ä
            // ¡æ °¡±î¿î ³ëµå³¢¸® ¿¬°áÇØ¼­ ±³Â÷ ¹æÁö
            List<MapNodeData> sortedCurrent =
                currentLayer.OrderBy(n => n.position.x).ToList();
            List<MapNodeData> sortedNext =
                nextLayer.OrderBy(n => n.position.x).ToList();

            // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            // ´ÙÀ½ Ãþ ¸ðµç ³ëµå°¡ ÃÖ¼Ò 1°³ ¿¬°á º¸Àå
            // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            for (int i = 0; i < sortedNext.Count; i++)
            {
                // ´ÙÀ½ Ãþ ³ëµåÀÇ X À§Ä¡¿Í
                // °¡Àå °¡±î¿î ÇöÀç Ãþ ³ëµå Ã£±â
                MapNodeData closest = GetClosestNode(
                    sortedNext[i], sortedCurrent);

                if (!closest.nextNodeIds
                    .Contains(sortedNext[i].nodeId))
                {
                    closest.nextNodeIds
                        .Add(sortedNext[i].nodeId);
                }
            }

            // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            // ÇöÀç Ãþ ³ëµå Áß ¿¬°á ¾ø´Â ³ëµå Ã³¸®
            // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            foreach (MapNodeData current in sortedCurrent)
            {
                if (current.nextNodeIds.Count == 0)
                {
                    MapNodeData closest = GetClosestNode(
                        current, sortedNext);

                    current.nextNodeIds.Add(closest.nodeId);
                }
            }

            // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            // Ãß°¡ ¿¬°á (¼±ÅÃÀû)
            // ÇöÀç Ãþ ³ëµå°¡ ´ÙÀ½ Ãþ ÀÎÁ¢ ³ëµå¿¡
            // Ãß°¡ ¿¬°á Çã¿ë (ÃÖ´ë 2°³)
            // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            foreach (MapNodeData current in sortedCurrent)
            {
                if (current.nextNodeIds.Count >= 2) continue;

                // ÀÎÁ¢ÇÑ ´ÙÀ½ ³ëµå Ã£±â
                List<MapNodeData> adjacentNodes =
                    GetAdjacentNodes(current, sortedNext);

                foreach (MapNodeData adjacent in adjacentNodes)
                {
                    if (current.nextNodeIds.Count >= 2) break;

                    if (!current.nextNodeIds
                        .Contains(adjacent.nodeId))
                    {
                        // ±³Â÷ ¿©ºÎ È®ÀÎ ÈÄ ¿¬°á
                        if (!WouldCross(
                            current, adjacent, sortedCurrent, sortedNext))
                        {
                            current.nextNodeIds
                                .Add(adjacent.nodeId);
                        }
                    }
                }
            }
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // °¡Àå °¡±î¿î ³ëµå Ã£±â
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    MapNodeData GetClosestNode(
        MapNodeData from, List<MapNodeData> candidates)
    {
        MapNodeData closest = null;
        float minDist = float.MaxValue;

        foreach (MapNodeData candidate in candidates)
        {
            float dist = Mathf.Abs(
                from.position.x - candidate.position.x);

            if (dist < minDist)
            {
                minDist = dist;
                closest = candidate;
            }
        }

        return closest;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÀÎÁ¢ ³ëµå ¸ñ·Ï (X °Å¸® ±âÁØ)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    List<MapNodeData> GetAdjacentNodes(
        MapNodeData from, List<MapNodeData> candidates)
    {
        return candidates
            .OrderBy(n => Mathf.Abs(
                n.position.x - from.position.x))
            .Take(2)
            .ToList();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¼± ±³Â÷ ¿©ºÎ È®ÀÎ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    bool WouldCross(
        MapNodeData from, MapNodeData to,
        List<MapNodeData> currentLayer,
        List<MapNodeData> nextLayer)
    {
        foreach (MapNodeData other in currentLayer)
        {
            if (other == from) continue;

            foreach (string nextId in other.nextNodeIds)
            {
                MapNodeData otherNext = nextLayer.Find(
                    n => n.nodeId == nextId);

                if (otherNext == null || otherNext == to)
                    continue;

                // µÎ ¼±ºÐÀÌ ±³Â÷ÇÏ´ÂÁö È®ÀÎ
                if (LinesIntersect(
                    from.position, to.position,
                    other.position, otherNext.position))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¼±ºÐ ±³Â÷ ÆÇÁ¤
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    bool LinesIntersect(
        Vector2 a1, Vector2 a2,
        Vector2 b1, Vector2 b2)
    {
        float d1 = CrossProduct(b2 - b1, a1 - b1);
        float d2 = CrossProduct(b2 - b1, a2 - b1);
        float d3 = CrossProduct(a2 - a1, b1 - a1);
        float d4 = CrossProduct(a2 - a1, b2 - a1);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        return false;
    }

    float CrossProduct(Vector2 a, Vector2 b)
        => a.x * b.y - a.y * b.x;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // À¯Æ¿
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    int GetTotalNodeCount()
    {
        int count = 0;
        foreach (List<MapNodeData> layer in mapLayers)
            count += layer.Count;
        return count;
    }

    public List<List<MapNodeData>> GetMapLayers() => mapLayers;
}