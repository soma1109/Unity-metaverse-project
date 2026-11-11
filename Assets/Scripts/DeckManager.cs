using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// プレイヤーの種類を識別するための「列挙型（Enum）」
public enum PlayerType
{
    Player1, // 自分
    Player2  // 相手（CPUやマルチの対戦相手）
}

public enum CardSource
{
    FromHand, //手札から
    FromDisplay //見せ札から
}

[System.Serializable]
public class PlayerCollection
{
    // ① 4つのリスト（光、タネ、短冊、カス）を束ねた配列の箱を用意する
    private List<GameObject>[] lists = new List<GameObject>[]
    {
        new List<GameObject>(), // 0番：光札のリスト
        new List<GameObject>(), // 1番：タネ札のリスト
        new List<GameObject>(), // 2番：短冊札のリスト
        new List<GameObject>()  // 3番：カス札のリスト
    };

    // ★全種類のリストが詰まった配列をそのまま丸ごと返す窓口！
    public List<GameObject>[] GetAllLists()
    {
        return lists;
    }

    // ② 指定した種類（CardType）のリストをパッと取り出すための関数
    public List<GameObject> GetList(CardType type)
    {
        return lists[(int)type];
    }

    // ③ 指定した種類の「現在の枚数」を返す関数（表示をズラす計算に使う！）
    public int GetCount(CardType type)
    {
        return lists[(int)type].Count;
    }

    // ④ カードが回収されたときに、適切なリストへ「追加（Add）」する関数
    public void AddCard(GameObject card, CardType type)
    {
        lists[(int)type].Add(card);
    }

    // ★追加：持っているカードのリストをすべて空っぽにする関数
    public void Clear()
    {
        foreach (var list in lists)
        {
            if (list != null)
            {
                list.Clear();
            }
        }
    }
}

public class DeckManager : MonoBehaviour
{
    [Header("GameManagerの参照")]
    [SerializeField] private GameManager gameManager;

    [Header("シーン内にあるすべてのカードのリスト")]
    [SerializeField] private List<GameObject> allCardsInScene = new List<GameObject>();

    [Header("山札として積み上げる基準の位置")]
    [SerializeField] private Transform deckPileTransform;

    [Header("見せ札(一時的にプレイヤーに見えるようにしている札)に関する設定")]
    [SerializeField] private Transform displayPositionTransform;
    private List<GameObject> displayCards = new List<GameObject>();

    [Header("プレイヤーのラック（運）のステータス")]
    public int playerLuck = 10;

    [Header("自分の手札の並びに関する設定")]
    [SerializeField] private float handCardSpacing = 3f; 
    [SerializeField] private Vector3 handBaseLocalPos = new Vector3(0f, 2f, 15f); 

    [Header("手札の管理（自分と相手を分離）")]
    private List<GameObject> myHandCards = new List<GameObject>();     // 自分の手札リスト
    private List<GameObject> enemyHandCards = new List<GameObject>();   // 相手（CPU）の手札リスト

    [Header("持ち札の管理")]
    [SerializeField] private float heldCardSpacing = 1f; // 獲得札を横にずらす間隔
    // UnityのInspectorで、上から順に [0:光, 1:タネ, 2:短冊, 3:カス] の順番でアサインできるようにする！
    [Header("自分の獲得札エリア")]
    [SerializeField] private Transform[] areaTransforms = new Transform[4];
    [Header("相手の獲得札エリア")]
    [SerializeField] private Transform[] enemyAreaTransforms = new Transform[4];
    private Vector3[] areaOffsets = new Vector3[4];
    private Vector3[] enemyAreaOffsets = new Vector3[4];

    private void Start()
    {
        // for文で一気に初期化するから、変数が何個増えてもコードの長さは変わらへん！
        for (int i = 0; i < areaTransforms.Length; i++)
        {
            if (areaTransforms[i] != null)
            {
                areaOffsets[i] = areaTransforms[i].position;
            }
        }
        for (int i = 0; i < enemyAreaTransforms.Length; i++)
        {
            if (enemyAreaTransforms[i] != null)
            {
                enemyAreaOffsets[i] = enemyAreaTransforms[i].position;
            }
        }
    }
    private PlayerCollection playerCollection = new PlayerCollection(); // 自分の獲得札の管理
    private PlayerCollection enemyCollection = new PlayerCollection(); // 相手（CPU）の獲得札の管理

    [Header("場札に関する設定")]
    [SerializeField] private Transform[] fieldSlots; 
    private List<GameObject>[] fieldSlotCardLists = new List<GameObject>[18]; // 場札のスロットごとのカードリスト
    private void Awake()
    {
        // 18個すべての棚の扉を開けて、中に「空のリスト（箱）」を1つずつ入れていく作業
        for (int i = 0; i < fieldSlotCardLists.Length; i++)
        {
            fieldSlotCardLists[i] = new List<GameObject>(); 
        }
    }

    [Header("山札の厚み設定")]
    [SerializeField] private float pileSpacing = 0.1f;

    [Header("CPUの手札の表示設定")]
    [SerializeField] private Transform cpuHandTransform; // CPUの手札を表示する基準の位置


    // 1. 山札をまとめる処理（リセット）
    public void PileUpCards()
    {
        myHandCards.Clear();
        enemyHandCards.Clear();
        displayCards.Clear();
        // fieldSlotCardListsのクリア処理(配列自体を消すのではなく、それぞれのスロットのリストの中身をキレイに空にする)
        foreach (var slotList in fieldSlotCardLists)
        {
            if (slotList != null)
            {
                slotList.Clear();
            }
        }
        // 持ち札のクリア処理
        playerCollection.Clear();
        enemyCollection.Clear();

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
            Debug.Log("山札なし");
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
            Debug.Log("自分がカードを1枚引いたで！（現在の手札枚数: " + myHandCards.Count + "枚）");
        }
        else
        {
            // --- 相手（CPU）の手札に追加する場合 ---
            DrawCardToCpu(selectedCard); // CPUの手札位置に移動させる
            UpdateHandLayout(enemyHandCards, PlayerType.Player2);
        }
    }

    public void DrawCardToCpu(GameObject card)
    {
        enemyHandCards.Add(card);

        card.transform.position = cpuHandTransform.position;
        card.transform.rotation = cpuHandTransform.rotation;

        Debug.Log("相手（CPU）がカードを1枚引いた（現在の手札枚数: " + enemyHandCards.Count + "枚）");
    }

    // 手札を横一列に綺麗に並べ替える
    private void UpdateHandLayout(List<GameObject> handCards, PlayerType TargetPlayer)
    {
        int count = handCards.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject card = handCards[i];
            if (card == null) continue;

            float xOffset = (i - (count - 1) / 2f) * handCardSpacing;

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

    //カードの位置移動汎用関数
    private IEnumerator MoveCardToPosition(GameObject card, Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            card.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            card.transform.rotation = Quaternion.Lerp(startRot, targetRotation, t);

            yield return null; // 次のフレームまで待つ
        }

        // 最後にピタッと位置を合わせる
        card.transform.position = targetPosition;
        card.transform.rotation = targetRotation;
    }

    // 山札から場札にカードを出す処理
    public void PlaceCardFromDisplayToField(GameObject selectedCard, int slotIndex, PlayerType player)
    {
        // 見せ札リストからカードを取り除く
        if (displayCards.Contains(selectedCard))
        {
            displayCards.Remove(selectedCard);
        }

        // 2. 指定された場札スロットにセットする
        fieldSlots[slotIndex] = fieldSlots[slotIndex]; // 念のため
        selectedCard.transform.SetParent(null); // 一旦親を外す
        Transform targetPosition = fieldSlots[slotIndex];
        StartCoroutine(AnimateAndPlaceCard(selectedCard, targetPosition, slotIndex, CardSource.FromDisplay, player));
        selectedCard.transform.SetParent(fieldSlots[slotIndex]);

        fieldSlotCardLists[slotIndex].Add(selectedCard);

        //displayの並びをきれいにする処理はここに！

        Debug.Log("見せ札からカードを場札のスロット " + slotIndex + " に配置しました！");
    }

    //山札をdisplayに置く処理
    public void onDrawCardToDisplayButtonClicked()
    {
        PlaceCardFromDeckToDisplay();
    }

    public void PlaceCardFromDeckToDisplay()
    {
        List<GameObject> availableCards = GetAvailableCards();

        if (availableCards.Count == 0)
        {
            Debug.Log("山札なし！");
            return;
        }

        // ランダムに1枚選ぶ
        GameObject selectedCard = DecideCardByLuck(availableCards);

        // displayに置く
        displayCards.Add(selectedCard);
        Vector3 displayPosition = displayPositionTransform.position;
        Quaternion displayRotation = displayPositionTransform.rotation;
        StartCoroutine(MoveCardToPosition(selectedCard, displayPosition, displayRotation, 0.5f));
        selectedCard.transform.SetParent(displayPositionTransform);

        Debug.Log("山札から見せ札にカードを配置しました！（現在の見せ札枚数: " + displayCards.Count + "枚）");
    }

    //持ち札の横にずらして表示する
    private void UpdateHeldLayout(PlayerType targetPlayer)
    {
        List<GameObject>[] heldCards;
        Transform[] targetAreas;
     
        if (targetPlayer == PlayerType.Player1)
        {
            heldCards = playerCollection.GetAllLists();
            targetAreas = areaTransforms;
        }
        else
        {
            heldCards = enemyCollection.GetAllLists();
            targetAreas = enemyAreaTransforms;
        }

        int length = heldCards.Length;

        for (int i = 0; i < length; i++)
        {
            Transform targetArea = targetAreas[i];
            if (targetArea == null) continue;

            int count = heldCards[i].Count;
            for (int j = 0; j < count; j++)
            {
                GameObject card = heldCards[i][j];
                if (card == null) continue;

                float xOffset = j * heldCardSpacing;
                float zOffset = j * heldCardSpacing * -0.05f; // 少しだけ高さもずらす（好みで調整）

                if (targetPlayer == PlayerType.Player1)
                {
                    // 自分の持ち札の場合
                    card.transform.SetParent(targetArea); // 確実に親にしておく
                
                    // 親（置き場）のローカル座標を基準にして、横に少しずつずらす
                    card.transform.localPosition = new Vector3(xOffset, 0f, zOffset);
                    card.transform.localRotation = Quaternion.identity; // 置き場の向きに合わせる
                }
                else // 相手（CPU）の持ち札の場合
                {
                    card.transform.SetParent(targetArea);
                    card.transform.localPosition = new Vector3(xOffset, 0f, zOffset);
                    card.transform.localRotation = Quaternion.identity;
                }
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
            bool inInDisplay = displayCards.Contains(card);
            bool isInHeld = playerCollection.GetAllLists().Any(list => list.Contains(card)) || enemyCollection.GetAllLists().Any(list => list.Contains(card));
            // 例：指定したカードが、どのフィールドスロットのリストのいずれかに含まれているか探す
            bool isInAnySlot = System.Array.Exists(fieldSlotCardLists, slotList => slotList != null && slotList.Contains(card));

            // どこにも含まれていなければ、山札の残りとする
            if (!isInMyHand && !isInEnemyHand && !isInAnySlot && !inInDisplay && !isInHeld)
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
            Debug.LogWarning("指定された場札のスロット番号が無効: " + slotIndex);
            return;
        }

        if (fieldSlotCardLists[slotIndex] != null && fieldSlotCardLists[slotIndex].Count > 0)
        {
            Debug.Log("そのマスにはすでにカードがある");
            return;
        }

        GameObject selectedCard = DecideCardByLuck(availableCards);

        selectedCard.transform.SetParent(fieldSlots[slotIndex]);
        selectedCard.transform.position = fieldSlots[slotIndex].position;
        selectedCard.transform.rotation = fieldSlots[slotIndex].rotation;

        fieldSlotCardLists[slotIndex].Add(selectedCard);

        Debug.Log("場札のスロット " + slotIndex + " にカードを配置");
    }

    // 4. ゲームスタートボタンが押されたときの処理
    public void OnGameStartButtonClicked()
    {
        Debug.Log("ゲームスタート！初期配置を開始");

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

        Debug.Log("初期配置がすべて完了");
    }

    public void OnDrawCardButtonClicked()
    {
        DrawCardForPlayer(PlayerType.Player1);
    }

    // ★CPUのターンを実行する関数
    public void ExecuteCPUTurn()
    {
        Debug.Log("--- CPUのターン開始 ---");

        // 1. まずCPUも山札から1枚引く
        DrawCardForPlayer(PlayerType.Player2);

        // 2. CPUの手札（enemyHandCards）にカードが何枚あるかチェック
        if (enemyHandCards.Count == 0)
        {
            Debug.Log("CPUの手札がないから何も出せない");
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
            if (fieldSlotCardLists[i] == null || fieldSlotCardLists[i].Count == 0) // 空きマスを発見！
            {
                cardToPlace.transform.SetParent(fieldSlots[i]);
                cardToPlace.transform.position = fieldSlots[i].position;
                cardToPlace.transform.rotation = fieldSlots[i].rotation;
                
                // 相手の手札だったから、画面に出すためにアクティブ（表示）にする
                cardToPlace.SetActive(true);

                fieldSlotCardLists[i].Add(cardToPlace);
                Debug.Log("CPUがカードを場札のスロット " + i + " に配置");
                return;
            }
        }
        Debug.Log("場札に空きがない");
    }


    // GameManagerから安全に呼び出すための公開メソッド

    //fieldSlotCardListsの内容を外部から覗き見できるようにする
    public List<GameObject>[] GetFieldSlotCardLists()
    {
        return fieldSlotCardLists;
    }

    // ② 自分の手札リスト（myHandCards）を外部から覗き見できるようにする
    public List<GameObject> GetMyHandCards()
    {
        return myHandCards;
    }

    // 相手の手札リスト外部から見る用
    public List<GameObject> GetCpuHandCards()
    {
        return enemyHandCards;
    }

    // ② 見せ札リスト（displayCards）を外部から覗き見できるようにする
    public List<GameObject> GetDisplayCards()
    {
        return displayCards;
    }

    // ③ 場札を置くスロットの総数（fieldSlots.Length）を教える
    public int GetFieldSlotCount()
    {
        return fieldSlots.Length;
    }

    public PlayerCollection GetPlayerCollection()
    {
        return playerCollection;
    }
    public PlayerCollection GetEnemyCollection()
    {
        return enemyCollection;
    }

    // ④ 指定されたカードを、手札から取り除いて指定の場札スロットに配置する実務処理
    public void PlaceCardFromHandToField(GameObject selectedCard, int slotIndex, PlayerType player)
    {
        // 1. 自分の手札リストからカードを取り除く
        if (myHandCards.Contains(selectedCard)) myHandCards.Remove(selectedCard);
        if (enemyHandCards.Contains(selectedCard)) enemyHandCards.Remove(selectedCard);

        // 2. 指定された場札スロットにセットする
        fieldSlots[slotIndex] = fieldSlots[slotIndex]; // 念のため
        selectedCard.transform.SetParent(null); // 一旦親を外す
        Transform targetPosition = fieldSlots[slotIndex];
        StartCoroutine(AnimateAndPlaceCard(selectedCard, targetPosition, slotIndex, CardSource.FromHand, player));
        selectedCard.transform.SetParent(fieldSlots[slotIndex]);

        fieldSlotCardLists[slotIndex].Add(selectedCard);

        // 3. 残った手札の並びを綺麗に再計算して詰める
        if (player == PlayerType.Player1) UpdateHandLayout(myHandCards, PlayerType.Player1);
        if (player == PlayerType.Player2) UpdateHandLayout(enemyHandCards, PlayerType.Player2);

        Debug.Log("手札からカードを場札のスロット " + slotIndex + " に配置しました！");
    }


    private IEnumerator AnimateAndPlaceCard(GameObject card, Transform targetSlot, int slotIndex, CardSource source, PlayerType player)
    {
        float duration = 1f; // 移動にかかる時間（秒）
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        float elapsedTime = 0f;

        Vector3 targetPosition = targetSlot.position;

        if (fieldSlotCardLists[slotIndex] != null && fieldSlotCardLists[slotIndex].Count > 0)
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

        Debug.Log("カードがスライドして場にセットされた");

        // アニメーションが終わったこの瞬間に、枚数をチェックして回収を呼ぶ！
        if (fieldSlotCardLists[slotIndex].Count >= 2)
        {
            Debug.Log($"スロット {slotIndex} の札が2枚になったので回収を開始");

            List<GameObject> collectedCards = new List<GameObject>(fieldSlotCardLists[slotIndex]);
            fieldSlotCardLists[slotIndex].Clear();

            // 獲得エリアへの回収アニメーション（これもコルーチン）をここでスタート！
            yield return StartCoroutine(CollectAndCategorizeCards(collectedCards, player, this));
        }

        if (gameManager != null)
        {
            if (source == CardSource.FromHand)
            {
                gameManager.OnActionFromHandFinished(player);
            }
            else if(source == CardSource.FromDisplay)
            {
                gameManager.OnDisplayPlayFinished();
            }

        }
    }

    // 指定したスロットの札が2枚以上になったときに呼ばれる回収関数
    public IEnumerator CollectAndCategorizeCards(List<GameObject> cardsToCollect, PlayerType player, MonoBehaviour runner)
    {
        foreach (GameObject card in cardsToCollect)
        {
            if (card == null) continue;

            // 1. カードのデータを取得して種類（CardType）を調べる
            CardData data = card.GetComponent<CardData>();
            if (data == null) continue;

            // 2. 種類に応じて、どのエリアに持っていくかを決める
            Transform targetArea = GetTargetAreaByType(data.cardType, player);
            Debug.Log($"カード {card.name} は {data.cardType} なので、{targetArea.name} に回収する");

            //獲得札リストに追加
            if (player == PlayerType.Player1) playerCollection.AddCard(card, data.cardType); 
            if (player == PlayerType.Player2) enemyCollection.AddCard(card, data.cardType);
            //追加されたカードタイプの獲得枚数を表示するデバッグ
            Debug.Log($"現在の {data.cardType} の獲得枚数: {playerCollection.GetCount(data.cardType)}");

            // 親子関係をいったん外す
            card.transform.SetParent(null);

            // 4. エリアに向けて滑らかに移動させる（さっきのコルーチンを応用！）
            yield return runner.StartCoroutine(SlideCardToCollectionArea(card, targetArea, player));
        }

        Debug.Log("獲得した札の分類と回収が完了した");
    }

    // カードの種類（CardType）から、対応する置き場を返す関数
    private Transform GetTargetAreaByType(CardType type, PlayerType player)
    {
        Transform[] targetAreas = areaTransforms; 

        if (player == PlayerType.Player2)
        {
            targetAreas = enemyAreaTransforms;
        }

        switch (type)
        {
            case CardType.Bright: return targetAreas[0]; // 光札のエリア
            case CardType.Animal: return targetAreas[1]; // タネ札のエリア
            case CardType.Ribbon: return targetAreas[2]; // 短冊札のエリア
            case CardType.Chaff:  return targetAreas[3]; // カス札のエリア
            default: return targetAreas[3]; // デフォルトはカス札のエリア
        }
    }

    // 1枚ずつコレクションエリアへスライド移動させるコルーチン
    private IEnumerator SlideCardToCollectionArea(GameObject card, Transform targetArea, PlayerType player)
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
        UpdateHeldLayout(player); // 自分の獲得札の表示を更新
        
    }
}