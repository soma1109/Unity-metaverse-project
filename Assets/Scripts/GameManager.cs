using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("DeckManagerの参照")]
    [SerializeField] private DeckManager deckManager;

    // プレイヤーが手札のカードをクリックして場に出そうとしたときのメイン処理
    public void PlayCardFromHand(GameObject selectedCard, List<GameObject> myHandCards, Transform[] fieldSlots, GameObject[] fieldCards)
    {
        CardData playedCardData = selectedCard.GetComponent<CardData>();
        if (playedCardData == null)
        {
            Debug.LogWarning("このカードには CardData がついていません！");
            return;
        }

        int targetMonth = playedCardData.month;
        Debug.Log("プレイしたカードの月: " + targetMonth + "月");

        // 1. 場札（fieldCards）の中に「同じ月」のカードがあるか探す
        List<int> matchingFieldSlots = new List<int>();

        for (int i = 0; i < fieldCards.Length; i++)
        {
            GameObject fieldCard = fieldCards[i];
            if (fieldCard == null) continue; // 空きマスはスキップ

            CardData fieldCardData = fieldCard.GetComponent<CardData>();
            if (fieldCardData != null && fieldCardData.month == targetMonth)
            {
                matchingFieldSlots.Add(i); // 同じ月のスロット番号を記録
            }
        }

        // 2. 判定と移動の分岐
        if (matchingFieldSlots.Count > 0)
        {
            // --- パターンA：同じ月の場札がある場合 ---
            // ※まずは一番最初に見つかった同じ月の場札に重ねる処理（後から選択式に拡張可能）
            int targetSlotIndex = matchingFieldSlots[0];
            PlaceCardToFieldSlot(selectedCard, targetSlotIndex, myHandCards, fieldSlots, fieldCards);
            Debug.Log("同じ月の場札（スロット " + targetSlotIndex + "）と合わせました！");
        }
        else
        {
            // --- パターンB：同じ月の場札がない場合 ---
            // 一番スロット番号が小さい「空いている場所」を探す
            int emptySlotIndex = GetLowestEmptyFieldSlot(fieldCards);

            if (emptySlotIndex != -1)
            {
                PlaceCardToFieldSlot(selectedCard, emptySlotIndex, myHandCards, fieldSlots, fieldCards);
                Debug.Log("同じ月の場札がないため、一番若い空きスロット（スロット " + emptySlotIndex + "）に置きました！");
            }
            else
            {
                Debug.Log("場札に置ける空きスペースがありません！");
            }
        }
    }

    // 一番スロット番号が小さい空きマスを探す補助関数
    private int GetLowestEmptyFieldSlot(GameObject[] fieldCards)
    {
        for (int i = 0; i < fieldCards.Length; i++)
        {
            if (fieldCards[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    // カードを実際に指定された場札のスロットに移動させる共通処理
    private void PlaceCardToFieldSlot(GameObject card, int slotIndex, List<GameObject> myHandCards, Transform[] fieldSlots, GameObject[] fieldCards)
    {
        // 自分の手札リストから取り除く
        myHandCards.Remove(card);

        // 指定スロットの位置に移動させる
        card.transform.SetParent(fieldSlots[slotIndex]);
        card.transform.position = fieldSlots[slotIndex].position;
        card.transform.rotation = fieldSlots[slotIndex].rotation;

        fieldCards[slotIndex] = card;
    }
}