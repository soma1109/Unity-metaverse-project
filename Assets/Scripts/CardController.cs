using UnityEngine;
using System.Collections.Generic;

public class CardController : MonoBehaviour
{
    private GameManager gameManager;
    private DeckManager deckManager;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        deckManager = FindAnyObjectByType<DeckManager>();
    }

    private void OnMouseDown()
    {
        if (gameManager == null || deckManager == null) return;

        // 1. クリックされたカードが「自分の手札」に含まれているかチェック
        List<GameObject> myHand = deckManager.GetMyHandCards();
        if (myHand.Contains(gameObject))
        {
            // 手札のカードがクリックされた場合（1回目＝選択、2回目＝決定の判定へ）
            gameManager.OnHandCardClicked(gameObject);
            return;
        }

        // 2. クリックされたカードが「場札」に含まれているかチェック
        GameObject[] fieldCards = deckManager.GetFieldCards();
        bool isFieldCard = System.Array.Exists(fieldCards, card => card == gameObject);

        if (isFieldCard)
        {
            // 場札のカードがクリックされた場合（複数ある場合の選択用）
            gameManager.OnFieldCardClicked(gameObject);
            return;
        }
    }

    public void DrawCardToHand()
    {
        transform.SetParent(Camera.main.transform);
    }
}