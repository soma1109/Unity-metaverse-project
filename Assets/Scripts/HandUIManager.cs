using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class HandUIManager : MonoBehaviour
{
    [Header("UIを並べる親パネル (Horizontal Layout Group付き)")]
    [SerializeField] private Transform handPanelParent;

    [Header("カードボタンのプレハブ")]
    [SerializeField] private GameObject handCardButtonPrefab;

    [Header("GameManagerの参照")]
    [SerializeField] private GameManager gameManager;

    [Header("並べ方と持ち上げ設定")]
    [SerializeField] private float cardSpacing = 80f;
    [SerializeField] private float baseY = 0f;
    [SerializeField] private float liftOffsetY = 30f;

    // 3Dカードとそれに対応するUI用手札のRectangular(長方形)の領域・座標情報の辞書を再代入禁止(readonly)で用意 (addとかclearはできる)
    private readonly Dictionary<GameObject, RectTransform> cardToUiMap = new Dictionary<GameObject, RectTransform>();

    // 手札のリストを受け取って、画面にボタンを並べ直す関数
    public void RefreshHandUI(List<GameObject> myHandCards)
    {
        // 1. いま画面に出ている古いボタンを全部キレイに消す
        foreach (Transform child in handPanelParent)
        {
            Destroy(child.gameObject);
        }
        cardToUiMap.Clear();

        //親パネルの中心を左右方向の中心にする
        float totalWidth = (myHandCards.Count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f;

        // 2. 現在の手札の枚数分だけ、ボタンを新しく生成する
        for (int i = 0; i < myHandCards.Count; i++)
        {
            GameObject card3D = myHandCards[i];
            if (card3D == null) continue;

            // プレハブからボタン生成
            GameObject buttonObj = Instantiate(handCardButtonPrefab, handPanelParent);
            RectTransform rect = buttonObj.GetComponent<RectTransform>();

            if (rect != null)
            {
                // 親パネルのHorizontal Layout Groupは使わず、ここで横並びの位置をセット
                float posX = startX + i * cardSpacing;
                rect.anchoredPosition = new Vector2(posX, baseY);
                cardToUiMap[card3D] = rect;
            }

            // 3. 3Dカードについている CardData から Sprite を一発取得してボタンにセット！
            CardData cardData = card3D.GetComponent<CardData>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            if (cardData != null && cardData.cardSprite != null && buttonImage != null)
            {
                buttonImage.sprite = cardData.cardSprite;
            }
            else
            {
                Debug.LogWarning($"[HandUIManager] カード {card3D.name} に CardData または cardSprite が設定されていません！");
            }

            // 4. ボタンが押されたときの処理を登録
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                GameObject targetCard3D = card3D; // クロージャ対策
                btn.onClick.AddListener(() => {
                    OnUIHandCardClicked(targetCard3D);
                });
            }
        }
    }

    // 画面のボタンがポチッと押されたとき
    private void OnUIHandCardClicked(GameObject selectedCard3D)
    {
        Debug.Log($"UIの手札ボタンが押された！選ばれたカード: {selectedCard3D.name}");
        
        if (gameManager != null)
        {
            gameManager.OnHandCardClicked(selectedCard3D);
        }
    }

    // GameManagerから呼ばれる：指定した3Dカードに対応するUIだけ上げ、他は下げる（nullなら全部下げる）
    public void SetLiftedCardBy3D(GameObject targetCard3D)
    {
        foreach (var pair in cardToUiMap)
        {
            GameObject card3D = pair.Key;
            RectTransform uiRect = pair.Value;
            if (card3D == null || uiRect == null) continue;

            Debug.Log($"card3D={card3D.name}/targetCard3D={targetCard3D.name}");
            bool shouldLift = card3D == targetCard3D;
            float targetY;
            if (shouldLift)
            {
                targetY = baseY + liftOffsetY;
                Debug.Log("UI手札をリフト");
            }
            else
            {
                targetY = baseY;
            }
            //float targetY = shouldLift ? baseY + liftOffsetY : baseY;
            uiRect.anchoredPosition = new Vector2(uiRect.anchoredPosition.x, targetY);
        }
    }
}