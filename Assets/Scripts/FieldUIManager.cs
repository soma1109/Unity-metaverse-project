using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class FieldUIManager : MonoBehaviour
{
    [Header("UIを並べる親パネル (Horizontal Layout Group付き)")]
    [SerializeField] private Transform fieldPanelParent;

    [Header("カードボタンのプレハブ")]
    [SerializeField] private GameObject fieldCardButtonPrefab;

    [Header("GameManagerの参照")]
    [SerializeField] private GameManager gameManager;

    [Header("並べ方と持ち上げ設定")]
    [SerializeField] private float cardWidthSpacing = 80f;
    [SerializeField] private float cardHeightSpacing = 120f;
    [SerializeField] private float topMargin = 20f;
    [SerializeField] private int columns = 4; //列(column)の数

    // 3Dカードとそれに対応するUI用場札のRectangular(長方形)の領域・座標情報の辞書を再代入禁止(readonly)で用意 (addとかclearはできる)
    private readonly Dictionary<GameObject, RectTransform> fieldCardToUiMap = new Dictionary<GameObject, RectTransform>();

    // 手札のリストを受け取って、画面にボタンを並べ直す関数
    public void RefreshFieldUI(List<GameObject>[] fieldCards)
    {
        // 1. いま画面に出ている古いボタンを全部キレイに消す
        foreach (Transform child in fieldPanelParent)
        {
            Destroy(child.gameObject);
        }
        fieldCardToUiMap.Clear();

        if (fieldCards == null || fieldCards.Length == 0 || columns <= 0) return;

        RectTransform parentRect = fieldPanelParent as RectTransform;
        RectTransform prefabRect = fieldCardButtonPrefab.GetComponent<RectTransform>();

        //親パネルの中心を場札の中心にする
        float totalWidth = (columns - 1) * cardWidthSpacing;
        float startX = -totalWidth * 0.5f;
        
        // 親パネルの上端Y座標（Middle-Center基準なら +高さ/2）
        float parentTopY = parentRect.rect.height * 0.5f;
        float buttonHalfHeight = prefabRect.rect.height * 0.5f;
        // 1行目（row = 0）の中心Y座標：親の上端 - topMargin - ボタン半分の高さ
        float firstRowCenterY = parentTopY - topMargin - buttonHalfHeight;

        // 2. 場札スロットごとにボタンを新しく生成する
        for (int i = 0; i < fieldCards.Length; i++)
        {
            if (fieldCards[i] == null || fieldCards[i].Count == 0) continue;

            int col = i % columns;
            int row = i / columns;

            GameObject card3D = fieldCards[i][0];
            if (card3D == null) continue;

            // プレハブからボタン生成
            GameObject buttonObj = Instantiate(fieldCardButtonPrefab, fieldPanelParent);
            RectTransform rect = buttonObj.GetComponent<RectTransform>();

            if (rect != null)
            {
                // 親パネルのHorizontal Layout Groupは使わず、ここで横並びの位置をセット
                float posX = startX + col * cardWidthSpacing;
                float posY = firstRowCenterY - row * cardHeightSpacing;
                rect.anchoredPosition = new Vector2(posX, posY);
                fieldCardToUiMap[card3D] = rect;
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
                Debug.LogWarning($"[FieldUIManager] カード {card3D.name} に CardData または cardSprite が設定されていません！");
            }

            // 4. ボタンが押されたときの処理を登録
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                GameObject targetCard3D = card3D; // クロージャ対策
                btn.onClick.AddListener(() => {
                    OnUIFieldCardClicked(targetCard3D);
                });
            }
        }
    }

    // 画面のボタンがポチッと押されたとき
    private void OnUIFieldCardClicked(GameObject selectedCard3D)
    {
        Debug.Log($"UIの場札ボタンが押された！選ばれたカード: {selectedCard3D.name}");
        
        if (gameManager != null)
        {
            gameManager.OnFieldCardClicked(selectedCard3D);
        }
    }
}