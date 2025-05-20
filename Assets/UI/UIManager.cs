using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum LabelType 
{
    Normal = 0,
    Warning = 1,
    Danger = 2
};
public class UIManager : MonoBehaviour
{
    public static Color32 warningColor = new Color32(255, 212, 96, 255);
    public static Color32 dangerColor = new Color32(234, 84, 85, 255);

    [SerializeField] private GameObject labelUI;
    [SerializeField] private GameObject labelUIElementPrefab;

    [SerializeField] private GameObject warningLabelText;
    [SerializeField] private GameObject warningDescText;
    private void Awake()
    {
        warningLabelText.SetActive(false);
        warningDescText.SetActive(false);
    }
    public void RequestLabel(string label, LabelType type) 
    {
        for (int i = 0; i < labelUI.transform.childCount; i++) 
        {
            if (labelUI.transform.GetChild(i).GetComponent<LabelUIElement>().label == label) 
            {
                labelUI.transform.GetChild(i).GetComponent<LabelUIElement>().AddLifeTime();
                return;
            }
        }

        var ui = Instantiate(labelUIElementPrefab, labelUI.transform);
        ui.GetComponent<LabelUIElement>().InitLabelUIElement(label, type, this);
    }

    public void DestroyLabel(GameObject element) 
    {
        Destroy(element);
    }

    public void RequestWarning(string label)
    {
        if (isDanger) return;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = warningColor;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = label;
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";

        warningLabelText.SetActive(true);
        warningDescText.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(WarningTextCooltime(5.0f));
    }
    private bool isDanger = false;
    public void RequestDanger(string label, string suggestion)
    {
        isDanger = true;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = dangerColor;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = label;
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = suggestion;

        warningLabelText.SetActive(true);
        warningDescText.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(WarningTextCooltime(10.0f));
    }
    IEnumerator WarningTextCooltime(float cooltime) 
    {
        yield return new WaitForSeconds(cooltime);
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
        warningLabelText.SetActive(false);
        warningDescText.SetActive(false);
        isDanger = false;
    }
}
