using System;
using UnityEngine;

[Serializable]
public class CustomerMaterialTarget
{
    [Tooltip("Renderer whose material will be replaced.")]
    public Renderer Renderer;

    [Tooltip(
        "Material slot to replace. " +
        "Use 0 when the renderer has one material.")]
    [Min(0)]
    public int MaterialIndex;
}

public class CustomerVisualVariation : MonoBehaviour
{
    [Header("Material Targets")]
    [Tooltip(
        "Add the body or clothing renderer that should receive the random skin.")]
    [SerializeField]
    private CustomerMaterialTarget[] materialTargets;

    [Header("Available Skin Materials")]
    [Tooltip(
        "Drag every available customer skin material into this array.")]
    [SerializeField]
    private Material[] skinMaterials;

    [Header("Random Scale")]
    [SerializeField]
    private bool randomizeScale;

    [Min(0.1f)]
    [SerializeField]
    private float minimumScale = 0.95f;

    [Min(0.1f)]
    [SerializeField]
    private float maximumScale = 1.05f;

    [Header("Testing")]
    [Tooltip(
        "Enable only when testing a manually placed customer. " +
        "The future spawner will call ApplyRandomVariation itself.")]
    [SerializeField]
    private bool randomizeOnAwake;

    [SerializeField]
    private int appliedSkinIndex = -1;

    public int AppliedSkinIndex =>
        appliedSkinIndex;

    public int SkinCount =>
        skinMaterials != null
            ? skinMaterials.Length
            : 0;

    private void Awake()
    {
        if (randomizeOnAwake)
        {
            ApplyRandomVariation();
        }
    }

    public void ApplyRandomVariation()
    {
        ApplyRandomSkin();
        ApplyRandomScale();
    }

    public bool ApplyRandomSkin()
    {
        if (skinMaterials == null ||
            skinMaterials.Length == 0)
        {
            Debug.LogWarning(
                name +
                " has no skin materials assigned.",
                this);

            return false;
        }

        int randomStartIndex =
            UnityEngine.Random.Range(
                0,
                skinMaterials.Length);

        for (int offset = 0;
             offset < skinMaterials.Length;
             offset++)
        {
            int candidateIndex =
                (randomStartIndex + offset) %
                skinMaterials.Length;

            if (skinMaterials[candidateIndex] == null)
            {
                continue;
            }

            return ApplySkin(candidateIndex);
        }

        Debug.LogWarning(
            name +
            " does not contain any valid skin materials.",
            this);

        return false;
    }

    public bool ApplySkin(int skinIndex)
    {
        if (skinMaterials == null ||
            skinIndex < 0 ||
            skinIndex >= skinMaterials.Length)
        {
            return false;
        }

        Material selectedMaterial =
            skinMaterials[skinIndex];

        if (selectedMaterial == null)
        {
            return false;
        }

        if (materialTargets == null ||
            materialTargets.Length == 0)
        {
            Debug.LogWarning(
                name +
                " has no material targets assigned.",
                this);

            return false;
        }

        bool appliedToAtLeastOneRenderer = false;

        for (int i = 0;
             i < materialTargets.Length;
             i++)
        {
            CustomerMaterialTarget target =
                materialTargets[i];

            if (target == null ||
                target.Renderer == null)
            {
                continue;
            }

            Material[] currentMaterials =
                target.Renderer.sharedMaterials;

            if (currentMaterials == null ||
                currentMaterials.Length == 0)
            {
                continue;
            }

            int materialIndex =
                Mathf.Clamp(
                    target.MaterialIndex,
                    0,
                    currentMaterials.Length - 1);

            currentMaterials[materialIndex] =
                selectedMaterial;

            target.Renderer.sharedMaterials =
                currentMaterials;

            appliedToAtLeastOneRenderer = true;
        }

        if (!appliedToAtLeastOneRenderer)
        {
            return false;
        }

        appliedSkinIndex = skinIndex;
        return true;
    }

    public void ApplyRandomScale()
    {
        if (!randomizeScale)
        {
            transform.localScale =
                Vector3.one;

            return;
        }

        float scale =
            UnityEngine.Random.Range(
                minimumScale,
                maximumScale);

        transform.localScale =
            Vector3.one * scale;
    }

    public void ResetScale()
    {
        transform.localScale =
            Vector3.one;
    }

    private void OnValidate()
    {
        minimumScale =
            Mathf.Max(0.1f,minimumScale);

        maximumScale =
            Mathf.Max(
                minimumScale,
                maximumScale);
    }
}