namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;

    [NetworkSettings(sendInterval = 0.05f)]   //Update States at 10fps
    public class SyncAgents : NetworkBehaviour
    {
        [SyncVar]
        private Vector3 syncPos;
        [SyncVar]
        private Quaternion syncRot;

        private float _snapDistance;                            //Distance discrepency that forces object to snap in place.

        private void Awake()
        {
            _snapDistance = this.transform.lossyScale.x/2;      //Snap if distance threshold is half the object's width apart.
        }

        private void Update()
        {
            if (isServer)
            {
                syncPos = this.transform.position;
                syncRot = this.transform.rotation;
                return;
            }

            if (Vector3.Distance(this.transform.position,syncPos) >= _snapDistance)
            {
                this.transform.position = syncPos;
                this.transform.rotation = syncRot;
            }
            else
            {
                this.transform.position = Vector3.Lerp(this.transform.position, syncPos, 0.1f);
                this.transform.rotation = Quaternion.Lerp(this.transform.rotation, syncRot, 0.1f);
            }
        }
    }
}
