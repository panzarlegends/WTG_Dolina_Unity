using UnityEngine;

// Pułapka z kolcami: kolce wysuwają się i chowają; dotknięcie wysuniętych odbiera życie.
public class Pulapka : MonoBehaviour
{
    public Transform kolce;
    public float okres = 2.5f;
    public float wysokosc = 0.6f;
    float przesuniecie;

    void Start() { przesuniecie = Random.value * okres; }

    bool Wysuniete => Mathf.Repeat(Time.time + przesuniecie, okres) < okres * 0.5f;

    void Update()
    {
        if (!kolce) return;
        float cel = Wysuniete ? 0f : -wysokosc;
        var p = kolce.localPosition;
        p.y = Mathf.MoveTowards(p.y, cel, Time.deltaTime * 4f);
        kolce.localPosition = p;
    }

    void OnTriggerStay(Collider inny)
    {
        if (inny.CompareTag("Player") && Wysuniete && kolce.localPosition.y > -0.2f)
            StanGry.Instancja.Zraniony();
    }
}
