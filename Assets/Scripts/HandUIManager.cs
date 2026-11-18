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

    // 手札のリストを受け取って、画面にボタンを並べ直す関数
    public void RefreshHandUI(List<GameObject> myHandCards)
    {
        // 1. いま画面に出ている古いボタンを全部キレイに消す
        foreach (Transform child in handPanelParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 現在の手札の枚数分だけ、ボタンを新しく生成する
        for (int i = 0; i < myHandCards.Count; i++)
        {
            GameObject card3D = myHandCards[i];
            if (card3D == null) continue;

            // プレハブからボタン生成
            GameObject buttonObj = Instantiate(handCardButtonPrefab, handPanelParent);

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
}