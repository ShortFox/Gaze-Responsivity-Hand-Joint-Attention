// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using Tobii.G2OM;
    using Tobii.XR;
    using UnityEngine;

    /// <summary>
    /// Shows how you can feed a list of known objects instead of raycasting to find objects.
    /// </summary>
    public class G2OM_ExampleRegisterObjects : MonoBehaviour
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

            var description = new G2OM_Description
            {
                HowLongToKeepCandidatesInSeconds = 0,
                ExpectedNumberOfObjects = settings.ExpectedNumberOfObjects,
                ObjectFinder = new G2OM_RegisterObjectsFinder(),
                Distinguisher = new G2OM_RegisterObjectDistinguisher(),
                LayerMask = settings.LayerMask
            };

            settings.G2OM = G2OM.Create(description);

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