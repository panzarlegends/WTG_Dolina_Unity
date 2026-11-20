using UnityEngine;

// Złota moneta: obraca się, unosi i znika po dotknięciu przez gracza.
public class Moneta : MonoBehaviour
{
    public float obrotNaSekunde = 120f;
    public AudioClip dzwiek;
    Vector3 start;

    void Start() { start = transform.position; }

    void Update()
    {
        transform.Rotate(0, obrotNaSekunde * Time.deltaTime, 0, Space.World);
        transform.position = start + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.15f;
    }

    void OnTriggerEnter(Collider inny)
    {
        if (!inny.CompareTag("Player")) return;
        if (dzwiek) AudioSource.PlayClipAtPoint(dzwiek, transform.position);
        StanGry.Instancja.DodajMonete();
        Destroy(gameObject);
    }
}
