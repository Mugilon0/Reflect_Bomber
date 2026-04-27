using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMaterialHandler : NetworkBehaviour
{
    [Header("Character Visuals")]
    public SkinnedMeshRenderer bodySkinnedMeshRenderer;
    public List<Material> bodyMaterials;

    [Networked(OnChanged = nameof(OnMaterialIndexChanged))]
    public int MaterialIndex { get; set; } = -1;

    public override void Spawned()
    {
        if (MaterialIndex != -1)
            ApplyMaterial();
    }

    public void SetInitialMaterialIndex(int index)
    {
        if (Object.HasStateAuthority)
        {
            MaterialIndex = index;
            ApplyMaterial();
        }
    }

    private void ApplyMaterial()
    {
        if (bodySkinnedMeshRenderer == null || bodyMaterials == null || bodyMaterials.Count == 0)
        {
            Debug.LogWarning("SkinnedMeshRenderer or BodyMaterials list is not assigned.");
            return;
        }

        if (MaterialIndex >= 0 && MaterialIndex < bodyMaterials.Count)
            bodySkinnedMeshRenderer.material = bodyMaterials[MaterialIndex];
        else
            Debug.LogWarning($"MaterialIndex {MaterialIndex} is out of bounds (count: {bodyMaterials.Count})");
    }

    static void OnMaterialIndexChanged(Changed<CharacterMaterialHandler> changed)
    {
        changed.Behaviour.ApplyMaterial();
    }
}