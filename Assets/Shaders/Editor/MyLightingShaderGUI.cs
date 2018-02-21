﻿using UnityEngine;
using UnityEditor;

public class MyLightingShaderGUI : ShaderGUI
{
    enum SmoothnessSource
    {
        Uniform,
        Albedo,
        Metallic
    }

    static readonly GUIContent           staticLabel    = new GUIContent();
    static readonly ColorPickerHDRConfig emissionConfig = new ColorPickerHDRConfig(0f, 99f, 1f / 99f, 3f);

    Material           target;
    MaterialEditor     editor;
    MaterialProperty[] properties;

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
            foreach (Material m in editor.targets)
            {
                m.EnableKeyword(keyword);
            }
        } else {
            foreach (Material m in editor.targets)
            {
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

    void DoMain()
    {
        GUILayout.Label("Main Maps", EditorStyles.boldLabel);

        MaterialProperty mainTex     = FindProperty("_MainTex");
        MaterialProperty tint        = FindProperty("_Tint");
        GUIContent       albedoLabel = MakeLabel(mainTex, "Albedo (RGB)");

        editor.TexturePropertySingleLine(albedoLabel, mainTex, tint);
        DoMetallic();
        DoSmoothness();
        DoNormals();
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

        if (EditorGUI.EndChangeCheck()) {
            SetKeyword("_EMISSION_MAP", map.textureValue);
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

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        this.target     = editor.target as Material;
        this.editor     = editor;
        this.properties = properties;
        DoMain();
        DoSecondary();
    }
}