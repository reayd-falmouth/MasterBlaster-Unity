// Import an AngelCode BMFont (.fnt + .png) into a **Legacy/Standard** Unity Font asset
// so you can use it with the built‑in UI Text / Text (Mesh) components (not TextMesh Pro).
//
// Drop this file anywhere under an `Editor/` folder.
// Menu: Tools → Legacy Text → Import BMFont (.fnt) → Legacy Font
//
// What it does
// - Parses .fnt (text or XML)
// - Creates/updates a Unity `Font` asset (legacy) and fills CharacterInfo[]
// - Creates a Material using a GUI/Text shader and assigns your atlas .png
// - Sets crisp import settings on the atlas (Point, No Mips, Clamp, No Compression)
//
// Notes
// - UVs are normalized and Y is flipped from top‑left BMFont to bottom‑left UVs.
// - Vert rect uses xoffset, yoffset, width, height with negative height (legacy convention).
// - Advance uses xadvance from the .fnt.
// - This is for **Legacy** Text, not TMP.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class BMFont_To_LegacyFont_Importer : EditorWindow
{
    [MenuItem("Tools/Legacy Text/Import BMFont (.fnt) → Legacy Font")]
    public static void ShowWindow() =>
        GetWindow<BMFont_To_LegacyFont_Importer>(false, "BMFont → Legacy Font");

    public TextAsset fntFile; // .fnt (text or XML)
    public Texture2D atlasTexture; // .png atlas
    public Font targetFont; // existing Font asset (optional). If empty, a new Font asset will be created next to the .fnt
    public bool flipY = true; // BMFont y is top‑left → flip to bottom‑left UVs
    public bool createMaterialIfMissing = true;

    void OnGUI()
    {
        GUILayout.Label("BMFont (.fnt + .png) → Legacy Font", EditorStyles.boldLabel);
        fntFile = (TextAsset)
            EditorGUILayout.ObjectField("BMFont .fnt", fntFile, typeof(TextAsset), false);
        atlasTexture = (Texture2D)
            EditorGUILayout.ObjectField(
                "Atlas Texture (.png)",
                atlasTexture,
                typeof(Texture2D),
                false
            );
        targetFont = (Font)
            EditorGUILayout.ObjectField("Target Font (Legacy)", targetFont, typeof(Font), false);
        flipY = EditorGUILayout.ToggleLeft("Flip Y (BMFont top‑left → UV bottom‑left)", flipY);
        createMaterialIfMissing = EditorGUILayout.ToggleLeft(
            "Create/Update Font Material",
            createMaterialIfMissing
        );

        using (new EditorGUI.DisabledScope(fntFile == null || atlasTexture == null))
        {
            if (GUILayout.Button(targetFont ? "Update Selected Font" : "Create New Font"))
            {
                try
                {
                    Import();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    EditorUtility.DisplayDialog("BMFont Importer", e.Message, "OK");
                }
            }
        }

        EditorGUILayout.HelpBox(
            "After import, assign this Font to a standard UI Text component. Keep the Text object's scale at (1,1,1) and scale via Font Size.",
            MessageType.Info
        );
    }

    class BMCommon
    {
        public int lineHeight,
            baseLine,
            scaleW,
            scaleH;
    }

    class BMChar
    {
        public int id,
            x,
            y,
            width,
            height,
            xoffset,
            yoffset,
            xadvance;
    }

    void Import()
    {
        // Parse .fnt
        if (!TryParseFNT(fntFile, out BMCommon common, out List<BMChar> chars))
            throw new Exception(
                "Could not parse .fnt file. Make sure it is an AngelCode BMFont (.fnt) in text or XML format."
            );

        // Ensure target Font asset
        if (!targetFont)
        {
            var fntPath = AssetDatabase.GetAssetPath(fntFile);
            var dir = Path.GetDirectoryName(fntPath);
            var name = Path.GetFileNameWithoutExtension(fntPath);
            var fontPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(dir, name + ".fontsettings")
            );
            targetFont = new Font(name);
            AssetDatabase.CreateAsset(targetFont, fontPath);
        }

        // Fix atlas import settings for crisp pixels
        FixAtlasImportSettings(atlasTexture);

        // Create / update material
        if (createMaterialIfMissing || targetFont.material == null)
        {
            var mat = targetFont.material;
            if (mat == null)
            {
                var shader = Shader.Find("Legacy Shaders/GUI/Text Shader");
                if (shader == null)
                    shader = Shader.Find("GUI/Text Shader");
                if (shader == null)
                    shader = Shader.Find("UI/Default");
                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");

                mat = new Material(shader) { name = targetFont.name + " Font Material" };
                AssetDatabase.AddObjectToAsset(mat, targetFont);
                targetFont.material = mat;
            }
            mat.mainTexture = atlasTexture;
            EditorUtility.SetDirty(mat);
        }

        // Build CharacterInfo[]
        var texW = (float)atlasTexture.width;
        var texH = (float)atlasTexture.height;

        // Deduplicate IDs (some .fnt files repeat ids)
        var byId = new Dictionary<int, BMChar>();
        foreach (var c in chars)
            byId[c.id] = c; // last wins
        var ordered = new List<BMChar>(byId.Values);
        ordered.Sort((a, b) => a.id.CompareTo(b.id));

        var infos = new CharacterInfo[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            var info = new CharacterInfo();
            info.index = c.id;

            // UVs normalized; BMFont y is top‑left → convert if requested
            float u = c.x / texW;
            float v = flipY ? (1f - (c.y + c.height) / texH) : (c.y / texH);
            float w = c.width / texW;
            float h = c.height / texH;
            info.uv = new Rect(u, v, w, h);

            // Vert rect (pixels in font space). Legacy convention: negative height to flip Y.
            // BMFont: xoffset = bearingX; yoffset = distance from top to glyph top.
            // For most BMFonts this mapping works well:
            info.vert = new Rect(c.xoffset, -c.yoffset, c.width, -c.height);
            info.advance = c.xadvance;
            info.flipped = false;

            infos[i] = info;
        }

        // Apply to font
        targetFont.characterInfo = infos; // legacy/editor API

        // Set line spacing on the asset (serialized field)
        var so = new SerializedObject(targetFont);

        // set the native size to your bitmap font's pixel height
        so.FindProperty("m_FontSize").intValue = Mathf.Max(1, common.lineHeight); // ← 7 for your atlas
        so.FindProperty("m_LineSpacing").floatValue = Mathf.Max(1, common.lineHeight); // keep consistent

        so.ApplyModifiedProperties();

        var lineProp = so.FindProperty("m_LineSpacing");
        if (lineProp != null)
            lineProp.floatValue = Mathf.Max(1, common.lineHeight);
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(targetFont);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "BMFont Importer",
            $"Imported {infos.Length} glyphs into '{targetFont.name}'.\nAssign this Font to a standard UI Text component.",
            "OK"
        );
        Selection.activeObject = targetFont;
    }

    static void FixAtlasImportSettings(Texture2D tex)
    {
        var path = AssetDatabase.GetAssetPath(tex);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null)
            return;
        ti.textureType = TextureImporterType.Default;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Point;
        ti.sRGBTexture = true;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
    }

    // --------- .fnt parser (text or XML) ---------
    static bool TryParseFNT(TextAsset fnt, out BMCommon common, out List<BMChar> chars)
    {
        common = new BMCommon();
        chars = new List<BMChar>();
        var txt = fnt.text.TrimStart();
        if (txt.StartsWith("<"))
            return ParseXML(txt, out common, out chars);
        return ParseText(txt, out common, out chars);
    }

    static bool ParseXML(string xml, out BMCommon common, out List<BMChar> chars)
    {
        common = new BMCommon();
        chars = new List<BMChar>();
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var n = doc.SelectSingleNode("/font/common");
            common.lineHeight = int.Parse(
                n.Attributes["lineHeight"].Value,
                CultureInfo.InvariantCulture
            );
            common.baseLine = int.Parse(n.Attributes["base"].Value, CultureInfo.InvariantCulture);
            common.scaleW = int.Parse(n.Attributes["scaleW"].Value, CultureInfo.InvariantCulture);
            common.scaleH = int.Parse(n.Attributes["scaleH"].Value, CultureInfo.InvariantCulture);
            foreach (XmlNode ch in doc.SelectNodes("/font/chars/char"))
            {
                chars.Add(
                    new BMChar
                    {
                        id = int.Parse(ch.Attributes["id"].Value),
                        x = int.Parse(ch.Attributes["x"].Value),
                        y = int.Parse(ch.Attributes["y"].Value),
                        width = int.Parse(ch.Attributes["width"].Value),
                        height = int.Parse(ch.Attributes["height"].Value),
                        xoffset = int.Parse(ch.Attributes["xoffset"].Value),
                        yoffset = int.Parse(ch.Attributes["yoffset"].Value),
                        xadvance = int.Parse(ch.Attributes["xadvance"].Value)
                    }
                );
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("BMFont XML parse error: " + e.Message);
            return false;
        }
    }

    static bool ParseText(string text, out BMCommon common, out List<BMChar> chars)
    {
        common = new BMCommon();
        chars = new List<BMChar>();
        try
        {
            using var sr = new StringReader(text);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("common "))
                {
                    foreach (var part in line.Split(' '))
                    {
                        var kv = part.Split('=');
                        if (kv.Length != 2)
                            continue;
                        if (kv[0] == "lineHeight")
                            common.lineHeight = int.Parse(kv[1]);
                        if (kv[0] == "base")
                            common.baseLine = int.Parse(kv[1]);
                        if (kv[0] == "scaleW")
                            common.scaleW = int.Parse(kv[1]);
                        if (kv[0] == "scaleH")
                            common.scaleH = int.Parse(kv[1]);
                    }
                }
                else if (line.StartsWith("char "))
                {
                    var ch = new BMChar();
                    foreach (var part in line.Split(' '))
                    {
                        var kv = part.Split('=');
                        if (kv.Length != 2)
                            continue;
                        switch (kv[0])
                        {
                            case "id":
                                ch.id = int.Parse(kv[1]);
                                break;
                            case "x":
                                ch.x = int.Parse(kv[1]);
                                break;
                            case "y":
                                ch.y = int.Parse(kv[1]);
                                break;
                            case "width":
                                ch.width = int.Parse(kv[1]);
                                break;
                            case "height":
                                ch.height = int.Parse(kv[1]);
                                break;
                            case "xoffset":
                                ch.xoffset = int.Parse(kv[1]);
                                break;
                            case "yoffset":
                                ch.yoffset = int.Parse(kv[1]);
                                break;
                            case "xadvance":
                                ch.xadvance = int.Parse(kv[1]);
                                break;
                        }
                    }
                    chars.Add(ch);
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("BMFont text parse error: " + e.Message);
            return false;
        }
    }
}
