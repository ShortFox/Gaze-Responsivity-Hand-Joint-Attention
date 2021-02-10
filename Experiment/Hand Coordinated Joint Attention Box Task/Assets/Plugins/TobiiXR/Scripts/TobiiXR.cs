// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using System.Collections.Generic;
    using G2OM;
    using UnityEngine;

    public static class TobiiXR
    {
        private static IEyeTrackingProvider _eyeTrackingProvider;
        private static G2OM _g2om;
        private static GameObject _updaterGameObject;

        public static TobiiXR_EyeTrackingData EyeTrackingData
        {
            get
            {
                VerifyInstanceIntegrity();

                return _eyeTrackingProvider.EyeTrackingData;
            }
        }

        public static List<FocusedCandidate> FocusedObjects
        {
            get
            {
                VerifyInstanceIntegrity();

                return _g2om.GazeFocusedObjects;
            }
        }

        public static bool Start(TobiiXR_Settings settings = null)
        {
            if (_eyeTrackingProvider != null)
            {
                Debug.LogWarning(string.Format("TobiiXR already started with provider ({0})", _eyeTrackingProvider));
                VerifyInstanceIntegrity();
                return false;
            }

            if (settings == null)
            {
                settings = TobiiXR_Settings.CreateDefaultSettings();
            }

            _eyeTrackingProvider = settings.EyeTrackingProvider;

            if (_eyeTrackingProvider == null)
            {
                _eyeTrackingProvider = new NoseDirectionProvider();
                Debug.LogWarning(string.Format("Creating ({0}) failed. Using ({1}) as fallback", settings.GetProviderType(), _eyeTrackingProvider.GetType().Name));
            }

            Debug.Log(string.Format("Starting TobiiXR with ({0}) as provider for eye tracking", _eyeTrackingProvider));

            _g2om = settings.G2OM;

            VerifyInstanceIntegrity();

            return true;
        }

        public static void Stop()
        {
            if (_eyeTrackingProvider == null) return;

            _g2om.Destroy();
            _eyeTrackingProvider.Destroy();

            if (_updaterGameObject != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Object.Destroy(_updaterGameObject.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(_updaterGameObject.gameObject);
                }
#else
            Object.Destroy(_updaterGameObject.gameObject);
#endif
            }


            _updaterGameObject = null;
            _g2om = null;
            _eyeTrackingProvider = null;
        }

        private static void VerifyInstanceIntegrity()
        {
            if (_updaterGameObject != null) return;

            _updaterGameObject = new GameObject("TobiiXR")
            {
                hideFlags = HideFlags.HideInHierarchy
            };

            if (_eyeTrackingProvider == null)
            {
                Start();
            }

            var updater = _updaterGameObject.AddComponent<TobiiXR_Lifecycle>();

            updater.OnUpdateAction += _eyeTrackingProvider.Tick;
            updater.OnUpdateAction += Tick;

            updater.OnDisableAction += _g2om.Clear;

            updater.OnApplicationQuitAction += Stop;
        }

        private static void Tick()
        {
            var data = CreateDeviceData(EyeTrackingData);
            _g2om.Tick(data);
        }

        private static G2OM_DeviceData CreateDeviceData(TobiiXR_EyeTrackingData data)
        {
            var leftEyeOpen = data.Left.EyeOpennessIsValid ? data.Left.EyeOpenness > 0.5f : true;
            var rightEyeOpen = data.Right.EyeOpennessIsValid ? data.Right.EyeOpenness > 0.5f : true;

            return new G2OM_DeviceData
            {
                timestamp = data.Timestamp,
                combined = new G2OM_GazeRay(new G2OM_Ray(data.CombinedRay.Origin, data.CombinedRay.Direction), data.CombinedRay.IsValid && leftEyeOpen && rightEyeOpen),
                leftEye = new G2OM_GazeRay(new G2OM_Ray(data.Left.Ray.Origin, data.Left.Ray.Direction), data.Left.Ray.IsValid && leftEyeOpen),
                rightEye = new G2OM_GazeRay(new G2OM_Ray(data.Right.Ray.Origin, data.Right.Ray.Direction), data.Right.Ray.IsValid && rightEyeOpen)
            };
        }
    }
}