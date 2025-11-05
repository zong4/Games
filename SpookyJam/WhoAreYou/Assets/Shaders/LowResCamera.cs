using UnityEngine;

namespace Shaders
{
    [ExecuteInEditMode]
    public class LowResCamera : MonoBehaviour
    {
        [Range(0.1f, 1f)] public float resolutionScale = 0.5f;
        public Material postEffect; // 你的滋波特效材质

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            int width = Mathf.RoundToInt(src.width * resolutionScale);
            int height = Mathf.RoundToInt(src.height * resolutionScale);

            // 创建低分辨率的临时渲染纹理
            RenderTexture lowRes = RenderTexture.GetTemporary(width, height, 0, src.format);

            // 把场景渲染成低分辨率
            Graphics.Blit(src, lowRes);

            // 再放大回全屏，并叠加滋波特效
            if (postEffect)
                Graphics.Blit(lowRes, dest, postEffect);
            else
                Graphics.Blit(lowRes, dest);

            RenderTexture.ReleaseTemporary(lowRes);
        }
    }
}