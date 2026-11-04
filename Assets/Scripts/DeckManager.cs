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

    [Header("【新しく追加】手札の並びに関する設定")]
    [SerializeField] private float cardSpacing = 0.15f; // カード同士の間隔
    [SerializeField] private Vector3 handBaseLocalPos = new Vector3(0f, 2f, 15f); // 手札の基準位置（中心）

    // 現在手札にあるカードを管理するリスト
    private List<GameObject> handCards = new List<GameObject>();

    // 1. 山札をまとめる処理
    public void PileUpCards()
    {
        // 山札をリセットするときは手札リストもクリアする
        handCards.Clear();

        float heightOffset = 0.0f;

        foreach (var card in allCardsInScene)
        {
            if (card == null) continue;

            card.transform.position = deckPileTransform.position + new Vector3(0, heightOffset, 0);
            card.transform.rotation = deckPileTransform.rotation;
            card.transform.SetParent(null);

            To_tefuda ctrl = card.GetComponent<To_tefuda>();
            if (ctrl != null)
            {
                ctrl.IsInDrawPile = true;
            }
            
            heightOffset += 0.002f;
        }

        Debug.Log("すべてのカードを山札として積み上げました！");
    }

    // 2. カードを引く処理
    public void DrawCardDynamic()
    {
        List<GameObject> availableCards = GetAvailableCards();

        if (availableCards.Count == 0)
        {
            Debug.Log("山札もうないで！");
            return;
        }

        // ラックに応じた抽選で1枚選ぶ
        GameObject selectedCard = DecideCardByLuck(availableCards);

        To_tefuda cardController = selectedCard.GetComponent<To_tefuda>();
        if (cardController != null)
        {
            // A. 手札リストに新しく追加する
            handCards.Add(selectedCard);

            // B. カードをカメラの子どもにして、山札フラグをオフにする
            cardController.DrawCardToHand();

            // C. 手札全体の並びを再計算して綺麗に並べ替える！
            UpdateHandLayout();
        }
    }

    // ★【重要】手札のカードを横一列に均等に並べる関数
    private void UpdateHandLayout()
    {
        int count = handCards.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject card = handCards[i];
            if (card == null) continue;

            // 中央を基準にして、左右に等間隔で広がるオフセット（X座標）を計算する
            // 例：3枚なら i=0 は左、i=1 は中央、i=2 は右に配置される
            float xOffset = (i - (count - 1) / 2f) * cardSpacing;

            // カメラから見たローカル座標を計算
            Vector3 targetLocalPos = new Vector3(
                handBaseLocalPos.x + xOffset,
                handBaseLocalPos.y,
                handBaseLocalPos.z
            );

            // カードの位置と向きを更新
            card.transform.localPosition = targetLocalPos;
            card.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private GameObject DecideCardByLuck(List<GameObject> candidates)
    {
        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    private List<GameObject> GetAvailableCards()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (var card in allCardsInScene)
        {
            if (card == null) continue;

            To_tefuda ctrl = card.GetComponent<To_tefuda>();
            if (ctrl != null && ctrl.IsInDrawPile)
            {
                list.Add(card);
            }
        }
        return list;
    }
}