using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using SocketIOClient.Transport;

public class SocketIOUnityClient : MonoBehaviour
{
    [Header("���� ����")]
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
        if (micInitialized) Microphone.End(null);
    }

    void StartMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("����ũ�� ã�� �� �����ϴ�.");
            return;
        }

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, recordLength, sampleRate);
        micInitialized = true;
        Debug.Log("����ũ ���۵�: " + micDevice);
    }

    void ConnectSocket()
    {
        socket = new SocketIOUnity(new Uri(serverUrl), new SocketIOOptions
        {
            EIO = 4,
            Transport = TransportProtocol.WebSocket
        });

        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) => Debug.Log("Socket.IO �����");

        socket.OnUnityThread("data_response", response =>
        {
            var data = response.GetValue<Dictionary<string, string>>();
            if (data.TryGetValue("label", out string label))
            {
                Debug.Log("�������� ���� ��: " + label);
                mainThreadActions.Enqueue(() => currentLabel = label);
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
                socket.Emit("audio", audioSamples); // float[] �״�� ���� (SocketIOUnity�� JSON �迭�� ����ȭ)
                Debug.Log("����� ������ ���۵�: " + audioSamples.Length + "�� ����");
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
        }
    }

    #region UI �ִϸ��̼�
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
}
