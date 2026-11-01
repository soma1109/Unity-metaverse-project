using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    // 掲示板が開いているかどうかの状態
    private bool isPlayerInside = false;
    public bool IsOpen { get { return isPlayerInside; } }

    // プレイヤーがエリアに入った瞬間
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Robot")) // ロボットを検知
        {
            isPlayerInside = true;
            Debug.Log("掲示板の範囲に入りました！ [E]キーを押してね");
        }
    }

    // プレイヤーがエリアから出た瞬間
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Robot"))
        {
            isPlayerInside = false;
            Debug.Log("掲示板の範囲から出ました。");
        }
    }
}