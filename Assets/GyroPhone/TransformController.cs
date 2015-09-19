using UnityEngine;
using System.Collections;

namespace VildNinja.GyroPhone
{
    public class TransformController : PhoneController
    {
        // sorry for horrible hack, but didn't want to break the flow
        public override void WriteStatus()
        {
            Write(transform.rotation); // gyro attitude
            Write(Input.gyro.gravity);
            Write(Input.gyro.rotationRate);
            Write(Input.gyro.rotationRateUnbiased);
            Write(transform.position);
            Write(Input.acceleration);
            Write(Input.compass.rawVector);
            Write(Input.compass.magneticHeading);
            Write(Input.compass.trueHeading);
        }
    }
}
