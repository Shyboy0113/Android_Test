using System;
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
    private bool isFadeOut = false;

    [SerializeField] private GameObject feedbackUI;
 
    private List<Tuple<string, string>> suggestionMessages;
    private void Awake()
    {
        warningLabelText.GetComponent<CanvasGroup>().alpha = 0;
        warningDescText.GetComponent<CanvasGroup>().alpha = 0;

        feedbackUI.SetActive(false);

        SetSuggestionMessage();
    }
    private void Update()
    {
        if (isFadeOut)
        {
            warningLabelText.GetComponent<CanvasGroup>().alpha -= Time.deltaTime * 2;
            warningDescText.GetComponent<CanvasGroup>().alpha -= Time.deltaTime * 2;
            if (warningLabelText.GetComponent<CanvasGroup>().alpha <= 0) isFadeOut = false;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            feedbackUI.SetActive(true);
            feedbackUI.GetComponent<FeedbackUI>().SetFeedbackUI("test", "test");
        }
    }
    private void SetSuggestionMessage()
    {
        var file = FileRead.Read("suggestion_message", out Dictionary<string, int> colInfo);
        if (file == null) return;

        suggestionMessages = new();
        for (int i = 0; i < file.Count; i++) 
        {
            Tuple<string, string> tuple = new(file[i][0], file[i][1]);
            suggestionMessages.Add(tuple);
        }
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
        //element.transform.SetParent(this.transform);
        //element.transform.SetAsFirstSibling();
        element.GetComponent<LabelUIElement>().DestroyLabelUI();
    }

    public void RequestWarning(string label)
    {
        if (isDanger) return;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = warningColor;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "주의: " + label;
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = GetWarningText(label);

        isFadeOut = false;
        warningLabelText.GetComponent<CanvasGroup>().alpha = 1;
        warningDescText.GetComponent<CanvasGroup>().alpha = 1;

        StopAllCoroutines();
        StartCoroutine(WarningTextCooltime(5.0f, false));
    }
    private string GetWarningText(string label) 
    {
        foreach (var m in suggestionMessages) 
        {
            if (m.Item1 == label) return m.Item2;
        }
        return "등록되지 않은 라벨 정보입니다.";
    }
    private bool isDanger = false;
    public void RequestDanger(string label, string suggestion)
    {
        LabelUIElement dangerLabel = null;
        for (int i = 0; i < labelUI.transform.childCount; i++)
        {
            if (labelUI.transform.GetChild(i).GetComponent<LabelUIElement>().label == label)
            {
                dangerLabel = labelUI.transform.GetChild(i).GetComponent<LabelUIElement>();
                if(dangerLabel.isCallAI) return;
                break;
            }
        }
        dangerLabel.GetComponent<LabelUIElement>().CallAI();

        isDanger = true;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = dangerColor;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "경고: " + label;
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = suggestion;

        isFadeOut = false;
        warningLabelText.GetComponent<CanvasGroup>().alpha = 1;
        warningDescText.GetComponent<CanvasGroup>().alpha = 1;

        StopAllCoroutines();
        StartCoroutine(WarningTextCooltime(10.0f, true));
    }
    IEnumerator WarningTextCooltime(float cooltime, bool wasDanger) 
    {
        yield return new WaitForSeconds(cooltime);

        if (wasDanger)
        {
            feedbackUI.SetActive(true);
            feedbackUI.GetComponent<FeedbackUI>().SetFeedbackUI
                (
                warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text,
                warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text
                );
        }

        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
        warningLabelText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
        warningDescText.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";

        isFadeOut = true;
        isDanger = false;
    }
}
