using UnityEngine;

// Brama na polanie: wejście gracza przenosi (z przejściem) do pierwszej rundy gry.
public class BramaPolany : MonoBehaviour
{
    public string scena = "Poziom_1";
    bool uzyta;

    void OnTriggerEnter(Collider inny)
    {
        if (uzyta || !inny.CompareTag("Player")) return;
        uzyta = true;
        Przejscie.Zaladuj(scena);
    }
}
