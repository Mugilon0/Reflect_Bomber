using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自分のHPをライフアイコンで可視化するUI
/// 
/// 【セットアップ】
/// 1. LifeIcons (HorizontalLayoutGroup) の下に Image x3 を配置
/// 2. このスクリプトを LifeIcons にアタッチ
/// 3. lifeIcons 配列に3つのImageをドラッグ&ドロップ
/// 
/// 【HPHandler側の修正（1行のみ）】
/// public byte CurrentHP => HP;
/// </summary>
public class LifeIconManager : MonoBehaviour
{
    [Header("Life Icons (HP1, HP2, HP3の順)")]
    [SerializeField] private Image[] lifeIcons = new Image[3];

    [Header("Appearance")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private HPHandler localHPHandler;
    private int lastDisplayedHP = -1;
    private const int MAX_HP = 3;

    private void Update()
    {
        // ローカルプレイヤーのHPHandlerを取得
        if (localHPHandler == null)
        {
            if (NetworkPlayer.Local != null)
            {
                localHPHandler = NetworkPlayer.Local.HPHandler;
            }
            return;
        }

        // 現在のHPを取得
        int currentHP = localHPHandler.isDead ? 0 : localHPHandler.CurrentHP;

        // 変化があった場合のみ更新
        if (currentHP != lastDisplayedHP)
        {
            UpdateLifeIcons(currentHP);
            lastDisplayedHP = currentHP;
        }
    }

    private void UpdateLifeIcons(int currentHP)
    {
        currentHP = Mathf.Clamp(currentHP, 0, MAX_HP);

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;

            bool hasThisLife = (i < currentHP);
            lifeIcons[i].color = hasThisLife ? activeColor : inactiveColor;
        }
    }
}