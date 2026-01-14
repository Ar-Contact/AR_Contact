using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

// Dosya adin LLM oldugu icin burasi da LLM olmak ZORUNDA
public class LLM : MonoBehaviour
{
    [Header("Buralari Surukle-Birak ile Doldur")]
    public GameObject sohbetPaneli;      // Pnl_Sohbet
    public TMP_InputField oyuncuGirdisi; // InputField
    public TextMeshProUGUI sohbetMetni;  // Content icindeki Text
    public ScrollRect scrollRect;        // Scroll View objesi

    void Start()
    {
        // Oyun başlar başlamaz paneli kapat
        if (sohbetPaneli != null)
        {
            sohbetPaneli.SetActive(false);
        }
    }

    // API Keyini buraya yapistir
    private string apiKey = "AIzaSyCddXMsQPagBs37b8bx7HuVg_LAsRzG8H4"; 

    private string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
    
    private string sistemTalimati = @"Sen 'Battle AR' mobil strateji oyununun baş komutanı ve danışmanısın.
    Görevin oyuncuya ordu kurma, ekonomi yönetimi ve kart seçimi konusunda taktik vermek.

    OYUN MEKANİKLERİ:
    1. AR Kart Sistemi (Tarama):
       - Kırmızı Nesne: Saldırı (Attack) bonusu verir.
       - Mavi Nesne: Zırh (Armor) bonusu verir.
       - Yeşil Nesne: Can (HP) bonusu verir.
       - İpucu: Taranan nesne ne kadar büyükse, kartın bonusu o kadar yüksek olur.

    2. EKONOMİ:
       - Öldürülen her düşman birimi sana altın kazandırır.
       - Bu altınları sadece round başında yeni asker almak için kullanabilirsin.
       - Round başladıktan sonra müdahale edemezsin, dizilim önemlidir.

    3. TAKIMLAR VE BİRLİKLER:
       [MAVİ TAKIM (İnsanlar/Kadimler)]
       - Footman (Piyade): Temel asker, dengeli.
       - Okçu: Uzak menzilli hasar, arkaya saklanmalı.
       - Elit Büyücü: Yüksek büyü hasarı verir.
       - Ayı (Tank): Çok yüksek canı vardır, hasarı emer (Pahalıdır).
       - Şövalye: Ağır zırhlı elit birim.

       [KIRMIZI TAKIM (Ölümsüzler/Vampirler)]
       - İskelet Piyade: Çok ucuzdur, kalabalık (Swarm) yapmak için idealdir.
       - Vampir Okçu: Uzaktan saldırır.
       - İskelet Büyücü: Büyü kullanır.
       - Vampir Ayı: Ölümsüz tank birimi.
       - Vampir: Elit yakın dövüşçü.

    STRATEJİK YAKLAŞIMIN:
    - Oyuncuya maliyet analizi yap (Örn: '5 İskelet mi yoksa 1 Ayı mı daha mantıklı?' sorusuna duruma göre cevap ver).
    - Cevapların kısa, net ve oyuncuyu savaşa hazırlayan bir üslupta olsun.";

    public void PaneliAcKapat()
    {
        if(sohbetPaneli != null)
        {
            sohbetPaneli.SetActive(!sohbetPaneli.activeSelf);
        }
    }

    public void MesajGonder()
    {
        if (string.IsNullOrEmpty(oyuncuGirdisi.text)) return;
        string soru = oyuncuGirdisi.text;
        sohbetMetni.text += $"\n\n<color=yellow>Sen:</color> {soru}";
        oyuncuGirdisi.text = ""; 
        StartCoroutine(GeminiIstegiAt(soru));
    }

  IEnumerator GeminiIstegiAt(string kullaniciMesaji)
    {
        string tamMesaj = sistemTalimati + " Oyuncu Sorusu: " + kullaniciMesaji;
        string jsonPayload = "{\"contents\": [{\"parts\": [{\"text\": \"" + tamMesaj.Replace("\"", "\\\"") + "\"}]}]}";
        string url = apiUrl + "?key=" + apiKey;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            sohbetMetni.text += "\n<color=green>Asistan:</color> Yaziyor...";
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Hata: " + request.error);
                sohbetMetni.text += $"\n<color=red>Hata:</color> {request.error}";
            }
            else
            {
                string gelenCevap = CevabiAyikla(request.downloadHandler.text);
                
                // Sadece temiz cevabı konsola yaz, JSON kalabalığı yapma
                Debug.Log("Gelen Cevap: " + gelenCevap);

                sohbetMetni.text = sohbetMetni.text.Replace("\n<color=green>Asistan:</color> Yaziyor...", "");
                sohbetMetni.text += $"\n<color=green>Asistan:</color> {gelenCevap}";
                
                Canvas.ForceUpdateCanvases();
                
            }
        }
    }
    

    string CevabiAyikla(string jsonVerisi)
    {
        // 1. "text": " kısmını bulup başlangıcı yakalıyoruz
        string anahtar = "\"text\": \"";
        int baslangic = jsonVerisi.IndexOf(anahtar);
        if (baslangic == -1) return "Cevap anlaşılamadı.";
        
        baslangic += anahtar.Length; // İmleci asıl yazının başına getir

        // 2. Bitiş tırnağını akıllıca arıyoruz
        // (Basit IndexOf yerine, içindeki kaçış karakterli tırnakları \" atlamak için döngü kullanıyoruz)
        int bitis = -1;
        for (int i = baslangic; i < jsonVerisi.Length; i++)
        {
            if (jsonVerisi[i] == '"') // Bir tırnak bulduk
            {
                // Eğer bu tırnağın önünde \ işareti YOKSA, bu gerçek bitiş tırnağıdır.
                if (jsonVerisi[i - 1] != '\\') 
                {
                    bitis = i;
                    break;
                }
            }
        }

        if (bitis == -1) return "Veri hatası: Son bulunamadı.";

        // 3. Aradaki temiz metni alıyoruz
        string hamCevap = jsonVerisi.Substring(baslangic, bitis - baslangic);
             
        // 4. Satır başlarını (\n) ve tırnakları düzeltip gönderiyoruz
        return hamCevap.Replace("\\n", "\n").Replace("\\\"", "\"");
    }
}
