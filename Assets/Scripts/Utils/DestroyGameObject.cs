using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyGameObject : MonoBehaviour
{
    // プレイヤーデス時のパーティクルを消す
    public float lifeTime = 1.5f;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(lifeTime);

        Destroy(gameObject);
    }
}
