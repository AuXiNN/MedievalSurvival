using UnityEditor;
using UnityEngine;

/// <summary>
/// The WebGL build was ~420MB (over itch.io's 200MB per-file cap) because every texture falls
/// back to the Default/Standalone 4096 max size with no WebGL-specific override - textures alone
/// were 79% of the build (582MB uncompressed). This caps texture size for the WebGL platform only;
/// Mac/Windows builds are untouched since their own platform overrides are set separately.
/// </summary>
public static class WebGLTextureOptimizer
{
    private const int MaxWebGLTextureSize = 1024;

    [MenuItem("Tools/Optimize Textures For WebGL")]
    public static void OptimizeTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        int changed = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("Optimizing textures for WebGL", path, (float)i / guids.Length);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            TextureImporterPlatformSettings webglSettings = importer.GetPlatformTextureSettings("WebGL");

            if (webglSettings.overridden && webglSettings.maxTextureSize <= MaxWebGLTextureSize)
                continue;

            webglSettings.overridden = true;
            webglSettings.maxTextureSize = MaxWebGLTextureSize;
            webglSettings.format = TextureImporterFormat.Automatic;
            webglSettings.textureCompression = TextureImporterCompression.Compressed;

            importer.SetPlatformTextureSettings(webglSettings);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            changed++;
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"WebGLTextureOptimizer: added/updated WebGL override on {changed} of {guids.Length} textures (max size {MaxWebGLTextureSize}).");
    }
}
