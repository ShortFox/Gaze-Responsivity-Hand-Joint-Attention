// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using UnityEngine;

    public class TobiiXR_EyeTrackingData
    {
        public float Timestamp;
        public TobiiXR_GazeRay CombinedRay;
        public TobiiXR_PerEyeData Left;
        public TobiiXR_PerEyeData Right;

        // TODO: Should we supply local gaze data?
    }

    public struct TobiiXR_PerEyeData
    {
        public TobiiXR_GazeRay Ray;
        public float EyeOpenness;
        public bool EyeOpennessIsValid;
    }

    public struct TobiiXR_GazeRay
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public bool IsValid;
    }
}