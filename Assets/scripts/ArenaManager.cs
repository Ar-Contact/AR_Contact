using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    [Header("Drag Panelleri")]
    public GameObject dragBluePanel; // Hierarchy'deki 'DragBlue' objesi
    public GameObject dragRedPanel;  // Hierarchy'deki 'DragRed' objesi

    [Header("Kameralar")]
    public GameObject blueCamera;    // Hierarchy'deki 'Main Camera Blue'
    public GameObject redCamera;     // Hierarchy'deki 'Main Camera Red'

    void Start()
    {
        // Temizlik: Baþlangýçta hepsini kapatalým ki karýþýklýk olmasýn
        if (dragBluePanel != null) dragBluePanel.SetActive(false);
        if (dragRedPanel != null) dragRedPanel.SetActive(false);
        if (blueCamera != null) blueCamera.SetActive(false);
        if (redCamera != null) redCamera.SetActive(false);

        // Menüden gelen veriyi kontrol et
        if (PlayerTeamer.SecilenTakim == "Blue")
        {
            // Mavi Takým Ýþlemleri
            if (dragBluePanel != null) dragBluePanel.SetActive(true);
            if (blueCamera != null) blueCamera.SetActive(true);

            Debug.Log("Mavi Takým ve Kamera Aktif Edildi.");
        }
        else if (PlayerTeamer.SecilenTakim == "Red")
        {
            // Kýrmýzý Takým Ýþlemleri
            if (dragRedPanel != null) dragRedPanel.SetActive(true);
            if (redCamera != null) redCamera.SetActive(true);

            Debug.Log("Kýrmýzý Takým ve Kamera Aktif Edildi.");
        }
        else
        {
            // Eðer direkt sahneden baþlattýysan test için birini varsayýlan açabilirsin
            Debug.LogWarning("Takým seçilmedi! Test amaçlý Mavi açýlýyor...");
            if (blueCamera != null) blueCamera.SetActive(true);
            if (dragBluePanel != null) dragBluePanel.SetActive(true);
        }
    }
}