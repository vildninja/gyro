using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace VildNinja.GyroPhone
{

    public class PhoneSettings : MonoBehaviour
    {
        private PhoneController phone;

        public InputField filter;
        public InputField port;
        public Toggle multiSend;
        public RectTransform serverList;
        public PhoneSettingsServerToggle serverTemplate;
        
        private void OnEnable()
        {
            phone = FindObjectOfType<PhoneController>();
            port.text = phone.port.ToString();
            filter.text = phone.filter ?? "";
            multiSend.isOn = phone.multiSend;
            
            UpdateServerList();
        }

        public void UpdateServerList()
        {
            Debug.Log("Update server list");
            OnDisable();
            foreach (var server in phone.servers)
            {
                var item = Instantiate(serverTemplate);
                item.server = server;
                item.transform.SetParent(serverList, false);
                item.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            var items = FindObjectsOfType<PhoneSettingsServerToggle>();
            for (int i = 0; i < items.Length; i++)
            {
                Destroy(items[i].gameObject);
            }
        }

        public void SetPort(string value)
        {
            ushort parsed;
            if (ushort.TryParse(value, out parsed) && parsed > 0)
            {
                phone.SetPort(parsed);
            }
            else
            {
                port.text = phone.port.ToString();
            }
        }

        public void SetFilter(string value)
        {
            phone.SetFilter(value);
        }

        public void SetMultiSend(bool isOn)
        {
            phone.SetMultiSend(isOn);
        }
    }
}