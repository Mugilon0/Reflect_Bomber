//using UnityEngine;
//using UnityEngine.AI;
//using Fusion;
//using System.Collections.Generic;

///// <summary>
///// デバッグ用ボットマネージャー（NavMesh対応・正確な武器システム版）
///// 
///// 3種類のボム:
///// - ShortBomb: 近距離用（WeaponHandler.Fire）
///// - LongBomb: 中〜長距離用（LongWeaponHandler.LongFire）チャージ時間で飛距離が変わる
///// - PutBomb: 後方設置型（PutWeaponHandler.PutBomb）
///// </summary>
//public class BotManager : MonoBehaviour
//{
//    //===========================================
//    // ボットの性格タイプ
//    //===========================================
//    public enum BotPersonality
//    {
//        Aggressive,  // 攻撃特化
//        Defensive,   // 逃げ特化
//        Balanced,    // バランス型
//        Camper,      // 待ち伏せ型
//        Hunter       // 追跡特化
//    }

//    [Header("=== Bot Settings ===")]
//    [Tooltip("ボットのオン/オフ")]
//    public bool enableBots = false;

//    [Tooltip("ボットの数（1〜10）")]
//    [Range(1, 10)] public int botCount = 1;

//    [Tooltip("プレイヤーと同じプレハブをセット")]
//    public NetworkObject botPrefab;

//    [Tooltip("ボットの性格をランダムにする")]
//    public bool randomizePersonality = true;

//    [Header("=== NavMesh Settings ===")]
//    [Tooltip("NavMeshを使用する")]
//    public bool useNavMesh = true;

//    [Tooltip("経路再計算の間隔（秒）")]
//    [Range(0.1f, 1f)] public float pathUpdateInterval = 0.3f;

//    [Header("=== Base AI Settings ===")]
//    [Tooltip("基本照準精度")]
//    [Range(0f, 1f)] public float baseAimAccuracy = 0.7f;

//    [Tooltip("基本反応速度")]
//    [Range(0f, 1f)] public float baseReactionTime = 0.3f;

//    [Tooltip("基本予測能力")]
//    [Range(0f, 1f)] public float basePredictionSkill = 0.5f;

//    [Header("=== Detection Settings ===")]
//    [Tooltip("基本検知範囲")]
//    [Range(10f, 100f)] public float baseDetectionRange = 50f;

//    [Header("=== Combat Settings ===")]
//    [Tooltip("基本攻撃クールダウン")]
//    [Range(0.5f, 5f)] public float baseAttackCooldown = 1.5f;

//    [Tooltip("基本攻撃範囲")]
//    [Range(5f, 25f)] public float baseAttackRange = 15f;

//    [Header("=== Evasion Settings ===")]
//    [Tooltip("爆弾回避を有効にする")]
//    public bool enableBombEvasion = true;

//    [Tooltip("爆弾検知距離")]
//    [Range(3f, 10f)] public float bombDetectionRange = 6f;

//    [Header("=== Obstacle Avoidance ===")]
//    [Tooltip("障害物検知の距離")]
//    [Range(1f, 5f)] public float obstacleCheckDistance = 2.5f;

//    [Tooltip("障害物検知のレイヤー")]
//    public LayerMask obstacleLayer = ~0;

//    // 内部変数
//    private List<BotBrain> activeBots = new List<BotBrain>();
//    private NetworkRunner runner;
//    private bool botsSpawned = false;
//    private bool navMeshAvailable = false;

//    void Start()
//    {
//        CheckNavMeshAvailability();
//    }

//    void CheckNavMeshAvailability()
//    {
//        NavMeshHit hit;
//        navMeshAvailable = NavMesh.SamplePosition(Vector3.zero, out hit, 100f, NavMesh.AllAreas);

//        if (useNavMesh && !navMeshAvailable)
//        {
//            Debug.LogWarning("[BotManager] NavMesh not found! Falling back to Raycast avoidance.");
//        }
//        else if (useNavMesh && navMeshAvailable)
//        {
//            Debug.Log("[BotManager] NavMesh detected and ready to use.");
//        }
//    }

//    void FixedUpdate()
//    {
//        if (!enableBots) return;

//        if (runner == null) runner = FindObjectOfType<NetworkRunner>();
//        if (runner == null || !runner.IsServer) return;

//        if (GameStateManager.Instance != null &&
//            GameStateManager.Instance.Current == GameStateManager.EGameState.Game)
//        {
//            if (!botsSpawned)
//            {
//                CheckNavMeshAvailability();
//                SpawnBots();
//                botsSpawned = true;
//            }
//            UpdateBots();
//        }
//        else
//        {
//            if (botsSpawned)
//            {
//                DespawnBots();
//                botsSpawned = false;
//            }
//        }
//    }

//    //===========================================
//    // ボットのスポーン・デスポーン
//    //===========================================

//    void SpawnBots()
//    {
//        for (int i = 0; i < botCount; i++)
//        {
//            Vector3 pos = GetSpawnPosition();
//            NetworkObject botObj = runner.Spawn(botPrefab, pos, Quaternion.identity, PlayerRef.None);

//            var np = botObj.GetComponent<NetworkPlayer>();

//            BotPersonality personality;
//            if (randomizePersonality)
//            {
//                personality = (BotPersonality)Random.Range(0, 5);
//            }
//            else
//            {
//                personality = (BotPersonality)(i % 5);
//            }

//            BotStats stats = GetStatsForPersonality(personality);

//            if (np != null)
//            {
//                np.nickName = $"{GetPersonalityPrefix(personality)}{i + 1}";
//            }

//            var brain = new BotBrain
//            {
//                obj = botObj,
//                networkPlayer = np,
//                cc = botObj.GetComponent<NetworkCharacterControllerPrototypeCustom>(),
//                hp = botObj.GetComponent<HPHandler>(),
//                // 武器ハンドラーを取得
//                weaponHandler = botObj.GetComponent<WeaponHandler>(),
//                longWeaponHandler = botObj.GetComponent<LongWeaponHandler>(),
//                putWeaponHandler = botObj.GetComponent<PutWeaponHandler>(),
//                personality = personality,
//                stats = stats,
//                attackTimer = Random.Range(0f, 1f),
//                wanderTimer = 0f,
//                targetPos = pos,
//                stuckTimer = 0f,
//                lastPosition = pos,
//                reactionTimer = 0f,
//                lastKnownEnemyPos = Vector3.zero,
//                lastKnownEnemyVel = Vector3.zero,
//                currentTarget = null,
//                isEvading = false,
//                fleeTimer = 0f,
//                campTimer = 0f,
//                idleTimer = 0f,
//                navPath = new NavMeshPath(),
//                currentPathIndex = 0,
//                pathUpdateTimer = 0f
//            };
//            activeBots.Add(brain);

//            Debug.Log($"[BotManager] Spawned {brain.networkPlayer?.nickName} with personality: {personality}");
//        }
//        Debug.Log($"[BotManager] Spawned {botCount} bots total (NavMesh: {(useNavMesh && navMeshAvailable ? "ON" : "OFF")})");
//    }

//    string GetPersonalityPrefix(BotPersonality personality)
//    {
//        switch (personality)
//        {
//            case BotPersonality.Aggressive: return "Atk_";
//            case BotPersonality.Defensive: return "Def_";
//            case BotPersonality.Balanced: return "Bal_";
//            case BotPersonality.Camper: return "Cmp_";
//            case BotPersonality.Hunter: return "Hnt_";
//            default: return "Bot_";
//        }
//    }

//    BotStats GetStatsForPersonality(BotPersonality personality)
//    {
//        BotStats stats = new BotStats();

//        switch (personality)
//        {
//            case BotPersonality.Aggressive:
//                stats.aimAccuracy = baseAimAccuracy + 0.15f;
//                stats.reactionTime = baseReactionTime * 0.6f;
//                stats.predictionSkill = basePredictionSkill + 0.2f;
//                stats.attackCooldown = baseAttackCooldown * 0.7f;
//                stats.detectionRange = baseDetectionRange * 0.8f;
//                stats.attackRange = baseAttackRange * 1.1f;
//                stats.preferredDistance = 5f;
//                stats.fleeHealthThreshold = 0;
//                stats.aggressionLevel = 1.0f;
//                stats.preferShortBomb = true;  // 近距離を好む
//                stats.usePutBomb = false;
//                break;

//            case BotPersonality.Defensive:
//                stats.aimAccuracy = baseAimAccuracy;
//                stats.reactionTime = baseReactionTime * 0.8f;
//                stats.predictionSkill = basePredictionSkill;
//                stats.attackCooldown = baseAttackCooldown * 1.2f;
//                stats.detectionRange = baseDetectionRange * 1.2f;
//                stats.attackRange = baseAttackRange * 1.3f;
//                stats.preferredDistance = 12f;
//                stats.fleeHealthThreshold = 2;
//                stats.aggressionLevel = 0.3f;
//                stats.preferShortBomb = false;  // 遠距離を好む
//                stats.usePutBomb = true;  // 逃げながらPutBomb
//                break;

//            case BotPersonality.Balanced:
//                stats.aimAccuracy = baseAimAccuracy;
//                stats.reactionTime = baseReactionTime;
//                stats.predictionSkill = basePredictionSkill;
//                stats.attackCooldown = baseAttackCooldown;
//                stats.detectionRange = baseDetectionRange;
//                stats.attackRange = baseAttackRange;
//                stats.preferredDistance = 8f;
//                stats.fleeHealthThreshold = 1;
//                stats.aggressionLevel = 0.6f;
//                stats.preferShortBomb = false;
//                stats.usePutBomb = true;
//                break;

//            case BotPersonality.Camper:
//                stats.aimAccuracy = baseAimAccuracy + 0.2f;
//                stats.reactionTime = baseReactionTime * 1.5f;
//                stats.predictionSkill = basePredictionSkill + 0.3f;
//                stats.attackCooldown = baseAttackCooldown * 0.8f;
//                stats.detectionRange = baseDetectionRange * 0.6f;
//                stats.attackRange = baseAttackRange * 1.2f;
//                stats.preferredDistance = 10f;
//                stats.fleeHealthThreshold = 1;
//                stats.aggressionLevel = 0.2f;
//                stats.campDuration = Random.Range(8f, 15f);
//                stats.preferShortBomb = false;
//                stats.usePutBomb = true;  // 周囲にPutBomb
//                break;

//            case BotPersonality.Hunter:
//                stats.aimAccuracy = baseAimAccuracy;
//                stats.reactionTime = baseReactionTime * 0.9f;
//                stats.predictionSkill = basePredictionSkill + 0.1f;
//                stats.attackCooldown = baseAttackCooldown;
//                stats.detectionRange = baseDetectionRange * 1.5f;
//                stats.attackRange = baseAttackRange;
//                stats.preferredDistance = 7f;
//                stats.fleeHealthThreshold = 1;
//                stats.aggressionLevel = 0.8f;
//                stats.preferShortBomb = false;
//                stats.usePutBomb = false;
//                break;
//        }

//        stats.aimAccuracy = Mathf.Clamp01(stats.aimAccuracy);
//        stats.predictionSkill = Mathf.Clamp01(stats.predictionSkill);

//        return stats;
//    }

//    void DespawnBots()
//    {
//        foreach (var bot in activeBots)
//        {
//            if (bot.obj != null && bot.obj.IsValid)
//                runner.Despawn(bot.obj);
//        }
//        activeBots.Clear();
//        Debug.Log("[BotManager] Despawned all bots");
//    }

//    //===========================================
//    // ボットAI更新
//    //===========================================

//    void UpdateBots()
//    {
//        foreach (var bot in activeBots)
//        {
//            if (bot.obj == null || !bot.obj.IsValid) continue;
//            if (bot.hp != null && bot.hp.isDead) continue;

//            if (enableBombEvasion && CheckBombEvasion(bot))
//            {
//                continue;
//            }

//            UpdateBotAI(bot);
//            CheckIfStuck(bot);
//        }
//    }

//    void UpdateBotAI(BotBrain bot)
//    {
//        Vector3 botPos = bot.obj.transform.position;

//        if (bot.hp != null && bot.hp.CurrentHP <= bot.stats.fleeHealthThreshold && bot.stats.fleeHealthThreshold > 0)
//        {
//            FleeFromDanger(bot);
//            return;
//        }

//        NetworkPlayer target = FindNearestEnemy(bot);

//        if (target != bot.currentTarget)
//        {
//            bot.currentTarget = target;
//            bot.reactionTimer = bot.stats.reactionTime;
//            bot.pathUpdateTimer = 0f;
//        }

//        if (target != null)
//        {
//            Vector3 currentEnemyPos = target.transform.position;
//            bot.lastKnownEnemyVel = (currentEnemyPos - bot.lastKnownEnemyPos) / Time.fixedDeltaTime;
//            bot.lastKnownEnemyPos = currentEnemyPos;

//            float dist = Vector3.Distance(botPos, currentEnemyPos);

//            if (bot.reactionTimer > 0)
//            {
//                bot.reactionTimer -= Time.fixedDeltaTime;
//            }

//            switch (bot.personality)
//            {
//                case BotPersonality.Aggressive:
//                    AggressiveBehavior(bot, target, dist);
//                    break;
//                case BotPersonality.Defensive:
//                    DefensiveBehavior(bot, target, dist);
//                    break;
//                case BotPersonality.Camper:
//                    CamperBehavior(bot, target, dist);
//                    break;
//                case BotPersonality.Hunter:
//                    HunterBehavior(bot, target, dist);
//                    break;
//                default:
//                    BalancedBehavior(bot, target, dist);
//                    break;
//            }
//        }
//        else
//        {
//            if (bot.personality == BotPersonality.Camper)
//            {
//                CamperWander(bot);
//            }
//            else
//            {
//                Wander(bot);
//            }
//        }
//    }

//    //===========================================
//    // 性格別の行動パターン
//    //===========================================

//    void AggressiveBehavior(BotBrain bot, NetworkPlayer target, float dist)
//    {
//        Vector3 targetPos = target.transform.position;
//        RotateToward(bot, targetPos);

//        if (dist <= bot.stats.attackRange)
//        {
//            if (bot.reactionTimer <= 0) TryAttack(bot, target, dist);
//            if (dist > bot.stats.preferredDistance) MoveToward(bot, targetPos);
//        }
//        else
//        {
//            MoveToward(bot, targetPos);
//        }
//    }

//    void DefensiveBehavior(BotBrain bot, NetworkPlayer target, float dist)
//    {
//        Vector3 targetPos = target.transform.position;
//        RotateToward(bot, targetPos);

//        if (dist <= bot.stats.attackRange)
//        {
//            if (bot.reactionTimer <= 0) TryAttack(bot, target, dist);

//            if (dist < bot.stats.preferredDistance)
//            {
//                MoveAwayFrom(bot, targetPos);
//                // 逃げながらPutBomb
//                if (bot.stats.usePutBomb && Random.value < 0.1f)
//                {
//                    TryPutBomb(bot);
//                }
//            }
//            else if (dist < bot.stats.preferredDistance + 3f)
//            {
//                Strafe(bot, targetPos);
//            }
//        }
//        else
//        {
//            if (dist < bot.stats.preferredDistance * 1.5f)
//                MoveAwayFrom(bot, targetPos);
//            else
//                MoveToward(bot, targetPos);
//        }
//    }

//    void BalancedBehavior(BotBrain bot, NetworkPlayer target, float dist)
//    {
//        Vector3 targetPos = target.transform.position;
//        RotateToward(bot, targetPos);

//        if (dist <= bot.stats.attackRange)
//        {
//            if (bot.reactionTimer <= 0) TryAttack(bot, target, dist);

//            if (dist < bot.stats.preferredDistance - 2f)
//                MoveAwayFrom(bot, targetPos);
//            else if (dist > bot.stats.preferredDistance + 2f)
//                MoveToward(bot, targetPos);
//            else
//                Strafe(bot, targetPos);
//        }
//        else
//        {
//            MoveToward(bot, targetPos);
//        }
//    }

//    void CamperBehavior(BotBrain bot, NetworkPlayer target, float dist)
//    {
//        Vector3 targetPos = target.transform.position;
//        RotateToward(bot, targetPos);

//        if (dist <= bot.stats.attackRange && bot.reactionTimer <= 0)
//        {
//            TryAttack(bot, target, dist);
//        }

//        // 周囲にPutBomb設置（たまに）
//        if (bot.stats.usePutBomb && Random.value < 0.02f)
//        {
//            TryPutBomb(bot);
//        }
//    }

//    void HunterBehavior(BotBrain bot, NetworkPlayer target, float dist)
//    {
//        Vector3 targetPos = target.transform.position;
//        RotateToward(bot, targetPos);

//        if (dist <= bot.stats.attackRange)
//        {
//            if (bot.reactionTimer <= 0) TryAttack(bot, target, dist);

//            if (dist < bot.stats.preferredDistance - 1f)
//                Strafe(bot, targetPos);
//            else if (dist > bot.stats.preferredDistance + 1f)
//                MoveToward(bot, targetPos);
//        }
//        else
//        {
//            MoveToward(bot, targetPos);
//        }
//    }

//    void FleeFromDanger(BotBrain bot)
//    {
//        NetworkPlayer nearestEnemy = FindNearestEnemy(bot);
//        if (nearestEnemy != null)
//        {
//            MoveAwayFrom(bot, nearestEnemy.transform.position);
//            // 逃げながらPutBomb
//            if (bot.stats.usePutBomb && Random.value < 0.15f)
//            {
//                TryPutBomb(bot);
//            }
//        }
//        else
//        {
//            Wander(bot);
//        }
//    }

//    void CamperWander(BotBrain bot)
//    {
//        bot.campTimer -= Time.fixedDeltaTime;

//        if (bot.campTimer <= 0)
//        {
//            bot.targetPos = GetWanderPosition(bot.obj.transform.position);
//            bot.campTimer = Random.Range(6f, 12f);
//            bot.wanderTimer = Random.Range(3f, 5f);
//        }

//        if (bot.wanderTimer > 0)
//        {
//            bot.wanderTimer -= Time.fixedDeltaTime;
//            MoveToward(bot, bot.targetPos);
//        }
//    }

//    //===========================================
//    // 攻撃システム（プレイヤーと同じ武器を使用）
//    //===========================================

//    void TryAttack(BotBrain bot, NetworkPlayer target, float distance)
//    {
//        bot.attackTimer -= Time.fixedDeltaTime;
//        if (bot.attackTimer > 0) return;

//        Vector3 botPos = bot.obj.transform.position + Vector3.up * 1f;
//        Vector3 targetPos = target.transform.position + Vector3.up * 1f;

//        // 視線チェック
//        Vector3 toTarget = targetPos - botPos;
//        if (Physics.Raycast(botPos, toTarget.normalized, out RaycastHit hit, distance))
//        {
//            if (hit.collider.GetComponentInParent<NetworkPlayer>() != target)
//            {
//                return; // 障害物で見えない
//            }
//        }

//        // 予測照準
//        Vector3 predictedPos = targetPos;
//        if (bot.stats.predictionSkill > 0 && bot.lastKnownEnemyVel.magnitude > 0.5f)
//        {
//            float estimatedFlightTime = distance / 25f;
//            predictedPos = targetPos + bot.lastKnownEnemyVel * estimatedFlightTime * bot.stats.predictionSkill;
//        }

//        // 照準方向を計算
//        Vector3 aimDir = (predictedPos - botPos).normalized;

//        // 精度に応じたブレ
//        float errorAngle = (1f - bot.stats.aimAccuracy) * 15f;
//        aimDir = Quaternion.Euler(
//            Random.Range(-errorAngle, errorAngle),
//            Random.Range(-errorAngle, errorAngle),
//            0
//        ) * aimDir;

//        // 距離に応じた武器選択
//        if (distance < 6f)
//        {
//            // 近距離: ShortBomb
//            FireShortBomb(bot, aimDir);
//        }
//        else if (distance < 20f)
//        {
//            // 中〜長距離: LongBomb
//            // 距離に応じたチャージ時間を計算（0.3〜1.3秒）
//            float chargeTime = Mathf.Lerp(0.3f, 1.3f, (distance - 6f) / 14f);
//            FireLongBomb(bot, aimDir, chargeTime);
//        }
//        else
//        {
//            // 超長距離: 最大チャージのLongBomb
//            FireLongBomb(bot, aimDir, 1.3f);
//        }

//        bot.attackTimer = bot.stats.attackCooldown + Random.Range(-0.2f, 0.2f);
//    }

//    /// <summary>
//    /// ShortBomb発射（WeaponHandler.Fire相当）
//    /// </summary>
//    void FireShortBomb(BotBrain bot, Vector3 aimDir)
//    {
//        if (bot.weaponHandler == null) return;

//        // NetworkInputDataを作成
//        NetworkInputData input = new NetworkInputData
//        {
//            isShortThrow = true,
//            aimForwardVector = aimDir
//        };

//        // WeaponHandler.Fireを直接呼ぶ
//        bot.weaponHandler.Fire(input);
//    }

//    /// <summary>
//    /// LongBomb発射（LongWeaponHandler.LongFire相当）
//    /// </summary>
//    void FireLongBomb(BotBrain bot, Vector3 aimDir, float chargeTime)
//    {
//        if (bot.longWeaponHandler == null) return;

//        // NetworkInputDataを作成
//        NetworkInputData input = new NetworkInputData
//        {
//            isLongThrow = true,
//            aimForwardVector = aimDir,
//            longThrowCharge = chargeTime  // チャージ時間（0.3〜1.3秒）
//        };

//        // LongWeaponHandler.LongFireを直接呼ぶ
//        bot.longWeaponHandler.LongFire(input);
//    }

//    /// <summary>
//    /// PutBomb設置（PutWeaponHandler.PutBomb相当）
//    /// </summary>
//    void TryPutBomb(BotBrain bot)
//    {
//        if (bot.putWeaponHandler == null) return;
//        if (bot.attackTimer > 0) return;

//        // PutWeaponHandler.PutBombを直接呼ぶ
//        bot.putWeaponHandler.PutBomb();

//        bot.attackTimer = bot.stats.attackCooldown + Random.Range(-0.2f, 0.2f);
//    }

//    //===========================================
//    // 爆弾回避
//    //===========================================

//    bool CheckBombEvasion(BotBrain bot)
//    {
//        float effectiveRange = bombDetectionRange;
//        if (bot.personality == BotPersonality.Defensive) effectiveRange *= 1.5f;
//        else if (bot.personality == BotPersonality.Aggressive) effectiveRange *= 0.7f;

//        Vector3 botPos = bot.obj.transform.position;
//        Collider[] colliders = Physics.OverlapSphere(botPos, effectiveRange);

//        GrenadeHandler nearestBomb = null;
//        float nearestDist = float.MaxValue;

//        foreach (var col in colliders)
//        {
//            var grenade = col.GetComponentInParent<GrenadeHandler>();
//            if (grenade != null)
//            {
//                float dist = Vector3.Distance(botPos, grenade.transform.position);
//                if (dist < nearestDist)
//                {
//                    nearestDist = dist;
//                    nearestBomb = grenade;
//                }
//            }
//        }

//        if (nearestBomb != null && nearestDist < effectiveRange)
//        {
//            MoveAwayFrom(bot, nearestBomb.transform.position);
//            bot.isEvading = true;
//            return true;
//        }

//        bot.isEvading = false;
//        return false;
//    }

//    //===========================================
//    // 敵の検索
//    //===========================================

//    NetworkPlayer FindNearestEnemy(BotBrain bot)
//    {
//        NetworkPlayer nearest = null;
//        float minDist = float.MaxValue;

//        foreach (var entry in NetworkPlayer.ActivePlayers)
//        {
//            var player = entry.Value;
//            if (player == null) continue;
//            if (player.gameObject == bot.obj.gameObject) continue;
//            if (player.IsDead) continue;

//            float dist = Vector3.Distance(bot.obj.transform.position, player.transform.position);
//            if (dist > bot.stats.detectionRange) continue;

//            if (dist < minDist)
//            {
//                minDist = dist;
//                nearest = player;
//            }
//        }
//        return nearest;
//    }

//    //===========================================
//    // 移動（NavMesh対応）
//    //===========================================

//    void MoveToward(BotBrain bot, Vector3 destination)
//    {
//        if (bot.cc == null) return;

//        Vector3 botPos = bot.obj.transform.position;
//        Vector3 moveTarget = destination;

//        if (useNavMesh && navMeshAvailable)
//        {
//            moveTarget = GetNextPathPoint(bot, destination);
//        }

//        Vector3 toTarget = moveTarget - botPos;
//        toTarget.y = 0;

//        if (toTarget.magnitude < 0.5f) return;

//        RotateToward(bot, moveTarget);

//        Vector3 forward = bot.obj.transform.forward;
//        forward.y = 0;
//        float angle = Vector3.Angle(forward, toTarget);

//        if (angle < 60f)
//        {
//            Vector3 rayOrigin = botPos + Vector3.up * 0.5f;
//            if (!Physics.Raycast(rayOrigin, forward, obstacleCheckDistance, obstacleLayer))
//            {
//                bot.cc.Move(forward, 1f);
//            }
//        }
//        else if (angle < 90f)
//        {
//            bot.cc.Move(forward * 0.5f, 0.5f);
//        }
//    }

//    void MoveAwayFrom(BotBrain bot, Vector3 dangerPos)
//    {
//        if (bot.cc == null) return;

//        Vector3 botPos = bot.obj.transform.position;
//        Vector3 awayDir = (botPos - dangerPos).normalized;
//        awayDir.y = 0;

//        Vector3 fleeTarget = botPos + awayDir * 10f;

//        if (useNavMesh && navMeshAvailable)
//        {
//            NavMeshHit hit;
//            if (NavMesh.SamplePosition(fleeTarget, out hit, 5f, NavMesh.AllAreas))
//            {
//                fleeTarget = hit.position;
//            }
//        }

//        RotateToward(bot, fleeTarget);

//        Vector3 forward = bot.obj.transform.forward;
//        forward.y = 0;
//        float angle = Vector3.Angle(forward, awayDir);

//        if (angle < 90f)
//        {
//            Vector3 rayOrigin = botPos + Vector3.up * 0.5f;
//            if (!Physics.Raycast(rayOrigin, forward, obstacleCheckDistance, obstacleLayer))
//            {
//                bot.cc.Move(forward, 1f);
//            }
//            else
//            {
//                TryAlternativeDirection(bot, awayDir);
//            }
//        }
//    }

//    Vector3 GetNextPathPoint(BotBrain bot, Vector3 destination)
//    {
//        Vector3 botPos = bot.obj.transform.position;

//        bot.pathUpdateTimer -= Time.fixedDeltaTime;

//        if (bot.pathUpdateTimer <= 0 || bot.navPath.corners.Length == 0)
//        {
//            CalculatePath(bot, destination);
//            bot.pathUpdateTimer = pathUpdateInterval;
//            bot.currentPathIndex = 0;
//        }

//        if (bot.navPath.status == NavMeshPathStatus.PathComplete ||
//            bot.navPath.status == NavMeshPathStatus.PathPartial)
//        {
//            if (bot.navPath.corners.Length > 0)
//            {
//                if (bot.currentPathIndex < bot.navPath.corners.Length)
//                {
//                    Vector3 currentWaypoint = bot.navPath.corners[bot.currentPathIndex];
//                    float distToWaypoint = Vector3.Distance(
//                        new Vector3(botPos.x, 0, botPos.z),
//                        new Vector3(currentWaypoint.x, 0, currentWaypoint.z)
//                    );

//                    if (distToWaypoint < 1.5f)
//                    {
//                        bot.currentPathIndex++;
//                    }

//                    if (bot.currentPathIndex < bot.navPath.corners.Length)
//                    {
//                        return bot.navPath.corners[bot.currentPathIndex];
//                    }
//                }
//            }
//        }

//        return destination;
//    }

//    void CalculatePath(BotBrain bot, Vector3 destination)
//    {
//        Vector3 botPos = bot.obj.transform.position;

//        NavMeshHit startHit;
//        Vector3 startPos = botPos;
//        if (NavMesh.SamplePosition(botPos, out startHit, 5f, NavMesh.AllAreas))
//        {
//            startPos = startHit.position;
//        }

//        NavMeshHit endHit;
//        Vector3 endPos = destination;
//        if (NavMesh.SamplePosition(destination, out endHit, 5f, NavMesh.AllAreas))
//        {
//            endPos = endHit.position;
//        }

//        NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, bot.navPath);
//    }

//    void TryAlternativeDirection(BotBrain bot, Vector3 preferredDir)
//    {
//        Vector3 botPos = bot.obj.transform.position;
//        Vector3 rayOrigin = botPos + Vector3.up * 0.5f;

//        float[] angles = { -45f, 45f, -90f, 90f, -135f, 135f };

//        foreach (float angle in angles)
//        {
//            Vector3 testDir = Quaternion.Euler(0, angle, 0) * preferredDir;

//            if (!Physics.Raycast(rayOrigin, testDir, obstacleCheckDistance, obstacleLayer))
//            {
//                RotateToward(bot, botPos + testDir * 5f);

//                Vector3 forward = bot.obj.transform.forward;
//                forward.y = 0;
//                float angleToDir = Vector3.Angle(forward, testDir);

//                if (angleToDir < 60f)
//                {
//                    bot.cc.Move(forward, 1f);
//                }
//                return;
//            }
//        }
//    }

//    void RotateToward(BotBrain bot, Vector3 targetPos)
//    {
//        if (bot.cc == null) return;

//        Vector3 botPos = bot.obj.transform.position;
//        Vector3 toTarget = targetPos - botPos;
//        toTarget.y = 0;

//        if (toTarget.magnitude < 0.1f) return;

//        Vector3 forward = bot.obj.transform.forward;
//        forward.y = 0;

//        float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

//        float rotationInput = 0f;
//        if (Mathf.Abs(angle) > 5f)
//        {
//            rotationInput = Mathf.Clamp(angle / 45f, -1f, 1f);
//        }

//        bot.cc.Rotate(rotationInput);
//    }

//    void Strafe(BotBrain bot, Vector3 enemyPos)
//    {
//        if (bot.cc == null) return;

//        RotateToward(bot, enemyPos);

//        float time = Time.time + bot.obj.GetHashCode() * 0.001f;
//        float moveInput = Mathf.Sin(time * 2f) * 0.5f;

//        if (Mathf.Abs(moveInput) > 0.3f)
//        {
//            Vector3 forward = bot.obj.transform.forward;
//            forward.y = 0;
//            bot.cc.Move(forward * (moveInput > 0 ? 1f : -1f), moveInput > 0 ? 1f : -1f);
//        }
//    }

//    //===========================================
//    // 巡回
//    //===========================================

//    void Wander(BotBrain bot)
//    {
//        bot.wanderTimer -= Time.fixedDeltaTime;

//        Vector3 botPos = bot.obj.transform.position;
//        Vector3 toTarget = bot.targetPos - botPos;
//        toTarget.y = 0;

//        if (bot.wanderTimer <= 0 || toTarget.magnitude < 3f)
//        {
//            bot.targetPos = GetWanderPosition(botPos);
//            bot.wanderTimer = Random.Range(4f, 8f);
//            bot.currentPathIndex = 0;
//            bot.pathUpdateTimer = 0f;
//        }

//        MoveToward(bot, bot.targetPos);
//    }

//    Vector3 GetWanderPosition(Vector3 currentPos)
//    {
//        for (int i = 0; i < 10; i++)
//        {
//            float randomAngle = Random.Range(0f, 360f);
//            float randomDist = Random.Range(10f, 20f);

//            Vector3 offset = new Vector3(
//                Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDist,
//                0f,
//                Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDist
//            );

//            Vector3 targetPos = currentPos + offset;

//            if (useNavMesh && navMeshAvailable)
//            {
//                NavMeshHit hit;
//                if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
//                {
//                    return hit.position;
//                }
//            }
//            else
//            {
//                targetPos.y = 10f;
//                if (Physics.Raycast(targetPos, Vector3.down, out RaycastHit hit, 20f))
//                {
//                    return hit.point + Vector3.up * 1f;
//                }
//            }
//        }

//        return GetSpawnPosition();
//    }

//    //===========================================
//    // スタック検出
//    //===========================================

//    void CheckIfStuck(BotBrain bot)
//    {
//        float movedDistance = Vector3.Distance(bot.obj.transform.position, bot.lastPosition);

//        float threshold = 0.05f;
//        float stuckTime = (bot.personality == BotPersonality.Camper) ? 8f : 2f;

//        if (movedDistance < threshold)
//        {
//            bot.stuckTimer += Time.fixedDeltaTime;

//            if (bot.stuckTimer > stuckTime)
//            {
//                ForceMove(bot);
//                bot.stuckTimer = 0f;
//            }
//        }
//        else
//        {
//            bot.stuckTimer = 0f;
//        }

//        bot.idleTimer += Time.fixedDeltaTime;
//        if (bot.currentTarget != null || movedDistance > 0.1f)
//        {
//            bot.idleTimer = 0f;
//        }

//        if (bot.idleTimer > 5f)
//        {
//            ForceMove(bot);
//            bot.idleTimer = 0f;
//        }

//        bot.lastPosition = bot.obj.transform.position;
//    }

//    void ForceMove(BotBrain bot)
//    {
//        bot.targetPos = GetWanderPosition(bot.obj.transform.position);
//        bot.wanderTimer = Random.Range(3f, 6f);
//        bot.currentTarget = null;
//        bot.currentPathIndex = 0;
//        bot.pathUpdateTimer = 0f;

//        if (bot.cc != null)
//        {
//            float randomRotation = Random.Range(-1f, 1f);
//            bot.cc.Rotate(randomRotation);
//        }

//        Debug.Log($"[BotManager] {bot.networkPlayer?.nickName} forced to move");
//    }

//    //===========================================
//    // スポーン位置
//    //===========================================

//    Vector3 GetSpawnPosition()
//    {
//        if (useNavMesh && navMeshAvailable)
//        {
//            for (int i = 0; i < 10; i++)
//            {
//                Vector3 randomPos = new Vector3(
//                    Random.Range(-18f, 18f),
//                    0f,
//                    Random.Range(-18f, 18f)
//                );

//                NavMeshHit hit;
//                if (NavMesh.SamplePosition(randomPos, out hit, 10f, NavMesh.AllAreas))
//                {
//                    return hit.position + Vector3.up * 0.1f;
//                }
//            }
//        }

//        for (int i = 0; i < 10; i++)
//        {
//            Vector3 randomPos = new Vector3(
//                Random.Range(-18f, 18f),
//                10f,
//                Random.Range(-18f, 18f)
//            );

//            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 20f))
//            {
//                return hit.point + Vector3.up * 1f;
//            }
//        }

//        return new Vector3(0, 1f, 0);
//    }

//    //===========================================
//    // デバッグGUI
//    //===========================================

//    void OnGUI()
//    {
//        if (!enableBots) return;
//        if (!Debug.isDebugBuild && !Application.isEditor) return;

//        GUILayout.BeginArea(new Rect(10, 200, 300, 350));
//        GUI.Box(new Rect(0, 0, 300, 350), "");

//        GUILayout.Label("<b>=== Bot Manager ===</b>");
//        GUILayout.Label($"Bots Active: {activeBots.Count}");
//        GUILayout.Label($"NavMesh: {(useNavMesh && navMeshAvailable ? "<color=green>ON</color>" : "<color=red>OFF</color>")}");
//        GUILayout.Label($"State: {(botsSpawned ? "Spawned" : "Waiting for Game")}");

//        GUILayout.Space(5);
//        GUILayout.Label("<b>Bot Status:</b>");
//        foreach (var bot in activeBots)
//        {
//            if (bot.networkPlayer != null)
//            {
//                string status = bot.currentTarget != null ? "Fighting" : "Wandering";
//                if (bot.isEvading) status = "Evading";
//                GUILayout.Label($"  {bot.networkPlayer.nickName}: {status}");
//            }
//        }

//        GUILayout.Space(5);

//        if (GUILayout.Button("Force Spawn Bots"))
//        {
//            if (runner != null && runner.IsServer && !botsSpawned)
//            {
//                SpawnBots();
//                botsSpawned = true;
//            }
//        }

//        if (GUILayout.Button("Force Despawn Bots"))
//        {
//            if (botsSpawned)
//            {
//                DespawnBots();
//                botsSpawned = false;
//            }
//        }

//        if (GUILayout.Button("Refresh NavMesh"))
//        {
//            CheckNavMeshAvailability();
//        }

//        GUILayout.Space(5);
//        enableBots = GUILayout.Toggle(enableBots, "Enable Bots");
//        useNavMesh = GUILayout.Toggle(useNavMesh, "Use NavMesh");
//        enableBombEvasion = GUILayout.Toggle(enableBombEvasion, "Bomb Evasion");
//        randomizePersonality = GUILayout.Toggle(randomizePersonality, "Random Personality");

//        GUILayout.EndArea();
//    }

//    //===========================================
//    // 内部クラス
//    //===========================================

//    private class BotStats
//    {
//        public float aimAccuracy;
//        public float reactionTime;
//        public float predictionSkill;
//        public float attackCooldown;
//        public float detectionRange;
//        public float attackRange;
//        public float preferredDistance;
//        public int fleeHealthThreshold;
//        public float aggressionLevel;
//        public float campDuration;
//        public bool preferShortBomb;
//        public bool usePutBomb;
//    }

//    private class BotBrain
//    {
//        public NetworkObject obj;
//        public NetworkPlayer networkPlayer;
//        public NetworkCharacterControllerPrototypeCustom cc;
//        public HPHandler hp;

//        // 武器ハンドラー
//        public WeaponHandler weaponHandler;
//        public LongWeaponHandler longWeaponHandler;
//        public PutWeaponHandler putWeaponHandler;

//        public BotPersonality personality;
//        public BotStats stats;

//        public float attackTimer;
//        public float wanderTimer;
//        public Vector3 targetPos;

//        public float stuckTimer;
//        public Vector3 lastPosition;

//        public float reactionTimer;
//        public Vector3 lastKnownEnemyPos;
//        public Vector3 lastKnownEnemyVel;
//        public NetworkPlayer currentTarget;

//        public bool isEvading;
//        public float fleeTimer;
//        public float campTimer;
//        public float idleTimer;

//        // NavMesh用
//        public NavMeshPath navPath;
//        public int currentPathIndex;
//        public float pathUpdateTimer;
//    }
//}