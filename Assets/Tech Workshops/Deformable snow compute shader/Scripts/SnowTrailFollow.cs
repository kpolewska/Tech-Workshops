using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowTrailFollow : MonoBehaviour
{
    // The snow trail manager will use this object position as the center of the snow trail.
    void Update()
    {
        SnowTrailManager.Instance.SnowTrailCenterPosition = new Vector3(transform.position.x, 0, transform.position.z);
    }
}