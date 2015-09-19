using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;


namespace VildNinja.GyroPhone
{
    public class PhoneServer : MonoBehaviour
    {

        public struct Data
        {
            public Quaternion gyroAttitude;
            public Vector3 gyroGravity;
            public Vector3 gyroRotationRate;
            public Vector3 gyroRotationRateUnbiased;
            public Vector3 gyroUserAcceleration;
            public Vector3 acceleration;
            public Vector3 compassRawVector;
            public float compassMagneticHeading;
            public float compassTrueHeading;

            public Quaternion rotation {get { return gyroAttitude; } }
            public Vector3 position { get { return gyroUserAcceleration; } }
        }

        public int port = 9112;

        public readonly Data[] phones = new Data[10];
        private readonly int[] clients = new int[10];

        private int host;
        private int state;
        private int reliable;

        private byte error;

        private bool isConnected;

        public Text debugText = null;
        
        private readonly byte[] data = new byte[1000];

        private MemoryStream ms;
        private BinaryReader reader;
        private BinaryWriter writer;

        public static void TestError(byte error)
        {
            if (error > 0)
            {
                Debug.Log("Error " + error);
            }
        }

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
            var topology = new HostTopology(config, 10);

            host = NetworkTransport.AddHost(topology, port);
            NetworkTransport.SetBroadcastCredentials(host, 1, 1, 0, out error);

            TestError(error);

            ms = new MemoryStream(data);
            reader = new BinaryReader(ms);
            writer = new BinaryWriter(ms);
        }

        // Update is called once per frame
        private void Update()
        {
            int rConn;
            int rChan;
            int rSize;

            var rec = NetworkTransport.ReceiveFromHost(host, out rConn, out rChan, data, data.Length, out rSize,
                out error);
            TestError(error);
            ms.Position = 0;

            switch (rec)
            {
                case NetworkEventType.DataEvent:
                    if (rChan == state)
                    {
                        int num = reader.ReadInt32();
                        if (num >= 0 && num < 10)
                        {
                            clients[num] = rConn;
                            Read(out phones[num].gyroAttitude);
                            Read(out phones[num].gyroGravity);
                            Read(out phones[num].gyroRotationRate);
                            Read(out phones[num].gyroRotationRateUnbiased);
                            Read(out phones[num].gyroUserAcceleration);
                            Read(out phones[num].acceleration);
                            Read(out phones[num].compassRawVector);
                            phones[num].compassMagneticHeading = reader.ReadSingle();
                            phones[num].compassTrueHeading = reader.ReadSingle();
                        }
                    }
                    break;
                case NetworkEventType.ConnectEvent:
                    break;
                case NetworkEventType.DisconnectEvent:
                    break;
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.BroadcastEvent:

                    int cPort;
                    var bi = NetworkTransport.GetBroadcastConnectionInfo(host, out cPort, out error);
                    TestError(error);
                    NetworkTransport.Connect(host, bi, cPort, 0, out error);
                    TestError(error);
                    Debug.Log("Broadcast " + bi + " - " + cPort);

                    break;
            }

            if (debugText != null)
            {
                string txt = "";
                for (int i = 1; i <= 2; i++)
                {
                    txt += i + "\n";
                    txt += "acc " + phones[i].acceleration + "\n";
                    txt += "comMH " + phones[i].compassMagneticHeading + "\n";
                    txt += "comTH " + phones[i].compassTrueHeading + "\n";
                    txt += "comRaw " + phones[i].compassRawVector + "\n";
                    txt += "gyroAtt " + phones[i].gyroAttitude + "\n";
                    txt += "gyroRot " + phones[i].gyroRotationRate + "\n";
                    txt += "gyroGrav " + phones[i].gyroGravity + "\n";
                    txt += "rotation " + phones[i].rotation + "\n";
                    txt += "position " + phones[i].position + "\n";
                }
                debugText.text = txt;
            }
        }

        [ContextMenu("vib on")]
        public void VibrateOne()
        {
            SetVibrate(1, 1);
        }

        [ContextMenu("vib off")]
        public void StopVibrateOne()
        {
            SetVibrate(1, 0);
        }

        public void SetVibrate(int phone, float value)
        {
            ms.Position = 0;
            writer.Write(value);
            NetworkTransport.Send(host, clients[phone], reliable, data, 4, out error);
            TestError(error);
        }

        private void Read(out Vector3 vec)
        {
            vec.x = reader.ReadSingle();
            vec.y = reader.ReadSingle();
            vec.z = reader.ReadSingle();
        }

        private void Read(out Quaternion qua)
        {
            qua.x = reader.ReadSingle();
            qua.y = reader.ReadSingle();
            qua.z = reader.ReadSingle();
            qua.w = reader.ReadSingle();
        }
    }
}