using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lekcja 2: prefaby gry (moneta, pułapka, meta) i rozstawienie ich w dolinie według danych z wersji Unreal.
public static class DolinaGra
{
    const string Gra = "Assets/Dolina/Gra";
    const string Dane = "Assets/Dolina/Dane";

    static Material Material(string nazwa, Color kolor, float metal, float gladkosc, Color? swiecenie = null)
    {
        string s = $"{Gra}/Materialy/{nazwa}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(s);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, s); }
        m.SetColor("_BaseColor", kolor); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", gladkosc);
        if (swiecenie.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", swiecenie.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Mesh Stozek()
    {
        string s = $"{Gra}/Stozek.asset";
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(s);
        if (m) return m;
        const int n = 12; var v = new Vector3[n + 2]; var t = new int[n * 6];
        v[n] = new Vector3(0, 1, 0); v[n + 1] = Vector3.zero;
        for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f); }
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; t[i * 6] = i; t[i * 6 + 1] = n; t[i * 6 + 2] = j; t[i * 6 + 3] = j; t[i * 6 + 4] = n + 1; t[i * 6 + 5] = i; }
        m = new Mesh { name = "Stozek", vertices = v, triangles = t }; m.RecalculateNormals(); m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, s);
        return m;
    }

    static GameObject Ksztalt(PrimitiveType typ, Transform rodzic, Vector3 poz, Vector3 skala, Material mat, bool kolizja = false)
    {
        var o = GameObject.CreatePrimitive(typ); o.transform.SetParent(rodzic, false);
        o.transform.localPosition = poz; o.transform.localScale = skala;
        o.GetComponent<Renderer>().sharedMaterial = mat;
        if (!kolizja) Object.DestroyImmediate(o.GetComponent<Collider>());
        return o;
    }

    [MenuItem("Dolina/5. Prefaby gry: moneta, pułapka, meta")]
    public static void Prefaby()
    {
        Directory.CreateDirectory($"{Gra}/Materialy"); Directory.CreateDirectory($"{Gra}/Prefaby"); AssetDatabase.Refresh();
        var zloto = Material("M_Zloto", new Color(1f, 0.77f, 0.34f), 1f, 0.85f, new Color(0.25f, 0.16f, 0.03f));
        var stal = Material("M_Stal", new Color(0.45f, 0.45f, 0.47f), 1f, 0.55f);
        var rdza = Material("M_Rdza", new Color(0.22f, 0.13f, 0.08f), 0.6f, 0.25f);
        var kamien = Material("M_Kamien", new Color(0.42f, 0.4f, 0.37f), 0f, 0.15f);
        var blask = Material("M_Blask", new Color(1f, 0.85f, 0.4f), 0f, 0.9f, new Color(4f, 2.8f, 0.9f));

        // Moneta: płaski złoty walec, obrócony pionowo; kula-wyzwalacz
        var moneta = new GameObject("Moneta");
        Ksztalt(PrimitiveType.Cylinder, moneta.transform, Vector3.zero, new Vector3(0.7f, 0.04f, 0.7f), zloto).transform.localRotation = Quaternion.Euler(90, 0, 0);
        Ksztalt(PrimitiveType.Cylinder, moneta.transform, Vector3.zero, new Vector3(0.5f, 0.05f, 0.5f), zloto).transform.localRotation = Quaternion.Euler(90, 0, 0);
        var kula = moneta.AddComponent<SphereCollider>(); kula.isTrigger = true; kula.radius = 0.6f;
        moneta.AddComponent<Moneta>();
        PrefabUtility.SaveAsPrefabAsset(moneta, $"{Gra}/Prefaby/Moneta.prefab"); Object.DestroyImmediate(moneta);

        // Pułapka: zardzewiała płyta z 9 stalowymi kolcami, które się wysuwają
        var pulapka = new GameObject("Pulapka");
        Ksztalt(PrimitiveType.Cube, pulapka.transform, new Vector3(0, 0.05f, 0), new Vector3(2.2f, 0.1f, 2.2f), rdza, true);
        var kolce = new GameObject("Kolce").transform; kolce.SetParent(pulapka.transform, false);
        var stozek = Stozek();
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                var k = new GameObject("Kolec", typeof(MeshFilter), typeof(MeshRenderer));
                k.transform.SetParent(kolce, false); k.transform.localPosition = new Vector3(x * 0.65f, 0.1f, z * 0.65f);
                k.transform.localScale = new Vector3(0.28f, 0.75f, 0.28f);
                k.GetComponent<MeshFilter>().sharedMesh = stozek; k.GetComponent<MeshRenderer>().sharedMaterial = stal;
            }
        var strefa = pulapka.AddComponent<BoxCollider>(); strefa.isTrigger = true; strefa.center = new Vector3(0, 0.6f, 0); strefa.size = new Vector3(2.2f, 1.2f, 2.2f);
        pulapka.AddComponent<Pulapka>().kolce = kolce;
        PrefabUtility.SaveAsPrefabAsset(pulapka, $"{Gra}/Prefaby/Pulapka.prefab"); Object.DestroyImmediate(pulapka);

        // Meta: kamienna brama ze świecącym nadprożem i światłem
        var meta = new GameObject("Meta");
        Ksztalt(PrimitiveType.Cube, meta.transform, new Vector3(-2f, 2f, 0), new Vector3(0.8f, 4f, 0.8f), kamien, true);
        Ksztalt(PrimitiveType.Cube, meta.transform, new Vector3(2f, 2f, 0), new Vector3(0.8f, 4f, 0.8f), kamien, true);
        Ksztalt(PrimitiveType.Cube, meta.transform, new Vector3(0, 4.3f, 0), new Vector3(5f, 0.6f, 0.9f), kamien, true);
        Ksztalt(PrimitiveType.Cube, meta.transform, new Vector3(0, 3.85f, 0), new Vector3(3.2f, 0.12f, 0.3f), blask);
        var swiatlo = new GameObject("Swiatlo", typeof(Light)); swiatlo.transform.SetParent(meta.transform, false); swiatlo.transform.localPosition = new Vector3(0, 3.2f, 0);
        var l = swiatlo.GetComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.8f, 0.45f); l.intensity = 8f; l.range = 10f;
        var brama = meta.AddComponent<BoxCollider>(); brama.isTrigger = true; brama.center = new Vector3(0, 1.5f, 0); brama.size = new Vector3(3.2f, 3f, 1.2f);
        meta.AddComponent<Meta>();
        PrefabUtility.SaveAsPrefabAsset(meta, $"{Gra}/Prefaby/Meta.prefab"); Object.DestroyImmediate(meta);
        AssetDatabase.SaveAssets();
        Debug.Log("Prefaby gotowe: Moneta, Pulapka, Meta");
    }

    static float[][] Czytaj(string plik) =>
        File.ReadAllText($"{Dane}/{plik}").Split(new[] { "],", "]]" }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('[', ']', ' ', '\n', '\r')).Where(w => w.Length > 0)
            .Select(w => w.Split(',').Select(z => float.Parse(z, System.Globalization.CultureInfo.InvariantCulture)).ToArray())
            .Where(w => w.Length >= 3).ToArray();

    static Vector3 NaTerenie(float xCm, float yCm, float nad)
    {
        var t = Terrain.activeTerrain; var p = new Vector3(xCm / 100f, 0, yCm / 100f);
        p.y = t.SampleHeight(p) + t.transform.position.y + nad;
        return p;
    }

    static GameObject Wstaw(string prefab, Transform rodzic, Vector3 poz, float obrot = 0)
    {
        var o = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Gra}/Prefaby/{prefab}.prefab"), rodzic);
        o.transform.SetPositionAndRotation(poz, Quaternion.Euler(0, obrot, 0));
        Undo.RegisterCreatedObjectUndo(o, prefab);
        return o;
    }

    [MenuItem("Dolina/6. Rozstaw monety, pułapki i metę (dane z Unreala)")]
    public static void Rozstaw()
    {
        foreach (var n in new[] { "Monety", "Pulapki", "Meta" }) { var s = GameObject.Find(n); if (s) Undo.DestroyObjectImmediate(s); }
        var monety = new GameObject("Monety").transform; var pulapki = new GameObject("Pulapki").transform;
        foreach (var m in Czytaj("monety.json")) Wstaw("Moneta", monety, NaTerenie(m[0], m[1], 1.1f));
        foreach (var p in Czytaj("pulapki.json")) Wstaw("Pulapka", pulapki, NaTerenie(p[0], p[1], -0.05f));
        var meta = Czytaj("meta.json")[0];
        var kier = NaTerenie(meta[0], meta[1], 0) - NaTerenie(meta[0] - 500, meta[1], 0);   // brama w poprzek ścieżki
        Wstaw("Meta", null, NaTerenie(meta[0], meta[1], -0.1f), Quaternion.LookRotation(new Vector3(kier.x, 0, kier.z)).eulerAngles.y).name = "Meta";
        Undo.RegisterCreatedObjectUndo(monety.gameObject, "Monety"); Undo.RegisterCreatedObjectUndo(pulapki.gameObject, "Pulapki");
        Debug.Log($"Rozstawiono {monety.childCount} monet, {pulapki.childCount} pułapek i metę");
    }

    // Rundy 2 i 3: kopie sceny z innym światłem, dodatkowymi pułapkami i następną sceną
    [MenuItem("Dolina/7. Zbuduj rundy 2 i 3 + lista scen")]
    public static void Rundy()
    {
        EditorSceneManager.SaveOpenScenes();
        string p1 = "Assets/Scenes/Poziom_1.unity";
        var los = new System.Random(11);
        var ustawienia = new[] {
            (nazwa: "Poziom_2", runda: "Runda 2", nastepna: "Poziom_3", slonceX: 22f, slonceY: 250f, kolor: new Color(1f, 0.78f, 0.55f), extra: 6, zycia: 3),
            (nazwa: "Poziom_3", runda: "Runda 3", nastepna: "Menu", slonceX: 8f, slonceY: 200f, kolor: new Color(1f, 0.55f, 0.35f), extra: 12, zycia: 2) };
        foreach (var u in ustawienia)
        {
            string sc = $"Assets/Scenes/{u.nazwa}.unity";
            AssetDatabase.DeleteAsset(sc); AssetDatabase.CopyAsset(p1, sc);
            var scena = EditorSceneManager.OpenScene(sc, OpenSceneMode.Single);
            var stan = Object.FindAnyObjectByType<StanGry>();
            stan.nazwaRundy = u.runda; stan.nastepnaScena = u.nastepna; stan.zycia = u.zycia;
            var slonce = Object.FindObjectsByType<Light>().First(l => l.type == LightType.Directional);
            slonce.transform.rotation = Quaternion.Euler(u.slonceX, u.slonceY, 0); slonce.color = u.kolor;
            var pulapki = GameObject.Find("Pulapki").transform;
            for (int i = 0; i < u.extra; i++)
            {
                float x = -180f + (float)los.NextDouble() * 380f;
                float px = x + 252f, z = 45f * Mathf.Sin(px / 85f) + 12f * Mathf.Sin(px / 23f) + ((float)los.NextDouble() - 0.5f) * 3f;
                Wstaw("Pulapka", pulapki, NaTerenie(x * 100f, z * 100f, -0.05f));
            }
            EditorSceneManager.MarkSceneDirty(scena); EditorSceneManager.SaveScene(scena);
        }
        EditorBuildSettings.scenes = new[] { "Menu", "Poziom_1", "Poziom_2", "Poziom_3", "Polana" }
            .Select(n => $"Assets/Scenes/{n}.unity").Where(File.Exists).Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
        EditorSceneManager.OpenScene(p1, OpenSceneMode.Single);
        Debug.Log("Rundy gotowe. Sceny w Build Profiles: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
    }
}
