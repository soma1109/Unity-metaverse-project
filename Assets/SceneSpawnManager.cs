using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnManager : MonoBehaviour
{
    void Start()
    {
        // シーンが切り替わった瞬間に、シーン内から「SpawnPoint」という名前の目印を探す
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        
        if (spawnPoint != null)
        {
            // ロボット（プレイヤー）を探して、その出口の座標に強制ワープさせる！
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // 物理挙動のバグを防ぎつつ座標を合わせる
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                player.transform.position = spawnPoint.transform.position;
                player.transform.rotation = spawnPoint.transform.rotation;

                if (rb != null) rb.isKinematic = false;
                if (cc != null) cc.enabled = true;

                Debug.Log("指定された出口（SpawnPoint）にスポーンしました！");
            }
        }
    }
}