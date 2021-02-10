// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using Tobii.G2OM;
    using Tobii.XR;
    using UnityEngine;

    /// <summary>
    /// Shows the recommended way to use the C# G2OM abstraction that is created for Unity.
    /// </summary>
    public class G2OM_Example : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("Can be null if there is no need for debug visualization")]
        public G2OM_DebugVisualization DebugVisualization;
        public KeyCode DebugVisualizationOnOff = KeyCode.Space;
        public KeyCode DebugVisualizationFreezeOnOff = KeyCode.LeftControl;
        [Tooltip("Used by Debug Visualization to sync with Main Camera.")]
        public Camera MainCamera;

        void Start()
        {
            var settings = TobiiXR_Settings.CreateDefaultSettings();

            if (DebugVisualization != null)
            {
                DebugVisualization.Setup(settings.G2OM, MainCamera);
                Debug.Log("G2OM debug visualization available.");
            }
            
            TobiiXR.Start(settings);
        }

        void Update()
        {
            if (DebugVisualization == null) return;

            if (Input.GetKeyUp(DebugVisualizationOnOff))
            {
                DebugVisualization.ToggleVisualization();
            }

            if (Input.GetKeyUp(DebugVisualizationFreezeOnOff))
            {
                DebugVisualization.ToggleFreeze();
            }
        }
    }
}