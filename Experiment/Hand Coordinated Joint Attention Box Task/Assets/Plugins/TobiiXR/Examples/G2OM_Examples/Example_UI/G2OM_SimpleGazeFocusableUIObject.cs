// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using Tobii.XR;
    using UnityEngine;
    using UnityEngine.UI;

    public class G2OM_SimpleGazeFocusableUIObject : MonoBehaviour, IGazeFocusable
    {
        private Color _defaultColor;

        void Start()
        {
            _defaultColor = GetComponent<Graphic>().color;
        }

        public void GazeFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                GetComponent<Graphic>().color = Color.red;
            }
            else
            {
                GetComponent<Graphic>().color = _defaultColor;
            }
        }
    }
}