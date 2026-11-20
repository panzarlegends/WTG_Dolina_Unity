using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Lekcja 4: realistyczna scena „Polana” od zera — własny teren, las grabowy, trawa i kwiaty,
// niskie słońce, niebo, mgła i efekty obrazu (Volume), brama przejścia do gry.
public static class DolinaPolana
{
    const string Folder = "Assets/Dolina/Polana";
    const float R = 160f, H = 30f;                 // rozmiar i wysokość terenu (m)
    const int N = 257;                             // rozdzielczość mapy wysokości

    static float Wysokosc(float x, float z)        // x, z w metrach od środka polany
    {
        float d = Mathf.Sqrt(x * x + z * z);
        float wzgorza = Mathf.SmoothStep(0, 1, (d - 38f) / 42f) * 14f;               // polana płaska do ~38 m, potem zbocza
        float fale = (Mathf.PerlinNoise(x * 0.025f + 3, z * 0.025f + 7) - 0.5f) * 3f;
        float drobne = (Mathf.PerlinNoise(x * 0.12f, z * 0.12f) - 0.5f) * 0.35f;
        return 3f + wzgorza + fale + drobne;
    }

    [MenuItem("Dolina/9. Scena Polana od zera (teren, las, światło, niebo)")]
    public static void Zbuduj()
    {
        EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var scena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- teren ---
        // zasób terenu najpierw zapisany na dysku, potem wszystkie zmiany na nim
        AssetDatabase.DeleteAsset(Folder + "/Polana_Teren.asset");
        var nowy = new TerrainData(); nowy.heightmapResolution = N; nowy.alphamapResolution = 512; nowy.size = new Vector3(R, H, R);
        AssetDatabase.CreateAsset(nowy, Folder + "/Polana_Teren.asset"); AssetDatabase.SaveAssets();
        var td = AssetDatabase.LoadAssetAtPath<TerrainData>(Folder + "/Polana_Teren.asset");
        var h = new float[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                h[y, x] = Wysokosc(x / (N - 1f) * R - R / 2, y / (N - 1f) * R - R / 2) / H;
        td.SetHeights(0, 0, h);
        string wl = "Assets/TerrainSampleAssets/TerrainLayers/";
        td.terrainLayers = new[] { "Grass_A", "Grass_Moss", "Grass_Soil", "Rock" }
            .Select(n => AssetDatabase.LoadAssetAtPath<TerrainLayer>(wl + n + "_TerrainLayer.terrainlayer")).ToArray();
        var terenGO = Terrain.CreateTerrainGameObject(td); terenGO.name = "Polana";
        terenGO.transform.position = new Vector3(-R / 2, 0, -R / 2);
        var t = terenGO.GetComponent<Terrain>();
        t.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat");
        t.drawInstanced = true; t.heightmapPixelError = 3;

        int A = td.alphamapResolution; var w = new float[A, A, 4];
        for (int y = 0; y < A; y++) for (int x = 0; x < A; x++)
            {
                float u = (x + 0.5f) / A, v = (y + 0.5f) / A, wx = u * R - R / 2, wz = v * R - R / 2;
                float d = Mathf.Sqrt(wx * wx + wz * wz), nach = td.GetSteepness(u, v);
                float skala = Mathf.SmoothStep(0, 1, (nach - 24) / 10);
                float sciezka = Mathf.Clamp01(1 - Mathf.Abs(wz - 4f * Mathf.Sin(wx * 0.06f)) / 1.4f) * (wx > -60 && wx < 30 ? 1 : 0);
                float mech = Mathf.Clamp01((d - 34) / 12 + (Mathf.PerlinNoise(wx * 0.08f, wz * 0.08f) - 0.5f));
                sciezka *= 1 - skala; mech *= (1 - skala) * (1 - sciezka);
                w[y, x, 3] = skala; w[y, x, 2] = sciezka; w[y, x, 1] = mech; w[y, x, 0] = Mathf.Max(0, 1 - skala - sciezka - mech);
            }
        td.SetAlphamaps(0, 0, w);

        // --- drzewa: gęsty las wokół polany, kilka samotnych grabów na łące ---
        string pd = "Assets/Dolina/Drzewa/Prefaby/";
        var nazwy = new[] { "Grab_Forest_04", "Grab_Forest_05", "Grab_Forest_06", "Grab_Forest_09", "Grab_Field_01", "Grab_Field_03", "Grab_Sapling_01", "Grab_Sapling_03" };
        td.treePrototypes = nazwy.Select(n => new TreePrototype { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pd + n + ".prefab") }).ToArray();
        var los = new System.Random(21); var drzewa = new List<TreeInstance>();
        float L() => (float)los.NextDouble();
        void Posadz(float wx, float wz, int proto, float sk)
        {
            float u = (wx + R / 2) / R, v = (wz + R / 2) / R;
            if (u < 0.02f || u > 0.98f || v < 0.02f || v > 0.98f) return;
            drzewa.Add(new TreeInstance { prototypeIndex = proto, position = new Vector3(u, td.GetInterpolatedHeight(u, v) / H - 0.003f, v),
                rotation = L() * Mathf.PI * 2, widthScale = sk, heightScale = sk, color = Color.white, lightmapColor = Color.white });
        }
        for (int i = 0; i < 2600 && drzewa.Count < 700; i++)
        {
            float a = L() * Mathf.PI * 2, d = 40f + L() * 45f;
            float wx = Mathf.Cos(a) * d, wz = Mathf.Sin(a) * d;
            if (Mathf.Abs(wz - 4f * Mathf.Sin(wx * 0.06f)) < 3f && wx < 30) continue;          // wolna ścieżka
            Posadz(wx, wz, d < 48 && L() < 0.35f ? 6 + los.Next(2) : los.Next(4), 0.85f + L() * 0.45f);
        }
        foreach (var p in new[] { new Vector2(-12, 18), new Vector2(16, -14), new Vector2(22, 20) }) Posadz(p.x, p.y, 4 + los.Next(2), 1.1f);
        td.SetTreeInstances(drzewa.ToArray(), true);
        t.treeDistance = 300; t.treeBillboardDistance = 300; t.treeMaximumFullLODCount = 1000;

        // --- trawa, paprocie, kwiaty ---
        string pk = "Assets/TerrainSampleAssets/Prefabs/";
        var det = new[] { "Grass_A", "Grass_B", "Grass_C", "Grass_D", "Fern_A", "Fern_B", "Heather_A", "Plant_B", "Plant_C" };
        td.SetDetailResolution(512, 32);
        td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
        td.detailPrototypes = det.Select(n => new DetailPrototype {
            prototype = AssetDatabase.LoadAssetAtPath<GameObject>(pk + n + ".prefab"), usePrototypeMesh = true, useInstancing = true,
            renderMode = DetailRenderMode.VertexLit, minWidth = 0.8f, maxWidth = 1.35f, minHeight = 0.8f, maxHeight = 1.4f,
            noiseSpread = 0.4f, useDensityScaling = true, alignToGround = 0.7f }).ToArray();
        int D = td.detailResolution; var mapy = det.Select(_ => new int[D, D]).ToArray();
        for (int y = 0; y < D; y++) for (int x = 0; x < D; x++)
            {
                float u = (x + 0.5f) / D, v = (y + 0.5f) / D, wx = u * R - R / 2, wz = v * R - R / 2;
                float d = Mathf.Sqrt(wx * wx + wz * wz), nach = td.GetSteepness(u, v);
                bool sciezka = Mathf.Abs(wz - 4f * Mathf.Sin(wx * 0.06f)) < 1.3f && wx > -60 && wx < 30;
                if (nach > 28 || sciezka) continue;
                float kwiaty = Mathf.PerlinNoise(wx * 0.07f + 11, wz * 0.07f), drob = Mathf.PerlinNoise(wx * 0.4f, wz * 0.4f);
                mapy[(x + 2 * y) % 4][y, x] = d < 42 ? (drob > 0.3f ? 2 : 1) : 1;
                if (d < 40 && kwiaty > 0.58f) mapy[6 + (x + y) % 3][y, x] = drob > 0.45f ? 1 : 0;
                if (d > 36 && drob > 0.55f) mapy[4 + (x % 2)][y, x] = 1;
            }
        for (int i = 0; i < det.Length; i++) td.SetDetailLayer(0, 0, i, mapy[i]);
        t.detailObjectDistance = 90; t.detailObjectDensity = 1f;

        // --- światło, niebo, mgła ---
        var slonceGO = new GameObject("Slonce", typeof(Light));
        var slonce = slonceGO.GetComponent<Light>(); slonce.type = LightType.Directional;
        slonce.color = new Color(1f, 0.84f, 0.64f); slonce.intensity = 2.2f; slonce.shadows = LightShadows.Soft;
        slonceGO.transform.rotation = Quaternion.Euler(17, 120, 0);
        var niebo = new Material(Shader.Find("Skybox/Procedural"));
        niebo.SetFloat("_SunSize", 0.035f); niebo.SetFloat("_AtmosphereThickness", 1.15f);
        niebo.SetColor("_SkyTint", new Color(0.52f, 0.58f, 0.68f)); niebo.SetFloat("_Exposure", 1.25f);
        AssetDatabase.DeleteAsset(Folder + "/M_Niebo.mat"); AssetDatabase.CreateAsset(niebo, Folder + "/M_Niebo.mat");
        RenderSettings.skybox = niebo; RenderSettings.sun = slonce;
        RenderSettings.ambientMode = AmbientMode.Skybox; RenderSettings.ambientIntensity = 1.05f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.74f, 0.72f, 0.66f); RenderSettings.fogDensity = 0.0095f;

        // --- efekty obrazu (URP Volume) ---
        var profil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.DeleteAsset(Folder + "/Polana_Efekty.asset"); AssetDatabase.CreateAsset(profil, Folder + "/Polana_Efekty.asset");
        T Dodaj<T>() where T : VolumeComponent { var c = profil.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, profil); return c; }
        Dodaj<Tonemapping>().mode.value = TonemappingMode.ACES;
        var bloom = Dodaj<Bloom>(); bloom.threshold.value = 1f; bloom.intensity.value = 0.45f; bloom.scatter.value = 0.65f;
        var kol = Dodaj<ColorAdjustments>(); kol.postExposure.value = 0.35f; kol.contrast.value = 14f; kol.saturation.value = 6f;
        var bal = Dodaj<WhiteBalance>(); bal.temperature.value = 9f;
        var vin = Dodaj<Vignette>(); vin.intensity.value = 0.24f; vin.smoothness.value = 0.45f;
        var dof = Dodaj<DepthOfField>(); dof.mode.value = DepthOfFieldMode.Gaussian; dof.gaussianStart.value = 60f; dof.gaussianEnd.value = 160f; dof.gaussianMaxRadius.value = 1f;
        EditorUtility.SetDirty(profil);
        var vol = new GameObject("Efekty_Obrazu", typeof(Volume)).GetComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = profil;

        // --- gracz i kamera ---
        const string sa = "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/";
        GameObject Wstaw(string p) => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sa + p));
        var gracz = Wstaw("PlayerArmature.prefab");
        float sx = -34f, sz = 4f * Mathf.Sin(sx * 0.06f);
        gracz.transform.position = new Vector3(sx, t.SampleHeight(new Vector3(sx, 0, sz)) + 0.1f, sz);
        gracz.transform.rotation = Quaternion.LookRotation(Vector3.right);
        var kam = Wstaw("MainCamera.prefab");
        var dane = kam.GetComponent<Camera>().GetUniversalAdditionalCameraData(); dane.renderPostProcessing = true; dane.antialiasing = AntialiasingMode.TemporalAntiAliasing;
        var sledz = Wstaw("PlayerFollowCamera.prefab");
        foreach (var k in sledz.GetComponents<MonoBehaviour>())
        {
            var so = new SerializedObject(k); var pr = so.FindProperty("Target.TrackingTarget");
            if (pr != null) { pr.objectReferenceValue = gracz.transform.Find("PlayerCameraRoot"); so.ApplyModifiedProperties(); }
        }

        // --- brama do gry po drugiej stronie polany ---
        var brama = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dolina/Gra/Prefaby/Meta.prefab"));
        PrefabUtility.UnpackPrefabInstance(brama, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        brama.name = "Brama_do_gry";
        Object.DestroyImmediate(brama.GetComponent<Meta>()); brama.AddComponent<BramaPolany>();
        float bx = 26f, bz = 4f * Mathf.Sin(bx * 0.06f);
        brama.transform.SetPositionAndRotation(new Vector3(bx, t.SampleHeight(new Vector3(bx, 0, bz)) - 0.1f, bz), Quaternion.Euler(0, 90, 0));

        EditorUtility.SetDirty(td); AssetDatabase.SaveAssets();          // zapis tekstur, drzew i trawy w zasobie terenu
        EditorSceneManager.SaveScene(scena, "Assets/Scenes/Polana.unity");
        var sceny = EditorBuildSettings.scenes.Where(s => !s.path.EndsWith("Polana.unity")).ToList();
        sceny.Add(new EditorBuildSettingsScene("Assets/Scenes/Polana.unity", true)); EditorBuildSettings.scenes = sceny.ToArray();
        Selection.activeGameObject = terenGO;
        Debug.Log($"Polana gotowa: {td.treeInstanceCount} drzew, {det.Length} rodzajów roślin. Sceny: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
    }
}
