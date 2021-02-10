// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM.Examples
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class G2OM_ObjectRegistry
    {
        public static readonly Dictionary<int, GameObject> Registry = new Dictionary<int, GameObject>();
        public static Action OnRegistryChange = delegate { };
    }
}