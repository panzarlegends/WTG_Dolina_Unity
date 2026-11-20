using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Narzędzia edytora do budowy doliny (Unity 6.6). Menu: Dolina/...
// Dane w Assets/Dolina/Dane są te same co w wersji Unreal (centymetry Unreala: X, Y, Z).
// Przeliczenie na Unity: x = X/100, y = Z/100, z = Y/100 (metry).
public static class DolinaNarzedzia
{
    const string Drzewa = "Assets/Dolina/Drzewa";
    const string Dane = "Assets/Dolina/Dane";

    // ---------- 1. Drzewa grabowe z Fab: materiały URP i prefaby ----------
    [MenuItem("Dolina/1. Drzewa z Fab - materiały i prefaby")]
    public static void PrzygotujDrzewa()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        Material Mat(string nazwa, string zestaw, bool liscie)
        {
            string sciezka = $"{Drzewa}/Materialy/{nazwa}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(sciezka);
            if (!m) { m = new Material(lit); AssetDatabase.CreateAsset(m, sciezka); }
            m.SetTexture("_BaseMap", Tekstura($"Grab_{zestaw}_BaseMap", false, liscie));
            m.SetTexture("_MetallicGlossMap", Tekstura($"Grab_{zestaw}_MetalGloss", true, false));
            m.SetTexture("_BumpMap", Tekstura($"Grab_{zestaw}_Normal", true, false, true));
            m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", 1f);
            if (liscie)
            {
                m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0); m.doubleSidedGI = true;          // liście widoczne z obu stron
                m.SetColor("_BaseColor", new Color(0.95f, 1f, 0.9f));
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }
        Directory.CreateDirectory($"{Drzewa}/Materialy"); Directory.CreateDirectory($"{Drzewa}/Prefaby");
        AssetDatabase.Refresh();
        var pien = Mat("M_Grab_Pien", "Trunk_01", false);
        var mapa = new Dictionary<string, Material> {
            { "MI_EuropeanHornbeam_Trunk_01", pien }, { "MI_EuropeanHornbeam_Trunk_02", pien }, { "MI_EuropeanHornbeam_Trunk_03", pien },
            { "MI_EuropeanHornbeam_TwoSided", Mat("M_Grab_Liscie", "TwoSided_Summer", true) },
            { "MI_EuropeanHornbeam_Tileable", Mat("M_Grab_Galezie", "Tileable", true) },
            { "MI_EuropeanHornbeam_Decoration", Mat("M_Grab_Ozdoby", "Decoration", true) },
        };
        foreach (var fbx in Directory.GetFiles($"{Drzewa}/Modele", "*.fbx"))
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var k in mapa) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), k.Key), k.Value);
            imp.bakeAxisConversion = true;      // oś Z-góra z Unreala zapisana w siatce, obiekt bez obrotu (teren ignoruje obrót prefabu)
            imp.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            var granice = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var kol = go.AddComponent<CapsuleCollider>();               // kolizja tylko z pniem
            bool sadzonka = fbx.Contains("Sapling");
            kol.radius = sadzonka ? 0.08f : 0.3f; kol.height = Mathf.Min(granice.size.y, 6f); kol.center = new Vector3(0, kol.height / 2, 0);
            string nazwa = Path.GetFileNameWithoutExtension(fbx).Replace("SM_EuropeanHornbeam_", "Grab_");
            PrefabUtility.SaveAsPrefabAsset(go, $"{Drzewa}/Prefaby/{nazwa}.prefab");
            Object.DestroyImmediate(go);
            int tri = model.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            Debug.Log($"Drzewo {nazwa}: {tri} trójkątów, wysokość {granice.size.y:F1} m");
        }
        AssetDatabase.SaveAssets();
    }

    static Texture2D Tekstura(string nazwa, bool liniowa, bool alfa, bool normalna = false)
    {
        string s = $"{Drzewa}/Tekstury/{nazwa}.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(s);
        imp.textureType = normalna ? TextureImporterType.NormalMap : TextureImporterType.Default;
        imp.sRGBTexture = !liniowa;
        imp.alphaIsTransparency = alfa;
        imp.mipMapsPreserveCoverage = alfa; imp.alphaTestReferenceValue = 0.45f;   // liście nie znikają z odległości
        imp.maxTextureSize = 2048;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(s);
    }

    // ---------- 2. Malowanie terenu warstwami wg nachylenia, wysokości i ścieżki ----------
    static float OsDoliny(float xUnity)      // oś doliny (środek ścieżki) — ta sama funkcja co w mapie wysokości
    {
        float px = xUnity + 252f;
        return 45f * Mathf.Sin(px / 85f) + 12f * Mathf.Sin(px / 23f);   // z w metrach (252 - 252)
    }

    static int Warstwa(TerrainData td, string fragment)
    {
        for (int i = 0; i < td.terrainLayers.Length; i++)
            if (td.terrainLayers[i] && td.terrainLayers[i].name.Contains(fragment)) return i;
        return -1;
    }

    [MenuItem("Dolina/2. Maluj teren (trawa, mech, skały, ścieżka)")]
    public static void MalujTeren()
    {
        var t = Terrain.activeTerrain; var td = t.terrainData;
        int trawa = Warstwa(td, "Grass_A"), mech = Warstwa(td, "Grass_Moss"), skala = Warstwa(td, "Rock"), bloto = Warstwa(td, "Muddy");
        if (trawa < 0 || mech < 0 || skala < 0 || bloto < 0) { Debug.LogError("Dodaj warstwy: Grass_A, Grass_Moss, Rock, Muddy"); return; }
        int n = td.alphamapResolution, L = td.terrainLayers.Length;
        var w = new float[n, n, L];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float xw = t.transform.position.x + u * td.size.x, zw = t.transform.position.z + v * td.size.z;
                float nach = td.GetSteepness(u, v);
                float d = Mathf.Abs(zw - OsDoliny(xw));
                float szum = Mathf.PerlinNoise(xw * 0.05f, zw * 0.05f);
                float s = Mathf.SmoothStep(0, 1, (nach - 26f) / 12f);
                float b = Mathf.Clamp01((3.2f - d) / 1.5f) * (1 - s);
                float m = Mathf.Clamp01((d - 26f) / 20f + (szum - 0.5f)) * (1 - s) * (1 - b);
                float g = Mathf.Max(0, 1 - s - b - m);
                w[y, x, skala] = s; w[y, x, bloto] = b; w[y, x, mech] = m; w[y, x, trawa] += g;
            }
        Undo.RegisterCompleteObjectUndo(td, "Maluj teren");
        td.SetAlphamaps(0, 0, w);
        Debug.Log("Teren pomalowany: " + n + "×" + n);
    }

    // ---------- 3. Roślinność: drzewa z planu (jak w Unreal) + trawa i paprocie jako detale ----------
    public static Dictionary<string, List<float[]>> CzytajPlan(string sciezka, int kolumn)
    {
        var tekst = File.ReadAllText(sciezka); var wynik = new Dictionary<string, List<float[]>>();
        int i = 0;
        while ((i = tekst.IndexOf('"', i)) >= 0)
        {
            int k = tekst.IndexOf('"', i + 1); string klucz = tekst.Substring(i + 1, k - i - 1);
            int start = tekst.IndexOf('[', k), glebokosc = 0, j = start;
            do { if (tekst[j] == '[') glebokosc++; else if (tekst[j] == ']') glebokosc--; j++; } while (glebokosc > 0);
            var liczby = tekst.Substring(start, j - start).Split(new[] { '[', ']', ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(z => float.Parse(z, CultureInfo.InvariantCulture)).ToArray();
            var lista = new List<float[]>();
            for (int a = 0; a + kolumn <= liczby.Length; a += kolumn) lista.Add(liczby.Skip(a).Take(kolumn).ToArray());
            wynik[klucz] = lista; i = j;
        }
        return wynik;
    }

    [MenuItem("Dolina/3. Posadź roślinność (drzewa, trawa, paprocie)")]
    public static void PosadzRoslinnosc()
    {
        var t = Terrain.activeTerrain; var td = t.terrainData;
        Undo.RegisterCompleteObjectUndo(td, "Roślinność");
        // drzewa
        var prefaby = Directory.GetFiles($"{Drzewa}/Prefaby", "*.prefab").OrderBy(p => p).ToArray();
        GameObject P(string f) => AssetDatabase.LoadAssetAtPath<GameObject>(prefaby.First(p => p.Contains(f)));
        var grupy = new (string klucz, string[] wzory, int co)[] {
            ("drzewa_las", new[] { "Forest_04", "Forest_05", "Forest_06", "Forest_09" }, 1),
            ("drzewa_polne", new[] { "Field_01", "Field_03" }, 1),
            ("mlode_drzewka", new[] { "Sapling_01", "Sapling_02", "Sapling_03", "Sapling_04" }, 2) };
        var proto = new List<TreePrototype>(); var indeks = new Dictionary<string, int>();
        foreach (var g in grupy) foreach (var w in g.wzory) { indeks[w] = proto.Count; proto.Add(new TreePrototype { prefab = P(w) }); }
        td.treePrototypes = proto.ToArray();
        var plan = CzytajPlan($"{Dane}/roslinnosc.json", 5);
        var drzewa = new List<TreeInstance>(); var los = new System.Random(3);
        foreach (var g in grupy)
            for (int a = 0; a < plan[g.klucz].Count; a += g.co)
            {
                var r = plan[g.klucz][a];
                float u = (r[0] / 100f - t.transform.position.x) / td.size.x, v = (r[1] / 100f - t.transform.position.z) / td.size.z;
                float sk = r[4];
                drzewa.Add(new TreeInstance {
                    prototypeIndex = indeks[g.wzory[los.Next(g.wzory.Length)]],
                    position = new Vector3(u, td.GetInterpolatedHeight(u, v) / td.size.y - 0.002f, v),
                    rotation = r[3] * Mathf.Deg2Rad, widthScale = sk, heightScale = sk,
                    color = Color.white, lightmapColor = Color.white });
            }
        td.SetTreeInstances(drzewa.ToArray(), true);
        t.treeDistance = 400; t.treeBillboardDistance = 400; t.treeMaximumFullLODCount = 2000;

        // trawa, paprocie, kwiaty — prefaby z Terrain Sample Asset Pack jako detale terenu
        string pakiet = "Assets/TerrainSampleAssets/Prefabs/";
        var detale = new (string prefab, float min, float max)[] {
            ("Grass_A", 0.8f, 1.3f), ("Grass_B", 0.8f, 1.3f), ("Grass_C", 0.8f, 1.2f), ("Grass_D", 0.8f, 1.2f),
            ("Fern_A", 0.8f, 1.3f), ("Fern_B", 0.8f, 1.3f), ("Plant_A", 0.8f, 1.2f), ("Heather_A", 0.8f, 1.2f) };
        td.SetDetailResolution(1024, 32);
        td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);   // wartość w mapie = liczba roślin w komórce (ok. 0,5 × 0,5 m)
        td.detailPrototypes = detale.Select(d => new DetailPrototype {
            prototype = AssetDatabase.LoadAssetAtPath<GameObject>(pakiet + d.prefab + ".prefab"),
            usePrototypeMesh = true, useInstancing = true, renderMode = DetailRenderMode.VertexLit,
            minWidth = d.min, maxWidth = d.max, minHeight = d.min, maxHeight = d.max,
            noiseSpread = 0.3f, useDensityScaling = true, alignToGround = 0.6f }).ToArray();
        int n = td.detailResolution;
        var mapy = Enumerable.Range(0, detale.Length).Select(_ => new int[n, n]).ToArray();
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float xw = t.transform.position.x + u * td.size.x, zw = t.transform.position.z + v * td.size.z;
                float nach = td.GetSteepness(u, v), d = Mathf.Abs(zw - OsDoliny(xw));
                float kepy = Mathf.PerlinNoise(xw * 0.03f + 7, zw * 0.03f), drobne = Mathf.PerlinNoise(xw * 0.2f, zw * 0.2f);
                if (nach < 24 && d > 2.8f)
                {
                    int trawa = drobne > 0.35f ? 2 : 1;
                    mapy[(x + y) % 4][y, x] = trawa;                                // 4 rodzaje trawy na zmianę
                    if (d > 5 && d < 40 && kepy > 0.6f) mapy[6 + (x % 2)][y, x] = drobne > 0.55f ? 1 : 0;   // kwiaty i wrzos
                }
                if (nach < 32 && d > 16 && kepy > 0.45f) mapy[4 + (y % 2)][y, x] = drobne > 0.5f ? 1 : 0;  // paprocie w lesie
            }
        for (int i = 0; i < detale.Length; i++) td.SetDetailLayer(0, 0, i, mapy[i]);
        t.detailObjectDistance = 150; t.detailObjectDensity = 1f;
        Debug.Log($"Posadzono {drzewa.Count} drzew i {detale.Length} rodzajów roślin");
    }

    // ---------- 4. Gracz z Starter Assets na początku ścieżki ----------
    [MenuItem("Dolina/4. Gracz na starcie ścieżki")]
    public static void GraczNaStarcie()
    {
        const string sa = "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/";
        foreach (var c in Object.FindObjectsByType<Camera>())
            if (c.name == "Main Camera") Undo.DestroyObjectImmediate(c.gameObject);
        GameObject Wstaw(string p) { var o = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sa + p)); Undo.RegisterCreatedObjectUndo(o, p); return o; }
        var t = Terrain.activeTerrain;
        float x = -222f, z = OsDoliny(x);
        var gracz = Wstaw("PlayerArmature.prefab");
        gracz.transform.position = new Vector3(x, t.SampleHeight(new Vector3(x, 0, z)) + t.transform.position.y + 0.1f, z);
        gracz.transform.rotation = Quaternion.LookRotation(Vector3.right);
        Wstaw("MainCamera.prefab");
        var kamera = Wstaw("PlayerFollowCamera.prefab");
        var cel = gracz.transform.Find("PlayerCameraRoot");
        foreach (var k in kamera.GetComponents<MonoBehaviour>())
        {
            var so = new SerializedObject(k);
            foreach (var n in new[] { "TrackingTarget", "m_Follow", "Target.TrackingTarget" })
            {
                var pr = so.FindProperty(n);
                if (pr != null && pr.propertyType == SerializedPropertyType.ObjectReference) { pr.objectReferenceValue = cel; so.ApplyModifiedProperties(); }
            }
        }
        Selection.activeGameObject = gracz;
        Debug.Log($"Gracz na starcie: {gracz.transform.position}");
    }
}
