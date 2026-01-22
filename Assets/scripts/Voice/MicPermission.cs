using UnityEngine;
using UnityEngine.Android; // Sadece Android için

public class MicPermission : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID
        // Ýzin daha önce verilmemiþse iste
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }
}