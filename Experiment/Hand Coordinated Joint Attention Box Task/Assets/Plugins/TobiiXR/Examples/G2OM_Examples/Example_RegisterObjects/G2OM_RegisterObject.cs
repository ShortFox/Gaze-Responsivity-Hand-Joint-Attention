// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using UnityEngine;

    public class G2OM_RegisterObject : MonoBehaviour
    {
        private void OnEnable()
        {
            G2OM_ObjectRegistry.Registry.Add(gameObject.GetInstanceID(), gameObject);
            G2OM_ObjectRegistry.OnRegistryChange.Invoke();
        }

        private void OnDisable()
        {
            G2OM_ObjectRegistry.Registry.Remove(gameObject.GetInstanceID());
            G2OM_ObjectRegistry.OnRegistryChange.Invoke();
        }
    }
}