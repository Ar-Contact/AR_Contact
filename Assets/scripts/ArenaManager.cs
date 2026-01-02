using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    public GameObject dragBluePanel;
    public GameObject dragRedPanel;
    public GameObject blueCamera;
    public GameObject redCamera;

    void Start()
    {
        dragBluePanel.SetActive(false);
        dragRedPanel.SetActive(false);
        blueCamera.SetActive(false);
        redCamera.SetActive(false);

        if (PlayerSession.Team == "Blue")
        {
            dragBluePanel.SetActive(true);
            blueCamera.SetActive(true);
        }
        else if (PlayerSession.Team == "Red")
        {
            dragRedPanel.SetActive(true);
            redCamera.SetActive(true);
        }
        else
        {
            Debug.LogError("Takým bilgisi yok!");
        }
    }
}
