using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    public static Color32 toggledColor = new Color32(234, 156, 0, 102);
    public static Color32 untoggledColor = new Color32(0, 0, 0, 102);

    [SerializeField] GameObject settingButton;
    [SerializeField] List<GameObject> typeButtons;
    private int[] targetScales = { 0, 0, 0, 0, 0, 0 };

    private const float SCALE_SPEED = 2;
    void Update()
    {
        for (int i = 0; i < typeButtons.Count; i++)
        {
            if (targetScales[i] == 1)
            {
                if (typeButtons[i].GetComponent<RectTransform>().localScale.x < targetScales[i])
                {
                    typeButtons[i].GetComponent<RectTransform>().localScale += Vector3.one * Time.deltaTime * SCALE_SPEED;
                }
                else 
                {
                    typeButtons[i].GetComponent<RectTransform>().localScale = Vector3.one;
                }
            }

            if (targetScales[i] == 0)
            {
                if (typeButtons[i].GetComponent<RectTransform>().localScale.x > targetScales[i])
                {
                    typeButtons[i].GetComponent<RectTransform>().localScale -= Vector3.one * Time.deltaTime * SCALE_SPEED;
                }
                else
                {
                    typeButtons[i].GetComponent<RectTransform>().localScale = Vector3.zero;
                }
            }

            Color c = typeButtons[i].GetComponent<Image>().color;
            if (typeButtons[i].GetComponent<RectTransform>().localScale.x < 0.2f)
            {
                c.a = 0;
            }
            else 
            {
                c.a = toggledColor.a / 255.0f;
            }
            typeButtons[i].GetComponent<Image>().color = c;
        }
    }

    private bool isScaling = false;
    public void OnClickSettingBtn() 
    {
        if (isScaling) return;
        int scale = 0;
        if (settingButton.GetComponent<Toggle>().isOn) scale = 1;
        StartCoroutine(DelayedScaleSetting(scale));
    }
    IEnumerator DelayedScaleSetting(int scale)
    {
        isScaling = true;
        if (scale == 1)
        {
            for (int i = 0; i < typeButtons.Count; i++)
            {
                targetScales[i] = scale;
                yield return new WaitForSeconds(0.05f);
            }
        }
        else
        {
            for (int i = typeButtons.Count - 1; i >= 0; i--)
            {
                targetScales[i] = scale;
                yield return new WaitForSeconds(0.05f);
            }
        }
        isScaling = false;
    }

    public void OnToggleBtn(int idx) 
    {
        Color c;
        if (typeButtons[idx].GetComponent<Toggle>().isOn)
        {
            c = toggledColor;
        }
        else
        {
            c = untoggledColor;
        }
        typeButtons[idx].GetComponent<Image>().color = c;
    }
}
