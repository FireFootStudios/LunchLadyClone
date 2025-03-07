using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public sealed class DitheringRF : ScriptableRendererFeature
{
    [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    private DitheringPass _ditheringPass = null;

    private Material _material = null;
    private Shader _shader = null;
    
    private void OnEnable()
    {
        _shader = Shader.Find("CustomPP/Dither");
    }

    public override void Create()
    {
        _ditheringPass = new DitheringPass();
        _ditheringPass.renderPassEvent = _renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!_material && _shader != null)
            _material = CoreUtils.CreateEngineMaterial(_shader);

        _ditheringPass.Material = _material;

        renderer.EnqueuePass(_ditheringPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }


    [System.Serializable]
    private sealed class DitheringPass : ScriptableRenderPass
    {
        //RenderTargetIdentifier _src;
        //RenderTargetIdentifier _dest;

        private RTHandle _src;
        private RTHandle _dest;

        int _ditherID = Shader.PropertyToID("Dithering_Temp");

        public Material Material { get; set; }

        public DitheringPass()
        {
            //renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            _src = renderingData.cameraData.renderer.cameraColorTargetHandle;
            _dest = RTHandles.Alloc(in desc);


            //cmd.GetTemporaryRT(_ditherID, desc, FilterMode.Point);
            //var idk = new RenderTargetIdentifier(_ditherID);

            //_dest = RTHandles.Alloc(desc.width, desc.height, 1, (DepthBits)desc.depthBufferBits, desc.colorFormat);
            //_dest = RTHandles.Alloc(in desc, _src.rt.filterMode, _src.rt.wrapMode, false, 1, 0, _ditherID.ToString());
            //_dest = RTHandles.Alloc(
            //    desc.width, desc.height, 1,
            //    desc.colorFormat, (deothg)desc.depthBufferBits, desc.dimension,
            //    false, false, false, false,
            //    1, 0, MSAASamples.None, false, false, RenderTextureMemoryless.None,
            //    $"_DitherTemp_{_ditherID}"
            //);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Material == null) return;

            // Dont allow in scene view or preview
            //if (renderingData.cameraData.cameraType == CameraType.SceneView) return;
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;

            CommandBuffer commandBuffer = CommandBufferPool.Get("DitheringCommandBuffer");
            VolumeStack volumes = VolumeManager.instance.stack;

            Dithering ditheringComp = volumes.GetComponent<Dithering>();
            if (ditheringComp != null && ditheringComp.IsActive())
            {
                Material.SetFloat("_Spread", ditheringComp.Spread);
                Material.SetInt("_ColorCount", ditheringComp.ColorCount);
                Material.SetInt("_BayerLevel", ditheringComp.BayerLevel);

                Blit(commandBuffer, _src, _dest, Material, 0);
                Blit(commandBuffer, _dest, _src);
            }

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            _dest?.Release();
        }
    }
}