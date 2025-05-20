using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using DG.Tweening;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using SocketIOClient.Transport;
using UnityEngine.UI;
using System.Text;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;

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
    static public string url;
    static public string port;
    private string serverUrl = "http://cssv00.iptime.org:5001/";
    private SocketIOUnity socket;

    private string currentLabel = "Waiting...";
    private string lastLabel = "";
    private Queue<Action> mainThreadActions = new Queue<Action>();

    [SerializeField] private UIManager uiManager;
    void Start()
    {
        // ���⿡ PlayerPrefs ���� �ٽ� �ε��ؼ� static ������ �ݿ�
        url = PlayerPrefs.GetString("ServerURL", "cssv00.iptime.org");
        port = PlayerPrefs.GetString("ServerPort", "5001");

        serverUrl = "http://" + url + ":" + port;
        Debug.Log(" ���� ���� �ּ�: " + serverUrl);

        StartMicrophone();
        ConnectSocket();
        StartCoroutine(SendLoop());
    }

    void Update()
    {
        while (mainThreadActions.Count > 0)
            mainThreadActions.Dequeue().Invoke();

        //if (currentLabel != lastLabel && labelText != null)
        //{
        //    labelText.text = $"Label: {currentLabel}";
        //    AnimateText(labelText);
        //    AnimatePanel();
        //    lastLabel = currentLabel;
        //}
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
        serverUrl = "http://" + url + ":" + port;

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

            if (data.TryGetValue("label", out string label) && data.TryGetValue("category", out string category))
            {
                Debug.Log("�������� ���� ��: " + label + ", �з� ��ȣ : " + category);

                //mainThreadActions.Enqueue(() => currentLabel = label);
                mainThreadActions.Enqueue(() => uiManager.RequestLabel(label, (LabelType)(category[0] - '0')));

                if (category == "1") 
                {
                    mainThreadActions.Enqueue(() => uiManager.RequestWarning(label));
                }
                else if (category == "2")
                {
                    mainThreadActions.Enqueue(() => StartCoroutine(WS_CaptureWebcamImageAndSend()));
                }
            }
        });

        socket.OnUnityThread("gpt_response", response =>
        {
            var json = response.GetValue<string>();
            mainThreadActions.Enqueue(() => ShowOpenAIResponse(json));
        });

        socket.Connect();
    }

    IEnumerator SendLoop()
    {
        float overlapInterval = 0.5f;  // �����̵� �ֱ�
        int sampleCount = sampleRate * 1;  // 1�� �з� �����̽�

        while (true)
        {
            yield return new WaitForSeconds(overlapInterval);

            if (micInitialized && socket != null && socket.Connected)
            {
                float[] samples = GetSlidingAudioSamples(sampleCount);
                socket.Emit("audio", samples);
                Debug.Log($"�����̵� ����� ���۵�: {samples.Length}�� ����");
            }
        }
    }

    float[] GetSlidingAudioSamples(int sampleCount)
    {
        int micPos = Microphone.GetPosition(micDevice);
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


    //public TextMeshProUGUI labelText;
    //public RectTransform panelTransform;
    //public CanvasGroup panelGroup;

    //void AnimateText(TextMeshProUGUI text)
    //{
    //    if (text == null) return;
    //    text.transform.DOKill();
    //    text.transform.localScale = Vector3.one;
    //    text.transform.DOScale(1.2f, 0.15f).SetEase(Ease.OutBack).SetLoops(2, LoopType.Yoyo);
    //    Color originalColor = text.color;
    //    text.DOColor(Color.yellow, 0.1f).OnComplete(() => text.DOColor(originalColor, 0.2f));
    //}

    //void AnimatePanel()
    //{
    //    if (panelTransform == null) return;
    //    panelTransform.DOKill();
    //    panelTransform.localScale = Vector3.one;
    //    panelTransform.DOScale(1.05f, 0.15f).SetEase(Ease.OutBack).SetLoops(2, LoopType.Yoyo);
    //}

    public RawImage targetRawImage;

    public void OnCaptureButtonPressed()
    {
        serverUrl = "http://" + url + ":" + port;
        StartCoroutine(WS_CaptureWebcamImageAndSend());
    }

    IEnumerator WS_CaptureWebcamImageAndSend()
    {
        while (FitToScreen.webcamTexture == null || FitToScreen.webcamTexture.width <= 16)
            yield return null;

        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(FitToScreen.webcamTexture.width, FitToScreen.webcamTexture.height, TextureFormat.RGB24, false);
        tex.SetPixels(FitToScreen.webcamTexture.GetPixels());
        tex.Apply();

        string base64Image = Convert.ToBase64String(tex.EncodeToPNG());

        Dictionary<string, string> payload = new Dictionary<string, string>()
        {
            { "image", base64Image },
            { "label", currentLabel }
        };

        if (socket != null && socket.Connected)
        {
            socket.Emit("screenshot", payload);
            Debug.Log("�̹��� + �� ���� �Ϸ�");
        }
        else
        {
            Debug.LogWarning("���� ���� �� ��");
        }

        Destroy(tex);
    }

    //public TMP_Text responseTextUI;

    void ShowOpenAIResponse(string json)
    {
        try
        {
            JObject parsed = JObject.Parse(json);
            string message = parsed["message"]?.ToString();

            if (!string.IsNullOrEmpty(message))
            {
                //responseTextUI.text = "OpenAI : " + message;
                uiManager.RequestDanger("", message);
                Debug.Log("OpenAI ����: " + message);
            }
            else
            {
                //responseTextUI.text = "���� �޽����� ��� ����";
                uiManager.RequestDanger("", "���� �޽����� ��� ����");
                Debug.LogWarning("������ ���������� message �ʵ尡 ����");
            }
        }
        catch (System.Exception e)
        {
            //responseTextUI.text = "OpenAI ���� �Ľ� ����";
            uiManager.RequestDanger("", "OpenAI ���� �Ľ� ����");
            Debug.LogWarning("OpenAI JSON �Ľ� ����: " + e.Message);
        }
    }
}
