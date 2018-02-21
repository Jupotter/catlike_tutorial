using UnityEngine;
using UnityEditor;

public class MyLightingShaderGUI : ShaderGUI
{
    static GUIContent staticLabel = new GUIContent();

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
        editor.TextureScaleOffsetProperty(mainTex);
    }

    void DoMetallic()
    {
        MaterialProperty slider = FindProperty("_Metallic");
        EditorGUI.indentLevel += 2;
        editor.ShaderProperty(slider, MakeLabel(slider));
        EditorGUI.indentLevel -= 2;
    }

    void DoSmoothness()
    {
        MaterialProperty slider = FindProperty("_Smoothness");
        EditorGUI.indentLevel += 2;
        editor.ShaderProperty(slider, MakeLabel(slider));
        EditorGUI.indentLevel -= 2;
    }

    void DoNormals()
    {
        MaterialProperty map       = FindProperty("_NormalMap");
        MaterialProperty bumpScale = map.textureValue ? FindProperty("_BumpScale") : null;
        editor.TexturePropertySingleLine(MakeLabel(map), map, bumpScale);
    }

    void DoSecondary()
    {
        GUILayout.Label("Secondary Maps", EditorStyles.boldLabel);

        MaterialProperty detailTex = FindProperty("_DetailTex");
        editor.TexturePropertySingleLine(MakeLabel(detailTex, "Albedo (RGB) multiplied by 2"), detailTex);
        DoSecondaryNormals();
        editor.TextureScaleOffsetProperty(detailTex);

    }

    void DoSecondaryNormals()
    {
        MaterialProperty map = FindProperty("_DetailNormalMap");
        editor.TexturePropertySingleLine(
            MakeLabel(map), map,
            map.textureValue ? FindProperty("_DetailBumpScale") : null
        );
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        this.editor     = editor;
        this.properties = properties;
        DoMain();
        DoSecondary();
    }
}