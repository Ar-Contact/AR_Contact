using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class RenkDedektoru : MonoBehaviour
{
    [Header("Yönetici")]
    public MatchMaker matchMakerScripti;

    [Header("UI")]
    public RawImage kameraEkrani;
    public Image onizlemeKutusu;
    public Text durumMetni;

    [Header("Ayarlar")]
    [Range(1, 20)] public int atlamaOrani = 5;
    [Range(0.1f, 1f)] public float minDoygunluk = 0.30f;
    [Range(0.05f, 1f)] public float minParlaklik = 0.20f;

    private WebCamTexture kamera;
    private bool kameraAcikMi = false;
    private string anlikBaskinRenkAd = "Belirsiz";
    private AspectRatioFitter fit;
    private int sonGen, sonYuk, sonAci;

    void OnEnable() { StartCoroutine(KamerayiBaslat()); }

    void OnDisable()
    {
        if (kamera != null) { kamera.Stop(); kamera = null; }
        kameraAcikMi = false;
    }

    IEnumerator KamerayiBaslat()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.Camera));
        }
#endif
        WebCamDevice[] cihazlar = WebCamTexture.devices;
        if (cihazlar.Length == 0) { EkranaYaz("Kamera Yok!"); yield break; }

        string arkaKamera = cihazlar[0].name;
        foreach (var c in cihazlar) { if (!c.isFrontFacing) { arkaKamera = c.name; break; } }

        kamera = new WebCamTexture(arkaKamera, 640, 480, 30);
        kameraEkrani.texture = kamera;
        kamera.Play();
        yield return new WaitUntil(() => kamera.width > 100);

        kameraAcikMi = true;
        fit = kameraEkrani.GetComponent<AspectRatioFitter>();
        if (fit == null) fit = kameraEkrani.gameObject.AddComponent<AspectRatioFitter>();
        EkraniDuzelt();
        EkranaYaz("Renk aranıyor...");
    }

    void Update()
    {
        if (!kameraAcikMi || kamera == null) return;
        if (kamera.width != sonGen || kamera.height != sonYuk || kamera.videoRotationAngle != sonAci) EkraniDuzelt();
        RenkAlgila();
    }

    void EkraniDuzelt()
    {
        int aci = -kamera.videoRotationAngle;
        kameraEkrani.rectTransform.localEulerAngles = new Vector3(0, 0, aci);
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = (float)kamera.width / kamera.height;
        kameraEkrani.rectTransform.localScale = new Vector3(1, kamera.videoVerticallyMirrored ? -1 : 1, 1);
        sonGen = kamera.width; sonYuk = kamera.height; sonAci = kamera.videoRotationAngle;
    }

    void RenkAlgila()
    {
        if (kamera == null) return;
        Color32[] p = kamera.GetPixels32();
        if (p.Length == 0) return;
        float r = 0, g = 0, b = 0, sayac = 0;

        for (int i = 0; i < p.Length; i += atlamaOrani * 10)
        {
            Color c = p[i];
            Color.RGBToHSV(c, out float h, out float s, out float v);
            if (v < minParlaklik) continue;
            r += c.r; g += c.g; b += c.b; sayac++;
        }

        if (sayac < 50) { anlikBaskinRenkAd = "Belirsiz"; return; }

        float H, S, V;
        Color.RGBToHSV(new Color(r / sayac, g / sayac, b / sayac), out H, out S, out V);

        if ((H <= 0.04f || H >= 0.96f) && S > 0.6f) anlikBaskinRenkAd = "Kirmizi";
        else if (H > 0.55f && H <= 0.72f && S > 0.4f) anlikBaskinRenkAd = "Mavi";
        else if (V < 0.2f && S < 0.25f) anlikBaskinRenkAd = "Siyah";
        else anlikBaskinRenkAd = "Belirsiz";

        if (onizlemeKutusu != null) onizlemeKutusu.color = new Color(r / sayac, g / sayac, b / sayac);
        EkranaYaz("Algılanan: " + anlikBaskinRenkAd);
    }

    // --- BURAYA LOGLARI EKLEDİM ---
    public void OnaylaVeTamamla()
    {
        Debug.Log($"[RENK DEDEKTORU] Onayla Butonuna Basıldı. Şu anki algılanan: {anlikBaskinRenkAd}");

        if (anlikBaskinRenkAd == "Belirsiz")
        {
            EkranaYaz("Renk bulunamadı! Net göster.");
            return;
        }

        // 1. Veriyi kaydet
        GlobalVeri.SecilenRenk = anlikBaskinRenkAd;

        // KONTROL LOGU
        Debug.Log($"[RENK DEDEKTORU] GlobalVeri'ye KAYDEDİLEN Renk: {GlobalVeri.SecilenRenk}");

        // 2. Patronu çağır
        if (matchMakerScripti != null)
        {
            matchMakerScripti.RenkSecimiTamamlandi();
        }
        else
        {
            Debug.LogError("[RENK DEDEKTORU] MatchMaker Scripti atanmamış! Inspector'dan sürükle.");
        }
    }

    void EkranaYaz(string m) { if (durumMetni != null) durumMetni.text = m; }
}