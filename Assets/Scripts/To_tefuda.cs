using UnityEngine;

public class To_tefuda : MonoBehaviour
{
    void OnMouseDown()
    {
        // 手動クリックのときなどの処理が必要ならここに書く
    }

    // デッキマネージャーから呼ばれる共通処理（手札用）
    public void DrawCardToHand()
    {
        // メインカメラを親にする
        transform.SetParent(Camera.main.transform);
    }
}