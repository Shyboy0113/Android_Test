using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class ButtonEvent : MonoBehaviour
{
    public string GetLocalIPAddress()
    {
        string localIP = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            // IPv4 + 내부 IP만 필터링
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
