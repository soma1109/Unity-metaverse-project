using UnityEngine;
using UnityEngine.InputSystem;

public class BulletinBoardUI : MonoBehaviour
{
    [Header("接続するトリガーエリア")]
    public InteractionTrigger triggerZone;

    [Header("表示する掲示板パネル")]
    public GameObject boardPanel;

    void Start()
    {
        // 最初はパネルを非表示にしておく
        if (boardPanel != null)
        {
            boardPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (triggerZone == null || boardPanel == null) return;

        // プレイヤーがエリア内にいて、Eキーが押されたら
        if (triggerZone.IsOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // パネルの表示・非表示を反転させる
            bool isActive = boardPanel.activeSelf;
            boardPanel.SetActive(!isActive);

            // 掲示板を開いているときはマウスカーソルを一時的に復活させると操作しやすい！
            if (!isActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        // エリアの外に出たら強制的にパネルを閉じる
        if (!triggerZone.IsOpen && boardPanel.activeSelf)
        {
            boardPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}