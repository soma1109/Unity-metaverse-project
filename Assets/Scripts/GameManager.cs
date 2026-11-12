using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class GameManager : MonoBehaviour
{
    [Header("DeckManagerの参照")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private YakuManager yakuManager;

    // --- 選択状態を管理する変数 ---
    private GameObject selectedHandCard = null;        // 現在選択（持ち上げ）されている手札のカード
    private GameObject selectedDisplayCard = null;     // 現在選択（持ち上げ）されている見せ札のカード
    private bool isWaitingForFieldSelection = false;   // 同じ月が複数あって場札を選ぶのを待っている状態か    
    private List<int> validTargetFieldSlots = new List<int>();

    // 親が決まったかどうか、どちらが親になったかを保持する変数など
    public bool isDecidingDealer;
    public bool isPlayerParentFirst; // trueならプレイヤーが親

    // ゲームの進行状況に関するフラグ
    private bool isGameStarting  = false; //ゲームが進行中か
    private bool isMyTurn = false; //自分のターンか相手のターンか
    private bool isProcessing = false; //何らかの処理中か
    private bool canDecideHandCard = false; //自分のターンで出す手札を選んだかどうか

    public void GameStart()
    {
        StartCoroutine(GameStartRoutine());
    }
    private IEnumerator GameStartRoutine()
    {
        isGameStarting = true;
        isProcessing = true;
        Debug.Log("=== ゲームスタート処理開始 ===");

        deckManager.PileUpCards();
        yield return StartCoroutine(DecideInitialDealer());

        deckManager.OnGameStartButtonClicked();

        yield return new WaitForSeconds(1.0f);

        isProcessing = false; //処理中のフラグを解除

        //親がどちらかによって最初のターンを決める
        if (isPlayerParentFirst)
        {
            Debug.Log(" プレイヤーが親です");
            isMyTurn = true;
            canDecideHandCard = true;
        }
        else
        {
            Debug.Log(" 相手プレイヤー(CPU)が親です");
            isMyTurn = false;
            ExecuteCPUTurn();
        }

    }

    // ★ボタンから直接押されたときに呼ばれる「窓口用の普通のメソッド」
    public void OnClickDecideDealerButton()
    {
        // ★もしすでに処理中だったら、連打されても無視する！
        if (isDecidingDealer)
        {
            Debug.Log("現在、親決め処理中です。終わるまでお待ちください！");
            return;
        }
        
        deckManager.PileUpCards();
        // ここでコルーチンを起動する！
        StartCoroutine(DecideInitialDealer());
    }

    /// <summary>
    /// 最初の親を決めるコルーチン（決まるまで引き直すループ処理）
    /// </summary>
    public IEnumerator DecideInitialDealer()
    {
        isDecidingDealer = true;

        bool isDecided = false;
        // 何回特別枠に引いたかを記憶
        int SpecialCardsCount = 0;

        while (!isDecided)
        {
            Debug.Log("親決めのために、お互い札を1枚引きます…！");

            // 1. プレイヤーと敵がそれぞれ山札から1枚引く（CardDataを持っていると仮定）
            deckManager.PlaceCardFromDeckToSpecialSlot(PlayerType.Player1);
            List<GameObject> playerSpecialCards = deckManager.GetMySpecialCards();
            GameObject playerSpecialCard = playerSpecialCards[SpecialCardsCount];
            deckManager.PlaceCardFromDeckToSpecialSlot(PlayerType.Player2);
            List<GameObject> enemySpecialCards = deckManager.GetEnemySpecialCards();
            GameObject enemySpecialCard = enemySpecialCards[SpecialCardsCount];

            if (playerSpecialCard == null || enemySpecialCard == null)
            {
                Debug.LogError("山札にカードが足りません！");
                yield break;
            }

            CardData playerData = playerSpecialCard.GetComponent<CardData>();
            CardData enemyData = enemySpecialCard.GetComponent<CardData>();

            Debug.Log($"プレイヤーが引いた札: {playerData.month}月 / 敵が引いた札: {enemyData.month}月");

            // 2. 月の大小を比較する（※花札の基本は「月が小さい方」が親）
            if (playerData.month < enemyData.month)
            {
                isPlayerParentFirst = true;
                isDecided = true;
                Debug.Log($"【親決定】プレイヤーの月（{playerData.month}月）の方が小さいため、プレイヤーが親です！");
            }
            else if (enemyData.month < playerData.month)
            {
                isPlayerParentFirst = false;
                isDecided = true;
                Debug.Log($"【親決定】敵の月（{enemyData.month}月）の方が小さいため、敵が親です！");
            }
            else
            {
                Debug.Log($"お互い {playerData.month}月 で同点です！もう一度引き直します！");
                // 同点の場合はループが継続してもう一度引き直す
            }

            // 演出として少しウェイト（待機）を入れるとプレイヤーに見やすくて親切
            yield return new WaitForSeconds(3f);

            SpecialCardsCount += 1;

            // TODO: 引いたカードを山札に戻す、または場札にするなどの片付け処理をここに挟む
        }

        // 3. 親が決まったら、引いたカードを山札に一旦すべて戻してシャッフルする処理へ繋げる
        deckManager.PileUpCards();

        isDecidingDealer = false;
        Debug.Log("親決めと山札のリセットが完了");
    }

    // プレイヤーが手札のカードをクリックしたときの処理
    public void OnHandCardClicked(GameObject clickedCard)
    {
        if (!isMyTurn || isProcessing)
        {
            Debug.Log("今は自分のターンではない");
            return;
        }
        if (!canDecideHandCard)
        {
            Debug.Log("手札から場札への処理は終了している");
            return;
        }

        // --- パターン1：すでに別のカードが選択されていて、それとは「違うカード」をクリックした場合 ---
        if (selectedHandCard != null && selectedHandCard != clickedCard)
        {
            ResetCardPosition(selectedHandCard);

            selectedHandCard = clickedCard;
            LiftCardUp(selectedHandCard);
            Debug.Log(clickedCard.name + " を選択したで（1回目のクリック）");

            // 新しく選んだカードについて、同じ月の場札が複数あるか事前にチェックする
            CheckCardMatching(selectedHandCard, CardSource.FromHand);
            return;
        }

        // --- パターン2：「初めて」手札のカードをクリックした場合 ---
        if (selectedHandCard == null)
        {
            selectedHandCard = clickedCard;
            LiftCardUp(selectedHandCard);
            Debug.Log(clickedCard.name + " を選択したで（1回目のクリック）");

            // 選んだカードについて、同じ月の場札が複数あるか事前にチェックする
            CheckCardMatching(selectedHandCard, CardSource.FromHand);
            return;
        }

        // --- パターン3：すでに選択されているカードを「もう一度」クリックした場合（2回目のクリック＝決定！） ---
        if (selectedHandCard == clickedCard)
        {
            // もし「同じ月が複数あるから場の選択を待っている状態」のときに同じ手札をもう一度クリックしたら、選択キャンセルにする
            if (isWaitingForFieldSelection)
            {
                Debug.Log("選択をキャンセルしたで！");
                ResetCardPosition(selectedHandCard);
                isWaitingForFieldSelection = false;
                validTargetFieldSlots.Clear();
                selectedHandCard = null;
                return;
            }

            Debug.Log(clickedCard.name + " が決定されたで！場に出す処理を開始するよ！");

            canDecideHandCard = false; //場に出す札が決定された瞬間に手札を出せないようにする
            
            ResetCardPosition(selectedHandCard);
            
            // 1枚だけ、または空きスロットに置く処理を実行
            ExecuteNormalPlay(selectedHandCard, CardSource.FromHand, PlayerType.Player1);

            selectedHandCard = null;
        }
    }

    // カードを少し上に持ち上げる処理
    private void LiftCardUp(GameObject card)
    {
        Vector3 pos = card.transform.localPosition;
        card.transform.localPosition = new Vector3(pos.x, pos.y + 0.5f, pos.z);
    }

    // カードの位置を元の高さに戻す処理
    private void ResetCardPosition(GameObject card)
    {
        Vector3 pos = card.transform.localPosition;
        card.transform.localPosition = new Vector3(pos.x, pos.y - 0.5f, pos.z);
    }

    // ★【新設】手札をクリックした時点で、同じ月の場札が複数あるかを事前チェックする関数
    private void CheckCardMatching(GameObject selectedCard, CardSource source)
    {
        CardData playedCardData = selectedCard.GetComponent<CardData>();
        if (playedCardData == null) return;

        int targetMonth = playedCardData.month;
        List<GameObject>[] fieldSlotCardLists = deckManager.GetFieldSlotCardLists();

        List<int> matchingFieldSlots = new List<int>();

        for (int i = 0; i < fieldSlotCardLists.Length; i++)
        {
            if (fieldSlotCardLists[i] == null || fieldSlotCardLists[i].Count == 0) continue;

            GameObject fieldCard = fieldSlotCardLists[i][0]; // 1枚目をチェック
            CardData fieldCardData = fieldCard.GetComponent<CardData>();
            if (fieldCardData != null && fieldCardData.month == targetMonth)
            {
                matchingFieldSlots.Add(i);
            }
        }

        // もし同じ月の場札が「2枚以上」ある場合
        if (matchingFieldSlots.Count > 1)
        {
            isWaitingForFieldSelection = true;
            if (source == CardSource.FromHand)
            {
                selectedHandCard = selectedCard;
            }
            else if (source == CardSource.FromDisplay)
            {
                selectedDisplayCard = selectedCard;
            }
            validTargetFieldSlots = matchingFieldSlots;
            Debug.Log("同じ月の場札が複数あるで！ 持ち上がったまま、合わせたい場札のカードをどれかクリックしてな！");
        }
        else
        {
            // 1枚以下なら、選択待ちにはせず、2回目のクリックを待つ状態にする
            isWaitingForFieldSelection = false;
        }
    }

    // 通常のプレイ処理（同じ月が1枚、または0枚のとき）
    private void ExecuteNormalPlay(GameObject selectedCard, CardSource source, PlayerType player)
    {
        CardData playedCardData = selectedCard.GetComponent<CardData>();
        if (playedCardData == null) return;

        int targetMonth = playedCardData.month;
        List<GameObject>[] fieldSlotCardLists = deckManager.GetFieldSlotCardLists();

        List<int> matchingFieldSlots = new List<int>();
        for (int i = 0; i < fieldSlotCardLists.Length; i++)
        {
            if (fieldSlotCardLists[i] == null || fieldSlotCardLists[i].Count == 0) continue;

            GameObject fieldCard = fieldSlotCardLists[i][0];
            CardData fieldCardData = fieldCard.GetComponent<CardData>();
            if (fieldCardData != null && fieldCardData.month == targetMonth)
            {
                matchingFieldSlots.Add(i);
            }
        }

        if (matchingFieldSlots.Count == 1)
        {
            int targetSlotIndex = matchingFieldSlots[0];

            if (source == CardSource.FromHand)
            {
                deckManager.PlaceCardFromHandToField(selectedCard, targetSlotIndex, player);
            }
            else if (source == CardSource.FromDisplay)
            {
                deckManager.PlaceCardFromDisplayToField(selectedCard, targetSlotIndex, player);
            }
            Debug.Log("同じ月の場札が1枚だったので自動で合わせました！");
        }
        else
        {
            int emptySlotIndex = GetLowestEmptyFieldSlot(fieldSlotCardLists);
            if (emptySlotIndex != -1)
            {
                if (source == CardSource.FromHand)
                    {
                        deckManager.PlaceCardFromHandToField(selectedCard, emptySlotIndex, player);
                    }
                    else if (source == CardSource.FromDisplay)
                    {
                        deckManager.PlaceCardFromDisplayToField(selectedCard, emptySlotIndex, player);
                    }
                Debug.Log("同じ月の場札がないため、一番若い空きスロットに置きました！");
            }
            else
            {
                Debug.Log("場札に空きスペースがありません！");
            }
        }
    }

    // 見せ札がクリックされたときの処理
    public void OnDisplayCardClicked(GameObject clickedDisplayCard)
    {
        // --- パターン1：すでに別のカードが選択されていて、それとは「違うカード」をクリックした場合 ---
        if (selectedDisplayCard != null && selectedDisplayCard != clickedDisplayCard)
        {
            ResetCardPosition(selectedDisplayCard);

            selectedDisplayCard = clickedDisplayCard;
            LiftCardUp(selectedDisplayCard);
            Debug.Log(clickedDisplayCard.name + " を選択した（1回目のクリック）");

            // 新しく選んだカードについて、同じ月の場札が複数あるか事前にチェックする
            CheckCardMatching(selectedDisplayCard, CardSource.FromDisplay);
            return;
        }

        // --- パターン2：「初めて」手札のカードをクリックした場合 ---
        if (selectedDisplayCard == null)
        {
            selectedDisplayCard = clickedDisplayCard;
            LiftCardUp(selectedDisplayCard);
            Debug.Log(clickedDisplayCard.name + " を選択した（1回目のクリック）");

            // 選んだカードについて、同じ月の場札が複数あるか事前にチェックする
            CheckCardMatching(selectedDisplayCard, CardSource.FromDisplay);
            return;
        }

        // --- パターン3：すでに選択されているカードを「もう一度」クリックした場合（2回目のクリック＝決定！） ---
        if (selectedDisplayCard == clickedDisplayCard)
        {
            // もし「同じ月が複数あるから場の選択を待っている状態」のときに同じ手札をもう一度クリックしたら、選択キャンセルにする
            if (isWaitingForFieldSelection)
            {
                Debug.Log("選択をキャンセルした");
                ResetCardPosition(selectedDisplayCard);
                isWaitingForFieldSelection = false;
                validTargetFieldSlots.Clear();
                selectedDisplayCard = null;
                return;
            }

            Debug.Log(clickedDisplayCard.name + " が決定された！場に出す処理を開始するよ！");
            
            ResetCardPosition(selectedDisplayCard);
            
            // 1枚だけ、または空きスロットに置く処理を実行
            ExecuteNormalPlay(selectedDisplayCard, CardSource.FromDisplay, PlayerType.Player1);

            selectedDisplayCard = null;
        }
    }

    // 場札がクリックされたときの処理
    public void OnFieldCardClicked(GameObject clickedFieldCard)
    {
        if (!isWaitingForFieldSelection) return;

        List<GameObject>[] fieldSlotCardLists = deckManager.GetFieldSlotCardLists();
        int clickedSlotIndex = -1;

        for (int i = 0; i < fieldSlotCardLists.Length; i++)
        {
            if (fieldSlotCardLists[i] != null && fieldSlotCardLists[i].Contains(clickedFieldCard))
            {
                clickedSlotIndex = i;
                break;
            }
        }

        if (validTargetFieldSlots.Contains(clickedSlotIndex))
        {
            canDecideHandCard = false; //場札に出す札を選択されたらフラグ解除 (※ただしこれは見せ札を出すときも呼ばれてることに注意)
            // 持ち上がっている札の位置を元に戻してから場に置く
            if (selectedHandCard != null)
            {
                ResetCardPosition(selectedHandCard);
                deckManager.PlaceCardFromHandToField(selectedHandCard, clickedSlotIndex, PlayerType.Player1);
            }

            if (selectedDisplayCard != null)
            {
                ResetCardPosition(selectedDisplayCard);
                deckManager.PlaceCardFromDisplayToField(selectedDisplayCard, clickedSlotIndex, PlayerType.Player1);
            }
            
            // 状態をすべてリセット
            isWaitingForFieldSelection = false;
            validTargetFieldSlots.Clear();
            selectedHandCard = null;
            selectedDisplayCard = null;
            

            Debug.Log("選んだ場札とのペアリングが完了したで！");
        }
        else
        {
            Debug.Log("そこは合わせられないカードやで！");
        }
    }

    private int GetLowestEmptyFieldSlot(List<GameObject>[] fieldSlotCardLists)
    {
        for (int i = 0; i < fieldSlotCardLists.Length; i++)
        {
            if (fieldSlotCardLists[i] == null || fieldSlotCardLists[i].Count == 0) return i;
        }
        return -1;
    }

    public void OnActionFromHandFinished(PlayerType player)
    {
        Debug.Log("プレイヤーの手札処理が完了");

        //山札から見せ札を出す関数を呼ぶ
        deckManager.PlaceCardFromDeckToDisplay();

        if (player == PlayerType.Player2)
        {
            List<GameObject> displayCards = deckManager.GetDisplayCards();
            if (displayCards != null && displayCards.Count > 0)
            {
                GameObject displayCard = displayCards[0];

                // 3. 共通の判定器で「どこに置くべきか」を計算
                CardMatchResult matchResult = EvaluateCardMatching(displayCard);
                
                int targetSlotIndex = -1;
                if (matchResult.matchingSlots.Count > 0)
                {
                    // CPUの場合、同じ月が複数（2枚）あったらとりあえず最初の1枚に合わせる（あるいはランダム等）
                    targetSlotIndex = matchResult.matchingSlots[0];
                }
                else
                {
                    targetSlotIndex = matchResult.emptySlotIndex;
                }

                if (targetSlotIndex != -1)
                {
                    deckManager.PlaceCardFromDisplayToField(displayCard, targetSlotIndex, player);
                }
            }
            else
            {
                Debug.Log("見せ札なし");
            }
        }
    }

    public void OnDisplayPlayFinished(PlayerType player)
    {
        Debug.Log("見せ札からのプレイ完了");
        turnSwitching(player);
    }

    // 指定した位置（またはオブジェクト）へ、時間をかけて滑らかに移動させるコルーチン
    private IEnumerator SlideToPosition(GameObject card, Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        // 1. 移動を開始する直前の「世界座標（World Position）」と「回転」を記録する
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        float elapsedTime = 0f;

        // 2. 指定した時間（duration）が経過するまで毎フレーム少しずつ動かす
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime; // 経過時間を進める
            float t = elapsedTime / duration; // 0.0 から 1.0 の進捗率を計算

            // Start から Target までを割合 t で滑らかにつなぐ（Lerp）
            card.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            card.transform.rotation = Quaternion.Lerp(startRot, targetRotation, t);

            yield return null; // 次のフレームまで待つ
        }

        // 3. 最後にきっちり目標の位置と回転に合わせる（ズレ防止）
        card.transform.position = targetPosition;
        card.transform.rotation = targetRotation;
    }

    

    // 【共通の判定関数】指定したカードを置くべき場所をチェックする
    // 戻り値：判定結果のデータ（何枚マッチしたか、スロットのリスト等）
    private CardMatchResult EvaluateCardMatching(GameObject card)
    {
        CardData cardData = card.GetComponent<CardData>();
        if (cardData == null) return null;

        int targetMonth = cardData.month;
        List<GameObject>[] fieldSlotCardLists = deckManager.GetFieldSlotCardLists();
        List<int> matchingFieldSlots = new List<int>();

        // 1. 場札から同じ月のカードを探す
        for (int i = 0; i < fieldSlotCardLists.Length; i++)
        {
            if (fieldSlotCardLists[i] == null || fieldSlotCardLists[i].Count == 0) continue;

            GameObject fieldCard = fieldSlotCardLists[i][0];
            CardData fieldCardData = fieldCard.GetComponent<CardData>();
            if (fieldCardData != null && fieldCardData.month == targetMonth)
            {
                matchingFieldSlots.Add(i);
            }
        }

        CardMatchResult result = new CardMatchResult();
        result.matchingSlots = matchingFieldSlots;
        result.emptySlotIndex = GetLowestEmptyFieldSlot(fieldSlotCardLists);
        return result;
    }

    // 判定結果をまとめるための小さなクラス（構造体みたいなもの）
    private class CardMatchResult
    {
        public List<int> matchingSlots = new List<int>(); // 同じ月があったスロットのリスト
        public int emptySlotIndex = -1;                   // 空きスロットの番号
    }

    public void ExecuteCPUTurn()
    {
        Debug.Log("--- CPUのターン開始 ---");

        // 1. CPUが山札から1枚引く
        //deckManager.DrawCardForPlayer(PlayerType.Player2);
        
        List<GameObject> cpuHand = deckManager.GetCpuHandCards(); 
        if (cpuHand.Count == 0) return;

        // 2. 手札からランダムに1枚選ぶ
        int randomCardIndex = Random.Range(0, cpuHand.Count);
        GameObject chosenCard = cpuHand[randomCardIndex];
        cpuHand.RemoveAt(randomCardIndex);

        // 3. 共通の判定器で「どこに置くべきか」を瞬時に計算！
        CardMatchResult matchResult = EvaluateCardMatching(chosenCard);
        
        int targetSlotIndex = -1;
        if (matchResult.matchingSlots.Count > 0)
        {
            // CPUの場合、同じ月が複数（2枚）あったらとりあえず最初の1枚に合わせる（あるいはランダム等）
            targetSlotIndex = matchResult.matchingSlots[0];
        }
        else
        {
            targetSlotIndex = matchResult.emptySlotIndex;
        }

        // 4. 決まったスロットにCPUのカードを配置する処理を呼ぶ
        if (targetSlotIndex != -1)
        {
            deckManager.PlaceCardFromHandToField(chosenCard, targetSlotIndex, PlayerType.Player2);
        }

        Debug.Log("CPUの手札処理終了");

        turnSwitching(PlayerType.Player2);
    }

    public void turnSwitching(PlayerType player)
    {
        if (player == PlayerType.Player1)
        {
            //自分のターンが終わった場合
            isMyTurn = false;
            ExecuteCPUTurn();
        }
        else
        {
            //相手のターンが終わった場合
            isMyTurn = true;
            canDecideHandCard = true;
        }
    }

    public void CalculatePoint()
    {
        if (yakuManager == null)
        {
            Debug.LogError("YakuManagerがアサインされていない");
            return;
        }

        PlayerCollection playerCollection = deckManager.GetPlayerCollection();
        PlayerCollection enemyCollection = deckManager.GetEnemyCollection();

        YakuResult myresult = yakuManager.EvaluateYaku(playerCollection);
        YakuResult enemyresult = yakuManager.EvaluateYaku(enemyCollection);

        // string.Join(", ", myresult.yakuNames) で、リストの名前を「, (カンマ)」で繋いだ1つの文章にしてくれる！
        Debug.Log($"自分の点数: {myresult.totalScore}点 [{string.Join(", ", myresult.yakuNames)}]");
        Debug.Log($"相手の点数: {enemyresult.totalScore}点 [{string.Join(", ", enemyresult.yakuNames)}]");
    }
}


