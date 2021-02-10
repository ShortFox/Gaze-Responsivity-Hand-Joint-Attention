// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using Tobii.XR;
    using UnityEngine;

    public class G2OM_SimpleGazeFocusableObject : MonoBehaviour, IGazeFocusable
    {
        private Color _defaultColor;

        void Start()
        {
            _defaultColor = GetComponent<Renderer>().material.color;
        }

        public void GazeFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                GetComponent<Renderer>().material.color = Color.red;
            }
            else
            {
                GetComponent<Renderer>().material.color = _defaultColor;
            }
        }
    }
}