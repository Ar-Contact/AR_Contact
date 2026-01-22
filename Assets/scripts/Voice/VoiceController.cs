using UnityEngine;
using Photon.Voice.Unity;
using UnityEngine.UI;

public class VoiceController : MonoBehaviour
{
    [Header("Photon Voice Components")]
    public Recorder voiceRecorder;

    [Header("Mic UI (Kendi Sesin)")]
    public Image micButtonImage;
    public Sprite micOnSprite;
    public Sprite micOffSprite;

    [Header("Speaker UI (Karþý Taraf)")]
    public Image speakerButtonImage;
    public Sprite speakerOnSprite;
    public Sprite speakerOffSprite;

    private bool isMicOn = true;
    private bool isSpeakerOn = true;

    void Start()
    {
        // Baþlangýçta her þey açýk
        if (voiceRecorder != null) voiceRecorder.TransmitEnabled = true;
        AudioListener.pause = false;
        UpdateUI();
    }

    // Kendi mikrofonunu aç/kapat
    public void ToggleMicrophone()
    {
        if (voiceRecorder == null) return;
        isMicOn = !isMicOn;
        voiceRecorder.TransmitEnabled = isMicOn;
        UpdateUI();
    }

    // Karþý tarafýn sesini (tüm oyun sesini) aç/kapat
    public void ToggleSpeaker()
    {
        isSpeakerOn = !isSpeakerOn;

        // AudioListener.pause tüm sahne sesini keser. 
        // Eðer sadece karþý tarafý susturup oyun sesini (kýlýç sesi vs.) duymak istersen 
        // AudioListener.volume = isSpeakerOn ? 1f : 0f; kullanabilirsin.
        AudioListener.pause = !isSpeakerOn;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (micButtonImage != null)
            micButtonImage.sprite = isMicOn ? micOnSprite : micOffSprite;

        if (speakerButtonImage != null)
            speakerButtonImage.sprite = isSpeakerOn ? speakerOnSprite : speakerOffSprite;
    }
}