using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LabelUIElement : MonoBehaviour
{
    [SerializeField] private GameObject labelText;
    [SerializeField] private GameObject warningIcon;

    [SerializeField] private List<Sprite> icons;

    public string label { get; private set; }
    private float lifeTime;
    private UIManager uiManager;
    public bool isCallAI { get; private set; }

    private List<Color> colors = new List<Color>
    {
        Color.white,
        UIManager.warningColor,
        UIManager.dangerColor
    };
    public void InitLabelUIElement(string text, LabelType type, UIManager uim) 
    {
        labelText.GetComponent<TextMeshProUGUI>().text = text;
        labelText.GetComponent<TextMeshProUGUI>().color = colors[(int)type];
        warningIcon.GetComponent<Image>().sprite = icons[(int)type];

        label = text;
        lifeTime = 5.0f;
        uiManager = uim;

        isCallAI = false;
    }
    private void Update()
    {
        if (lifeTime > 0) 
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0) 
            {
                uiManager.DestroyLabel(this.gameObject);
            }
        }
    }
    public void AddLifeTime() 
    {
        if (lifeTime < 30.0f)
        {
            lifeTime += 5.0f;
        }
    }

    public void CallAI() 
    {
        isCallAI = true;
    }
}
