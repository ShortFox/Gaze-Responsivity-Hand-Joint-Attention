// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using System.Collections.Generic;
    using Tobii.G2OM;
    using UnityEngine;

    public class G2OM_RegisterObjectsFinder : IG2OM_ObjectFinder
    {
        private const float RaycastLength = float.MaxValue;

        private LayerMask _layerMask = ~0;

        public void GetRelevantGazeObjects(ref G2OM_DeviceData deviceData, Dictionary<int, GameObject> foundObjects, IG2OM_ObjectDistinguisher distinguisher)
        {
            foundObjects.Clear();

            foreach (var registeredObject in G2OM_ObjectRegistry.Registry)
            {
                foundObjects.Add(registeredObject.Key, registeredObject.Value);
            }
        }

        public G2OM_RaycastResult GetRaycastResult(ref G2OM_DeviceData deviceData, IG2OM_ObjectDistinguisher distinguisher)
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

        private static bool FindGameObject(ref G2OM_GazeRay gazeRay, LayerMask layerMask, out GameObject gameObject)
        {
            gameObject = null;

            if (gazeRay.IsValid == false) return gameObject;

            RaycastHit hit;
            if (Physics.Raycast(gazeRay.ray.origin.Vector, gazeRay.ray.direction.Vector, out hit, RaycastLength, layerMask) == false) return false;

            gameObject = hit.collider.gameObject;
            return true;
        }

        public void SetLayerMask(LayerMask layerMask)
        {
            _layerMask = layerMask;
        }
    }
}