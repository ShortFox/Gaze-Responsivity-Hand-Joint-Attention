namespace MQ.MultiAgent
{
    using UnityEngine;

    /// <summary>
    /// This script disables child game-objects if it is childed to Player character and it does not belong to LocalPlayer.
    /// </summary>
    public class DisableObjectOnStart : MonoBehaviour
    {
        void Start()
        {
            Transform myCamera = this.transform.parent.Find("Camera");
            if (myCamera.tag != "MainCamera") this.gameObject.SetActive(false);
        }
    }
}

