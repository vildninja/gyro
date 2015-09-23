using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace VildNinja.GyroPhone
{
    public class PhoneController : MonoBehaviour
    {
        public class Server
        {
            public readonly int connection;
            public bool send;
            public float vibrate;
            public string name = "A Server...";

            public Server(int conn)
            {
                connection = conn;
            }
        }

        public int number;
        public int port = 9112;
        [HideInInspector]
        public string filter = "";
        [HideInInspector]
        public bool multiSend = false;
        [HideInInspector]
        public int frequency = 5;

        public Text text;

        private int host;
        private int connection;
        private int state;
        private int reliable;

        private byte error;

        private bool isConnected;
        
        private readonly byte[] data = new byte[1000];

        private MemoryStream ms;
        private BinaryReader reader;
        private BinaryWriter writer;

        public readonly List<Server> servers = new List<Server>();

        public PhoneSettings settings;

        private void Awake()
        {
            NetworkTransport.Init();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        // Use this for initialization
        private void Start()
        {
            var config = new ConnectionConfig();
            state = config.AddChannel(QosType.Unreliable);
            reliable = config.AddChannel(QosType.ReliableSequenced);
            var topology = new HostTopology(config, 1);

            host = NetworkTransport.AddHost(topology);

            ms = new MemoryStream(data);
            reader = new BinaryReader(ms);
            writer = new BinaryWriter(ms);

            filter = PlayerPrefs.GetString("filter", "");
            multiSend = PlayerPrefs.GetInt("multiSend", 0) == 1;
            port = PlayerPrefs.GetInt("port", port);
            number = PlayerPrefs.GetInt("number", number);
            frequency = PlayerPrefs.GetInt("frequency", frequency);

            UpdateConnected();
        }

        private float timer = 0;
        private float timeStep = 0.2f;

        // Update is called once per frame
        private void Update()
        {
            int rConn;
            int rChan;
            int rSize;
            Server server;

            var rec = NetworkTransport.ReceiveFromHost(host, out rConn, out rChan, data, data.Length, out rSize,
                out error);
            PhoneServer.TestError(error);
            ms.Position = 0;

            switch (rec)
            {
                case NetworkEventType.DataEvent:
                    if (rChan == reliable)
                    {
                        server = servers.FirstOrDefault(s => s.connection == rConn);
                        if (server == null)
                            break;
                        var key = reader.ReadString();
                        switch (key)
                        {
                            case "name":
                                server.name = reader.ReadString();
                                break;
                            case "vibrate":
                                server.vibrate = reader.ReadSingle();
                                break;
                        }
                    }
                    break;
                case NetworkEventType.ConnectEvent:
                    server = new Server(rConn);
                    server.send = !isConnected && string.IsNullOrEmpty(filter);
                    servers.Add(server);
                    UpdateConnected();
                    break;
                case NetworkEventType.DisconnectEvent:
                    server = servers.FirstOrDefault(s => s.connection == rConn);
                    servers.Remove(server);
                    UpdateConnected();
                    break;
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.BroadcastEvent:
                    break;
            }

#if UNITY_ANDROID || UNITY_IOS
            if (servers.Count > 0 && servers.Max(s => s.send ? s.vibrate : 0) > 0.5f)
            {
                Handheld.Vibrate();
            }
#endif

            if (Time.time > timer && isConnected)
            {
                timer = Time.time + 1f/frequency;
                ms.Position = 0;
                writer.Write(number);
                WriteStatus();
                for (int i = 0; i < servers.Count; i++)
                {
                    if (servers[i].send)
                    {
                        NetworkTransport.Send(host, servers[i].connection, state, data, (int) ms.Position, out error);
                        PhoneServer.TestError(error);
                    }
                }
            }

            if (text != null)
            {
                string str = "Gyro: " + Input.gyro.attitude +
                    "\nCompas: " + Input.compass.trueHeading.ToString("F1") +
                    "\nAcc: " + Input.acceleration +
                    "\nSending #" + number + " with " + frequency + "Hz to:";
                for (int i = 0; i < servers.Count; i++)
                {
                    if (servers[i].send)
                    {
                        str += "\n" + servers[i].name;
                    }
                }
                text.text = str;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        public void UpdateConnected()
        {
            isConnected = servers.Any(s => s.send);
            Input.gyro.enabled = isConnected;
            Input.compass.enabled = isConnected;

            if (NetworkTransport.IsBroadcastDiscoveryRunning())
            {
                NetworkTransport.StopBroadcastDiscovery();
            }

            if (!isConnected || multiSend)
            {
                // don't stop and start broadcasting the same frame.
                StartCoroutine(RestartBroadcasting());
            }

            if (settings.gameObject.activeSelf)
            {
                settings.UpdateServerList();
            }
        }

        IEnumerator RestartBroadcasting()
        {
            yield return null;
            NetworkTransport.StartBroadcastDiscovery(host, port, 1, 1, 1, new byte[1], 1, 2000, out error);
            PhoneServer.TestError(error);
        }

        public void SetNumber(int n)
        {
            number = n;
            PlayerPrefs.SetInt("number", n);
            PlayerPrefs.Save();
        }

        public void SetPort(int n)
        {
            port = n;
            PlayerPrefs.SetInt("port", n);
            PlayerPrefs.Save();

            // restart broadcast if we are current searching for servers
            if (NetworkTransport.IsBroadcastDiscoveryRunning())
            {
                NetworkTransport.StopBroadcastDiscovery();
                UpdateConnected();
            }
        }

        public void SetFreq(int n)
        {
            frequency = n;
            PlayerPrefs.SetInt("frequency", n);
            PlayerPrefs.Save();
        }

        public void SetMultiSend(bool on)
        {
            multiSend = on;
            PlayerPrefs.SetInt("multiSend", on ? 1 : 0);
            PlayerPrefs.Save();

            // make sure to stop or start broadcasting
            if (NetworkTransport.IsBroadcastDiscoveryRunning())
            {
                NetworkTransport.StopBroadcastDiscovery();
            }
            UpdateConnected();
        }

        public void SetFilter(string str)
        {
            filter = str;
            PlayerPrefs.SetString("filter", str);
            PlayerPrefs.Save();
        }

        public void DoVibrate(float value)
        {
            Handheld.Vibrate();
        }

        public virtual void WriteStatus()
        {
            Write(Input.gyro.attitude);
            Write(Input.gyro.gravity);
            Write(Input.gyro.rotationRate);
            Write(Input.gyro.rotationRateUnbiased);
            Write(Input.gyro.userAcceleration);
            Write(Input.acceleration);
            Write(Input.compass.rawVector);
            Write(Input.compass.magneticHeading);
            Write(Input.compass.trueHeading);
        }

        public void Write(float value)
        {
            writer.Write(value);
        }

        public void Write(Vector3 vec)
        {
            writer.Write(vec.x);
            writer.Write(vec.y);
            writer.Write(vec.z);
        }

        public void Write(Quaternion qua)
        {
            writer.Write(qua.x);
            writer.Write(qua.y);
            writer.Write(qua.z);
            writer.Write(qua.w);
        }
    }
}