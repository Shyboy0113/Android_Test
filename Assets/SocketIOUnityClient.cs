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

    #region 스크린샷 저장 기능
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

    #region 스크린샷 기능 및 GPT로 보내기

    public void OnCaptureButtonPressed()
    {
        StartCoroutine(CaptureOnlyRawImage());
    }
    public RawImage targetRawImage;  // Inspector에 RawImage 연결

    IEnumerator CaptureOnlyRawImage()
    {
        yield return new WaitForEndOfFrame();

        // 1. RawImage의 화면상 위치 계산
        RectTransform rt = targetRawImage.rectTransform;
        Vector3[] worldCorners = new Vector3[4];
        rt.GetWorldCorners(worldCorners);

        // 2. UI → 스크린 좌표 변환
        float minX = worldCorners[0].x;
        float minY = worldCorners[0].y;
        float width = worldCorners[2].x - worldCorners[0].x;
        float height = worldCorners[2].y - worldCorners[0].y;

        // 3. ReadPixels로 해당 영역만 캡처
        Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(minX, minY, width, height), 0, 0);
        tex.Apply();

        // 4. PNG로 인코딩 + 전송 등 처리
        byte[] imageBytes = tex.EncodeToPNG();
        string base64Image = Convert.ToBase64String(imageBytes);

        Dictionary<string, string> payload = new Dictionary<string, string>()
    {
        { "image", base64Image }
    };

        if (socket != null && socket.Connected)
        {
            socket.Emit("screenshot", payload);
            Debug.Log("🔍 RawImage 영역만 캡처하여 전송 완료");
        }

        Destroy(tex);
    }


    IEnumerator CaptureScreenshotAndSend()
    {
        // 1. 화면 렌더링이 완료될 때까지 대기
        yield return new WaitForEndOfFrame();

        // 2. 현재 화면의 픽셀을 읽어서 Texture2D에 저장
        Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply(); // 실제 이미지 데이터 적용

        // 3. PNG 형식으로 인코딩 (또는 EncodeToJPG()도 가능)
        byte[] imageBytes = screenImage.EncodeToPNG();

        // 4. base64 문자열로 인코딩
        string base64Image = Convert.ToBase64String(imageBytes);

        // 5. SocketIO를 통해 JSON 형식으로 이미지 전송
        Dictionary<string, string> payload = new Dictionary<string, string>()
    {
        { "image", base64Image }
    };

        if (socket != null && socket.Connected)
        {
            socket.Emit("screenshot", payload); // 서버에서는 "screenshot" 이벤트로 받음
            Debug.Log("스크린샷 base64 이미지 전송 완료");
        }
        else
        {
            Debug.LogWarning("소켓 연결 안 됨. 스크린샷 전송 실패");
        }

        // 6. 메모리 정리
        UnityEngine.Object.Destroy(screenImage);
    }
    #endregion

}
