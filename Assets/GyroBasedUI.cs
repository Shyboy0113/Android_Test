using UnityEngine;

public class GyroBasedUI : MonoBehaviour
{
    private bool gyroEnabled;
    private Gyroscope gyro;

    private Quaternion rotFix; // ȸ�� ������

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
            rotFix = new Quaternion(0, 0, 1, 0); // Android�� ���� �ݴ�� ���� �ʿ�
#elif UNITY_IOS
            rotFix = new Quaternion(0, 0, 1, 0); // iOS�� ����
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
