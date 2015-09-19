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

        public Text text;

        private float vibrate;

        private int host;
        private int connection;
        private int state;
        private int reliable;

        private byte error;

        private bool isConnected;

        public readonly byte[] broadcast = new byte[1000];
        public readonly byte[] data = new byte[1000];

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
            var config = new ConnectionConfig();
            state = config.AddChannel(QosType.Unreliable);
            reliable = config.AddChannel(QosType.ReliableSequenced);
            var topology = new HostTopology(config, 1);

            host = NetworkTransport.AddHost(topology);

            ms = new MemoryStream(data);
            reader = new BinaryReader(ms);
            writer = new BinaryWriter(ms);

            NetworkTransport.StartBroadcastDiscovery(host, 9112, 1, 1, 0, broadcast, 1, 2000, out error);
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
                    NetworkTransport.StartBroadcastDiscovery(host, 9112, 1, 1, 0, broadcast, 1, 10, out error);
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
                WriteStatus();
                NetworkTransport.Send(host, connection, state, data, (int) ms.Position, out error);
                PhoneServer.TestError(error);
            }

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

        public void WriteStatus()
        {
            writer.Write(number);
            WriteQuaternion(Input.gyro.attitude);
            WriteVector(Input.gyro.gravity);
            WriteVector(Input.gyro.rotationRate);
            WriteVector(Input.gyro.rotationRateUnbiased);
            WriteVector(Input.gyro.userAcceleration);
            WriteVector(Input.acceleration);
            WriteVector(Input.compass.rawVector);
            writer.Write(Input.compass.magneticHeading);
            writer.Write(Input.compass.trueHeading);
        }

        private void WriteVector(Vector3 vec)
        {
            writer.Write(vec.x);
            writer.Write(vec.y);
            writer.Write(vec.z);
        }

        private void WriteQuaternion(Quaternion qua)
        {
            writer.Write(qua.x);
            writer.Write(qua.y);
            writer.Write(qua.z);
            writer.Write(qua.w);
        }
    }
}