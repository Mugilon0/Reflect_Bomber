using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

[OrderAfter(typeof(NetworkCharacterControllerPrototypeCustom))]
public class PlayerAnimationHandler : NetworkBehaviour
{
    // 必要なコンポーネントへの参照
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    private NetworkCharacterControllerPrototypeCustom ncc;
    private HPHandler hpHandler;

    // ネットワーク同期されるプロパティ
    [Networked]
    private NetworkBool IsCharging { get; set; }

    [Networked(OnChanged = nameof(OnThrowStateChanged))]
    private NetworkBool DidThrow { get; set; }

    // MONOBEHAVIOUR

    void Start()
    {
        // ゲームが開始された直後、またはオブジェクトが有効になった直後に呼ばれる
        if (animator == null)
        {
            Debug.LogError($"[ANIMATION DEBUG] Start: Animator is NULL on {gameObject.name}. Prefab assignment might be missing!", gameObject);
        }
        else
        {
            Debug.Log($"[ANIMATION DEBUG] Start: Animator is assigned on {gameObject.name}.", gameObject);
        }
    }


    private void Awake()
    {
        //animator = GetComponentInChildren<Animator>();
        ncc = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        hpHandler = GetComponent<HPHandler>();
    }

    // NetworkBehaviour INTERFACE

    public override void FixedUpdateNetwork()
    {
        // 入力権限があるプレイヤー（自分自身）の入力を取得し、[Networked]プロパティを更新
        if (GetInput(out NetworkInputData input))
        {
            IsCharging = input.IsCharging;

            // 投げる入力があった場合、DidThrowプロパティを反転させてOnChangedコールバックを起動
            if (input.isShortThrow || input.isLongThrow)
            {
                DidThrow = !DidThrow;
            }
        }

        // AnimatorのIsDeadパラメータをHPHandlerの状態と同期
        if (hpHandler != null)
        {
            animator.SetBool("IsDead", hpHandler.isDead);
        }
    }

    public override void Render()
    {
        // Renderメソッドの最初にチェックを追加
        if (animator == null)
        {
            Debug.LogError("Animatorがありません！！");
            // もし参照が失われていたら、再度取得を試みる
            animator = GetComponentInChildren<Animator>();

            // それでも見つからなければ、エラーを防ぐために処理を中断する
            if (animator == null)
            {
                Debug.LogError("Animator component could not be found on this object or its children!", gameObject);
                return;
            }
        }
        // --- シーンに応じたアニメーション制御 ---
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Ready")
        {
            // Readyシーンでは常にSpeedを0にしてIdleアニメーションにする
            animator.SetFloat("Speed", 0);
            return;
        }

        // --- バトルシーンでのアニメーション制御 ---

        // Speedパラメータの更新
        Vector3 horizontalVelocity = ncc.Velocity;
        horizontalVelocity.y = 0;
        animator.SetFloat("Speed", horizontalVelocity.magnitude);

        // IsChargingパラメータの更新
        animator.SetBool("IsCharging", IsCharging);
    }

    // DidThrowプロパティが変更されたときに全クライアントで呼ばれる
    static void OnThrowStateChanged(Changed<PlayerAnimationHandler> changed)
    {
        // 死亡していなければ、Throwトリガーを引く
        if (changed.Behaviour.hpHandler != null && !changed.Behaviour.hpHandler.isDead)
        {
            changed.Behaviour.animator.SetTrigger("Throw");
        }
    }
}