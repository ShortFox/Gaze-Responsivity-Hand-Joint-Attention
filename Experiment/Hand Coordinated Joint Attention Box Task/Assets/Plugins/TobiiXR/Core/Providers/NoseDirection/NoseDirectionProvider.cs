// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using UnityEngine;

    public class NoseDirectionProvider : IEyeTrackingProvider
    {
        private Transform _hmdOrigin;
        private readonly TobiiXR_EyeTrackingData _eyeTrackingData = new TobiiXR_EyeTrackingData();

        public TobiiXR_EyeTrackingData EyeTrackingData
        {
            get { return _eyeTrackingData; }
        }
        
        public NoseDirectionProvider()
        {
            _hmdOrigin = CreateNewOrigin(GetType().Name);
        }

        public void Tick()
        {
            if (_hmdOrigin == null)
            {
                _hmdOrigin = CreateNewOrigin(GetType().Name);
            }
            var forward = _hmdOrigin.forward;
            var origin = _hmdOrigin.position;

            _eyeTrackingData.Timestamp = Time.unscaledTime;
            
            _eyeTrackingData.CombinedRay.Origin = origin;
            _eyeTrackingData.CombinedRay.Direction = forward;
            _eyeTrackingData.CombinedRay.IsValid = true;
            
            _eyeTrackingData.Left.Ray.Origin = origin;
            _eyeTrackingData.Left.Ray.Direction = forward;
            _eyeTrackingData.Left.Ray.IsValid = true;
            
            _eyeTrackingData.Right.Ray.Origin = origin;
            _eyeTrackingData.Right.Ray.Direction = forward;
            _eyeTrackingData.Right.Ray.IsValid = true;
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

        private static Transform CreateNewOrigin(string name)
        {
            var parent = GetCameraOrigin();
            var hmdOrigin = new GameObject(string.Format("HmdOrigin_{0}", name)).transform;
            hmdOrigin.parent = parent;
            hmdOrigin.transform.localPosition = Vector3.zero;
            hmdOrigin.transform.localScale = Vector3.one;
            hmdOrigin.transform.localRotation = Quaternion.identity;

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