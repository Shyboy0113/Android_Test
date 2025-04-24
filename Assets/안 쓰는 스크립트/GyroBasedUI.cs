using UnityEngine;

public class GyroBasedUI : MonoBehaviour
{
    private bool gyroEnabled;
    private Gyroscope gyro;

    private Quaternion rotFix; // 회전 보정용

    void Start()
    {
        gyroEnabled = EnableGyro();
    }

    private bool EnableGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            gyro = Input.gyro;
            gyro.enabled = true;

#if UNITY_ANDROID
            rotFix = new Quaternion(0, 0, 1, 0); // Android는 축이 반대라 보정 필요
#elif UNITY_IOS
            rotFix = new Quaternion(0, 0, 1, 0); // iOS도 유사
#endif

            return true;
        }
        return false;
    }

    void Update()
    {
        if (gyroEnabled)
        {
            transform.localRotation = gyro.attitude * rotFix;
        }
    }
}
