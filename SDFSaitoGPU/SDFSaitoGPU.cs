using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Debug = UnityEngine.Debug;

namespace SDFSaitoGPU
{
    [ExecuteAlways]
    public class SDFSaitoGPU : MonoBehaviour
    {
        public ComputeShader SDFShader;
        public List<Texture2D> RawTex = new List<Texture2D>();
        [SerializeField]private List<RenderTexture> _sdfTex = new List<RenderTexture>();
        private List<RenderTexture> _rowDists = new List<RenderTexture>();
        private List<ComputeBuffer> _buffers = new List<ComputeBuffer>();
        uint[] _step = {0};

        [ContextMenu("Execute")]
        void Execute()
        {
            CalculateSDF(RawTex);
        }
        
        void CalculateSDF(List<Texture2D> rawTexs)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
            var stepBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
            stepBuffer.SetData(_step);
            _buffers.Add(stepBuffer);
            var shader = SDFShader;
            
            foreach (var rawTex in rawTexs)
            {
                if (rawTex.width > 2048 || rawTex.height > 2048)
                {
                    Debug.LogWarning("Texture Size is larger than 2048!");
                    return;
                }
                
                RenderSingleSDFTexture(rawTex, shader, stepBuffer);
            }
            
            stepBuffer.GetData(_step);
            Debug.Log($"Time : {stopwatch.ElapsedMilliseconds}ms");
        }

        private void RenderSingleSDFTexture(Texture2D rawTex, ComputeShader shader, ComputeBuffer stepBuffer)
        {
            RenderTexture rowDist = CreatRenderTexture(rawTex, GraphicsFormat.R32G32_SFloat);
            _rowDists.Add(rowDist);

            RenderTexture sdf = CreatRenderTexture(rawTex, GraphicsFormat.R8_UNorm);
            _sdfTex.Add(sdf);
            
            CalculateDistancePerRows(shader, rawTex, rowDist, stepBuffer);
            CalculateDistance(shader, rawTex, rowDist, sdf, stepBuffer);
        }

        RenderTexture CreatRenderTexture(Texture2D rawTex, GraphicsFormat format)
        {
            var descriptor = new RenderTextureDescriptor(rawTex.width, rawTex.height,format,0); 
            RenderTexture rt = new RenderTexture(descriptor);              
            rt.enableRandomWrite = true;                       
            rt.Create();
            return rt;
        }

        void CalculateDistancePerRows(ComputeShader shader, Texture2D rawTex ,RenderTexture rowDist,  ComputeBuffer stepBuffer)
        {
            int process = shader.FindKernel("Process0");
            shader.SetVector("_TexSize",new Vector2(rawTex.width,rawTex.height));
            shader.SetTexture(process, "DataProcess0", rowDist);
            shader.SetTexture(process, "Raw", rawTex);
            shader.Dispatch(process, 1,  rawTex.height /4, 1);
        }
        void CalculateDistance(ComputeShader shader, Texture2D rawTex, RenderTexture rowDist, RenderTexture sdf ,ComputeBuffer stepBuffer)
        {
            int process = shader.FindKernel("Process1");
            // shader.SetBuffer(kernelCalculateSDF,"StepBuffer",stepBuffer);
            shader.SetTexture(process, "DataProcess0", rowDist);
            shader.SetTexture(process, "Result", sdf);
            shader.Dispatch(process, rawTex.width/32 , rawTex.height/32 , 1);
        }

        [ContextMenu("Clear")]
        private void Clear()
        {
            foreach (var _rt in _sdfTex)
            {
                _rt.Release();
            }
            
            foreach (var row in _rowDists)
            {
                row.Release();
            }

            _sdfTex = new List<RenderTexture>();
            _rowDists = new List<RenderTexture>();

            foreach (var buffer in _buffers)
            {
                buffer.Release();
            }
        }

        private void OnDisable()
        {
            Clear();
        }
    }
}