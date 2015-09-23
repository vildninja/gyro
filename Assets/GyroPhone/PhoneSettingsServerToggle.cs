using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace VildNinja.GyroPhone
{
    public class PhoneSettingsServerToggle : MonoBehaviour
    {
        public PhoneController.Server server;

        // Use this for initialization
        private void Start()
        {
            GetComponentInChildren<Text>().text = server.name;
            GetComponent<Toggle>().isOn = server.send;
        }

        public void SetSend(bool on)
        {
            if (server.send == on)
                return;
            server.send = on;
            FindObjectOfType<PhoneController>().UpdateConnected();
        }
    }
}