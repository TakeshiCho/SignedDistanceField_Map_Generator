using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SDF8ssedtCPU
{
    public class SDF8SsedtCPU : MonoBehaviour
    {
        [SerializeField] private Texture2D rawTex;
        [SerializeField] private string savePath;

        [ContextMenu("Generate SDF")]
        void Main()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
            SDFGeneratorCore core = new SDFGeneratorCore();
            Texture2D sdf = core.CreateSDFTex(rawTex);
            var path = $"{savePath}/{rawTex.name}_sdf.png";
            File.WriteAllBytes(path,sdf.EncodeToPNG());
            Debug.Log($"Time : {stopwatch.ElapsedMilliseconds}ms");
            AssetDatabase.Refresh();
        }
    }
}