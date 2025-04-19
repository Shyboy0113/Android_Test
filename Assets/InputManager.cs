using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class InputManager : MonoBehaviour
{
    public TMP_InputField urlInputField;
    public TMP_InputField portInputField;
    public Button connectButton;
    public TMP_Text currentIP;

    private const string PREF_URL_KEY = "ServerURL";
    private const string PREF_PORT_KEY = "ServerPort";

    private const string DEFAULT_URL = "localhost";
    private const string DEFAULT_PORT = "5001";

    void Start()
    {
        string savedUrl = PlayerPrefs.GetString(PREF_URL_KEY, DEFAULT_URL);
        string savedPort = PlayerPrefs.GetString(PREF_PORT_KEY, DEFAULT_PORT);

        urlInputField.text = savedUrl;
        portInputField.text = savedPort;

        connectButton.onClick.AddListener(OnConnectPressed);

        currentIP.text = "Current IP = http://" + savedUrl + ":" + savedPort;

        StartCoroutine(RequestAllPermission());

    }

    private void OnConnectPressed()
    {
        string url = urlInputField.text.Trim();
        string port = portInputField.text.Trim();

        if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(port))
        {
            PlayerPrefs.SetString(PREF_URL_KEY, url);
            PlayerPrefs.SetString(PREF_PORT_KEY, port);
            PlayerPrefs.Save();

            Debug.Log("Saved URL: " + url);
            Debug.Log("Saved Port: " + port);

            SocketIOUnityClient.url = url;
            SocketIOUnityClient.port = port;

            currentIP.text = "Current IP = http://" + url + ":" + port;
        }
        else
        {
            Debug.LogWarning("Please enter both URL and port.");
        }
    }

    public void nextScene()
    {
        SceneManager.LoadScene(1);
    }

    public IEnumerator RequestAllPermission()
    {
        RequestCameraPermission();

        yield return new WaitForSeconds(1.5f);

        RequestMicPermission();

        yield return null;
    }

    public void RequestMicPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("Requesting microphone permission...");
            Permission.RequestUserPermission(Permission.Microphone);
        }
        else
        {
            Debug.Log("Microphone permission already granted.");
        }
#else
        Debug.Log("Microphone permission is only needed on Android.");
#endif
    }

    public void RequestCameraPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log("Requesting camera permission...");
            Permission.RequestUserPermission(Permission.Camera);
        }
        else
        {
            Debug.Log("Camera permission already granted.");
        }
#else
        Debug.Log("Camera permission is only needed on Android.");
#endif
    }
}
