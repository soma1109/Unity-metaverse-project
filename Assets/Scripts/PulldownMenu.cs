using UnityEngine;

public class PulldownMenu : MonoBehaviour
{
    [Header("開閉するパネル（子要素のリスト）")]
    [SerializeField] private GameObject contentPanel;

    private bool isOpen = false;

    private void Start()
    {
        // ゲーム開始時は閉じておく
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ▽のボタンが押されたときに呼ぶメソッド
    /// </summary>
    public void ToggleMenu()
    {
        isOpen = !isOpen;

        // 開閉状態を反転させる
        if (contentPanel != null)
        {
            contentPanel.SetActive(isOpen);
        }

        // TODO: ここで▽のアイコンを上向き（▲）に回転させたりする演出も入れられるよ！
    }

    // メニュー項目が選ばれたら閉じる用
    public void CloseMenu()
    {
        isOpen = false;
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
        }
    }
}