namespace MQ.MultiAgent
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class TestInitRead : MonoBehaviour
    {

        ExpInitializer INIT;

        private void Awake()
        {
            INIT = ExpInitializer.Instance;
        }
        void Start()
        {
            Debug.Log("Curent Avail: " + INIT.IsAvailable);
            if (INIT.IsAvailable)
            {
                Debug.Log("Everything works: "+INIT.CurrentItem[0]+" "+INIT.CurrentItem[1]);
            }
            else
            {
                Debug.Log("Something is wrong");
            }
        }
    }
}

