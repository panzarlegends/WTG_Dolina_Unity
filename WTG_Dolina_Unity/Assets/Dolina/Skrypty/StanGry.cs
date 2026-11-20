using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Stan jednej rundy: monety, życia, HUD i przejście do następnej sceny.
public class StanGry : MonoBehaviour
{
    public static StanGry Instancja;

    [Header("Runda")]
    public string nazwaRundy = "Runda 1";
    public string nastepnaScena = "Poziom_2";
    public int zycia = 3;
    [Tooltip("Wyłącz na czas testów, żeby sprawdzić przejście do następnej rundy bez zbierania monet")]
    public bool metaWymagaWszystkichMonet = true;

    [Header("HUD (tworzony w Start)")]
    public Font czcionka;

    int monety, wszystkieMonety;
    Text napisMonety, napisZycia, napisSrodek;
    Vector3 punktStartu;
    Transform gracz;
    bool koniec;

    void Awake() { Instancja = this; }

    void Start()
    {
        wszystkieMonety = FindObjectsByType<Moneta>().Length;
        var g = GameObject.FindGameObjectWithTag("Player");
        if (g) { gracz = g.transform; punktStartu = gracz.position; }
        ZbudujHud();
        Pokaz(nazwaRundy, 2.5f);
        Odswiez();
    }

    void ZbudujHud()
    {
        if (!czcionka) czcionka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var plotno = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        plotno.renderMode = RenderMode.ScreenSpaceOverlay;
        var skaler = plotno.GetComponent<CanvasScaler>();
        skaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        skaler.referenceResolution = new Vector2(1920, 1080);
        napisMonety = Napis(plotno.transform, new Vector2(0, 1), new Vector2(40, -40), TextAnchor.UpperLeft, 44);
        napisZycia = Napis(plotno.transform, new Vector2(1, 1), new Vector2(-40, -40), TextAnchor.UpperRight, 44);
        napisSrodek = Napis(plotno.transform, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 90);
    }

    Text Napis(Transform rodzic, Vector2 kotwica, Vector2 pozycja, TextAnchor wyrownanie, int rozmiar)
    {
        var o = new GameObject("Napis", typeof(RectTransform), typeof(Text), typeof(Shadow));
        o.transform.SetParent(rodzic, false);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = kotwica;
        r.anchoredPosition = pozycja;
        r.sizeDelta = new Vector2(1400, 200);
        var t = o.GetComponent<Text>();
        t.font = czcionka; t.fontSize = rozmiar; t.alignment = wyrownanie;
        t.color = new Color(1f, 0.93f, 0.7f);
        o.GetComponent<Shadow>().effectDistance = new Vector2(3, -3);
        return t;
    }

    void Odswiez()
    {
        napisMonety.text = $"Monety: {monety} / {wszystkieMonety}";
        napisZycia.text = "Życia: " + Mathf.Max(zycia, 0);
    }

    void Pokaz(string tekst, float sekundy)
    {
        CancelInvoke(nameof(Ukryj));
        napisSrodek.text = tekst;
        Invoke(nameof(Ukryj), sekundy);
    }

    void Ukryj() { napisSrodek.text = ""; }

    public void DodajMonete()
    {
        monety++;
        Odswiez();
        if (monety == wszystkieMonety) Pokaz("Wszystkie monety! Biegnij do mety", 3f);
    }

    float ostatniaRana = -10f;

    public void Zraniony()
    {
        if (koniec || Time.time - ostatniaRana < 1.5f) return;   // chwila ochrony po trafieniu
        ostatniaRana = Time.time;
        zycia--;
        Odswiez();
        if (zycia <= 0)
        {
            koniec = true;
            Pokaz("Koniec gry", 3f);
            Invoke(nameof(Restart), 3f);
        }
        else
        {
            Pokaz("Au! Zostało żyć: " + zycia, 1.5f);
            Teleport(punktStartu);
        }
    }

    public void Meta()
    {
        if (koniec) return;
        if (metaWymagaWszystkichMonet && monety < wszystkieMonety) { Pokaz($"Brakuje monet: {wszystkieMonety - monety}", 2f); return; }
        koniec = true;
        Pokaz(nazwaRundy + " ukończona!", 3f);
        Invoke(nameof(Dalej), 3f);
    }

    void Teleport(Vector3 cel)
    {
        if (!gracz) return;
        var cc = gracz.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        gracz.position = cel;
        if (cc) cc.enabled = true;
    }

    void Restart() { Przejscie.Zaladuj(SceneManager.GetActiveScene().name); }
    void Dalej() { Przejscie.Zaladuj(nastepnaScena); }
}
