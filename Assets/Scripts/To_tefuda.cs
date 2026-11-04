using UnityEngine;

public class To_tefuda : MonoBehaviour
{
    private bool isInDrawPile = true;

    public bool IsInDrawPile
    {
        get { return isInDrawPile; }
        set { isInDrawPile = value; }
    }

    void OnMouseDown()
    {
        // 手動クリックのときなどの処理が必要ならここに書く
    }

    // デッキマネージャーから呼ばれる共通処理
    public void DrawCardToHand()
    {
        // 1. メインカメラを親にする
        transform.SetParent(Camera.main.transform);

        // 2. もう山札にはないよフラグにする
        isInDrawPile = false;

        // （※位置の調整は DeckManager の UpdateHandLayout がやってくれるからここでは書かない！）
    }
}