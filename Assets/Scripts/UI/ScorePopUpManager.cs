using UnityEngine;
using DamageNumbersPro; // Damage Numbers Proの名前空間

/// <summary>
/// Damage Numbers Pro を使用してスコアポップアップを表示するマネージャー
/// Prefabに既に設定されているテキスト（"+1"など）をそのまま使用
/// </summary>
public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager Instance { get; private set; }

    [Header("Damage Number Settings")]
    [Tooltip("Damage Numbers ProのPrefabをアサイン（Prefab内で既に'+1'などのテキストを設定しておく）")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    [Header("Display Settings")]
    [Tooltip("プレイヤーの位置からのオフセット(右上に表示)")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(1.5f, 2.0f, 0);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Prefabの確認
        if (damageNumberPrefab == null)
        {
            Debug.LogError("ScorePopupManager: Damage Number Prefab is not assigned!");
        }
    }

    /// <summary>
    /// スコアポップアップを表示する
    /// Prefabに設定されているテキスト（"+1"など）をそのまま使用
    /// </summary>
    /// <param name="worldPosition">プレイヤーのワールド座標</param>
    /// <param name="customOffset">カスタムオフセット(オプション)</param>
    public static void ShowPopup(Vector3 worldPosition, Vector3? customOffset = null)
    {
        if (Instance == null)
        {
            Debug.LogWarning("ScorePopupManager: Instance is null! Make sure ScorePopupManager is in the World1 scene.");
            return;
        }

        if (Instance.damageNumberPrefab == null)
        {
            Debug.LogWarning("ScorePopupManager: Damage Number Prefab is not assigned!");
            return;
        }

        // オフセットを適用
        Vector3 offset = customOffset ?? Instance.defaultOffset;
        Vector3 spawnPosition = worldPosition + offset;

        // Prefabに設定されているテキストをそのまま使用してスポーン
        DamageNumber damageNumber = Instance.damageNumberPrefab.Spawn(spawnPosition);

        // 必要に応じてカスタマイズ（Prefabで既に設定している場合は不要）
        // damageNumber.SetColor(Color.green);
        // damageNumber.SetScale(1.2f);
    }
}