using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Płynne przejście między scenami: ekran ściemnia się, scena się wczytuje, obraz się rozjaśnia.
public class Przejscie : MonoBehaviour
{
    static Przejscie instancja;
    Image zaslona;
    public float czas = 1.2f;

    public static void Zaladuj(string scena)
    {
        if (!instancja)
        {
            var o = new GameObject("Przejscie", typeof(Canvas));
            DontDestroyOnLoad(o);
            var c = o.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 1000;
            instancja = o.AddComponent<Przejscie>();
            var z = new GameObject("Zaslona", typeof(RectTransform), typeof(Image));
            z.transform.SetParent(o.transform, false);
            var r = z.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero;
            instancja.zaslona = z.GetComponent<Image>(); instancja.zaslona.color = new Color(0, 0, 0, 0); instancja.zaslona.raycastTarget = false;
        }
        instancja.StopAllCoroutines();
        instancja.StartCoroutine(instancja.Przejdz(scena));
    }

    IEnumerator Przejdz(string scena)
    {
        yield return Zmien(0, 1);
        yield return SceneManager.LoadSceneAsync(scena);
        yield return new WaitForSecondsRealtime(0.2f);
        yield return Zmien(1, 0);
    }

    IEnumerator Zmien(float od, float doA)
    {
        for (float t = 0; t < czas; t += Time.unscaledDeltaTime)
        {
            zaslona.color = new Color(0, 0, 0, Mathf.SmoothStep(od, doA, t / czas));
            yield return null;
        }
        zaslona.color = new Color(0, 0, 0, doA);
    }
}
