// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using UnityEngine;

    public class G2OM_RegisterObjectDistinguisher : IG2OM_ObjectDistinguisher
    {
        public bool IsGameObjectGazeFocusable(int id, GameObject gameObject)
        {
            return G2OM_ObjectRegistry.Registry.ContainsKey(id);
        }
    }
}