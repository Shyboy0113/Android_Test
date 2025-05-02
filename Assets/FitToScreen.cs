using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Android;
using System.Collections;

[RequireComponent(typeof(AspectRatioFitter))]
public class FitToScreen : MonoBehaviour
{
    public RawImage rawImage;     // 화면에 보여줄 UI
    public static WebCamTexture webcamTexture;

    public TMP_Text text;

    void Start()
    {

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }

        webcamTexture = new WebCamTexture(requestedWidth: 4000, requestedHeight: 2252);

        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();

        StartCoroutine(AdjustAspectWhenReady());
    }

    private void Update()
    {
        text.text = "Current Resolution = " + Screen.width.ToString() + "x" + Screen.height.ToString();
    }

    private IEnumerator AdjustAspectWhenReady()
    {
        while (webcamTexture.width <= 16)
            yield return null;

        float camRatio = (float)webcamTexture.width / webcamTexture.height;

        var aspectFitter = rawImage.GetComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = camRatio;
    }


}
