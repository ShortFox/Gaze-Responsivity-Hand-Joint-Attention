// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM
{
    using System.Collections.Generic;
    using UnityEngine;

    public class G2OM_ObjectFinder : IG2OM_ObjectFinder
    {
        private const int RaysPerSecond = 900;
        private const int MinimumRaysPerFrame = 3;
        private const int MaxRaysPerFrame = 15;
        private const float RaycastLength = float.MaxValue;
        private const float FovealAngleDeg = 2f;

        internal struct RawRay
        {
            public Vector3 origin;
            public Vector3 direction;

            public RawRay(ref G2OM_Ray ray)
            {
                origin.x = ray.origin.x;
                origin.y = ray.origin.y;
                origin.z = ray.origin.z;
                direction.x = ray.direction.x;
                direction.y = ray.direction.y;
                direction.z = ray.direction.z;
            }
        }

        private readonly List<RawRay> _rays = new List<RawRay>(MaxRaysPerFrame);
        private LayerMask _layerMask = ~0;

        private float _previousTimestamp;

        public G2OM_ObjectFinder(float now = 0)
        {
            _previousTimestamp = now;
        }

        public void GetRelevantGazeObjects(ref G2OM_DeviceData deviceData, Dictionary<int, GameObject> foundObjects,
            IG2OM_ObjectDistinguisher distinguisher)
        {
            var numberOfRaysThisFrame = GetNumberOfRays(deviceData.timestamp - _previousTimestamp);
            _previousTimestamp = deviceData.timestamp;

            CreateMutatedRays(ref deviceData, numberOfRaysThisFrame, _rays);

            FindObjects(_rays, foundObjects, distinguisher, _layerMask);
        }

        // TODO: Is it possible to remove the redudant raycasts?
        public G2OM_RaycastResult GetRaycastResult(ref G2OM_DeviceData deviceData,
            IG2OM_ObjectDistinguisher distinguisher)
        {
            var raycastResult = new G2OM_RaycastResult();

            GameObject go;
            var result = FindGameObject(ref deviceData.combined, _layerMask, out go);
            if (result)
            {
                var id = go.GetInstanceID();
                var hitACandidate = distinguisher.IsGameObjectGazeFocusable(id, go);
                raycastResult.combined = new G2OM_Raycast(hitACandidate, id);
            }

            result = FindGameObject(ref deviceData.leftEye, _layerMask, out go);
            if (result)
            {
                var id = go.GetInstanceID();
                var hitACandidate = distinguisher.IsGameObjectGazeFocusable(id, go);
                raycastResult.left = new G2OM_Raycast(hitACandidate, id);
            }

            result = FindGameObject(ref deviceData.rightEye, _layerMask, out go);
            if (result)
            {
                var id = go.GetInstanceID();
                var hitACandidate = distinguisher.IsGameObjectGazeFocusable(id, go);
                raycastResult.right = new G2OM_Raycast(hitACandidate, id);
            }

            return raycastResult;
        }

        public void SetLayerMask(LayerMask layerMask)
        {
            _layerMask = layerMask;
        }

        private static int GetNumberOfRays(float dt)
        {
            var rays = Mathf.CeilToInt(RaysPerSecond * dt);
            return Mathf.Clamp(rays, MinimumRaysPerFrame, MaxRaysPerFrame);
        }

        private static void FindObjects(List<RawRay> rays, Dictionary<int, GameObject> foundObjects,
            IG2OM_ObjectDistinguisher distinguisher, LayerMask layerMask)
        {
            foundObjects.Clear();

            foreach (var ray in rays)
            {
                GameObject go;
                if (FindGameObject(ray, layerMask, out go) == false) continue;

                var id = go.GetInstanceID();
                if (foundObjects.ContainsKey(id)) continue;

                if (distinguisher.IsGameObjectGazeFocusable(id, go) == false) continue;

                foundObjects.Add(id, go);
            }
        }

        private static bool FindGameObject(ref G2OM_GazeRay gazeRay, LayerMask layerMask, out GameObject gameObject)
        {
            gameObject = null;
            return gazeRay.IsValid && FindGameObject(new RawRay(ref gazeRay.ray), layerMask, out gameObject);
        }

        private static bool FindGameObject(RawRay ray, LayerMask layerMask, out GameObject gameObject)
        {
            gameObject = null;
            RaycastHit hit;
            if (Physics.Raycast(ray.origin, ray.direction, out hit, RaycastLength, layerMask) == false) return false;

            gameObject = hit.collider.gameObject;
            return true;
        }

        private static void CreateMutatedRays(ref G2OM_DeviceData deviceData, int numberOfRays, IList<RawRay> rays)
        {
            rays.Clear();

            if (deviceData.combined.IsValid)
            {
                rays.Add(new RawRay(ref deviceData.combined.ray));
            }

            if (deviceData.leftEye.IsValid)
            {
                rays.Add(new RawRay(ref deviceData.leftEye.ray));
            }

            if (deviceData.rightEye.IsValid)
            {
                rays.Add(new RawRay(ref deviceData.rightEye.ray));
            }

            if (rays.Count == 0) return; // If we don't have any source rays to mutate, early return

            var numberOfInitialRays = rays.Count;
            var rayIndex = numberOfInitialRays;
            var indexOffset = Random.Range(0, Rotations.Length);

            while (rayIndex < numberOfRays)
            {
                for (int i = 0; i < numberOfInitialRays && i + rayIndex < numberOfRays; i++)
                {
                    var sourceRay = rays[i];
                    var randomRotation = Rotations[(rayIndex + indexOffset) % Rotations.Length];
                    var mutatedDirection = randomRotation * sourceRay.direction;
                    rays.Add(new RawRay {origin = sourceRay.origin, direction = mutatedDirection});
                }

                rayIndex += numberOfInitialRays;
            }

#if UNITY_EDITOR
            for (int i = 0; i < numberOfInitialRays; i++)
            {
                Debug.DrawRay(rays[i].origin, rays[i].direction * 100, Color.red);
            }

            for (int i = numberOfInitialRays; i < numberOfRays; i++)
            {
                Debug.DrawRay(rays[i].origin, rays[i].direction * 100, Color.green);
            }
#endif
        }

        private static readonly Quaternion[] Rotations =
        {
            Quaternion.AngleAxis(1, Vector3.right),
            Quaternion.AngleAxis(0.7f, Vector3.up) * Quaternion.AngleAxis(-0.7f, Vector3.right),
            Quaternion.AngleAxis(-0.7f, Vector3.up) * Quaternion.AngleAxis(-0.7f, Vector3.right),
            Quaternion.AngleAxis(1, Vector3.up),

            Quaternion.AngleAxis(-1, Vector3.right),
            Quaternion.AngleAxis(0.7f, Vector3.up) * Quaternion.AngleAxis(0.7f, Vector3.right),
            Quaternion.AngleAxis(-0.7f, Vector3.up) * Quaternion.AngleAxis(0.7f, Vector3.right),
            Quaternion.AngleAxis(-1, Vector3.up),

            Quaternion.AngleAxis(FovealAngleDeg * 1, Vector3.right),
            Quaternion.AngleAxis(FovealAngleDeg * 0.7f, Vector3.up) *
            Quaternion.AngleAxis(FovealAngleDeg * -0.7f, Vector3.right),
            Quaternion.AngleAxis(FovealAngleDeg * -0.7f, Vector3.up) *
            Quaternion.AngleAxis(FovealAngleDeg * -0.7f, Vector3.right),
            Quaternion.AngleAxis(FovealAngleDeg, Vector3.up),

            Quaternion.AngleAxis(FovealAngleDeg * -1, Vector3.right),
            Quaternion.AngleAxis(FovealAngleDeg * 0.7f, Vector3.up) *
            Quaternion.AngleAxis(FovealAngleDeg * 0.7f, Vector3.right),
            Quaternion.AngleAxis(FovealAngleDeg * -0.7f, Vector3.up) *
            Quaternion.AngleAxis(FovealAngleDeg * 0.7f, Vector3.right),
            Quaternion.AngleAxis(-FovealAngleDeg, Vector3.up),

            Quaternion.AngleAxis(3 * 1, Vector3.right),
            Quaternion.AngleAxis(3 * 0.7f, Vector3.up) * Quaternion.AngleAxis(3 * -0.7f, Vector3.right),
            Quaternion.AngleAxis(3 * -0.7f, Vector3.up) * Quaternion.AngleAxis(3 * -0.7f, Vector3.right),
            Quaternion.AngleAxis(3, Vector3.up),

            Quaternion.AngleAxis(3 * -1, Vector3.right),
            Quaternion.AngleAxis(3 * 0.7f, Vector3.up) * Quaternion.AngleAxis(3 * 0.7f, Vector3.right),
            Quaternion.AngleAxis(3 * -0.7f, Vector3.up) * Quaternion.AngleAxis(3 * 0.7f, Vector3.right),
            Quaternion.AngleAxis(-3, Vector3.up),

            Quaternion.AngleAxis(4 * 1, Vector3.right),
            Quaternion.AngleAxis(4 * 0.7f, Vector3.up) * Quaternion.AngleAxis(4 * -0.7f, Vector3.right),
            Quaternion.AngleAxis(4 * -0.7f, Vector3.up) * Quaternion.AngleAxis(4 * -0.7f, Vector3.right),
            Quaternion.AngleAxis(4, Vector3.up),

            Quaternion.AngleAxis(4 * -1, Vector3.right),
            Quaternion.AngleAxis(4 * 0.7f, Vector3.up) * Quaternion.AngleAxis(4 * 0.7f, Vector3.right),
            Quaternion.AngleAxis(4 * -0.7f, Vector3.up) * Quaternion.AngleAxis(4 * 0.7f, Vector3.right),
            Quaternion.AngleAxis(-4, Vector3.up),

            Quaternion.AngleAxis(5 * 1, Vector3.right),
            Quaternion.AngleAxis(5 * 0.7f, Vector3.up) * Quaternion.AngleAxis(5 * -0.7f, Vector3.right),
            Quaternion.AngleAxis(5 * -0.7f, Vector3.up) * Quaternion.AngleAxis(5 * -0.7f, Vector3.right),
            Quaternion.AngleAxis(5, Vector3.up),

            Quaternion.AngleAxis(5 * -1, Vector3.right),
            Quaternion.AngleAxis(5 * 0.7f, Vector3.up) * Quaternion.AngleAxis(5 * 0.7f, Vector3.right),
            Quaternion.AngleAxis(5 * -0.7f, Vector3.up) * Quaternion.AngleAxis(5 * 0.7f, Vector3.right),
            Quaternion.AngleAxis(-5, Vector3.up),
        };
    }
}