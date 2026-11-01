using UnityEngine;

public class CubeMove : MonoBehaviour
{
// 1. 中心になってほしい太陽のオブジェクトをUnity画面から登録する
    [SerializeField] private Transform sunTransform;
    // 2. 公転するスピード
    [SerializeField] private float orbitSpeed = 20f;

    void Update()
    {
        if (sunTransform != null)
        {
            // 3. 太陽の位置を中心に、真上の軸（Vector3.up）を基準にして、指定したスピードで回る
            transform.RotateAround(
                sunTransform.position, 
                Vector3.up, 
                orbitSpeed * Time.deltaTime
            );
        }
    }
}