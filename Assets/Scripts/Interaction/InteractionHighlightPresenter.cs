using System.Collections.Generic;
using UnityEngine;

[RequireComponent(
    typeof(PlayerInteractionController))]
public sealed class InteractionHighlightPresenter :
    MonoBehaviour
{
    [ColorUsage(true,true)]
    [SerializeField]
    private Color highlightColor =
        new Color(0.55f,0.9f,0.12f,0.55f);

    private int emissionColor;

    private readonly List<Renderer> activeRenderers =
        new List<Renderer>();

    private MaterialPropertyBlock block;

    private PlayerInteractionController player;

    private void Awake()
    {
        emissionColor =
            Shader.PropertyToID("_EmissionColor");

        block = new MaterialPropertyBlock();

        player =
            GetComponent<
                PlayerInteractionController>();
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.HighlightTargetChanged +=
                HandleTargetChanged;
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.HighlightTargetChanged -=
                HandleTargetChanged;
        }

        ClearHighlight();
    }

    private void HandleTargetChanged(
        MonoBehaviour target)
    {
        ClearHighlight();

        if (target == null)
        {
            return;
        }

        Renderer[] renderers =
            target.GetComponentsInChildren<
                Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(block);
            block.SetColor(
                emissionColor,
                highlightColor);
            renderer.SetPropertyBlock(block);
            activeRenderers.Add(renderer);
            block.Clear();
        }
    }

    private void ClearHighlight()
    {
        if (block == null)
        {
            activeRenderers.Clear();
            return;
        }

        for (int i = 0;
             i < activeRenderers.Count;
             i++)
        {
            Renderer renderer =
                activeRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(block);
            block.SetColor(
                emissionColor,
                Color.black);
            renderer.SetPropertyBlock(block);
            block.Clear();
        }

        activeRenderers.Clear();
    }
}
