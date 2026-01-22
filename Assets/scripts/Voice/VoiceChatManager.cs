using UnityEngine;
using Photon.Voice.Unity;
using UnityEngine.UI;

public class VoiceChatManager : MonoBehaviour
{
    public Recorder voiceRecorder; // Müfettiþten Recorder'ý buraya sürükle
    private bool isMuted = true;

    public void ToggleVoice()
    {
        isMuted = !isMuted;
        voiceRecorder.TransmitEnabled = !isMuted; // Sesi gönderip göndermeyi açar

        // Görsel geri bildirim (Opsiyonel)
        Debug.Log(isMuted ? "Mikrofon Kapalý" : "Mikrofon Açýk");
    }
}