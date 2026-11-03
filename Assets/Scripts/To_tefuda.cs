using UnityEngine;

public class To_tefuda : MonoBehaviour
{
    [Header("移動先（手札）の位置を示す目印のオブジェクト")]
    [SerializeField] private Transform handTargetPosition;

    [Header("今、山札にあるかどうか")]
    private bool isInDrawPile = true;

    // マウスでこのカードをクリックした瞬間に発動するUnityの便利機能
    void OnMouseDown()
    {
        // もし「山札にある状態」のときだけクリックを受け付ける
        if (isInDrawPile)
        {
            DrawCardToHand();
        }
    }

    void DrawCardToHand()
    {
        if (handTargetPosition != null)
        {
            // 1. 手札の位置と向きに一瞬で移動させる
            transform.position = handTargetPosition.position;
            transform.rotation = handTargetPosition.rotation;

            // 2. 「もう山札にはないよ」という状態にフラグを切り替える
            isInDrawPile = false;

            // 3. 必要なら、ここでレイヤーやZ軸の重なり順（Sorting Orderなど）を調整してもOK
            Debug.Log("カードを山札から手札に引きました！");
        }
        else
        {
            Debug.LogWarning("手札の目標地点（handTargetPosition）が設定されてへんよ！");
        }
    }
}
