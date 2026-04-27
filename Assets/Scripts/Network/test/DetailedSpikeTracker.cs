using Fusion;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class DetailedSpikeTracker : MonoBehaviour
{
    private Stopwatch stopwatch = new Stopwatch();
    private float lastSpikeTime = 0f;
    private List<float> spikeIntervals = new List<float>();

    void Update()
    {
        stopwatch.Restart();
    }

    void LateUpdate()
    {
        stopwatch.Stop();
        float frameTime = (float)stopwatch.Elapsed.TotalMilliseconds;

        //30ms以上をスパイクとして記録
        if (frameTime > 30f)
        {
            float interval = Time.time - lastSpikeTime;
            spikeIntervals.Add(interval);

            UnityEngine.Debug.LogError($"========== カクつき検出 ==========");
            UnityEngine.Debug.LogError($"フレーム時間: {frameTime:F1}ms ({1000f / frameTime:F0} FPS)");
            UnityEngine.Debug.LogError($"前回のカクつきからの経過時間: {interval:F2}秒");
            UnityEngine.Debug.LogError($"フレーム番号: {Time.frameCount}");
            UnityEngine.Debug.LogError($"ゲーム時間: {Time.time:F2}秒");

            // 直近5回のスパイク間隔を表示
            if (spikeIntervals.Count >= 2)
            {
                int start = Mathf.Max(0, spikeIntervals.Count - 5);
                string intervals = "";
                for (int i = start; i < spikeIntervals.Count; i++)
                {
                    intervals += spikeIntervals[i].ToString("F1") + "秒";
                    if (i < spikeIntervals.Count - 1) intervals += ", ";
                }
                UnityEngine.Debug.LogError($"直近のカクつき間隔: {intervals}");
            }

            lastSpikeTime = Time.time;

            // 各システムの状態を記録
            LogSystemState();
        }
    }

    void LogSystemState()
    {
        // Fusion stats
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            UnityEngine.Debug.LogError($"[Fusion] Tick: {runner.Simulation.Tick}");
            UnityEngine.Debug.LogError($"[Fusion] サーバー: {runner.IsServer}, クライアント: {runner.IsClient}");
            UnityEngine.Debug.LogError($"[Fusion] アクティブなプレイヤー数: {runner.ActivePlayers.Count()}");

            // NetworkObjects の数を数える
            var allNetworkObjects = FindObjectsOfType<NetworkObject>();
            UnityEngine.Debug.LogError($"[Fusion] ネットワークオブジェクト数: {allNetworkObjects.Length}");
        }
        else
        {
            UnityEngine.Debug.LogError($"[Fusion] NetworkRunner が動作していません");
        }

        // Object counts
        var rigidbodies = FindObjectsOfType<Rigidbody>();
        var colliders = FindObjectsOfType<Collider>();
        var characterControllers = FindObjectsOfType<CharacterController>();

        UnityEngine.Debug.LogError($"[オブジェクト] Rigidbody: {rigidbodies.Length}, Collider: {colliders.Length}, CharacterController: {characterControllers.Length}");

        // Memory
        long memoryBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        UnityEngine.Debug.LogError($"[メモリ] 使用量: {memoryBytes / 1024 / 1024}MB");

        // GameObjects
        var allGameObjects = FindObjectsOfType<GameObject>();
        UnityEngine.Debug.LogError($"[シーン] 全GameObjects: {allGameObjects.Length}個");

        // 爆弾やエフェクトをカウント（スクリプト名が分かれば追加）
        // var bombs = FindObjectsOfType<BombScript>(); // 実際の爆弾スクリプト名に変更
        // UnityEngine.Debug.LogError($"[ゲーム] 爆弾の数: {bombs.Length}");
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 150));

        // FPS表示
        float fps = 1f / Time.deltaTime;
        Color color = fps < 45 ? Color.red : (fps < 55 ? Color.yellow : Color.green);

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = color;

        GUILayout.Label($"FPS: {fps:F0} ({Time.deltaTime * 1000:F1}ms)", style);

        // スパイク統計
        GUIStyle normalStyle = new GUIStyle();
        normalStyle.fontSize = 16;
        normalStyle.normal.textColor = Color.white;

        GUILayout.Label($"総カクつき回数: {spikeIntervals.Count}", normalStyle);

        if (spikeIntervals.Count > 0)
        {
            float lastInterval = spikeIntervals[spikeIntervals.Count - 1];
            GUILayout.Label($"最後のカクつき: {lastInterval:F1}秒前", normalStyle);
        }

        GUILayout.EndArea();
    }
}