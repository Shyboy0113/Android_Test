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
    [Header("녹음 설정")]
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
        // 여기에 PlayerPrefs 값을 다시 로드해서 static 변수에 반영
        url = PlayerPrefs.GetString("ServerURL", "cssv00.iptime.org");
        port = PlayerPrefs.GetString("ServerPort", "5001");

        serverUrl = "http://" + url + ":" + port;
        Debug.Log(" 최종 서버 주소: " + serverUrl);

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
        serverUrl = "http://" + url + ":" + port;

        socket = new SocketIOUnity(new Uri(serverUrl), new SocketIOOptions
        {
            EIO = 4,
            Transport = TransportProtocol.WebSocket
        });

        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) => Debug.Log("Socket.IO 연결됨");


        socket.OnUnityThread("data_response", response =>
        {
            var data = response.GetValue<Dictionary<string, string>>();

            if (data.TryGetValue("label", out string label) && data.TryGetValue("category", out string category))
            {
                Debug.Log("서버에서 받은 라벨: " + label + ", 분류 번호 : " + category);

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
        float overlapInterval = 0.5f;  // 슬라이딩 주기
        int sampleCount = sampleRate * 1;  // 1초 분량 슬라이스

        while (true)
        {
            yield return new WaitForSeconds(overlapInterval);

            if (micInitialized && socket != null && socket.Connected)
            {
                float[] samples = GetSlidingAudioSamples(sampleCount);
                socket.Emit("audio", samples);
                Debug.Log($"슬라이딩 오디오 전송됨: {samples.Length}개 샘플");
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
            Debug.Log("이미지 + 라벨 전송 완료");
        }
        else
        {
            Debug.LogWarning("소켓 연결 안 됨");
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
                Debug.Log("OpenAI 응답: " + message);
            }
            else
            {
                //responseTextUI.text = "응답 메시지가 비어 있음";
                uiManager.RequestDanger("", "응답 메시지가 비어 있음");
                Debug.LogWarning("응답은 성공했으나 message 필드가 없음");
            }
        }
        catch (System.Exception e)
        {
            //responseTextUI.text = "OpenAI 응답 파싱 오류";
            uiManager.RequestDanger("", "OpenAI 응답 파싱 오류");
            Debug.LogWarning("OpenAI JSON 파싱 실패: " + e.Message);
        }
    }
}
