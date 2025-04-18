using UnityEngine;
using UnityEngine.UI;

using UnityEngine.Android;


public class CameraFeed : MonoBehaviour
{
    public RawImage rawImage;     // 화면에 보여줄 UI
    private WebCamTexture webcamTexture;

    void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }

        webcamTexture = new WebCamTexture();
        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();

        //해상도 가로 강제
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    
    }

}
