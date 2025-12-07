using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerTeamer : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Buraya geçiþ yapýlacak oyun sahnesinin tam adýný yazýn")]
    public string oyunSahnesiIsmi; // Inspector'da görünecek deðiþken

    // Diðer sahneye veriyi taþýmak için static deðiþken
    public static string SecilenTakim = "";

    public void StartAsBlue()
    {
        SecilenTakim = "Blue";
        SceneGecisYap();
    }

    public void StartAsRed()
    {
        SecilenTakim = "Red";
        SceneGecisYap();
    }

    // Kod tekrarýný önlemek için ortak fonksiyon
    private void SceneGecisYap()
    {
        if (!string.IsNullOrEmpty(oyunSahnesiIsmi))
        {
            SceneManager.LoadScene(oyunSahnesiIsmi);
        }
        else
        {
            Debug.LogError("HATA: Inspector'dan Sahne Ýsmini yazmayý unuttunuz!");
        }
    }
}