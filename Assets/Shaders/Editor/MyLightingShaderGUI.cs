using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class MyLightingShaderGUI : ShaderGUI
{
    enum SmoothnessSource
    {
        Uniform,
        Albedo,
        Metallic
    }

    enum RenderingMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
    }

    struct RenderingSettings
    {
        public RenderQueue queue;
        public string      renderType;
        public BlendMode   srcBlend, dstBlend;
        public bool        zWrite;

        public static readonly RenderingSettings[] modes =
        {
            new RenderingSettings()
            {
                queue      = RenderQueue.Geometry,
                renderType = "",
                srcBlend   = BlendMode.One,
                dstBlend   = BlendMode.Zero,
                zWrite     = true,
            },
            new RenderingSettings()
            {
                queue      = RenderQueue.AlphaTest,
                renderType = "TransparentCutout",
                srcBlend   = BlendMode.One,
                dstBlend   = BlendMode.Zero,
                zWrite     = true,
            },
            new RenderingSettings()
            {
                queue      = RenderQueue.Transparent,
                renderType = "Transparent",
                srcBlend   = BlendMode.SrcAlpha,
                dstBlend   = BlendMode.OneMinusSrcAlpha,
                zWrite     = false,
            },
            new RenderingSettings()
            {
                queue      = RenderQueue.Transparent,
                renderType = "Transparent",
                srcBlend   = BlendMode.One,
                dstBlend   = BlendMode.OneMinusSrcAlpha,
                zWrite     = false
            },
        };
    }

    static readonly GUIContent           staticLabel    = new GUIContent();
    static readonly ColorPickerHDRConfig emissionConfig = new ColorPickerHDRConfig(0f, 99f, 1f / 99f, 3f);

    Material           target;
    MaterialEditor     editor;
    MaterialProperty[] properties;

    bool showAlphaCutoff;

    static GUIContent MakeLabel(string text, string tooltip = null)
    {
        staticLabel.text    = text;
        staticLabel.tooltip = tooltip;

        return staticLabel;
    }

    static GUIContent MakeLabel(MaterialProperty property, string tooltip = null)
    {
        staticLabel.text    = property.displayName;
        staticLabel.tooltip = tooltip;

        return staticLabel;
    }

    void SetKeyword(string keyword, bool state)
    {
        if (state) {
            foreach (Material m in editor.targets) {
                m.EnableKeyword(keyword);
            }
        } else {
            foreach (Material m in editor.targets) {
                m.DisableKeyword(keyword);
            }
        }
    }

    bool IsKeywordEnabled(string keyword)
    {
        return target.IsKeywordEnabled(keyword);
    }

    void RecordAction(string label)
    {
        editor.RegisterPropertyChangeUndo(label);
    }

    MaterialProperty FindProperty(string name)
    {
        return FindProperty(name, properties);
    }

    void DoRenderingMode()
    {
        RenderingMode mode = RenderingMode.Opaque;
        showAlphaCutoff = false;

        if (IsKeywordEnabled("_RENDERING_CUTOUT"))
        {
            mode = RenderingMode.Cutout;
            showAlphaCutoff = true;
        }
        else if (IsKeywordEnabled("_RENDERING_FADE"))
        {
            mode = RenderingMode.Fade;
        }
        else if (IsKeywordEnabled("_RENDERING_TRANSPARENT"))
        {
            mode = RenderingMode.Transparent;
        }

        EditorGUI.BeginChangeCheck();
        mode = (RenderingMode)EditorGUILayout.EnumPopup(MakeLabel("Rendering Mode"), mode);

        if (EditorGUI.EndChangeCheck())
        {
            RecordAction("Rendering Mode");
            SetKeyword("_RENDERING_CUTOUT", mode == RenderingMode.Cutout);
            SetKeyword("_RENDERING_FADE", mode == RenderingMode.Fade);
            SetKeyword("_RENDERING_TRANSPARENT", mode == RenderingMode.Transparent);

            RenderingSettings settings = RenderingSettings.modes[(int)mode];

            foreach (Material m in editor.targets)
            {
                m.renderQueue = (int)settings.queue;
                m.SetOverrideTag("RenderType", settings.renderType);
                m.SetInt("_SrcBlend", (int)settings.srcBlend);
                m.SetInt("_DstBlend", (int)settings.dstBlend);
                m.SetInt("_ZWrite", settings.zWrite ? 1 : 0);
            }
        }

        if (mode == RenderingMode.Fade || mode == RenderingMode.Transparent)
        {
            DoSemitransparentShadows();
        }
    }

    void DoMain()
    {
        GUILayout.Label("Main Maps", EditorStyles.boldLabel);

        MaterialProperty mainTex     = FindProperty("_MainTex");
        MaterialProperty tint        = FindProperty("_Color");
        GUIContent       albedoLabel = MakeLabel(mainTex, "Albedo (RGB)");

        editor.TexturePropertySingleLine(albedoLabel, mainTex, tint);

        if (showAlphaCutoff) {
            DoAlphaCutoff();
        }

        DoMetallic();
        DoSmoothness();
        DoNormals();
        DoParallax();
        DoOcclusion();
        DoEmission();
        DoDetailMask();
        editor.TextureScaleOffsetProperty(mainTex);
    }

    void DoMetallic()
    {
        MaterialProperty map    = FindProperty("_MetallicMap");
        MaterialProperty slider = map.textureValue ? null : FindProperty("_Metallic");

        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(MakeLabel(map, "Metallic (R)"), map, slider);

        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_METALLIC_MAP", map.textureValue);
        }
    }

    void DoSmoothness()
    {
        SmoothnessSource source = SmoothnessSource.Uniform;

        if (IsKeywordEnabled("_SMOOTHNESS_ALBEDO")) {
            source = SmoothnessSource.Albedo;
        } else if (IsKeywordEnabled("_SMOOTHNESS_METALLIC")) {
            source = SmoothnessSource.Metallic;
        }

        MaterialProperty slider = FindProperty("_Smoothness");
        EditorGUI.indentLevel += 2;
        editor.ShaderProperty(slider, MakeLabel(slider));
        EditorGUI.indentLevel += 1;

        EditorGUI.BeginChangeCheck();
        source = (SmoothnessSource) EditorGUILayout.EnumPopup(MakeLabel("Source"), source);

        if (EditorGUI.EndChangeCheck()) {
            RecordAction("Smoothness Source");
            SetKeyword("_SMOOTHNESS_ALBEDO",   source == SmoothnessSource.Albedo);
            SetKeyword("_SMOOTHNESS_METALLIC", source == SmoothnessSource.Metallic);
        }

        EditorGUI.indentLevel -= 3;
    }

    void DoOcclusion()
    {
        MaterialProperty map    = FindProperty("_OcclusionMap");
        MaterialProperty slider = map.textureValue ? FindProperty("_OcclusionStrength") : null;

        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(MakeLabel(map, "Occlusion (G)"), map, slider);


        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_OCCLUSION_MAP", map.textureValue);
        }
    }

    void DoEmission()
    {
        MaterialProperty map = FindProperty("_EmissionMap");

        EditorGUI.BeginChangeCheck();
        GUIContent label = MakeLabel(map, "Emission (RGB)");
        editor.TexturePropertyWithHDRColor(label, map, FindProperty("_Emission"), emissionConfig, false);

        editor.LightmapEmissionProperty(2);

        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_EMISSION_MAP", map.textureValue);

            foreach (Material m in editor.targets)
            {
                m.globalIlluminationFlags &=
                    ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }

    void DoDetailMask()
    {
        MaterialProperty mask = FindProperty("_DetailMask");

        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(MakeLabel(mask, "Detail Mask (A)"), mask);

        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_DETAIL_MASK", mask.textureValue);
        }
    }

    void DoNormals()
    {
        MaterialProperty map       = FindProperty("_NormalMap");
        MaterialProperty bumpScale = map.textureValue ? FindProperty("_BumpScale") : null;

        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(MakeLabel(map), map, bumpScale);

        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_NORMAL_MAP", map.textureValue);
        }
    }

    void DoSecondary()
    {
        GUILayout.Label("Secondary Maps", EditorStyles.boldLabel);

        MaterialProperty detailTex = FindProperty("_DetailTex");

        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(MakeLabel(detailTex, "Albedo (RGB) multiplied by 2"), detailTex);

        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_DETAIL_ALBEDO_MAP", detailTex.textureValue);
        }

        DoSecondaryNormals();
        editor.TextureScaleOffsetProperty(detailTex);
    }

    void DoSecondaryNormals()
    {
        MaterialProperty map    = FindProperty("_DetailNormalMap");
        MaterialProperty slider = map.textureValue ? FindProperty("_DetailBumpScale") : null;

        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(MakeLabel(map), map, slider);

        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_DETAIL_NORMAL_MAP", map.textureValue);
        }
    }

    void DoAlphaCutoff()
    {
        MaterialProperty slider = FindProperty("_Cutoff");
        EditorGUI.indentLevel += 2;
        editor.ShaderProperty(slider, MakeLabel(slider));
        EditorGUI.indentLevel -= 2;
    }

    void DoSemitransparentShadows()
    {
        EditorGUI.BeginChangeCheck();
        bool semitransparentShadows =
            EditorGUILayout.Toggle(
                MakeLabel("Semitransp. Shadows", "Semitransparent Shadows"),
                IsKeywordEnabled("_SEMITRANSPARENT_SHADOWS")
            );
        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword("_SEMITRANSPARENT_SHADOWS", semitransparentShadows);
        }
        if (!semitransparentShadows)
        {
            this.showAlphaCutoff = true;
        }
    }

    void DoParallax()
    {
        MaterialProperty map = FindProperty("_ParallaxMap");
        Texture          tex = map.textureValue;
        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(
            MakeLabel(map, "Parallax (G)"), map,
            tex ? FindProperty("_ParallaxStrength") : null
        );
        if (EditorGUI.EndChangeCheck() && tex != map.textureValue)
        {
            SetKeyword("_PARALLAX_MAP", map.textureValue);
        }
    }

    void DoAdvanced()
    {
        GUILayout.Label("Advanced Options", EditorStyles.boldLabel);

        editor.EnableInstancingField();
    }

    void DoWireframe()
    {
        GUILayout.Label("Wireframe", EditorStyles.boldLabel);
        EditorGUI.indentLevel += 2;
        editor.ShaderProperty(
            FindProperty("_WireframeColor"),
            MakeLabel("Color")
        );
        editor.ShaderProperty(
            FindProperty("_WireframeSmoothing"),
            MakeLabel("Smoothing", "In screen space.")
        );
        editor.ShaderProperty(
            FindProperty("_WireframeThickness"),
            MakeLabel("Thickness", "In screen space.")
        );
        EditorGUI.indentLevel -= 2;
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        this.target     = editor.target as Material;
        this.editor     = editor;
        this.properties = properties;
        DoRenderingMode();

        if (target.HasProperty("_WireframeColor"))
        {
            DoWireframe();
        }

        DoMain();
        DoSecondary();
        DoAdvanced();
    }
}