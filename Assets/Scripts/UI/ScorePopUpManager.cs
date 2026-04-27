using UnityEngine;
using DamageNumbersPro;

/// <summary>
/// Damage Numbers Pro を使用してスコアポップアップを表示するマネージャー
/// UIManager配下のCanvasを自動で見つけてScreenSpace上に表示する
/// </summary>
public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager Instance { get; private set; }

    [Header("Damage Number Settings")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    private RectTransform _canvasRect;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (damageNumberPrefab == null)
            Debug.LogError("ScorePopupManager: Damage Number Prefab is not assigned!");
    }

    private void Start()
    {
        // UIManager配下のCanvasを自動で探す
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            _canvasRect = uiManager.GetComponentInChildren<Canvas>()?.GetComponent<RectTransform>();
            if (_canvasRect == null)
                Debug.LogError("ScorePopupManager: Canvas not found under UIManager!");
        }
        else
        {
            Debug.LogError("ScorePopupManager: UIManager not found!");
        }
    }

    /// <summary>
    /// ワールド座標をScreenSpace変換してCanvas上にポップアップを表示する
    /// </summary>
    public static void ShowPopup(Vector3 worldPosition, Vector3? customOffset = null)
    {
        if (Instance == null || Instance.damageNumberPrefab == null || Instance._canvasRect == null)
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // ワールド座標にオフセットを加算
        Vector3 offset = customOffset ?? Vector3.zero;
        Vector3 targetWorldPos = worldPosition + offset;

        // ワールド座標 → スクリーン座標
        Vector3 screenPos = cam.WorldToScreenPoint(targetWorldPos);

        // カメラの後ろなら表示しない
        if (screenPos.z < 0) return;

        // スクリーン座標 → Canvas上のローカル座標
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Instance._canvasRect,
            screenPos,
            null,
            out Vector2 localPoint);

        // Canvas上にスポーン
        Instance.damageNumberPrefab.SpawnGUI(Instance._canvasRect, localPoint);
    }
}










//using UnityEngine;
//using DamageNumbersPro;

///// <summary>
///// Damage Numbers Pro を使用してスコアポップアップを表示するマネージャー
///// ScreenSpace CanvasにスポーンすることでカメラアングルによらずUIとして表示する
///// </summary>
//public class ScorePopupManager : MonoBehaviour
//{
//    public static ScorePopupManager Instance { get; private set; }

//    [Header("Damage Number Settings")]
//    [SerializeField] private DamageNumber damageNumberPrefab;

//    [Header("Canvas Settings")]
//    [SerializeField] private RectTransform canvasRect; // GameUI上のCanvasをアタッチ

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Destroy(gameObject);
//            return;
//        }

//        if (damageNumberPrefab == null)
//            Debug.LogError("ScorePopupManager: Damage Number Prefab is not assigned!");

//        if (canvasRect == null)
//            Debug.LogError("ScorePopupManager: Canvas Rect is not assigned!");
//    }

//    /// <summary>
//    /// ワールド座標をScreenSpace変換してCanvas上にポップアップを表示する
//    /// </summary>
//    /// <param name="worldPosition">攻撃者のワールド座標</param>
//    /// <param name="customOffset">ワールド空間でのオフセット（オプション）</param>
//    public static void ShowPopup(Vector3 worldPosition, Vector3? customOffset = null)
//    {
//        if (Instance == null || Instance.damageNumberPrefab == null || Instance.canvasRect == null)
//            return;

//        // ワールド座標にオフセットを加算
//        Vector3 offset = customOffset ?? Vector3.zero;
//        Vector3 targetWorldPos = worldPosition + offset;

//        // カメラが存在しない場合はスキップ
//        Camera cam = Camera.main;
//        if (cam == null) return;

//        // ワールド座標 → スクリーン座標
//        Vector3 screenPos = cam.WorldToScreenPoint(targetWorldPos);

//        // 画面外なら表示しない
//        if (screenPos.z < 0) return;

//        // スクリーン座標 → Canvas上のローカル座標
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            Instance.canvasRect,
//            screenPos,
//            null,
//            out Vector2 localPoint);

//        // Canvas上の座標にスポーン
//        Instance.damageNumberPrefab.SpawnGUI(Instance.canvasRect, localPoint);
//    }
//}