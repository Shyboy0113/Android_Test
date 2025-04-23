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
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;


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
    static public string url ="localhost";
    static public string port = "5001";

    private string serverUrl = "http://localhost:5001";

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
            if (data.TryGetValue("label", out string label))
            {
                Debug.Log("서버에서 받은 라벨: " + label);
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
                socket.Emit("audio", audioSamples); // float[] 그대로 전송 (SocketIOUnity가 JSON 배열로 직렬화)
                Debug.Log("오디오 데이터 전송됨: " + audioSamples.Length + "개 샘플");
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

    public RawImage targetRawImage;  // Inspector에 RawImage 연결

    #region 안드로이드 스크린샷 저장 기능
    public void OnSaveButtonPressed()
    {
        StartCoroutine(SaveRawImageToGallery());
    }

    IEnumerator SaveRawImageToGallery()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    // WebCamTexture 준비 상태까지 대기
    while (FitToScreen.webcamTexture == null || FitToScreen.webcamTexture.width <= 16)
        yield return null;

    yield return new WaitForEndOfFrame();

    // WebCamTexture에서 픽셀을 직접 추출
    Texture2D tex = new Texture2D(FitToScreen.webcamTexture.width, FitToScreen.webcamTexture.height, TextureFormat.RGB24, false);
    tex.SetPixels(FitToScreen.webcamTexture.GetPixels());
    tex.Apply();

    string fileName = "screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
    string galleryPath = "/storage/emulated/0/Pictures/MyApp";
    string fullPath = Path.Combine(galleryPath, fileName);

    // 권한 체크
    if (AndroidVersion() >= 33)
    {
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_IMAGES"))
        {
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.READ_MEDIA_IMAGES");
            yield break;
        }
    }
    else
    {
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
            yield break;
        }
    }

    // 디렉토리 생성
    if (!Directory.Exists(galleryPath))
        Directory.CreateDirectory(galleryPath);

    File.WriteAllBytes(fullPath, tex.EncodeToPNG());
    Debug.Log("저장 완료: " + fullPath);

    // 갤러리에 등록
    using (AndroidJavaClass jc = new AndroidJavaClass("android.media.MediaScannerConnection"))
    using (AndroidJavaObject context = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                                   .GetStatic<AndroidJavaObject>("currentActivity"))
    {
        jc.CallStatic("scanFile", context, new string[] { fullPath }, null, null);
    }

    Destroy(tex);
#else
        Debug.LogWarning("에디터에서는 갤러리 저장이 지원되지 않습니다.");
        yield break;
#endif
    }
    int AndroidVersion()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    using (var versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
    {
        return versionClass.GetStatic<int>("SDK_INT");
    }
#else
        return -1;
#endif
    }



    #endregion

    #region 캡쳐해서 GPT로 보내기(갤러리에 저장 X)

    public void OnCaptureButtonPressed()
    {
        serverUrl = "http://" + url + ":" + port;

        StartCoroutine(HTTP_CaptureWebcamImageAndSend());
    }

    //HTTP로 보내기(코루틴)
    IEnumerator HTTP_CaptureWebcamImageAndSend()
    {
        while (FitToScreen.webcamTexture == null || FitToScreen.webcamTexture.width <= 16)
            yield return null;

        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(FitToScreen.webcamTexture.width, FitToScreen.webcamTexture.height, TextureFormat.RGB24, false);
        tex.SetPixels(FitToScreen.webcamTexture.GetPixels());
        tex.Apply();

        byte[] imageBytes = tex.EncodeToPNG();
        string base64Image = Convert.ToBase64String(imageBytes);
        Destroy(tex);

        // 질문 텍스트
        string promptText = $"'{currentLabel}'이라는 소리가 감지된 상황에서 찍힌 이미지입니다. 가장 안전한 방향으로 대피하라는 안내 문구를 생성해주세요.";

        // JSON 데이터 구성
        Dictionary<string, string> jsonData = new Dictionary<string, string>()
    {
        { "image_data", base64Image },
        { "text", promptText }
    };
        string jsonString = JsonUtility.ToJson(new JsonWrapper(jsonData));

        // HTTP POST 요청
        using (UnityWebRequest request = new UnityWebRequest(serverUrl + "/api/openai", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("OpenAI 응답: " + request.downloadHandler.text);
                // 여기서 응답 내용을 파싱해 화면에 출력하거나 다음 동작으로 넘기기

                //Newton 파싱 이용해서 응답 표현
                ShowOpenAIResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("HTTP 전송 실패: " + request.error);
            }
        }
    }

    //HTTP용 JSON Class
    [System.Serializable]
    public class JsonWrapper
    {
        public string image_data;
        public string text;

        public JsonWrapper(Dictionary<string, string> dict)
        {
            image_data = dict["image_data"];
            text = dict["text"];
        }
    }

//WebSocket으로 보내기(코루틴)
IEnumerator WS_CaptureWebcamImageAndSend()
    {
        // WebCamTexture 준비 상태까지 대기
        while (FitToScreen.webcamTexture == null || FitToScreen.webcamTexture.width <= 16)
            yield return null;

        yield return new WaitForEndOfFrame();

        // webcamTexture의 픽셀 데이터를 기반으로 Texture2D 생성
        Texture2D tex = new Texture2D(FitToScreen.webcamTexture.width, FitToScreen.webcamTexture.height, TextureFormat.RGB24, false);
        tex.SetPixels(FitToScreen.webcamTexture.GetPixels());
        tex.Apply();

        // PNG 인코딩
        byte[] imageBytes = tex.EncodeToPNG();
        string base64Image = Convert.ToBase64String(imageBytes);

        // JSON payload 생성
        Dictionary<string, string> payload = new Dictionary<string, string>()
    {
        { "image", base64Image }
    };

        // 서버로 전송
        if (socket != null && socket.Connected)
        {
            socket.Emit("screenshot", payload);
            Debug.Log("webcamTexture 기반 이미지 전송 완료");
        }
        else
        {
            Debug.LogWarning("소켓 연결이 되어있지 않음");
        }

        Destroy(tex);
    }
    #endregion

    #region OpenAI 응답 파싱 및 표시 (Newtonsoft.Json 사용)

    public TMP_Text responseTextUI; // UI 연결 (TextMeshProUGUI)

    void ShowOpenAIResponse(string json)
    {
        try
        {
            JObject parsed = JObject.Parse(json);
            string message = parsed["message"]?.ToString();

            if (!string.IsNullOrEmpty(message))
            {
                responseTextUI.text = "OpenAI : " + message;
                Debug.Log("OpenAI 응답: " + message);
            }
            else
            {
                responseTextUI.text = "응답 메시지가 비어 있음";
                Debug.LogWarning("응답은 성공했으나 message 필드가 없음");
            }
        }
        catch (System.Exception e)
        {
            responseTextUI.text = "OpenAI 응답 파싱 오류";
            Debug.LogWarning("OpenAI JSON 파싱 실패: " + e.Message);
        }
    }

    #endregion


}
