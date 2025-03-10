using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class DitherRF : ScriptableRendererFeature
{
    [SerializeField] private RenderPassEvent _injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    [SerializeField] private Material _material = null;

    private DitherEffectPass _pass;

    /// <inheritdoc/>
    public override void Create()
    {
        _pass = new DitherEffectPass();

        // Configures where the render pass should be injected.
        _pass.renderPassEvent = _injectionPoint;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!_material)
        {
            Debug.LogWarning("No dither material blablalba");
            return;
        }

        _pass.Setup(_material);
        renderer.EnqueuePass(_pass);
    }

    class DitherEffectPass : ScriptableRenderPass
    {
        const string _passName = "DitherEffectPass";
        Material _blitMaterial;


        public void Setup(Material material)
        {
            _blitMaterial = material;
            requiresIntermediateTexture = true;
        }


        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var customEffect = stack.GetComponent<SphereVolumeComponent>();

            if (!customEffect.IsActive()) return;

            // Acces point to all renderers texture handles
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError("blablabla error dither pass...");
                return;
            }

            TextureHandle source = resourceData.activeColorTexture;

            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = $"CameraColor-{_passName}";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            RenderGraphUtils.BlitMaterialParameters para = new(source, destination, _blitMaterial, 0);
            renderGraph.AddBlitPass(para, passName: _passName);

            resourceData.cameraColor = destination;
        }
    }
}