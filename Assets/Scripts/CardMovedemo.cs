using UnityEngine;

public class CardMovedemo : MonoBehaviour
{
    [SerializeField] private Vector3 TargetPosition = new Vector3(5f, 0f, 0f);
    [SerializeField] private float moveSpeed = 5f; // 移動速度

    private bool shouldMove = false;

    //Unityのイベント関数onMouseDownを使って、マウスクリック時に移動を開始する
    private void OnMouseDown()
    {
        shouldMove = true;
    }

    private void Update()
    {
        if (shouldMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, moveSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, TargetPosition) < 0.1f)
        {
            shouldMove = false; // 目標位置に到達したら移動を停止
        }
    }
}
