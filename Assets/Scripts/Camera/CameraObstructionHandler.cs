using System.Collections.Generic;
using UnityEngine;

public class CameraObstructionHandler : MonoBehaviour
{
    [Header("Obstruction Settings")]
    public LayerMask obstructionLayerMask;
    public int maxRaycastHits = 10;

    [Header("Range Settings")]
    [Tooltip("ヒット位置からこの範囲内の障害物も透明化")]
    public float obstructionRadius = 2f;
    [Tooltip("周辺検索の最大オブジェクト数")]
    public int maxNearbyObjects = 20;

    [Header("Optimization")]
    public float raycastInterval = 0.05f;

    [Header("Debug")]
    public bool showDebugLog = false;
    public bool showDebugGizmos = true;

    private RaycastHit[] hitCache;
    private Collider[] overlapCache;
    private HashSet<Renderer> currentObstructions;
    private HashSet<Renderer> previousObstructions;
    private List<Renderer> toDisable;
    private List<Renderer> toEnable;

    private float raycastTimer = 0f;
    private List<Vector3> debugHitPositions = new List<Vector3>();

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        hitCache = new RaycastHit[maxRaycastHits];
        overlapCache = new Collider[maxNearbyObjects];
        currentObstructions = new HashSet<Renderer>();
        previousObstructions = new HashSet<Renderer>();
        toDisable = new List<Renderer>(maxRaycastHits);
        toEnable = new List<Renderer>(maxRaycastHits);
    }

    public void HandleObstructions(Vector3 cameraPosition, Vector3 targetPosition)
    {
        raycastTimer += Time.deltaTime;

        if (raycastTimer >= raycastInterval)
        {
            raycastTimer = 0f;
            DetectAndUpdateObstructions(cameraPosition, targetPosition);
        }
    }

    private void DetectAndUpdateObstructions(Vector3 cameraPosition, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - cameraPosition;
        float distance = direction.magnitude;

        var temp = previousObstructions;
        previousObstructions = currentObstructions;
        currentObstructions = temp;
        currentObstructions.Clear();

        debugHitPositions.Clear();

        int hitCount = Physics.RaycastNonAlloc(
            cameraPosition,
            direction.normalized,
            hitCache,
            distance,
            obstructionLayerMask
        );

        if (showDebugLog)
        {
            Debug.Log($"========== Raycast結果 ==========");
            Debug.Log($"ヒット数: {hitCount}");
            Debug.Log($"距離: {distance:F2}m");
        }

        // 各ヒットポイントの周辺オブジェクトを収集
        for (int i = 0; i < hitCount; i++)
        {
            Vector3 hitPoint = hitCache[i].point;
            debugHitPositions.Add(hitPoint);

            if (showDebugLog)
            {
                Debug.Log($"--- Hit {i} ---");
                Debug.Log($"  位置: {hitPoint}");
                Debug.Log($"  距離: {hitCache[i].distance:F2}m");
            }

            // ヒット位置周辺の障害物を収集
            CollectNearbyObstructions(hitPoint);
        }

        if (showDebugLog)
        {
            Debug.Log($"合計非表示対象Renderer数: {currentObstructions.Count}");
            Debug.Log($"================================");
        }

        UpdateRendererStates();
    }

    private void CollectNearbyObstructions(Vector3 hitPoint)
    {
        // ヒット位置を中心に球状の範囲で障害物を検索
        int nearbyCount = Physics.OverlapSphereNonAlloc(
            hitPoint,
            obstructionRadius,
            overlapCache,
            obstructionLayerMask
        );

        if (showDebugLog)
        {
            Debug.Log($"  周辺オブジェクト: {nearbyCount}個");
        }

        for (int i = 0; i < nearbyCount; i++)
        {
            Renderer renderer = overlapCache[i].GetComponent<Renderer>();

            if (renderer != null)
            {
                currentObstructions.Add(renderer);

                if (showDebugLog)
                {
                    Debug.Log($"    → 追加: {renderer.gameObject.name}");
                }
            }
        }
    }

    private void UpdateRendererStates()
    {
        toDisable.Clear();
        toEnable.Clear();

        foreach (Renderer renderer in currentObstructions)
        {
            if (!previousObstructions.Contains(renderer))
            {
                toDisable.Add(renderer);
            }
        }

        foreach (Renderer renderer in previousObstructions)
        {
            if (!currentObstructions.Contains(renderer))
            {
                toEnable.Add(renderer);
            }
        }

        // 実際に切り替え
        foreach (Renderer renderer in toDisable)
        {
            if (renderer != null)
            {
                if (showDebugLog)
                {
                    Debug.Log($"[非表示] {renderer.gameObject.name}");
                }
                renderer.enabled = false;
            }
        }

        foreach (Renderer renderer in toEnable)
        {
            if (renderer != null)
            {
                if (showDebugLog)
                {
                    Debug.Log($"[表示] {renderer.gameObject.name}");
                }
                renderer.enabled = true;
            }
        }
    }

    public void ResetAllObstructions()
    {
        foreach (Renderer renderer in currentObstructions)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        foreach (Renderer renderer in previousObstructions)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        currentObstructions.Clear();
        previousObstructions.Clear();
    }

    private void OnDisable()
    {
        ResetAllObstructions();
    }

    private void OnDestroy()
    {
        ResetAllObstructions();
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying)
            return;

        // ヒット位置と検索範囲を表示
        Gizmos.color = Color.yellow;
        foreach (Vector3 hitPos in debugHitPositions)
        {
            Gizmos.DrawWireSphere(hitPos, obstructionRadius);
            Gizmos.DrawSphere(hitPos, 0.1f);
        }
    }
}




//using System.Collections.Generic;
//using UnityEngine;

//public class CameraObstructionHandler : MonoBehaviour
//{
//    [Header("Obstruction Settings")]
//    public LayerMask obstructionLayerMask;
//    public int maxRaycastHits = 10;
    
//    [Header("Optimization")]
//    public float raycastInterval = 0.05f;
    
//    [Header("Debug")]
//    public bool showDebugLog = true; // デバッグログのON/OFF
    
//    private RaycastHit[] hitCache;
//    private HashSet<Renderer> currentObstructions;
//    private HashSet<Renderer> previousObstructions;
//    private List<Renderer> toDisable;
//    private List<Renderer> toEnable;
    
//    private float raycastTimer = 0f;

//    private void Awake()
//    {
//        Initialize();
//    }

//    private void Initialize()
//    {
//        hitCache = new RaycastHit[maxRaycastHits];
//        currentObstructions = new HashSet<Renderer>();
//        previousObstructions = new HashSet<Renderer>();
//        toDisable = new List<Renderer>(maxRaycastHits);
//        toEnable = new List<Renderer>(maxRaycastHits);
//    }

//    public void HandleObstructions(Vector3 cameraPosition, Vector3 targetPosition)
//    {
//        raycastTimer += Time.deltaTime;
        
//        if (raycastTimer >= raycastInterval)
//        {
//            raycastTimer = 0f;
//            DetectAndUpdateObstructions(cameraPosition, targetPosition);
//        }
//    }

//    private void DetectAndUpdateObstructions(Vector3 cameraPosition, Vector3 targetPosition)
//    {
//        Vector3 direction = targetPosition - cameraPosition;
//        float distance = direction.magnitude;
        
//        var temp = previousObstructions;
//        previousObstructions = currentObstructions;
//        currentObstructions = temp;
//        currentObstructions.Clear();
        
//        int hitCount = Physics.RaycastNonAlloc(
//            cameraPosition, 
//            direction.normalized, 
//            hitCache, 
//            distance, 
//            obstructionLayerMask
//        );
        
//        if (showDebugLog)
//        {
//            Debug.Log($"========== Raycast結果 ==========");
//            Debug.Log($"ヒット数: {hitCount}");
//            Debug.Log($"距離: {distance:F2}m");
//        }
        
//        for (int i = 0; i < hitCount; i++)
//        {
//            GameObject hitObject = hitCache[i].collider.gameObject;
            
//            if (showDebugLog)
//            {
//                Debug.Log($"--- Hit {i} ---");
//                Debug.Log($"  GameObject: {hitObject.name}");
//                Debug.Log($"  距離: {hitCache[i].distance:F2}m");
//                Debug.Log($"  レイヤー: {LayerMask.LayerToName(hitObject.layer)}");
//            }
            
//            // ヒットしたオブジェクト自身のRendererを取得
//            Renderer renderer = hitObject.GetComponent<Renderer>();
            
//            if (renderer != null)
//            {
//                if (showDebugLog)
//                {
//                    Debug.Log($"  → Renderer発見: {renderer.name}");
//                }
//                currentObstructions.Add(renderer);
//            }
//            else
//            {
//                if (showDebugLog)
//                {
//                    Debug.Log($"  → Rendererなし（親オブジェクト？）");
//                }
//            }
//        }
        
//        if (showDebugLog)
//        {
//            Debug.Log($"合計非表示対象Renderer数: {currentObstructions.Count}");
//            Debug.Log($"================================");
//        }
        
//        UpdateRendererStates();
//    }

//    private void UpdateRendererStates()
//    {
//        toDisable.Clear();
//        toEnable.Clear();
        
//        foreach (Renderer renderer in currentObstructions)
//        {
//            if (!previousObstructions.Contains(renderer))
//            {
//                toDisable.Add(renderer);
//            }
//        }
        
//        foreach (Renderer renderer in previousObstructions)
//        {
//            if (!currentObstructions.Contains(renderer))
//            {
//                toEnable.Add(renderer);
//            }
//        }
        
//        // 実際に切り替え
//        foreach (Renderer renderer in toDisable)
//        {
//            if (renderer != null)
//            {
//                if (showDebugLog)
//                {
//                    Debug.Log($"[非表示] {renderer.gameObject.name} (親: {renderer.transform.parent?.name ?? "なし"})");
//                }
//                renderer.enabled = false;
//            }
//        }
        
//        foreach (Renderer renderer in toEnable)
//        {
//            if (renderer != null)
//            {
//                if (showDebugLog)
//                {
//                    Debug.Log($"[表示] {renderer.gameObject.name}");
//                }
//                renderer.enabled = true;
//            }
//        }
//    }

//    public void ResetAllObstructions()
//    {
//        foreach (Renderer renderer in currentObstructions)
//        {
//            if (renderer != null)
//            {
//                renderer.enabled = true;
//            }
//        }
        
//        foreach (Renderer renderer in previousObstructions)
//        {
//            if (renderer != null)
//            {
//                renderer.enabled = true;
//            }
//        }
        
//        currentObstructions.Clear();
//        previousObstructions.Clear();
//    }

//    private void OnDisable()
//    {
//        ResetAllObstructions();
//    }

//    private void OnDestroy()
//    {
//        ResetAllObstructions();
//    }
//}
