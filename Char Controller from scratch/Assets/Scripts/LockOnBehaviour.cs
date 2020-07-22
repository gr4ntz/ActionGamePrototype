using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnBehaviour : MonoBehaviour
{
    public static LockOnBehaviour Instance;

    public bool isLockOn;
    public Transform target;
    public List<Transform> targets;

    Animator anim;
    Camera cam;

    float h;
    Transform newTarget;
    bool canSwitch = true;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (targets.Count == 0)
        {
            target = null;
        }
        for (int i = 0; i < targets.Count; i++)
        {
            if (target == null)
            {
                target = targets[i];
            }
            else
            {
                Vector3 targetDistance = cam.WorldToViewportPoint(target.position);
                float targetDis = Mathf.Abs(targetDistance.x - 0.5f) + Mathf.Abs(targetDistance.y - 0.5f);

                Vector3 thisDistance = cam.WorldToViewportPoint(targets[i].position);
                float thisDis = Mathf.Abs(thisDistance.x - 0.5f) + Mathf.Abs(thisDistance.y - 0.5f);

                if (thisDis<targetDis)
                {
                    target = targets[i];
                }
            }
        }

        isLockOn = anim.GetBool("LockOn");

        if (Input.GetButtonDown("LockOn")&& targets.Count !=0)
        {
            anim.SetBool("LockOn", !isLockOn);
        }

        //switchAxis
        // Joystick:
        h = Input.GetAxis("SwitchLockOn");
        if (h > 0 && canSwitch && isLockOn)
        {
            float newFloat = 2;
            for (int i = 0; i < targets.Count; i++)
            {

                
                if (targets[i] != target)
                {
                    Vector3 thisDistance = cam.WorldToViewportPoint(targets[i].position);
                    float thisDis = thisDistance.x - 0.5f;

                    if (thisDis < newFloat && thisDis > 0)
                    {
                        newTarget = targets[i];
                        newFloat = thisDis;
                    }
                }
            }
            target = newTarget;
            canSwitch = false;
        }
        else if (h < 0 && canSwitch && isLockOn)
        {
            float newFloat = -2;
            for (int i = 0; i < targets.Count; i++)
            {

                
                if (targets[i] != target)
                {
                    Vector3 thisDistance = cam.WorldToViewportPoint(targets[i].position);
                    float thisDis = thisDistance.x - 0.5f;

                    if (thisDis > newFloat && thisDis < 0)
                    {
                        newTarget = targets[i];
                        newFloat = thisDis;
                    }
                }
            }
            target = newTarget;
            canSwitch = false;
        }
        else if (h == 0 && !canSwitch)
        {
            canSwitch = true;
        }
    }
}
