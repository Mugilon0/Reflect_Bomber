using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils {
    public static Vector3 GetRandomSpawnPoint() //スポーン用にランダムな座標を用意する
    {
        return new Vector3(Random.Range(-20, 20), 4, Random.Range(-20, 20));
    }

    public static void SetRenderLayerInChildren(Transform transform, int layerNumber) // レイヤーを変更する関数を定義する
    {
        foreach (Transform trans in transform.GetComponentsInChildren<Transform>(true))
            trans.gameObject.layer = layerNumber;
    }

}
