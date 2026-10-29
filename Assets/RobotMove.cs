using UnityEngine;
using UnityEngine.InputSystem;

public class RobotMove : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 5.0f;

    private Animator anim;
    private Rigidbody rb;
    private Transform cameraTransform;

    void Start()
    {
        // 自分自身、または子オブジェクトからAnimatorとRigidbodyを取得
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();

        // メインカメラの情報を取得
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void FixedUpdate()
    {
        // キーボード入力を取得
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
        }

        Vector3 movement = Vector3.zero;

        if (cameraTransform != null)
        {
            // カメラの向いている方向（前・右）を取得し、高低差（Y軸）の影響を消す
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            // 「カメラが見ている方向」を基準に移動ベクトルを計算
            movement = (camForward * moveZ + camRight * moveX).normalized;
        }
        else
        {
            movement = new Vector3(moveX, 0.0f, moveZ).normalized;
        }

        // Rigidbodyを使って移動（重力を維持）
        Vector3 targetVelocity = movement * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        // 進む方向にロボットの向きを滑らかに回転させる
        if (movement != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime);
        }

        // アニメーションの制御（移動中なら歩行アニメをON）
        if (anim != null)
        {
            bool isWalking = (movement != Vector3.zero);
            anim.SetBool("Walk_Anim", isWalking);
        }
    }
}