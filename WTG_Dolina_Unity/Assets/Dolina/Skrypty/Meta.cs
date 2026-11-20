using UnityEngine;

// Meta rundy: wejście gracza kończy rundę i ładuje następną scenę.
public class Meta : MonoBehaviour
{
    void OnTriggerEnter(Collider inny)
    {
        if (inny.CompareTag("Player")) StanGry.Instancja.Meta();
    }
}
