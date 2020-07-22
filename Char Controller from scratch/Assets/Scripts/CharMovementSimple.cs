using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharMovementSimple : MonoBehaviour
{
    public float movementSpeed;

    public float turnSmoothing;    

    Rigidbody rb;
    Camera cam;

    float h;
    float v;
    Vector3 lastDirection;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");
    }

    private void FixedUpdate()
    {
        Movement(h, v);
    }

    void Movement(float h,float v)
    {
            Vector3 forward = cam.transform.TransformDirection(Vector3.forward);
            forward.y = 0.0f;
            forward = forward.normalized;
            Vector3 right = cam.transform.TransformDirection(Vector3.right);
            right.y = 0.0f;
            right = right.normalized;

            rb.AddForce(forward *movementSpeed * v, ForceMode.Acceleration);
            rb.AddForce(right * movementSpeed * h, ForceMode.Acceleration);


            Vector3 targetDirection = forward * v + right * h;

            if (rb.velocity != Vector3.zero && targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSmoothing);
                rb.MoveRotation(newRotation);
                lastDirection = targetDirection;
            }
            else
            {
                if (lastDirection != Vector3.zero)
                {
                    lastDirection.y = 0.0f;

                    Quaternion targetRotation = Quaternion.LookRotation(lastDirection);

                    Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSmoothing);
                    rb.MoveRotation(newRotation);
                }
            }

    }
}
