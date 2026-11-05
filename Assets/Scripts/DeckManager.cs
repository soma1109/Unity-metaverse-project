using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("シーン内にあるすべてのカードのリスト")]
    [SerializeField] private List<GameObject> allCardsInScene = new List<GameObject>();

    [Header("山札として積み上げる基準の位置")]
    [SerializeField] private Transform deckPileTransform;

    [Header("プレイヤーのラック（運）のステータス")]
    public int playerLuck = 10;

    [Header("手札の並びに関する設定")]
    [SerializeField] private float cardSpacing = 0.15f; 
    [SerializeField] private Vector3 handBaseLocalPos = new Vector3(0f, 2f, 15f); 

    // 現在手札にあるカードを管理するリスト
    private List<GameObject> handCards = new List<GameObject>();

    [Header("場札に関する設定")]
    [SerializeField] private Transform[] fieldSlots; // シーン内に置いた場札の置き場（スロット）のリスト
    private GameObject[] fieldCards = new GameObject[20]; // どのスロットにどのカードがあるかを保持する配列

    [Header("山札の厚み設定")]
    [SerializeField] private float pileSpacing = 0.05f;

    // 1. 山札をまとめる処理
    public void PileUpCards()
    {
        // リセット時は手札も場札の記録もクリアする
        handCards.Clear();
        System.Array.Clear(fieldCards, 0, fieldCards.Length);

        float heightOffset = 0.0f;

        foreach (var card in allCardsInScene)
        {
            if (card == null) continue;

            card.transform.position = deckPileTransform.position + new Vector3(0, heightOffset, 0);
            card.transform.rotation = deckPileTransform.rotation;
            card.transform.SetParent(null);

            heightOffset += pileSpacing;
        }

        Debug.Log("すべてのカードを山札として積み上げました！");
    }

    // 2. カードを手札に引く処理
    public void DrawCardDynamic()
    {
        List<GameObject> availableCards = GetAvailableCards();

        if (availableCards.Count == 0)
        {
            Debug.Log("山札もうないで！");
            return;
        }

        GameObject selectedCard = DecideCardByLuck(availableCards);

        To_tefuda cardController = selectedCard.GetComponent<To_tefuda>();
        if (cardController != null)
        {
            // A. 手札リストに追加
            handCards.Add(selectedCard);

            // B. カメラを親にする
            cardController.DrawCardToHand();

            // C. 並びを更新
            UpdateHandLayout();
        }
    }

    // 手札の横一列の並び替え
    private void UpdateHandLayout()
    {
        int count = handCards.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject card = handCards[i];
            if (card == null) continue;

            float xOffset = (i - (count - 1) / 2f) * cardSpacing;

            Vector3 targetLocalPos = new Vector3(
                handBaseLocalPos.x + xOffset,
                handBaseLocalPos.y,
                handBaseLocalPos.z
            );

            card.transform.localPosition = targetLocalPos;
            card.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private GameObject DecideCardByLuck(List<GameObject> candidates)
    {
        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    // ★【重要】「手札」にも「場札」にも入っていないカードを、山札の残りとして探す
    private List<GameObject> GetAvailableCards()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (var card in allCardsInScene)
        {
            if (card == null) continue;

            // すでに手札に含まれているかチェック
            bool isInHand = handCards.Contains(card);
            
            // すでに場札に含まれているかチェック
            bool isInField = System.Array.Exists(fieldCards, element => element == card);

            // どっちにも入っていなければ、まだ山札に残っているカードとみなす
            if (!isInHand && !isInField)
            {
                list.Add(card);
            }
        }
        return list;
    }

    // 3. 場札にカードを配る処理（指定したスロット番号 `slotIndex` に置く例）
    public void PlaceCardToField(int slotIndex) 
    {
        List<GameObject> availableCards = GetAvailableCards();

        if (availableCards.Count == 0)
        {
            Debug.Log("山札なし！");
            return;
        }

        // スロットの範囲外じゃないかチェック
        if (slotIndex < 0 || slotIndex >= fieldSlots.Length)
        {
            Debug.LogWarning("指定された場札のスロット番号が無効やで！: " + slotIndex);
            return;
        }

        // すでにそのスロットにカードがある場合は置かない（空きマスを維持）
        if (fieldCards[slotIndex] != null)
        {
            Debug.Log("そのマスにはすでにカードがあるで！");
            return;
        }

        // ラックに応じた抽選で1枚選ぶ
        GameObject selectedCard = DecideCardByLuck(availableCards);

        // 場札のスロットの位置と向きに合わせる
        selectedCard.transform.SetParent(fieldSlots[slotIndex]);
        selectedCard.transform.position = fieldSlots[slotIndex].position;
        selectedCard.transform.rotation = fieldSlots[slotIndex].rotation * Quaternion.Euler(90f, 0, 0);

        // 場札の管理配列に記録する
        fieldCards[slotIndex] = selectedCard;
    }

    // スタートボタンが押されたときに呼ぶメインの処理
    public void OnGameStartButtonClicked()
    {
        Debug.Log("ゲームスタート！初期配置を開始するで！");

        // 1. まず山札を綺麗にリセット・積み上げる
        PileUpCards();

        // 2. 先に手札を配る（例：8枚引く）
        for (int i = 0; i < 8; i++)
        {
            DrawCardDynamic();
        }

        // 3. その後、ラックを考慮して場札に初期の8枚を配置する（スロット 0 〜 7 番目に置く）
        for (int i = 0; i < 8; i++)
        {
            PlaceCardToField(i);
        }

        Debug.Log("手札と場札の初期配置がすべて完了したで！");
    }
}