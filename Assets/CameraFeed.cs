using UnityEngine;
using UnityEngine.UI;

using UnityEngine.Android;


public class CameraFeed : MonoBehaviour
{
    public RawImage rawImage;     // ȭ�鿡 ������ UI
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

        //�ػ� ���� ����
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    
    }

}
