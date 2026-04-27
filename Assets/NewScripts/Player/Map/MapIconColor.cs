using Fusion;
using UnityEngine;

public class MapIconColor : NetworkBehaviour
{
    [SerializeField] private MeshRenderer visualsRenderer;
    [SerializeField] private PlayerColorConfig colorConfig;

    [Networked(OnChanged = nameof(OnColorIndexChanged))]
    public int ColorIndex { get; set; } = 0;

    public override void Spawned()
    {
        //if (ColorIndex != 0)
            ApplyColor();
    }

    /// <summary>
    /// Spawner.OnPlayerJoined()‚ÅServer_Add()Œã‚ÉŒÄ‚Ô
    /// </summary>
    public void SetColorIndex()
    {
        if (!Object.HasStateAuthority) return;
        var networkPlayer = GetComponentInParent<NetworkPlayer>();
        if (networkPlayer == null) return;
        ColorIndex = networkPlayer.Index;
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (visualsRenderer == null || colorConfig == null) return;
        visualsRenderer.material.color = colorConfig.GetColor(ColorIndex);
    }

    static void OnColorIndexChanged(Changed<MapIconColor> changed)
    {
        changed.Behaviour.ApplyColor();
    }

    public void SetMinimapCamera(GameObject minimapCameraObject) { }
}