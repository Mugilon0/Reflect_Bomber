using System.Collections.Generic;
using UnityEngine;

// エフェクト専用の管理者（Fusionには依存しない）
public class EffectPoolManager : MonoBehaviour
{
    public static EffectPoolManager Instance { get; private set; }



    // エフェクトの種類一覧
    public enum EffectType
    {
        Explosion_Short, // 短距離ボムの爆発
        Explosion_Long,  // 長距離ボムの爆発
        Explosion_Put,   // 置きボムの爆発
        Death,            // 死亡時のエフェクト
        Death2
    }

    [System.Serializable]
    public class PoolSetting
    {
        public EffectType type;
        public GameObject prefab;
        public int initialCount = 10;
    }

    [Header("Settings")]
    [SerializeField] private List<PoolSetting> poolSettings;

    // 種類ごとにリストを分けた辞書
    private Dictionary<EffectType, List<ParticleSystem>> pools = new Dictionary<EffectType, List<ParticleSystem>>();
    private Transform poolParent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        poolParent = new GameObject("EffectPool_Parent").transform;
        poolParent.SetParent(transform);

        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var setting in poolSettings)
        {
            if (setting.prefab == null) continue;

            var list = new List<ParticleSystem>();
            for (int i = 0; i < setting.initialCount; i++)
            {
                CreateNewParticle(setting.prefab, list);
            }
            pools[setting.type] = list;
        }
    }

    private ParticleSystem CreateNewParticle(GameObject prefab, List<ParticleSystem> list)
    {
        var obj = Instantiate(prefab, poolParent);
        obj.SetActive(false);
        var ps = obj.GetComponent<ParticleSystem>();
        list.Add(ps);
        return ps;
    }

    public void Play(EffectType type, Vector3 position)
    {
        if (!pools.ContainsKey(type)) return;

        var list = pools[type];
        ParticleSystem ps = null;

        // 未使用のものを探す
        foreach (var p in list)
        {
            if (!p.gameObject.activeSelf)
            {
                ps = p;
                break;
            }
        }

        // 足りなければ先頭を強制再利用（循環させる）
        if (ps == null && list.Count > 0)
        {
            ps = list[0];
            list.RemoveAt(0);
            list.Add(ps);
        }

        if (ps != null)
        {
            ps.transform.position = position;

            ps.gameObject.SetActive(true);

            // 2. 以前の状態を完全に停止・消去 (子階層も含める: true)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 3. システムの状態を初期化 (ここが重要。時間を0に戻し、放出済み粒子を消す)
            ps.Clear(true);

            // 4. 明示的に再生 (子階層も含める: true)
            ps.Play(true);
        }
    }
}