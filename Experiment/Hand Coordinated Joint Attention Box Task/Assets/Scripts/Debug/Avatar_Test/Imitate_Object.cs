using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Imitate_Object : MonoBehaviour {

    [SerializeField]
    Transform _object;

	// Update is called once per frame
	void Update ()
    {
        transform.localPosition = _object.localPosition;
        transform.localRotation = _object.localRotation;
    }
}
