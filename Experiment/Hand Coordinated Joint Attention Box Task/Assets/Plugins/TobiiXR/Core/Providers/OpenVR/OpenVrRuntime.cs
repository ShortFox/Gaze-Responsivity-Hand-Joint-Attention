// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using UnityEngine;
    using Valve.VR;

    public class OpenVrRuntime : IHmdRuntime
    {
        private Transform _hmdOrigin;

        public OpenVrRuntime()
        {
            _hmdOrigin = CreateNewHmdOrigin(GetType().Name);
        }

        public Transform GetHmdOrigin()
        {
            if (_hmdOrigin != null) return _hmdOrigin;

            Debug.Log("Missing HMD Origin, creating a new instance");
            _hmdOrigin = CreateNewHmdOrigin(GetType().Name);
            return _hmdOrigin;
        }

        public float InterpupilarDistanceInMeters()
        {
            var hmdIpdInMeter = 0.063f;

            var sys = OpenVR.System;
            if (sys == null) return hmdIpdInMeter;

            var error = ETrackedPropertyError.TrackedProp_Success;
            hmdIpdInMeter = OpenVR.System.GetFloatTrackedDeviceProperty(
                0,
                ETrackedDeviceProperty.Prop_UserIpdMeters_Float,
                ref error);

            if (error == ETrackedPropertyError.TrackedProp_Success)
            {
                return hmdIpdInMeter;
            }

            Debug.LogWarning("Failed to get IPD from OpenVR runtime. Fallbacking to default IPD.");
            hmdIpdInMeter = 0.063f;

            return hmdIpdInMeter;
        }

        public void Destroy()
        {
            if (_hmdOrigin == null) return;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Object.Destroy(_hmdOrigin.gameObject);
            }
            else
            {
                Object.DestroyImmediate(_hmdOrigin.gameObject);
            }
#else
            Object.Destroy(_hmdOrigin.gameObject);
#endif
            _hmdOrigin = null;
        }

        private static float OffsetFromHmdToEyeInMeters()
        {
            var hmdOffset = 0.015f;
            var error = ETrackedPropertyError.TrackedProp_Success;

            var sys = OpenVR.System;
            if (sys == null) return hmdOffset;

            hmdOffset = sys.GetFloatTrackedDeviceProperty(
                0,
                ETrackedDeviceProperty.Prop_UserHeadToEyeDepthMeters_Float,
                ref error);

            if (error != ETrackedPropertyError.TrackedProp_Success)
            {
                hmdOffset = 0.015f;
            }

            return hmdOffset;
        }

        private static Transform CreateNewHmdOrigin(string name)
        {
            var hmdOrigin = new GameObject(string.Format("HmdOrigin_{0}", name)).transform;
            hmdOrigin.parent = GetCameraOrigin();
            hmdOrigin.transform.localScale = Vector3.one;
            hmdOrigin.transform.localRotation = Quaternion.identity;

            // This compensates for the main camera in Unity is not being in the hmd origin.
            hmdOrigin.localPosition = new Vector3(0, 0, OffsetFromHmdToEyeInMeters());

            return hmdOrigin;
        }

        private static Transform GetCameraOrigin()
        {
            return Camera.main != null ?
                Camera.main.transform :
                Camera.allCameras[0].transform;
        }
    }
}