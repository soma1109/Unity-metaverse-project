using UnityEngine;
using UnityEngine.InputSystem;

public class BulletinBoardUI : MonoBehaviour
{
    [Header("接続するトリガーエリア")]
    public InteractionTrigger triggerZone;

    [Header("表示する掲示板パネル（CanvasやPanel）")]
    public GameObject boardPanel;

    private bool isOpen = false;

    void Start()
    {
        if (boardPanel != null)
        {
            boardPanel.SetActive(false); // 最初は非表示
        }
    }

    void Update()
    {
        if (triggerZone == null || boardPanel == null) return;

        // プレイヤーがエリア内にいるときだけEキーを検知
        if (triggerZone.IsOpen)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
                boardPanel.SetActive(isOpen);

                // パネルが開いているときはマウスカーソルを出す、閉じたらロックする
                if (isOpen)
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
        }
        else
        {
            // エリアの外に出たら強制的に閉じる
            if (isOpen)
            {
                isOpen = false;
                boardPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}