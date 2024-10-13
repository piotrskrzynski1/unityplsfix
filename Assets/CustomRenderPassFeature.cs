using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering;
using Unity.VisualScripting;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material raytraceMaterial;
        private Material averageMaterial;
        private RenderTexture currRender;
        private RenderTexture prevRender;
        private RenderTexture resultTexture;
     
        public bool Accumulate = true;
        public bool first = true;
        public int numRenderedFrames = 1;

        public bool First;
        Settings SX;

        public CustomRenderPass(Settings s)
        {
            this.raytraceMaterial = s.raytraceMaterial;
            this.averageMaterial =  s.averageMaterial;
            this.SX = s;
            this.First = true;
        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (resultTexture == null)
            {
                RenderTextureDescriptor cam = renderingData.cameraData.cameraTargetDescriptor;
                resultTexture = new RenderTexture(cam.width,cam.height,0);
                resultTexture.graphicsFormat = cam.graphicsFormat;
                resultTexture.enableRandomWrite = true;
                resultTexture.Create();

                resultTexture.wrapMode = TextureWrapMode.Clamp;
                resultTexture.filterMode = FilterMode.Trilinear;
            }
            ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
            UpdateCameraParams(renderingData.cameraData.camera);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingSample(cmd, "MultiPass"))
            {
                UpdateCameraParams(renderingData.cameraData.camera);

                RenderTextureDescriptor cam = renderingData.cameraData.cameraTargetDescriptor;
                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                RenderTexture prevFrameCopy = RenderTexture.GetTemporary(cam.width, cam.height, 0, cam.graphicsFormat);
                RenderTargetIdentifier prevFrameCopyId = prevFrameCopy;
                if (First)
                {
                    cmd.Blit(null, prevFrameCopyId, raytraceMaterial, -1);
                    numRenderedFrames++;
                    averageMaterial.SetInt("NumRenderedFrames", numRenderedFrames);
                    raytraceMaterial.SetInt("NumRenderedFrames", numRenderedFrames);
                    First = false;
                }
                else
                {
                    cmd.Blit(resultTexture, prevFrameCopy);
                }
                


                RenderTexture currentFrame = RenderTexture.GetTemporary(cam.width, cam.height, 0, cam.graphicsFormat);
                UpdateCameraParams(renderingData.cameraData.camera);
                RenderTargetIdentifier currFrameIdentifier = currentFrame;
                cmd.Blit(null, currFrameIdentifier, raytraceMaterial, -1);
                numRenderedFrames++;

                averageMaterial.SetInt("NumRenderedFrames", numRenderedFrames);
                raytraceMaterial.SetInt("NumRenderedFrames", numRenderedFrames);

                averageMaterial.SetTexture("_PrevRender", prevFrameCopy);
                averageMaterial.SetTexture("_CurrRender", currentFrame);

                RenderTargetIdentifier renderTargetIdentifier = resultTexture;
                cmd.Blit(currentFrame, renderTargetIdentifier, averageMaterial, -1);

                cmd.Blit(resultTexture, source);

                RenderTexture.ReleaseTemporary(prevFrameCopy);
                RenderTexture.ReleaseTemporary(currentFrame);
            }
            context.ExecuteCommandBuffer(cmd);
 
            

            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
           
        }
        void UpdateCameraParams(Camera cam)
        {
            float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
            float planeWidth = planeHeight * cam.aspect;

            raytraceMaterial.SetVector("_ViewParams", new Vector3(planeWidth, planeHeight, cam.nearClipPlane));
            raytraceMaterial.SetMatrix("_CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
            raytraceMaterial.SetVector("_Color", new Vector4(0f, 1f, 0f, 1f));
            raytraceMaterial.SetInt("MaxBounceCount", SX.maxBounceRay);
            raytraceMaterial.SetInt("NumRaysPerPixel", SX.numRaysPerPixel);
            raytraceMaterial.SetVector("GroundColour", SX.GroundColour);
            raytraceMaterial.SetVector("SkyColourHorizon", SX.SkyColourHorizon);
            raytraceMaterial.SetVector("SkyColourZenith", SX.SkyColourZenith);
            raytraceMaterial.SetFloat("SunFocus", SX.SunFocus);
            raytraceMaterial.SetFloat("SunIntensity", SX.SunIntensity);

        }
    }


    CustomRenderPass m_ScriptablePass;

    // The materials for the ray tracing and averaging

    [System.Serializable]
    public class Settings
    {
        public Material raytraceMaterial;
        public Material averageMaterial;
        public bool Accumulate = true;

        public int maxBounceRay = 1;
        public int numRaysPerPixel = 1;
        public int numRenderedFrames = 1;
        public float weight = 0.4f;

        public Vector4 GroundColour;
        public Vector4 SkyColourHorizon;
        public Vector4 SkyColourZenith;
        public float SunFocus;
        public float SunIntensity;
    }
    public Settings settings;
    public override void Create()
    {
        // Initialize the custom render pass
        m_ScriptablePass = new CustomRenderPass(settings)
        {
            Accumulate = settings.Accumulate,
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Enqueue the custom render pass
        renderer.EnqueuePass(m_ScriptablePass);
    }
}