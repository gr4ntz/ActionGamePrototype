using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public GameObject lockOnCursor;

    MeshRenderer mr;

    // Start is called before the first frame update
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mr.isVisible)
        {
            if (!LockOnBehaviour.Instance.targets.Contains(this.transform))
            {
                LockOnBehaviour.Instance.targets.Add(this.transform);
            }
        }
        else
        {
            if (LockOnBehaviour.Instance.targets.Contains(this.transform))
            {
                LockOnBehaviour.Instance.targets.Remove(this.transform);
            }
        }

        if (LockOnBehaviour.Instance.target == this.transform && LockOnBehaviour.Instance.isLockOn)
        {
            lockOnCursor.SetActive(true);
        }
        else
        {
            lockOnCursor.SetActive(false);
        }
    }
}
