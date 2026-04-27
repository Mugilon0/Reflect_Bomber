using Fusion;
using UnityEngine;

public class CharacterOutfitHandler : NetworkBehaviour
{
    [Header("Character Visuals")]
    public SkinnedMeshRenderer bodySkinnedMeshRenderer;

    [Header("Color Config")]
    [SerializeField] private PlayerColorConfig colorConfig;

    [Networked(OnChanged = nameof(OnColorIndexChanged))]
    public int ColorIndex { get; set; } = 0;

    public override void Spawned()
    {
        // ColorIndexが設定済みなら色を適用（クライアント側の初期化）
        if (ColorIndex != 0)
            ApplyColor();
    }

    /// <summary>
    /// Spawner.OnPlayerJoined()でServer_Add()後に呼ぶ
    /// NetworkPlayer.Indexが確定してから色を設定する
    /// </summary>
    public void SetColorIndex()
    {
        if (!Object.HasStateAuthority) return;
        var networkPlayer = GetComponent<NetworkPlayer>();
        if (networkPlayer == null) return;
        ColorIndex = networkPlayer.Index;
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (bodySkinnedMeshRenderer == null || colorConfig == null) return;
        bodySkinnedMeshRenderer.material.color = colorConfig.GetColor(ColorIndex);
    }

    static void OnColorIndexChanged(Changed<CharacterOutfitHandler> changed)
    {
        changed.Behaviour.ApplyColor();
    }
}