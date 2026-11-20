using UnityEngine;

// Kamera w menu: powoli krąży nad doliną i patrzy na jej środek.
public class PrzelotKamery : MonoBehaviour
{
    public Vector3 srodek = new Vector3(0, 5, 0);
    public float promien = 140f, wysokosc = 55f, stopniNaSekunde = 3f;
    float kat = 200f;

    void Update()
    {
        kat += stopniNaSekunde * Time.deltaTime;
        var r = Quaternion.Euler(0, kat, 0) * Vector3.forward * promien;
        transform.position = srodek + r + Vector3.up * wysokosc;
        transform.LookAt(srodek);
    }
}
