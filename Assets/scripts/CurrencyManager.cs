using UnityEngine;
using UnityEngine.UI; // UI Text (Legacy) kullanýmý için gerekli

public class CurrencyManager : MonoBehaviour
{
    // Singleton yapýsý: Bu sayede diðer scriptlerden (Health, DragSpawner)
    // CurrencyManager.Instance diyerek buraya ulaþabiliriz.
    public static CurrencyManager Instance;

    [Header("Ekonomi Ayarlarý")]
    public int baslangicParasi = 500; // Sahne açýldýðýnda kaç puan olsun?
    public Text paraGostergesiText;   // Puanýn yazacaðý UI Text objesi

    // Arka planda tutulan gerçek para miktarý
    private int mevcutPara;

    private void Awake()
    {
        // Sahnede sadece bir tane CurrencyManager olduðundan emin oluyoruz
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Oyuna baþlarken parayý ayarla ve ekrana yaz
        mevcutPara = baslangicParasi;
        ArayuzuGuncelle();
    }

    // --- PARA HARCAMA FONKSÝYONU ---
    // DragAndDropSpawner bu fonksiyonu çaðýrýr.
    // Eðer para yeterliyse düþer ve 'true' döner, yetmezse 'false' döner.
    public bool ParaHarcayabilirMi(int miktar)
    {
        if (mevcutPara >= miktar)
        {
            mevcutPara -= miktar;
            ArayuzuGuncelle();
            return true; // Ýþlem baþarýlý, asker oluþturulabilir
        }
        else
        {
            Debug.Log("Yetersiz Bakiye! Gereken: " + miktar + ", Mevcut: " + mevcutPara);
            return false; // Ýþlem baþarýsýz
        }
    }

    // --- PARA KAZANMA FONKSÝYONU ---
    // Health scripti (mob ölünce) bu fonksiyonu çaðýrýr.
    public void ParaKazan(int miktar)
    {
        mevcutPara += miktar;
        ArayuzuGuncelle();
        Debug.Log("Para Kazanýldý: " + miktar + " | Yeni Bakiye: " + mevcutPara);
    }

    // UI Güncelleme Ýþlemi
    void ArayuzuGuncelle()
    {
        if (paraGostergesiText != null)
        {
            paraGostergesiText.text = "Puan: " + mevcutPara.ToString();
        }
    }
}