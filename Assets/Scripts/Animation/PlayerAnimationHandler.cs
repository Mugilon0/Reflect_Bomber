using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

[OrderAfter(typeof(NetworkCharacterControllerPrototypeCustom))]
public class PlayerAnimationHandler : NetworkBehaviour
{
    // ============================================================
    // アニメーションパラメータのハッシュ（パフォーマンス最適化）
    // 新しいパラメータを追加する場合はここに追加
    // ============================================================
    private static class AnimParams
    {
        // Float
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int Turn = Animator.StringToHash("Turn");

        // Bool
        public static readonly int IsCharging = Animator.StringToHash("IsCharging");
        public static readonly int IsDead = Animator.StringToHash("IsDead");

        // Trigger
        public static readonly int Throw = Animator.StringToHash("Throw");
        public static readonly int GetDamage = Animator.StringToHash("GetDamage");
    }

    // ============================================================
    // シーン名の定数（ハードコード回避）
    // ============================================================
    private static class SceneNames
    {
        public const string Ready = "Ready";
        public const string Lobby = "Lobby";
    }

    // ============================================================
    // コンポーネント参照
    // ============================================================
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;

    [Header("Turn Animation Settings")]
    [SerializeField]
    [Tooltip("回転アニメーションの感度。この値でAngularVelocityYを割って-1〜1に正規化")]
    private float turnAnimationSensitivity = 100f;

    [SerializeField]
    [Tooltip("Turnパラメータのスムージング速度")]
    private float turnSmoothSpeed = 10f;

    // コンポーネントキャッシュ
    private NetworkCharacterControllerPrototypeCustom ncc;
    private HPHandler hpHandler;
    private CharacterMovementHandler movementHandler;
    private WeaponManager weaponManager;

    // シーン名キャッシュ
    private string cachedSceneName;
    private bool isReadyScene;

    // Turnパラメータのスムージング用
    private float currentTurnValue;

    // ============================================================
    // ネットワーク同期プロパティ
    // ============================================================
    [Networked]
    private NetworkBool IsCharging { get; set; }

    [Networked(OnChanged = nameof(OnThrowStateChanged))]
    private NetworkBool DidThrow { get; set; }

    [Networked]
    public TickTimer AnimationLockTimer { get; set; }

    public bool CanPlayAnimation => AnimationLockTimer.ExpiredOrNotRunning(Runner);

    // ============================================================
    // 初期化
    // ============================================================
    private void Awake()
    {
        ncc = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        hpHandler = GetComponent<HPHandler>();
        movementHandler = GetComponent<CharacterMovementHandler>();
        weaponManager = GetComponent<WeaponManager>();
        CacheSceneName();
    }

    private void Start()
    {
        ValidateAnimator();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheSceneName();
    }

    private void CacheSceneName()
    {
        cachedSceneName = SceneManager.GetActiveScene().name;
        isReadyScene = (cachedSceneName == SceneNames.Ready);
    }

    private void ValidateAnimator()
    {
        if (animator == null)
        {
            Debug.LogError($"[PlayerAnimationHandler] Animator is NULL on {gameObject.name}. Check prefab assignment!", gameObject);
        }
    }

    // ============================================================
    // ネットワーク更新
    // ============================================================
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            IsCharging = input.IsCharging;

            if (input.isShortThrow || input.isLongThrow)
            {
                DidThrow = !DidThrow;
            }
        }

        if (hpHandler != null && animator != null)
        {
            animator.SetBool(AnimParams.IsDead, hpHandler.isDead);
        }
    }

    // ============================================================
    // レンダリング更新（ビジュアル同期）
    // ============================================================
    public override void Render()
    {
        if (!ValidateAnimatorForRender()) return;

        if (isReadyScene)
        {
            SetIdleState();
            return;
        }

        if (!CanPlayAnimation)
        {
            SetLockedState();
            return;
        }

        UpdateMovementAnimation();
        // UpdateTurnAnimation();  // コメントアウト中
        UpdateChargingAnimation();
    }

    // ============================================================
    // アニメーション制御メソッド
    // ============================================================

    private void SetIdleState()
    {
        animator.SetFloat(AnimParams.Speed, 0f);
        animator.SetFloat(AnimParams.Turn, 0f);
    }

    private void SetLockedState()
    {
        animator.SetFloat(AnimParams.Speed, 0f);
        animator.SetFloat(AnimParams.Turn, 0f);
        animator.SetBool(AnimParams.IsCharging, false);
    }

    private void UpdateMovementAnimation()
    {
        if (ncc == null) return;

        Vector3 horizontalVelocity = ncc.Velocity;
        horizontalVelocity.y = 0;
        animator.SetFloat(AnimParams.Speed, horizontalVelocity.magnitude);
    }

    private void UpdateTurnAnimation()
    {
        if (ncc == null) return;

        float targetTurn = Mathf.Clamp(ncc.AngularVelocityY / turnAnimationSensitivity, -1f, 1f);
        currentTurnValue = Mathf.Lerp(currentTurnValue, targetTurn, Time.deltaTime * turnSmoothSpeed);
        animator.SetFloat(AnimParams.Turn, currentTurnValue);
    }

    private void UpdateChargingAnimation()
    {
        animator.SetBool(AnimParams.IsCharging, IsCharging);
    }

    // ============================================================
    // アクションロック（移動・攻撃を無効化）
    // 今後追加するアニメーションでも使い回せる汎用メソッド
    // ============================================================

    /// <summary>
    /// アニメーション再生中のアクションをロックする
    /// </summary>
    /// <param name="duration">ロック時間（秒）</param>
    /// <param name="lockMovement">移動をロックするか</param>
    /// <param name="lockWeapon">攻撃をロックするか</param>
    /// <param name="lockAnimation">アニメーション更新をロックするか</param>
    public void LockActions(float duration, bool lockMovement = true, bool lockWeapon = true, bool lockAnimation = true)
    {
        if (!Object.HasStateAuthority) return;

        if (lockMovement && movementHandler != null)
        {
            movementHandler.MoveLockTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        if (lockWeapon && weaponManager != null)
        {
            weaponManager.LockWeapons(duration);
        }

        if (lockAnimation)
        {
            AnimationLockTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }
    }

    // ============================================================
    // 外部から呼び出すトリガーメソッド
    // ============================================================

    public void TriggerThrow()
    {
        if (animator != null && (hpHandler == null || !hpHandler.isDead))
        {
            animator.SetTrigger(AnimParams.Throw);
        }
    }

    /// <summary>
    /// 被弾アニメーションをトリガー（HPHandlerから呼び出される）
    /// </summary>
    /// <param name="lockDuration">アクションロック時間（秒）。0以下で無効</param>
    public void TriggerGetDamage(float lockDuration = 0.5f)
    {
        if (animator != null && (hpHandler == null || !hpHandler.isDead))
        {
            animator.SetTrigger(AnimParams.GetDamage);

            // アクションロック
            if (lockDuration > 0f)
            {
                LockActions(lockDuration, lockMovement: true, lockWeapon: true, lockAnimation: false);
            }
        }
    }

    // ============================================================
    // コールバック
    // ============================================================

    private static void OnThrowStateChanged(Changed<PlayerAnimationHandler> changed)
    {
        changed.Behaviour.TriggerThrow();
    }

    // ============================================================
    // ユーティリティ
    // ============================================================

    private bool ValidateAnimatorForRender()
    {
        if (animator != null) return true;

        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError($"[PlayerAnimationHandler] Animator not found on {gameObject.name}. Disabling animation handler.", gameObject);
            enabled = false;
            return false;
        }

        return true;
    }
}