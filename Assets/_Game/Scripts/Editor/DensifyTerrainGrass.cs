using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Makes the terrain's painted grass taller and denser everywhere it's already painted - "long
/// grass, close together" instead of short and sparse. Only touches cells that already have
/// grass (never paints new areas), so it respects whatever was hand-painted.
///
/// Checks TerrainData.detailScatterMode first: the classic GetDetailLayer/SetDetailLayer API
/// this tool uses is only valid in InstanceCountMode. If this terrain uses the newer
/// CoverageMode instead, density painting works completely differently there and this tool
/// deliberately does nothing to density (silently guessing wrong could corrupt the paint data)
/// - it still applies the height/width bump either way, since that part is unambiguous, and
/// tells you to use the Terrain's own Paint Details brush for density in that case.
///
/// Run:  Tools ▸ Medieval Survival ▸ Densify Terrain Grass     (then save the scene)
/// Re-running this again compounds the effect - use Ctrl+Z to undo if you overshoot rather than
/// running it repeatedly.
/// </summary>
public static class DensifyTerrainGrass
{
    private const float HeightMultiplier = 1.8f;
    private const float WidthMultiplier = 1.3f;
    private const float DensityMultiplier = 1.8f;
    private const int MaxDensityPerCell = 24;

    [MenuItem("Tools/Medieval Survival/Densify Terrain Grass")]
    public static void Run()
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Densify Terrain Grass", "No Terrain found in the open scene.", "OK");
            return;
        }

        TerrainData data = terrain.terrainData;
        DetailPrototype[] protos = data.detailPrototypes;
        if (protos == null || protos.Length == 0)
        {
            EditorUtility.DisplayDialog("Densify Terrain Grass", "This terrain has no grass/detail prototypes painted.", "OK");
            return;
        }

        for (int i = 0; i < protos.Length; i++)
        {
            protos[i].minHeight *= HeightMultiplier;
            protos[i].maxHeight *= HeightMultiplier;
            protos[i].minWidth *= WidthMultiplier;
            protos[i].maxWidth *= WidthMultiplier;
        }
        data.detailPrototypes = protos;

        bool densityApplied = false;
        long totalBefore = 0, totalAfter = 0;
        int paintedLayers = 0;

        if (data.detailScatterMode == DetailScatterMode.InstanceCountMode)
        {
            int w = data.detailWidth;
            int h = data.detailHeight;

            for (int layer = 0; layer < protos.Length; layer++)
            {
                int[,] map = data.GetDetailLayer(0, 0, w, h, layer);
                bool anyPainted = false;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int v = map[y, x];
                        if (v <= 0) continue;
                        anyPainted = true;
                        totalBefore += v;
                        int boosted = Mathf.Clamp(Mathf.RoundToInt(v * DensityMultiplier), v + 1, MaxDensityPerCell);
                        map[y, x] = boosted;
                        totalAfter += boosted;
                    }
                }

                if (anyPainted)
                {
                    data.SetDetailLayer(0, 0, layer, map);
                    paintedLayers++;
                }
            }

            densityApplied = true;
        }

        terrain.Flush();
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string message = $"Made {protos.Length} grass type(s) {HeightMultiplier:0.0}x taller, {WidthMultiplier:0.0}x wider.\n\n";

        if (densityApplied)
        {
            message += $"Boosted density on {paintedLayers} painted layer(s): total instances " +
                       $"{totalBefore:N0} -> {totalAfter:N0}.\n\n" +
                       "Re-running this again compounds the effect - use Ctrl+Z if you overshoot rather than running it repeatedly.";
        }
        else
        {
            message += $"This terrain uses detailScatterMode = {data.detailScatterMode}, not InstanceCountMode - " +
                       "the density API this tool uses doesn't apply there, so density wasn't touched (only height/width). " +
                       "Use the Terrain component's own Paint Details brush to increase density for this mode.";
        }

        EditorUtility.DisplayDialog("Densify Terrain Grass", message + "\n\nSave the scene to keep this.", "OK");
    }
}
