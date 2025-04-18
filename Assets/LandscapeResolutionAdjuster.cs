using UnityEngine;
using UnityEngine.UI;

public class LandscapeResolutionAdjuster : MonoBehaviour
{
    public CanvasScaler canvasScaler;

    void Start()
    {
        int w = Screen.width;
        int h = Screen.height;

        Debug.Log($"[🔍 현재 해상도]: {w} x {h}");

        // 가로/세로 판단
        if (w < h)
        {
            Debug.Log("[⚠️ 세로 모드 감지됨] 가로 모드로 강제 설정합니다.");
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080); // 가로 기준
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }
    }
}
