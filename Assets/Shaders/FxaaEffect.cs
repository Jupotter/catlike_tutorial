using System;
using JetBrains.Annotations;
using UnityEngine;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
public class FxaaEffect : MonoBehaviour
{
    public enum LuminanceMode
    {
        Alpha,
        Green,
        Calculate
    }

    private const int luminancePass = 0;
    private const int fxaaPass      = 1;

    public LuminanceMode luminanceSource;

    [Range(0.0312f, 0.0833f)] public float contrastThreshold = 0.0312f;
    [Range(0.063f, 0.333f)]   public float relativeThreshold = 0.063f;
    [Range(0f, 1f)]           public float subpixelBlending  = 1f;


    [HideInInspector] public Shader fxaaShader;

    [NonSerialized] private Material fxaaMaterial;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (fxaaMaterial == null) {
            fxaaMaterial = new Material(fxaaShader) {hideFlags = HideFlags.HideAndDontSave};
        }

        fxaaMaterial.SetFloat("_ContrastThreshold", contrastThreshold);
        fxaaMaterial.SetFloat("_RelativeThreshold", relativeThreshold);
        fxaaMaterial.SetFloat("_SubpixelBlending", subpixelBlending);

        if (luminanceSource == LuminanceMode.Calculate) {
            fxaaMaterial.DisableKeyword("LUMINANCE_GREEN");
            RenderTexture luminanceTex = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            Graphics.Blit(source, luminanceTex, fxaaMaterial, luminancePass);
            Graphics.Blit(luminanceTex, destination, fxaaMaterial, fxaaPass);
            RenderTexture.ReleaseTemporary(luminanceTex);
        } else {
            if (luminanceSource == LuminanceMode.Green) {
                fxaaMaterial.EnableKeyword("LUMINANCE_GREEN");
            } else {
                fxaaMaterial.DisableKeyword("LUMINANCE_GREEN");
            }

            Graphics.Blit(source, destination, fxaaMaterial, fxaaPass);
        }
    }
}
