using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedbackUI : MonoBehaviour
{
    [SerializeField] private GameObject dangerText;
    [SerializeField] private GameObject dangerDesc;
    private const int STRING_MAX_LENGTH = 27;

    [SerializeField] private GameObject thumbsUp;
    [SerializeField] private GameObject thumbsDown;
    private bool isPressed;

    [SerializeField] private GameObject timer;
    private float leftTime = 0;
    private const float FULL_TIME = 10.0f;

    public void SetFeedbackUI(string dt, string dd)
    {
        if (dt.Length > STRING_MAX_LENGTH) dt = dt.Substring(STRING_MAX_LENGTH) + "...";
        if (dd.Length > STRING_MAX_LENGTH) dd = dd.Substring(STRING_MAX_LENGTH) + "...";
        dangerText.GetComponent<TextMeshProUGUI>().text = dt;
        dangerDesc.GetComponent<TextMeshProUGUI>().text = dd;
        leftTime = FULL_TIME;

        isPressed = false;
        thumbsUp.GetComponent<Animator>().Rebind();
        thumbsDown.GetComponent<Animator>().Rebind();
    }

    private void Update()
    {
        if (leftTime > 0) 
        {
            leftTime -= Time.deltaTime;
            if (leftTime <= 0) 
            {
                leftTime = 0;
                thumbsUp.GetComponent<Animator>().SetTrigger("Reset");
                thumbsDown.GetComponent<Animator>().SetTrigger("Reset");
                gameObject.SetActive(false);
            }
            timer.GetComponent<Slider>().value = leftTime / FULL_TIME;
        }
    }

    public void OnClickThumb(bool isUp)
    {
        if (isPressed) return;
        isPressed = true;

        Animator a = thumbsUp.GetComponent<Animator>();
        Animator b = thumbsDown.GetComponent<Animator>();
        if (!isUp)
        {
            a = thumbsDown.GetComponent<Animator>();
            b = thumbsUp.GetComponent<Animator>();
        }
        a.GetComponent<Animator>().SetTrigger("Push");
        b.GetComponent<Animator>().SetTrigger("NotPush");


    }
}
