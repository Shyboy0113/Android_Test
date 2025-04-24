using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using SocketIOClient.Transport;

public class SocketIOUnityClient2 : MonoBehaviour
{
    [Header("녹음 설정")]
    public int sampleRate = 16000;
    public int windowSeconds = 1;

    private AudioClip micClip;
    private string micDevice;
    private int recordLength = 10;
    private bool micInitialized = false;

    [Header("Socket.IO")]
    public string serverUrl = "http://localhost:5001";
    private SocketIOUnity socket;

    private string currentLabel = "Waiting...";
    private string lastLabel = "";

    private Queue<Action> mainThreadActions = new Queue<Action>();

    void Start()
    {
        StartMicrophone();
        ConnectSocket();
        StartCoroutine(SendLoop());
    }

    void OnDestroy()
    {
        if (socket != null && socket.Connected) socket.Disconnect();
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

    void ConnectSocket()
    {
        socket = new SocketIOUnity(new Uri(serverUrl), new SocketIOOptions
        {
            EIO = 4,
            Transport = TransportProtocol.WebSocket
        });

        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) => Debug.Log("Socket.IO 연결됨");

        socket.OnUnityThread("data_response", response =>
        {
            var obj = response.GetValue<Dictionary<string, string>>();
            if (obj.TryGetValue("message", out string fullMessage))
            {
                // 예: "label = Speech" → "Speech" 만 추출
                string[] split = fullMessage.Split(new[] { "label = " }, StringSplitOptions.None);
                if (split.Length > 1)
                {
                    string label = split[1];
                    Debug.Log("[클라] 수신한 라벨: " + label);
                    mainThreadActions.Enqueue(() => currentLabel = label);
                }
            }
        });


        socket.Connect();
    }

    IEnumerator SendLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(windowSeconds);

            if (micInitialized && socket != null && socket.Connected)
            {
                float[] audioSamples = GetRecentAudioSamples();

                byte[] floatBytes = new byte[audioSamples.Length * 4];
                Buffer.BlockCopy(audioSamples, 0, floatBytes, 0, floatBytes.Length);
                string base64 = Convert.ToBase64String(floatBytes);

                var msg = new
                {
                    payload = base64,
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                socket.Emit("audio", json);
                Debug.Log($"{windowSeconds}초 분량 오디오 Socket.IO 전송됨 ({floatBytes.Length} bytes)");
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

        if (currentLabel != lastLabel && labelText != null)
        {
            labelText.text = $"Label: {currentLabel}";
            AnimateText(labelText);
            AnimatePanel();
            lastLabel = currentLabel;

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

    #region UI 애니메이션
    public TextMeshProUGUI labelText;
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

    #region 기타 기능
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
        if (socket != null && socket.Connected)
        {
            socket.Disconnect();
        }

        buttonEvent.QuitButton();
    }
    #endregion
}
