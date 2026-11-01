using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentRobot : MonoBehaviour
{
    private static PersistentRobot instance;

    [Header("ロボットを連れていきたくないシーン（例：Titleなど）")]
    public string[] excludeScenes; // インスペクターで除外するシーン名を設定できる

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. もし「連れていかないシーン」に指定されていたら、ロボット自体を消去する！
        foreach (string excludedScene in excludeScenes)
        {
            if (scene.name == excludedScene)
            {
                Destroy(gameObject);
                instance = null; // インスタンスの記憶もリセット
                return;
            }
        }

        // 2. 連れていくシーンの場合：スポーン地点を探してワープ
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;

            if (rb != null) rb.isKinematic = false;
            if (cc != null) cc.enabled = true;
        }

        // 3. 新しいシーンの「カメラ」に自分を追いかけさせる処理
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // もしカメラに追従用スクリプト（例：CameraFollowなど）がついているなら、ターゲットを自分に再設定する
            // ※カメラのスクリプトの仕様に合わせてここは調整可能！
            Debug.Log("新しいシーンのカメラと連携しました！");
        }
    }
}