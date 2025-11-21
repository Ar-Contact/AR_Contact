using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    public Transform target;
    public Image foreGround;
    public Image BackGround;
    public Vector3 offset;

    void LateUpdate()
    {
        transform.position = Camera.main.WorldToScreenPoint(target.position + offset);
    }
    public void SetHealthBarPertencage(float pertencage)
    {
        float parentWidth = GetComponent<RectTransform>().rect.width;
        float width = parentWidth * pertencage;
        foreGround.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width); 
    }
}
