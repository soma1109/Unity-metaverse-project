using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("追従するターゲット")]
    public Transform target;

    [Header("カメラの設定")]
    public float distance = 10.0f;        // ターゲットからの距離
    public float height = 5.0f;           // カメラの高さ
    public float rotationSpeed = 2.0f;    // マウスの感度

    private float currentYaw = -90.0f;    // 左右の回転角度
    private float currentPitch = 15.0f;   // 上下の回転角度

    void Start()
    {
        // ★ゲーム開始時にマウスカーソルを画面中央に固定して非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // マウスの移動量を新しいInput Systemで取得
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            currentYaw += mouseDelta.x * rotationSpeed * 0.05f;
            currentPitch -= mouseDelta.y * rotationSpeed * 0.05f;

            // 上を向きすぎたり下を向きすぎたりしないように角度を制限
            currentPitch = Mathf.Clamp(currentPitch, -10f, 60f);
        }

        // 回転の計算
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // ターゲットの位置を基準に、カメラの位置を計算
        Vector3 targetPosition = target.position + Vector3.up * height;
        transform.position = targetPosition - (rotation * Vector3.forward * distance);

        // 常にターゲットのほうを向く
        transform.LookAt(targetPosition);
    }
}