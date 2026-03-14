using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private CardSlotUI[] cardSlots;
    // → Inspector에서 CardSlot 3개 연결

    [Header("상점 설정")]
    [SerializeField] private int shopCardCount = 3;

    private List<CardData> allCards = new List<CardData>();
    private List<CardData> currentShopCards = new List<CardData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadAllCards();
        shopPanel.SetActive(false);
    }

    // ─────────────────────────────────────
    // 카드 로드
    // ─────────────────────────────────────

    private void LoadAllCards()
    {
        // Resources/Cards 하위 폴더 전체 로드
        CardData[] loaded = Resources
            .LoadAll<CardData>("Cards/Special");

        allCards.AddRange(loaded);

        Debug.Log($"카드 로드 완료: {allCards.Count}장");
    }

    // ─────────────────────────────────────
    // 상점 열기 / 닫기
    // ─────────────────────────────────────

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        GenerateShopCards();
        UpdateGoldText();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);

        // 노드 클리어 처리
        if (MapManager.Instance != null)
            MapManager.Instance.ClearCurrentNode();
    }

    // ─────────────────────────────────────
    // 카드 생성
    // ─────────────────────────────────────

    private void GenerateShopCards()
    {
        currentShopCards.Clear();

        // 전체 카드 복사 후 셔플
        List<CardData> pool = new List<CardData>(allCards);
        Shuffle(pool);

        // 앞에서 3장 선택
        int count = Mathf.Min(shopCardCount, pool.Count);
        for (int i = 0; i < count; i++)
            currentShopCards.Add(pool[i]);

        // 슬롯에 카드 표시
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (i < currentShopCards.Count)
                cardSlots[i].Setup(currentShopCards[i], this);
            else
                cardSlots[i].gameObject.SetActive(false);
        }
    }

    private void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    // ─────────────────────────────────────
    // 구매
    // ─────────────────────────────────────

    public void TryBuyCard(CardData card, CardSlotUI slot)
    {
        if (!PlayerData.Instance.HasEnoughGold(card.price))
        {
            Debug.Log("골드 부족!");
            // 추후 UI 알림 추가
            return;
        }

        PlayerData.Instance.SpendGold(card.price);
        PlayerData.Instance.AddCard(card);
        UpdateGoldText();

        // 구매 완료 → 슬롯 비활성화
        slot.SetSoldOut();

        Debug.Log($"{card.cardName} 구매 완료!");
    }

    // ─────────────────────────────────────
    // 골드 텍스트 업데이트
    // ─────────────────────────────────────

    private void UpdateGoldText()
    {
        goldText.text =
            $"골드: {PlayerData.Instance.GetGold()} G";
    }
}

