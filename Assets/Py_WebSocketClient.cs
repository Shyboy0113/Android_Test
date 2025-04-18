using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using TMPro;
using DG.Tweening;


/*
[Serializable]
public class AudioAnalysisMessage
{
    public string label;
    public float decibel;
}
*/

public class Py_WebSocketClient : MonoBehaviour
{    

    public string serverUrl = "ws://192.168.0.104:5001"; // ← 본인 IP로 수정
    private WebSocket ws;

    private string currentLabel = "Waiting...";
    private float currentDecibel = 0f;
    private string lastLabel = "";
    private float lastDecibel = -999f;

    private Queue<Action> mainThreadActions = new Queue<Action>();

    void Start()
    {
        ConnectWebSocket();
    }

    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("[WebSocket] Connected.");
        };

        ws.OnMessage += (sender, e) =>
        {
            try
            {
                string msg = e.Data;
                Debug.Log("[WebSocket] Raw message: " + msg);

                mainThreadActions.Enqueue(() =>
                {
                    try
                    {
                        var data = JsonUtility.FromJson<AudioAnalysisMessage>(msg);
                        currentLabel = data.label;
                        currentDecibel = data.decibel;
                    }
                    catch (Exception parseEx)
                    {
                        Debug.LogError("[WebSocket] JSON parse error: " + parseEx.Message);
                    }
                });
            }
            catch (Exception outerEx)
            {
                Debug.LogError("[WebSocket] Outer OnMessage error: " + outerEx.Message);
            }
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError("[WebSocket] Error: " + e.Message);
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.LogWarning("[WebSocket] Disconnected. Retrying...");
            Invoke(nameof(ConnectWebSocket), 2f);
        };

        ws.ConnectAsync();
    }

    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            mainThreadActions.Dequeue().Invoke();
        }

        bool changed = false;

        if (currentLabel != lastLabel && labelText != null)
        {
            labelText.text = $"Label: {currentLabel}";
            AnimateText(labelText);
            AnimatePanel();
            lastLabel = currentLabel;
            changed = true;
        }

        // DoTween 데시벨 텍스트 값 변환
        if (Mathf.Abs(currentDecibel - lastDecibel) > 0.1f && decibelText != null)
        {
            decibelText.text = $"Decibel: {currentDecibel:F1}";
            AnimateText(decibelText);
            AnimatePanel();
            lastDecibel = currentDecibel;
            changed = true;
        }

        // DoTween 처음 데이터가 들어온 경우
        if (changed)
        {
            if (!hasEverUpdated)
            {
                hasEverUpdated = true;
                panelGroup.alpha = 0f; // 혹시 초기 설정 안 돼 있다면 강제
                panelGroup.DOFade(1f, 0.3f); // 등장 연출
            }
            else if (isFaded)
            {
                panelGroup.DOFade(1f, 0.2f);
            }

            lastChangeTime = Time.time;
            isFaded = false;
        }

        // DoTween 최초 수신 이후부터만 fade-out 로직 적용
        if (hasEverUpdated && !isFaded && Time.time - lastChangeTime >= fadeDelay)
        {
            panelGroup.DOFade(0f, 0.2f);
            isFaded = true;
        }
    }


    #region DoTween활용
    // label and decibel Text
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI decibelText;

    public RectTransform panelTransform; // UI Panel (CanvasGroup 또는 빈 Panel)

    public CanvasGroup panelGroup; // ← Panel에 붙은 CanvasGroup

    private bool hasEverUpdated = false; // 처음 변화 발생 여부

    private float lastChangeTime = 0f;
    private float fadeDelay = 3f;
    private bool isFaded = false;

    void AnimateText(TextMeshProUGUI text)
    {
        if (text == null) return;

        text.transform.DOKill();
        text.transform.localScale = Vector3.one;

        text.transform
            .DOScale(1.2f, 0.15f)
            .SetEase(Ease.OutBack)
            .SetLoops(2, LoopType.Yoyo);

        Color originalColor = text.color;
        text.DOColor(Color.yellow, 0.1f)
            .OnComplete(() => text.DOColor(originalColor, 0.2f));
    }
    void AnimatePanel()
    {
        if (panelTransform == null) return;

        panelTransform.DOKill(); // 중복 방지
        panelTransform.localScale = Vector3.one; // 초기화
        panelTransform
            .DOScale(1.05f, 0.15f)
            .SetEase(Ease.OutBack)
            .SetLoops(2, LoopType.Yoyo);
    }
    #endregion

    #region IP주소불러오기
    
    public ButtonEvent buttonEvent;
    private string _currentIP;
    public TMP_Text IPText;

    public void Android_GetCurrentIP()
    {
        _currentIP = buttonEvent.GetLocalIPAddress();
        IPText.text = "Current IP : " + _currentIP;
    }

    public void Android_QuitApplication()
    {        
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }

        buttonEvent.QuitButton();

    }

    #endregion

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }
}
