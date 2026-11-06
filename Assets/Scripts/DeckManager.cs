using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// プレイヤーの種類を識別するための「列挙型（Enum）」
public enum PlayerType
{
    Player1, // 自分
    Player2  // 相手（CPUやマルチの対戦相手）
}

public class DeckManager : MonoBehaviour
{
    [Header("シーン内にあるすべてのカードのリスト")]
    [SerializeField] private List<GameObject> allCardsInScene = new List<GameObject>();

    [Header("山札として積み上げる基準の位置")]
    [SerializeField] private Transform deckPileTransform;

    [Header("プレイヤーのラック（運）のステータス")]
    public int playerLuck = 10;

    [Header("自分の手札の並びに関する設定")]
    [SerializeField] private float cardSpacing = 0.15f; 
    [SerializeField] private Vector3 handBaseLocalPos = new Vector3(0f, 2f, 15f); 

    [Header("手札の管理（自分と相手を分離）")]
    private List<GameObject> myHandCards = new List<GameObject>();     // 自分の手札リスト
    private List<GameObject> enemyHandCards = new List<GameObject>();   // 相手（CPU）の手札リスト

    [Header("場札に関する設定")]
    [SerializeField] private Transform[] fieldSlots; 
    private GameObject[] fieldCards = new GameObject[18]; 

    [Header("山札の厚み設定")]
    [SerializeField] private float pileSpacing = 0.1f;

    [Header("CPUの手札の表示設定")]
    [SerializeField] private Transform cpuHandTransform; // CPUの手札を表示する基準の位置

    [Header("プレイヤーの獲得エリア（種類ごとの置き場）")]
    [SerializeField] private Transform brightArea; // 光札の置き場
    [SerializeField] private Transform animalArea; // タネ札の置き場（※仮にanimalAreaなど）
    [SerializeField] private Transform ribbonArea; // 短冊の置き場
    [SerializeField] private Transform chaffArea;  // カスの置き場


    // 1. 山札をまとめる処理（リセット）
    public void PileUpCards()
    {
        myHandCards.Clear();
        enemyHandCards.Clear();
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

    // 2. 【拡張】誰が引くかを指定できるカードを引く処理
    public void DrawCardForPlayer(PlayerType targetPlayer)
    {
        List<GameObject> availableCards = GetAvailableCards();

        if (availableCards.Count == 0)
        {
            Debug.Log("山札もうないで！");
            return;
        }

        // ラックに応じた抽選で1枚選ぶ
        GameObject selectedCard = DecideCardByLuck(availableCards);
        CardController cardController = selectedCard.GetComponent<CardController>();

        if (cardController == null) return;

        if (targetPlayer == PlayerType.Player1)
        {
            // --- 自分の手札に追加する場合 ---
            myHandCards.Add(selectedCard);
            cardController.DrawCardToHand(); // カメラを親にする等の処理
            UpdateHandLayout(myHandCards, PlayerType.Player1);
            Debug.Log("自分がカードを1枚引いたで！");
        }
        else
        {
            // --- 相手（CPU等）の手札に追加する場合 ---
            enemyHandCards.Add(selectedCard);
            
            DrawCardToCpu(selectedCard); // CPUの手札位置に移動させる
            UpdateHandLayout(enemyHandCards, PlayerType.Player2);
            
            Debug.Log("相手（CPU）がカードを1枚引いたで！（現在の手札枚数: " + enemyHandCards.Count + "枚）");
        }
    }

    public void DrawCardToCpu(GameObject card)
    {
        enemyHandCards.Add(card);

        card.transform.position = cpuHandTransform.position;
        card.transform.rotation = cpuHandTransform.rotation;

        Debug.Log("相手（CPU）がカードを1枚引いたで！（現在の手札枚数: " + enemyHandCards.Count + "枚）");
    }

    // 手札を横一列に綺麗に並べ替える
    private void UpdateHandLayout(List<GameObject> handCards, PlayerType TargetPlayer)
    {
        int count = handCards.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject card = handCards[i];
            if (card == null) continue;

            float xOffset = (i - (count - 1) / 2f) * cardSpacing;

            if (TargetPlayer == PlayerType.Player1)
            {
                // 自分の手札の場合
                Vector3 targetLocalPos = new Vector3(
                    handBaseLocalPos.x + xOffset,
                    handBaseLocalPos.y,
                    handBaseLocalPos.z
                );

                card.transform.localPosition = targetLocalPos;
                card.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            else// 相手（CPU）の手札の場合
            {
                Vector3 targetLocalPos = new Vector3(
                    cpuHandTransform.position.x + xOffset,
                    cpuHandTransform.position.y,
                    cpuHandTransform.position.z
                );

                card.transform.localPosition = targetLocalPos;
                card.transform.localRotation = Quaternion.Euler(0, 180, 0); // CPUの手札は裏向きにする
            }

        }
    }

    private GameObject DecideCardByLuck(List<GameObject> candidates)
    {
        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    // ★【重要】山札に残っているカードを探す（自分・相手の手札、および場札のどれにも入っていないもの）
    private List<GameObject> GetAvailableCards()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (var card in allCardsInScene)
        {
            if (card == null) continue;

            bool isInMyHand = myHandCards.Contains(card);
            bool isInEnemyHand = enemyHandCards.Contains(card);
            bool isInField = System.Array.Exists(fieldCards, element => element == card);

            // どこにも含まれていなければ、山札の残りとする
            if (!isInMyHand && !isInEnemyHand && !isInField)
            {
                list.Add(card);
            }
        }
        return list;
    }

    // 3. 場札にカードを配る処理
    public void PlaceCardToField(int slotIndex) 
    {
        List<GameObject> availableCards = GetAvailableCards();

        if (availableCards.Count == 0)
        {
            Debug.Log("山札なし！");
            return;
        }

        if (slotIndex < 0 || slotIndex >= fieldSlots.Length)
        {
            Debug.LogWarning("指定された場札のスロット番号が無効やで！: " + slotIndex);
            return;
        }

        if (fieldCards[slotIndex] != null)
        {
            Debug.Log("そのマスにはすでにカードがあるで！");
            return;
        }

        GameObject selectedCard = DecideCardByLuck(availableCards);

        selectedCard.transform.SetParent(fieldSlots[slotIndex]);
        selectedCard.transform.position = fieldSlots[slotIndex].position;
        selectedCard.transform.rotation = fieldSlots[slotIndex].rotation;

        fieldCards[slotIndex] = selectedCard;
    }

    // 4. ゲームスタートボタンが押されたときの処理
    public void OnGameStartButtonClicked()
    {
        Debug.Log("ゲームスタート！初期配置を開始するで！");

        // 山札リセット
        PileUpCards();

        // 自分に8枚配る
        for (int i = 0; i < 8; i++)
        {
            DrawCardForPlayer(PlayerType.Player1);
        }

        // 相手（CPU）に8枚配る（データとして持たせる）
        for (int i = 0; i < 8; i++)
        {
            DrawCardForPlayer(PlayerType.Player2);
        }

        // 場札に8枚配置する（スロット 0 〜 7）
        for (int i = 0; i < 8; i++)
        {
            PlaceCardToField(i);
        }

        Debug.Log("初期配置がすべて完了したで！");
    }

    public void OnDrawCardButtonClicked()
    {
        DrawCardForPlayer(PlayerType.Player1);
    }

    // ★CPUのターンを実行する関数
    public void ExecuteCPUTurn()
    {
        Debug.Log("--- CPUのターン開始 ---");

        // 1. まずCPUも山札から1枚引く（すでに手札リストにある前提）
        DrawCardForPlayer(PlayerType.Player2);

        // 2. CPUの手札（enemyHandCards）にカードが何枚あるかチェック
        if (enemyHandCards.Count == 0)
        {
            Debug.Log("CPUの手札がないから何も出せないで！");
            return;
        }

        // 3. 思考ルーチン：手札の中から「どれを出すか」をランダムに1枚選ぶ
        // （※もっと賢いAIにするなら、ここで点数が高い札や月が一致する札を選ぶ計算を入れる！）
        int randomCardIndex = Random.Range(0, enemyHandCards.Count);
        GameObject chosenCard = enemyHandCards[randomCardIndex];

        // 4. 選んだカードを手札リストから削除する
        enemyHandCards.RemoveAt(randomCardIndex);

        // 5. 実際にカードを場に出す（例として、今空いている最初の場札スロットに置く処理など）
        PlaceCardToChosenSlot(chosenCard);

        Debug.Log("--- CPUのターン終了 ---");
    }

    // CPUが選んだカードを場に配置する補助関数
    private void PlaceCardToChosenSlot(GameObject cardToPlace)
    {
        // 空いている場札のスロットを探してそこに置く
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            if (fieldCards[i] == null) // 空きマスを発見！
            {
                cardToPlace.transform.SetParent(fieldSlots[i]);
                cardToPlace.transform.position = fieldSlots[i].position;
                cardToPlace.transform.rotation = fieldSlots[i].rotation;
                
                // 相手の手札だったから、画面に出すためにアクティブ（表示）にする
                cardToPlace.SetActive(true);

                fieldCards[i] = cardToPlace;
                Debug.Log("CPUがカードを場札のスロット " + i + " に配置したで！");
                return;
            }
        }
        Debug.Log("場札に空きがないで……！");
    }


    // GameManagerから安全に呼び出すための公開メソッド

    // ① 場札の配列（fieldCards）を外部から覗き見できるようにする（読み取り専用の公開プロパティやメソッド）
    public GameObject[] GetFieldCards()
    {
        return fieldCards;
    }

    // ② 自分の手札リスト（myHandCards）を外部から覗き見できるようにする
    public List<GameObject> GetMyHandCards()
    {
        return myHandCards;
    }

    // ③ 場札を置くスロットの総数（fieldSlots.Length）を教える
    public int GetFieldSlotCount()
    {
        return fieldSlots.Length;
    }

    // ④ 指定されたカードを、手札から取り除いて指定の場札スロットに配置する実務処理
    public void PlaceCardFromHandToField(GameObject selectedCard, int slotIndex, MonoBehaviour runner)
    {
        // 1. 自分の手札リストからカードを取り除く
        if (myHandCards.Contains(selectedCard))
        {
            myHandCards.Remove(selectedCard);
        }

        // 2. 指定された場札スロットにセットする
        fieldSlots[slotIndex] = fieldSlots[slotIndex]; // 念のため
        selectedCard.transform.SetParent(null); // 一旦親を外す
        Transform targetPosition = fieldSlots[slotIndex];
        runner.StartCoroutine(AnimateAndPlaceCard(selectedCard, targetPosition, slotIndex));
        selectedCard.transform.SetParent(fieldSlots[slotIndex]);

        if (fieldCards[slotIndex] == null)
        {
            fieldCards[slotIndex] = selectedCard;
        }

        // 3. 残った手札の並びを綺麗に再計算して詰める
        UpdateHandLayout(myHandCards, PlayerType.Player1);

        Debug.Log("手札からカードを場札のスロット " + slotIndex + " に配置しました！");
    }


    private IEnumerator AnimateAndPlaceCard(GameObject card, Transform targetSlot, int slotIndex)
    {
        float duration = 1f; // 移動にかかる時間（秒）
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        float elapsedTime = 0f;

        Vector3 targetPosition = targetSlot.position;

        if (fieldCards[slotIndex] != null)
        {
            targetPosition = targetPosition + new Vector3(1f, 0.1f, 0.5f); //少しずらす
        }

        // 1秒かけて滑らかに目標地点へ動かす
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            card.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            card.transform.rotation = Quaternion.Lerp(startRot, targetSlot.rotation, t);

            yield return null; // 次のフレームまで待つ
        }

        // 最後にピタッと位置を合わせる
        card.transform.position = targetPosition;
        card.transform.rotation = targetSlot.rotation;

        Debug.Log("カードがスライドして場にセットされたで！");
    }

    // 指定したスロットの札が2枚以上になったときに呼ばれる回収関数
    public IEnumerator CollectAndCategorizeCards(List<GameObject> cardsToCollect, MonoBehaviour runner)
    {
        foreach (GameObject card in cardsToCollect)
        {
            if (card == null) continue;

            // 1. カードのデータを取得して種類（CardType）を調べる
            CardData data = card.GetComponent<CardData>();
            if (data == null) continue;

            // 2. 種類に応じて、どのエリアに持っていくかを決める
            Transform targetArea = GetTargetAreaByType(data.cardType);

            // 3. 親子関係をいったん外す
            card.transform.SetParent(null);

            // 4. エリアに向けて滑らかに移動させる（さっきのコルーチンを応用！）
            yield return runner.StartCoroutine(SlideCardToCollectionArea(card, targetArea));
        }

        Debug.Log("獲得した札の分類と回収が完了したで！");
    }

    // カードの種類（CardType）から、対応する置き場を返す関数
    private Transform GetTargetAreaByType(CardType type)
    {
        switch (type)
        {
            case CardType.Bright: return brightArea;
            case CardType.Animal: return animalArea;
            case CardType.Ribbon: return ribbonArea;
            case CardType.Chaff:  return chaffArea;
            default: return chaffArea;
        }
    }

    // 1枚ずつコレクションエリアへスライド移動させるコルーチン
    private IEnumerator SlideCardToCollectionArea(GameObject card, Transform targetArea)
    {
        float duration = 0.2f;
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        
        // エリアの少しずつ位置をずらして重ねて置くためのオフセット計算（お好みで調整）
        Vector3 targetPos = targetArea.position + new Vector3(targetArea.childCount * 0.15f, 0, 0);

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            card.transform.rotation = Quaternion.Lerp(startRot, targetArea.rotation, t);

            yield return null;
        }

        card.transform.position = targetPos;
        card.transform.rotation = targetArea.rotation;
        card.transform.SetParent(targetArea); // 最終的に獲得エリアの子にする
    }
}