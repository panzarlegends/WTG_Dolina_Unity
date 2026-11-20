using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Lekcja 3: scena Menu — dolina w tle (ten sam teren), przelot kamery, tytuł i przyciski Graj / Polana / Wyjdź.
public static class DolinaMenu
{
    [MenuItem("Dolina/8. Scena Menu (tło, tytuł, przyciski)")]
    public static void ZbudujMenu()
    {
        EditorSceneManager.SaveOpenScenes();
        const string sc = "Assets/Scenes/Menu.unity";
        AssetDatabase.DeleteAsset(sc); AssetDatabase.CopyAsset("Assets/Scenes/Poziom_1.unity", sc);
        var scena = EditorSceneManager.OpenScene(sc, OpenSceneMode.Single);
        foreach (var n in new[] { "PlayerArmature", "PlayerFollowCamera", "Monety", "Pulapki", "Meta", "StanGry", "Moneta" })
        { var o = GameObject.Find(n); if (o) Object.DestroyImmediate(o); }
        var kamera = GameObject.Find("MainCamera");
        foreach (var k in kamera.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(k);   // bez Cinemachine Brain
        kamera.AddComponent<PrzelotKamery>();
        var slonce = Object.FindObjectsByType<Light>().First(l => l.type == LightType.Directional);
        slonce.transform.rotation = Quaternion.Euler(14, 235, 0); slonce.color = new Color(1f, 0.82f, 0.6f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var plotno = new GameObject("MenuUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        plotno.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sk = plotno.GetComponent<CanvasScaler>(); sk.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sk.referenceResolution = new Vector2(1920, 1080);
        var menu = plotno.AddComponent<MenuGlowne>();

        // półprzezroczysty pas pod przyciskami
        var pas = Ui("Tlo", plotno.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(620, 560));
        pas.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.05f, 0.55f);
        Tekst("Tytul", plotno.transform, "DOLINA", 120, new Vector2(0, 330), new Vector2(1200, 180), font, new Color(1f, 0.9f, 0.62f));
        Tekst("Podtytul", plotno.transform, "zbierz monety, omiń pułapki, dojdź do bramy", 36, new Vector2(0, 230), new Vector2(1200, 60), font, new Color(0.92f, 0.95f, 0.9f));
        Przycisk("Graj", plotno.transform, new Vector2(0, 90), font, menu.Graj);
        Przycisk("Polana", plotno.transform, new Vector2(0, -60), font, menu.Polana);
        Przycisk("Wyjdź", plotno.transform, new Vector2(0, -210), font, menu.Wyjdz);

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Selection.activeGameObject = plotno;
        EditorSceneManager.MarkSceneDirty(scena); EditorSceneManager.SaveScene(scena);
        var sceny = EditorBuildSettings.scenes.ToList();
        sceny.RemoveAll(s => s.path == sc); sceny.Insert(0, new EditorBuildSettingsScene(sc, true));
        EditorBuildSettings.scenes = sceny.ToArray();
        Debug.Log("Menu gotowe. Kolejność scen: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))));
    }

    static GameObject Ui(string nazwa, Transform rodzic, Vector2 kotwica, Vector2 poz, Vector2 rozmiar)
    {
        var o = new GameObject(nazwa, typeof(RectTransform)); o.transform.SetParent(rodzic, false);
        var r = o.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = kotwica; r.anchoredPosition = poz; r.sizeDelta = rozmiar;
        return o;
    }

    static Text Tekst(string nazwa, Transform rodzic, string tresc, int rozmiar, Vector2 poz, Vector2 wym, Font f, Color kolor)
    {
        var o = Ui(nazwa, rodzic, new Vector2(0.5f, 0.5f), poz, wym);
        var t = o.AddComponent<Text>(); t.text = tresc; t.font = f; t.fontSize = rozmiar; t.alignment = TextAnchor.MiddleCenter; t.color = kolor;
        o.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
        return t;
    }

    static void Przycisk(string napis, Transform rodzic, Vector2 poz, Font f, UnityEngine.Events.UnityAction akcja)
    {
        var o = Ui("Przycisk_" + napis, rodzic, new Vector2(0.5f, 0.5f), poz, new Vector2(460, 110));
        var img = o.AddComponent<Image>(); img.color = new Color(0.18f, 0.24f, 0.14f, 0.92f);
        var b = o.AddComponent<Button>();
        var c = b.colors; c.highlightedColor = new Color(1f, 0.85f, 0.5f); c.pressedColor = new Color(0.8f, 0.65f, 0.3f); b.colors = c;
        UnityEventTools.AddPersistentListener(b.onClick, akcja);          // widoczne w Inspectorze w sekcji On Click ()
        Tekst("Napis", o.transform, napis, 52, Vector2.zero, new Vector2(460, 110), f, Color.white);
    }
}
