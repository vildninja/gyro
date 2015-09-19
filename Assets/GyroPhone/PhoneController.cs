using System;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace VildNinja.GyroPhone
{
    public class PhoneController : MonoBehaviour
    {
        public int number;
        public int port = 9112;

        public Text text;

        private float vibrate;

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

        private void Awake()
        {
            NetworkTransport.Init();
        }

        // Use this for initialization
        private void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var config = new ConnectionConfig();
            state = config.AddChannel(QosType.Unreliable);
            reliable = config.AddChannel(QosType.ReliableSequenced);
            var topology = new HostTopology(config, 1);

            host = NetworkTransport.AddHost(topology);

            ms = new MemoryStream(data);
            reader = new BinaryReader(ms);
            writer = new BinaryWriter(ms);

            NetworkTransport.StartBroadcastDiscovery(host, port, 1, 1, 0, new byte[1], 1, 2000, out error);
            PhoneServer.TestError(error);

            Input.gyro.enabled = true;
            Input.compass.enabled = true;
        }

        private float timer = 0;

        // Update is called once per frame
        private void Update()
        {
            int rConn;
            int rChan;
            int rSize;

            var rec = NetworkTransport.ReceiveFromHost(host, out rConn, out rChan, data, data.Length, out rSize,
                out error);
            PhoneServer.TestError(error);
            ms.Position = 0;

            switch (rec)
            {
                case NetworkEventType.DataEvent:
                    if (rChan == reliable)
                    {
                        vibrate = reader.ReadSingle();
                    }
                    break;
                case NetworkEventType.ConnectEvent:
                    connection = rConn;
                    isConnected = true;
                    NetworkTransport.StopBroadcastDiscovery();
                    break;
                case NetworkEventType.DisconnectEvent:
                    isConnected = false;
                    NetworkTransport.StartBroadcastDiscovery(host, port, 1, 1, 0, new byte[1], 1, 10, out error);
                    break;
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.BroadcastEvent:
                    break;
            }

            if (vibrate > 0.5f)
            {
                Handheld.Vibrate();
            }

            if (Time.time > timer && isConnected)
            {
                timer = Time.time + 0.0333f;
                ms.Position = 0;
                writer.Write(number);
                WriteStatus();
                NetworkTransport.Send(host, connection, state, data, (int) ms.Position, out error);
                PhoneServer.TestError(error);
            }

            if (text != null)
                text.text = "#" + number + " " + (isConnected ? "connected" : "searching") + "\nv" + vibrate;
        }

        public void SetNumber(int n)
        {
            number = n;
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