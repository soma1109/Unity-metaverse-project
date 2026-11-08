using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("DeckManagerの参照")]
    [SerializeField] private DeckManager deckManager;

    // --- 選択状態を管理する変数 ---
    private GameObject selectedHandCard = null;        // 現在選択（持ち上げ）されている手札のカード
    private GameObject selectedDisplayCard = null;     // 現在選択（持ち上げ）されている見せ札のカード
    private bool isWaitingForFieldSelection = false;   // 同じ月が複数あって場札を選ぶのを待っている状態か    
    private List<int> validTargetFieldSlots = new List<int>();

    // プレイヤーが手札のカードをクリックしたときの処理
    public void OnHandCardClicked(GameObject clickedCard)
    {

        // --- パターン1：すでに別のカードが選択されていて、それとは「違うカード」をクリックした場合 ---
        if (selectedHandCard != null && selectedHandCard != clickedCard)
        {
            ResetCardPosition(selectedHandCard);

            selectedHandCard = clickedCard;
            LiftCardUp(selectedHandCard);
            Debug.Log(clickedCard.name + " を選択したで（1回目のクリック）");

            // 新しく選んだカードについて、同じ月の場札が複数あるか事前にチェックする
            CheckCardMatching(selectedHandCard);
            return;
        }

        // --- パターン2：「初めて」手札のカードをクリックした場合 ---
        if (selectedHandCard == null)
        {
            selectedHandCard = clickedCard;
            LiftCardUp(selectedHandCard);
            Debug.Log(clickedCard.name + " を選択したで（1回目のクリック）");

            // 選んだカードについて、同じ月の場札が複数あるか事前にチェックする
            CheckCardMatching(selectedHandCard);
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
            
            ResetCardPosition(selectedHandCard);
            
            // 1枚だけ、または空きスロットに置く処理を実行
            ExecuteNormalPlay(selectedHandCard);

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
    private void CheckCardMatching(GameObject selectedCard)
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
            selectedHandCard = selectedCard;
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
    private void ExecuteNormalPlay(GameObject selectedCard)
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
            deckManager.PlaceCardFromHandToField(selectedCard, targetSlotIndex);
            Debug.Log("同じ月の場札が1枚だったので自動で合わせました！");
        }
        else
        {
            int emptySlotIndex = GetLowestEmptyFieldSlot(fieldSlotCardLists);
            if (emptySlotIndex != -1)
            {
                deckManager.PlaceCardFromHandToField(selectedCard, emptySlotIndex);
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
            CheckCardMatching(selectedDisplayCard);
            return;
        }

        // --- パターン2：「初めて」手札のカードをクリックした場合 ---
        if (selectedDisplayCard == null)
        {
            selectedDisplayCard = clickedDisplayCard;
            LiftCardUp(selectedDisplayCard);
            Debug.Log(clickedDisplayCard.name + " を選択した（1回目のクリック）");

            // 選んだカードについて、同じ月の場札が複数あるか事前にチェックする
            CheckCardMatching(selectedDisplayCard);
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
            ExecuteNormalPlay(selectedDisplayCard);

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
            // 持ち上がっている手札の位置を元に戻してから場に置く
            if (selectedHandCard != null)
            {
                ResetCardPosition(selectedHandCard);
                deckManager.PlaceCardFromHandToField(selectedHandCard, clickedSlotIndex);
            }

            if (selectedDisplayCard != null)
            {
                ResetCardPosition(selectedDisplayCard);
                deckManager.PlaceCardFromDisplayToField(selectedDisplayCard, clickedSlotIndex);
            }
            
            // 状態をすべてリセット
            isWaitingForFieldSelection = false;
            validTargetFieldSlots.Clear();
            selectedHandCard = null;
            

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
}