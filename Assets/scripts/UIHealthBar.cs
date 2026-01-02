using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    public Transform target;
    public Image foreGround;
    public Vector3 offset;

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (cam == null) return;

        transform.position = cam.WorldToScreenPoint(target.position + offset);
    }

    public void SetHealthBarPertencage(float percentage)
    {
        if (foreGround == null) return;

        percentage = Mathf.Clamp01(percentage);

        float parentWidth = ((RectTransform)transform).rect.width;
        foreGround.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            parentWidth * percentage
        );
    }
}
