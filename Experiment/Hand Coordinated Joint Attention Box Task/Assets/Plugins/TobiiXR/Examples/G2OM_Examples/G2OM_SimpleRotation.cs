// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using UnityEngine;

    public class G2OM_SimpleRotation : MonoBehaviour
    {
        public Vector3 LengthAndDirection = new Vector3(5, 5, 0);

        void Update()
        {
            transform.Rotate(LengthAndDirection * Time.deltaTime);
        }
    }
}