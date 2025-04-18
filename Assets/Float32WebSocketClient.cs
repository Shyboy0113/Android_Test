using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using TMPro;
using DG.Tweening;


[Serializable]
public class AudioAnalysisMessage
{
    public string label;
    public float decibel;
}


public class Float32WebSocketClient : MonoBehaviour
{
    [Header("녹음 설정")]
    public int sampleRate = 16000;
    public int windowSeconds = 1;

    private AudioClip micClip;
    private string micDevice;
    private int recordLength = 10;
    private bool micInitialized = false;

    [Header("WebSocket")]
    public string serverUrl = "ws://localhost:5001";
    private WebSocket ws;

    // 결과 표시용 변수
    private string currentLabel = "Waiting...";
    private float currentDecibel = 0f;
    private string lastLabel = "";
    private float lastDecibel = -999f;

    private Queue<Action> mainThreadActions = new Queue<Action>();

    void Start()
    {
        StartMicrophone();
        ConnectWebSocket();
        StartCoroutine(SendLoop());
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive) ws.Close();
        Microphone.End(null);
    }

    void StartMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("마이크를 찾을 수 없습니다.");
            return;
        }

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, recordLength, sampleRate);
        micInitialized = true;

        Debug.Log("마이크 시작됨: " + micDevice);
    }

    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (sender, e) => Debug.Log("WebSocket 연결됨");

        ws.OnMessage += (sender, e) =>
        {
            string msg = e.Data;
            Debug.Log("서버 응답 수신: " + msg);

            mainThreadActions.Enqueue(() =>
            {
                try
                {
                    var data = JsonUtility.FromJson<AudioAnalysisMessage>(msg);
                    currentLabel = data.label;
                    currentDecibel = data.decibel;
                }
                catch (Exception ex)
                {
                    Debug.LogError("JSON 파싱 에러: " + ex.Message);
                }
            });
        };

        ws.OnError += (sender, e) => Debug.LogError("WebSocket 에러: " + e.Message);
        ws.OnClose += (sender, e) => Debug.Log("WebSocket 연결 종료됨");

        ws.ConnectAsync();
    }

    IEnumerator SendLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(windowSeconds);

            if (micInitialized && ws != null && ws.IsAlive)
            {
                float[] audioSamples = GetRecentAudioSamples();
                byte[] floatBytes = new byte[audioSamples.Length * 4];
                Buffer.BlockCopy(audioSamples, 0, floatBytes, 0, floatBytes.Length);

                ws.Send(floatBytes);
                Debug.Log($"{windowSeconds}초 분량 오디오 전송됨 ({floatBytes.Length} bytes)");
            }
        }
    }

    float[] GetRecentAudioSamples()
    {
        int micPos = Microphone.GetPosition(micDevice);
        int sampleCount = sampleRate * windowSeconds;
        float[] samples = new float[sampleCount];

        int startPos = micPos - sampleCount;
        if (startPos < 0)
        {
            float[] part1 = new float[-startPos];
            float[] part2 = new float[sampleCount + startPos];
            micClip.GetData(part2, 0);
            micClip.GetData(part1, micClip.samples + startPos);
            Array.Copy(part1, 0, samples, 0, part1.Length);
            Array.Copy(part2, 0, samples, part1.Length, part2.Length);
        }
        else
        {
            micClip.GetData(samples, startPos);
        }

        return samples;
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

        if (Mathf.Abs(currentDecibel - lastDecibel) > 0.1f && decibelText != null)
        {
            decibelText.text = $"Decibel: {currentDecibel:F1}";
            AnimateText(decibelText);
            AnimatePanel();
            lastDecibel = currentDecibel;
            changed = true;
        }

        if (changed)
        {
            if (!hasEverUpdated)
            {
                hasEverUpdated = true;
                panelGroup.alpha = 0f;
                panelGroup.DOFade(1f, 0.3f);
            }
            else if (isFaded)
            {
                panelGroup.DOFade(1f, 0.2f);
            }

            lastChangeTime = Time.time;
            isFaded = false;
        }

        if (hasEverUpdated && !isFaded && Time.time - lastChangeTime >= fadeDelay)
        {
            panelGroup.DOFade(0f, 0.2f);
            isFaded = true;
        }
    }

    #region DoTween 활용

    public TextMeshProUGUI labelText;
    public TextMeshProUGUI decibelText;

    public RectTransform panelTransform;
    public CanvasGroup panelGroup;

    private bool hasEverUpdated = false;
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

        panelTransform.DOKill();
        panelTransform.localScale = Vector3.one;
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


}
