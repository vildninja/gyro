using System;
using UnityEngine;
using System.Collections;

namespace VildNinja.GyroPhone
{
    public class CopyPhoneTransform : MonoBehaviour
    {
        public enum Conversion
        {
            X, Y, Z, O, nX, nY, nZ
        }

        public int number = 0;
        private Transform parent;
        private Transform child;
        private PhoneServer server;
        private Quaternion offset = Quaternion.identity;
        public float angle;

        public Conversion x = Conversion.X;
        public Conversion y = Conversion.Y;
        public Conversion z = Conversion.Z;

        // Use this for initialization
        private void Start()
        {
            server = FindObjectOfType<PhoneServer>();

            parent = new GameObject("Gyro " + number).transform;
            child = new GameObject("Local gyro").transform;
            child.parent = parent;
        }

        // Update is called once per frame
        private void Update()
        {
            var data = server.phones[number];

            child.localRotation = data.gyroAttitude;
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                parent.rotation = Quaternion.Inverse(data.gyroAttitude);
                offset = data.gyroAttitude;
                angle = data.compassMagneticHeading;
            }

            Vector3 gyro = child.eulerAngles;
            Vector3 angles = new Vector3(GetAngle(gyro, x), GetAngle(gyro, y), GetAngle(gyro, z));
            transform.localEulerAngles = angles;
        }

        private float GetAngle(Vector3 vec, Conversion axis)
        {
            switch (axis)
            {
                case Conversion.X:
                    return vec.x;
                case Conversion.Y:
                    return vec.y;
                case Conversion.Z:
                    return vec.z;
                case Conversion.nX:
                    return -vec.x;
                case Conversion.nY:
                    return -vec.y;
                case Conversion.nZ:
                    return -vec.z;
                default:
                    return 0;
            }
        }
    }
}
