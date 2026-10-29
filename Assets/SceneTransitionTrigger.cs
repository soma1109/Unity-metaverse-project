using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // シーン切り替えに絶対必要なおまじない

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("移動先のシーンの名前を正確に入力する")]
    public string targetSceneName;

    private bool isPlayerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Robot"))
        {
            isPlayerInside = true;
            Debug.Log("移動エリアに入りました！ [E]キーで別のマップへ移動");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Robot"))
        {
            isPlayerInside = false;
        }
    }

    void Update()
    {
        // エリア内にいて、かつEキーが押されたら！
        if (isPlayerInside)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (!string.IsNullOrEmpty(targetSceneName))
                {
                    Debug.Log(targetSceneName + " へ移動します！");
                    // 🎯 別のシーンをロードしてジャンプ！
                    SceneManager.LoadScene(targetSceneName);
                }
                else
                {
                    Debug.LogWarning("移動先のシーン名がインスペクターに入力されていません！");
                }
            }
        }
    }
}