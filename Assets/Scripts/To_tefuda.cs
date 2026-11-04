using UnityEngine;

public class To_tefuda : MonoBehaviour
{
    [Header("今、山札にあるかどうか")]
    private bool isInDrawPile = true;

    [SerializeField] private Vector3 Cardposition = new Vector3(-7f, 2f, 0.5f); // カメラの右下あたり

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
        // 1. メインカメラを「親」に設定する
        // （※ Camera.main でシーンの中にあるMain Cameraを自動で捕まえてくれるで）
        transform.SetParent(Camera.main.transform);

        // 2. カメラから見た相対的な位置（localPosition）と向きを調整する
        // 値はお好みの画角に合わせて微調整してな！
        transform.localPosition = Cardposition;
        transform.localRotation = Quaternion.Euler(0, 0, 0);       // まっすぐ正面を向かせる

        // 3. フラグを「もう山札にはない」に切り替える
        isInDrawPile = false;

        Debug.Log("カードを山札から手札（カメラの前）に移動したで！");
    }
}