// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using UnityEngine;

    public class TobiiOpenVRProvider : IEyeTrackingProvider
    {
        private StreamEngineTracker _streamEngineTracker;
        private IHmdRuntime _hmdRuntime;
        private readonly TobiiXR_EyeTrackingData _eyeTrackingData = new TobiiXR_EyeTrackingData();

        public TobiiXR_EyeTrackingData EyeTrackingData
        {
            get { return _eyeTrackingData; }
        }

        public TobiiOpenVRProvider()
        {
            _streamEngineTracker = new StreamEngineTracker();
            _hmdRuntime = new OpenVrRuntime();
        }

        public TobiiOpenVRProvider(IHmdRuntime hmdRuntime)
        {
            _streamEngineTracker = new StreamEngineTracker();
            _hmdRuntime = hmdRuntime;
        }

        public void Tick()
        {
            _streamEngineTracker.Tick();

            var data = _streamEngineTracker.LocalLatestData;

            _eyeTrackingData.Timestamp = Time.time;
            _eyeTrackingData.CombinedRay = data.CombinedRay;
            _eyeTrackingData.Left = data.Left;
            _eyeTrackingData.Right = data.Right;

            TransformGazeData(_eyeTrackingData, _hmdRuntime.GetHmdOrigin());
        }

        public void Destroy()
        {
            if (_streamEngineTracker != null)
            {
                _streamEngineTracker.Destroy();
            }

            if (_hmdRuntime != null)
            {
                _hmdRuntime.Destroy();
            }

            _streamEngineTracker = null;
            _hmdRuntime = null;
        }

        private static void TransformGazeData(TobiiXR_EyeTrackingData eyeTrackingData, Transform hmdOrigin)
        {
            if (eyeTrackingData.CombinedRay.IsValid)
            {
                TransformToWorldSpace(ref eyeTrackingData.CombinedRay, hmdOrigin);
            }
            if (eyeTrackingData.Left.Ray.IsValid)
            {
                TransformToWorldSpace(ref eyeTrackingData.Left.Ray, hmdOrigin);
            }
            if (eyeTrackingData.Right.Ray.IsValid)
            {
                TransformToWorldSpace(ref eyeTrackingData.Right.Ray, hmdOrigin);
            }
        }

        private static void TransformToWorldSpace(ref TobiiXR_GazeRay eyeData, Transform hmdOrigin)
        {
            eyeData.Origin = hmdOrigin.TransformPoint(eyeData.Origin);
            eyeData.Direction = hmdOrigin.TransformDirection(eyeData.Direction);
        }
    }
}