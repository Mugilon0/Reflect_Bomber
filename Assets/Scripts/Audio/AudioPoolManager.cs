using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// AudioSourceをオブジェクトプールで管理
/// Instantiate/Destroyを削減してパフォーマンス改善
/// </summary>
public class AudioPoolManager : MonoBehaviour
{
    public static AudioPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 50;

    private Queue<PooledAudioSource> availablePool = new Queue<PooledAudioSource>();
    private List<PooledAudioSource> activeAudios = new List<PooledAudioSource>();
    private Transform poolParent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        // プール用の親オブジェクトを作成
        poolParent = new GameObject("AudioPool").transform;
        poolParent.SetParent(transform);

        // 初期プールを生成
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    /// <summary>
    /// 新しいAudioSourceを作成してプールに追加
    /// </summary>
    private PooledAudioSource CreateNewAudioSource()
    {
        GameObject audioObj = new GameObject("PooledAudio");
        audioObj.transform.SetParent(poolParent);
        audioObj.SetActive(false);

        AudioSource source = audioObj.AddComponent<AudioSource>();
        PooledAudioSource pooled = audioObj.AddComponent<PooledAudioSource>();
        pooled.Initialize(source, this);

        availablePool.Enqueue(pooled);
        return pooled;
    }

    /// <summary>
    /// プールからAudioSourceを取得
    /// </summary>
    public PooledAudioSource Get()
    {
        PooledAudioSource pooled;

        if (availablePool.Count > 0)
        {
            pooled = availablePool.Dequeue();
        }
        else
        {
            // プールが空の場合
            if (activeAudios.Count < maxPoolSize)
            {
                pooled = CreateNewAudioSource();
                Debug.LogWarning($"AudioPool: Creating new audio source. Active: {activeAudios.Count}");
            }
            else
            {
                // 上限に達した場合、最も古いものを強制停止
                Debug.LogWarning($"AudioPool: Pool limit reached ({maxPoolSize}). Forcing oldest audio to stop.");
                pooled = activeAudios[0];
                pooled.ForceStop();
            }
        }

        pooled.gameObject.SetActive(true);
        activeAudios.Add(pooled);
        return pooled;
    }

    /// <summary>
    /// AudioSourceをプールに返却
    /// </summary>
    public void Return(PooledAudioSource pooled)
    {
        if (pooled == null) return;

        activeAudios.Remove(pooled);
        pooled.Reset();
        pooled.gameObject.SetActive(false);
        pooled.transform.SetParent(poolParent);
        availablePool.Enqueue(pooled);
    }

    /// <summary>
    /// デバッグ情報を表示
    /// </summary>
    private void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Label(new Rect(10, 100, 300, 20),
                $"Audio Pool - Active: {activeAudios.Count} / Available: {availablePool.Count} / Max: {maxPoolSize}");
        }
    }
}

/// <summary>
/// プールされたAudioSourceの管理クラス
/// </summary>
public class PooledAudioSource : MonoBehaviour
{
    private AudioSource source;
    private AudioPoolManager pool;
    private float playbackEndTime;
    private bool isPlaying;
    private Transform followTarget;

    public void Initialize(AudioSource audioSource, AudioPoolManager poolManager)
    {
        source = audioSource;
        pool = poolManager;
    }

    /// <summary>
    /// 音を再生
    /// </summary>
    public void Play(AudioClip clip, AudioMixerGroup mixerGroup, Vector3? position = null, Transform follow = null)
    {
        source.clip = clip;
        source.outputAudioMixerGroup = mixerGroup;
        followTarget = follow;

        if (position.HasValue)
        {
            transform.position = position.Value;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = 50f;
            source.dopplerLevel = 0f;
        }
        else
        {
            source.spatialBlend = 0f;
        }

        source.Play();
        isPlaying = true;
        playbackEndTime = Time.time + clip.length;
    }

    private void Update()
    {
        if (!isPlaying) return;

        // ターゲットを追従
        if (followTarget != null)
        {
            transform.position = followTarget.position;
        }

        // 再生終了チェック（タイムベース）
        if (Time.time >= playbackEndTime || !source.isPlaying)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 強制停止（プールが満杯の時）
    /// </summary>
    public void ForceStop()
    {
        if (source.isPlaying)
        {
            source.Stop();
        }
        ReturnToPool();
    }

    /// <summary>
    /// プールに返却
    /// </summary>
    private void ReturnToPool()
    {
        isPlaying = false;
        pool.Return(this);
    }

    /// <summary>
    /// リセット
    /// </summary>
    public void Reset()
    {
        source.Stop();
        source.clip = null;
        source.spatialBlend = 0f;
        followTarget = null;
        isPlaying = false;
    }
}