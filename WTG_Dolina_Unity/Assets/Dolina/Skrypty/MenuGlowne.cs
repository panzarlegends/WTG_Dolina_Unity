using UnityEngine;
using UnityEngine.SceneManagement;

// Przyciski menu głównego.
public class MenuGlowne : MonoBehaviour
{
    public string pierwszaScena = "Poziom_1";

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Graj() { Przejscie.Zaladuj(pierwszaScena); }
    public void Polana() { Przejscie.Zaladuj("Polana"); }

    public void Wyjdz()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
