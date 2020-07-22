using UnityEngine;

public class CharMovement : MonoBehaviour
{
    #region Public Variables
    [Header("Movement Properties")]
    public float turnSmoothing;
    public float jumpHeight;
    public float jumpSpeed;
    public float frontCheckRayOffset;
    public float frontCheckRayDistance;
    public float groundCheckRayDistance;
    public LayerMask groundCheckLayerMask;

    [Header("Foot IK Properties")]
    public bool enableFootIK = true;
    [SerializeField]float heightFromGroundRaycast;
    [SerializeField] float raycastDownDistance;
    [SerializeField] LayerMask environmentLayer;
    [SerializeField] float pelvisOffset;
    [SerializeField] float pelvisUpAndDownSpeed;
    [SerializeField] float feetToIKPositionSpeed;
    public string leftFootAnimVariableName = "LeftFootCurve";
    public string rightFootAnimVariableName = "RightFootCurve";
    public bool useProIKFeature = false;
    public bool showSolverDebug = true;
    #endregion

    #region Private Variables
    Vector3 rightFootPosition, leftFootPosition, rightFootIKPosition, leftFootIKPosition, lastDirection;
    Quaternion rightFootIKRotation, leftFootIKRotation;

    Rigidbody rb;
    Camera cam;
    Animator anim;
    Collider col;
    ThirdPersonOrbitCamBasic tob;
    LockOnBehaviour lob;

    float defDynFric, defStatFric, h,v,canDashDelayer, actualJumpSpeed, 
        lastPelvisPositionY,lastRightFootPositionY,lastLeftFootPositionY;    
    bool isSprint, canDash,jump,isGrounded,onSlope,onCliff,colliderFront;

    bool canMove = true;
    #endregion

    #region Initialization
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        tob = cam.GetComponent<ThirdPersonOrbitCamBasic>();
        lob = GetComponent<LockOnBehaviour>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider>();
        defDynFric = col.material.dynamicFriction;
        defStatFric = col.material.staticFriction;
    }
    #endregion

    #region Update
    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Attack") && !anim.GetBool("Attack"))
        {
            anim.SetBool("Attack", true);
        }
        else if (Input.GetButtonDown("Attack") && !anim.GetBool("ContinueAttack"))
        {
            anim.SetBool("ContinueAttack", true);
        }

        if (!canMove)
            return;

        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");
        
        if (Input.GetButtonDown("Sprint") && anim.GetBool("Grounded") && canDash && lob.isLockOn)
        {
            anim.SetTrigger("Dash");
            canDashDelayer = 0;
            canDash = false;
        }

        if (!canDash)
        {
            if (canDashDelayer <= (21f / 60f))
            {
                canDashDelayer += Time.deltaTime;
            }
            else
            {
                canDash = true;
            }
        }

        isSprint = (Input.GetButton("Sprint"));

        if (isSprint)
        {
            float speed;
            if (h == 0 && v == 0)
            {
                speed = 0;
            }
            else
            {
                speed = (Mathf.Abs(h) + Mathf.Abs(v)) / (Mathf.Abs(h) + Mathf.Abs(v))*2;
            }
            float actualSpeed = Mathf.Lerp(anim.GetFloat("Speed"), speed, .1f);
            anim.SetFloat("Speed", actualSpeed);

            if (!lob.isLockOn)
            {
                tob.SetFOV(100f);
            }
        }
        else
        {
            float speed;
            if (h == 0 && v == 0)
            {
                speed = 0;
            }
            else
            {
                speed = (Mathf.Abs(h) + Mathf.Abs(v)) / (Mathf.Abs(h) + Mathf.Abs(v));
            }
            float actualSpeed = Mathf.Lerp(anim.GetFloat("Speed"), speed, .1f);
            anim.SetFloat("Speed", actualSpeed);

            tob.ResetFOV();
        }

        anim.SetFloat("H", h);
        anim.SetFloat("V", v);

        if (Input.GetButtonDown("Jump") && isGrounded && !anim.GetCurrentAnimatorStateInfo(0).IsName("Land") && !anim.GetBool("Attack"))
        {
            jump = true;
        }

    }

    void FixedUpdate()
    {
        if (enableFootIK)
        {
            AdjustFootTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
            AdjustFootTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

            FootPositionSolver(rightFootPosition, ref rightFootIKPosition, ref rightFootIKRotation);
            FootPositionSolver(leftFootPosition, ref leftFootIKPosition, ref leftFootIKRotation);
        }

        CheckGrounded();
        CheckFront();

        if (!canMove)
            return;

        Movement(h, v);
        Jump();
        
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (enableFootIK && isGrounded && h == 0 && v == 0)
        {
            MovePelvisHeight();

            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);

            if (useProIKFeature)
            {
                anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, anim.GetFloat(rightFootAnimVariableName));
            }

            MoveFootToIKPosition(AvatarIKGoal.RightFoot,
                rightFootIKPosition, rightFootIKRotation, ref lastRightFootPositionY);


            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);

            if (useProIKFeature)
            {
                anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, anim.GetFloat(leftFootAnimVariableName));
            }

            MoveFootToIKPosition(AvatarIKGoal.LeftFoot,
                leftFootIKPosition, leftFootIKRotation, ref lastLeftFootPositionY);
        }

        else
        {
            lastPelvisPositionY = anim.bodyPosition.y;
        }
    }

    #endregion

    #region Movement
    void Movement(float h, float v)
    {
        if (!anim.GetBool("LockOn"))
        {
            Vector3 forward = cam.transform.TransformDirection(Vector3.forward);
            forward.y = 0.0f;
            forward = forward.normalized;
            Vector3 right = cam.transform.TransformDirection(Vector3.right);
            right.y = 0.0f;
            right = right.normalized;

            Vector3 targetDirection = forward * v + right * h;

            if (rb.velocity != Vector3.zero && targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                if (anim.GetBool("Attack"))
                {
                    Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, 0.7f);
                    rb.MoveRotation(newRotation);
                }
                else
                {
                    Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSmoothing);
                    rb.MoveRotation(newRotation);
                }
                lastDirection = targetDirection;
            }
            else
            {
                if (lastDirection != Vector3.zero)
                {
                    lastDirection.y = 0.0f;

                    Quaternion targetRotation = Quaternion.LookRotation(lastDirection);

                    if (anim.GetBool("Attack"))
                    {
                        Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, 0.7f);
                        rb.MoveRotation(newRotation);
                    }
                    else
                    {
                        Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSmoothing);
                        rb.MoveRotation(newRotation);
                    }
                }
            }
        }
        else
        {
            if (lob != null)
            {
                if (anim.GetBool("Grounded"))
                {
                    var lookPos = lob.target.position - transform.position;
                    lookPos.y = 0;
                    Quaternion rotation = Quaternion.LookRotation(lookPos);
                    Quaternion newRotation = Quaternion.Slerp(rb.rotation, rotation, turnSmoothing);
                    rb.MoveRotation(newRotation);
                    lastDirection = lookPos;
                }
            }
        }
        if (onSlope &&( h != 0 || v != 0 ))
        {
            rb.velocity += Vector3.up * Physics.gravity.y * 3f * Time.deltaTime;
        }
        if (onCliff && isGrounded)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * 10f * Time.deltaTime;
            col.material.dynamicFriction = 0f;
            col.material.staticFriction = 0f;
        }
    }

    void Jump()
    {
        anim.SetBool("Jump", jump);

        if (jump && isGrounded)
        {
            RemoveVelocity();

            float velocity = 2f * Mathf.Abs(Physics.gravity.y) * jumpHeight;
            velocity = Mathf.Sqrt(velocity);
            
            rb.AddForce(Vector3.up * velocity, ForceMode.VelocityChange);
            jump = false;
            if (isSprint)
            {
                actualJumpSpeed = jumpSpeed * 2;
            }
            else
            {
                actualJumpSpeed = jumpSpeed;
            }
        }
        else if (!isGrounded)
        {
            col.material.dynamicFriction = 0;
            col.material.staticFriction = 0;

            if (rb.velocity.y < 0)
            {
                rb.velocity += Vector3.up * Physics.gravity.y * 5f * Time.deltaTime;                
            }

            else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
            {
                rb.velocity += Vector3.up * Physics.gravity.y * 2.5f * Time.deltaTime;
            }

            if (lob == null || !lob.isLockOn)
            {
                Vector3 forward = cam.transform.TransformDirection(Vector3.forward);
                forward.y = 0.0f;
                forward = forward.normalized;
                Vector3 right = cam.transform.TransformDirection(Vector3.right);
                right.y = 0.0f;
                right = right.normalized;

                if (!onCliff && !colliderFront)
                {
                    rb.AddForce(forward * actualJumpSpeed * v * 10 * Physics.gravity.magnitude, ForceMode.Acceleration);
                    rb.AddForce(right * actualJumpSpeed * h * 10 * Physics.gravity.magnitude, ForceMode.Acceleration);
                }
            }
            else
            {
                Vector3 forward = transform.TransformDirection(Vector3.forward);
                forward.y = 0.0f;
                forward = forward.normalized;
                Vector3 right = transform.TransformDirection(Vector3.right);
                right.y = 0.0f;
                right = right.normalized;

                if (!onCliff && !colliderFront)
                {
                    rb.AddForce(forward * actualJumpSpeed * v * 10 * Physics.gravity.magnitude, ForceMode.Acceleration);
                    rb.AddForce(right * actualJumpSpeed * h * 10 * Physics.gravity.magnitude, ForceMode.Acceleration);
                }
            }
        }
        else
        {
            col.material.dynamicFriction = defDynFric;
            col.material.staticFriction = defStatFric;
        }
    }

    void CheckGrounded()
    {
        RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, groundCheckRayDistance, groundCheckLayerMask))
            {
                if (hit.normal == Vector3.up)
                {
                    onSlope = false;
                    onCliff = false;
                    isGrounded = true;
                }
                else if (hit.normal.y >= .7f)
                {
                    onSlope = true;
                    onCliff = false;
                    isGrounded = true;
                }
                else
                {
                    onSlope = false;
                    onCliff = true;
                    isGrounded = false;
                }
            }
            else
            {
                isGrounded = false;
                onSlope = false;
            }
        Debug.DrawRay(transform.position + Vector3.up, Vector3.down*groundCheckRayDistance, Color.red);

        anim.SetBool("Grounded", isGrounded);
    }

    void CheckFront()
    {
        if (!lob.isLockOn)
        {
            RaycastHit hit;
            Debug.DrawRay(transform.position + Vector3.up * frontCheckRayOffset, transform.TransformDirection(Vector3.forward) * frontCheckRayDistance, Color.red);
            if (Physics.Raycast(transform.position + Vector3.up * frontCheckRayOffset, transform.TransformDirection(Vector3.forward), out hit, frontCheckRayDistance))
            {
                colliderFront = true;
            }
            else
            {
                colliderFront = false;
            }
        }
        else
        {
            Vector3 forward = cam.transform.TransformDirection(Vector3.forward);
            forward.y = 0.0f;
            forward = forward.normalized;
            Vector3 right = cam.transform.TransformDirection(Vector3.right);
            right.y = 0.0f;
            right = right.normalized;

            Vector3 targetDirection = forward * v + right * h;

            RaycastHit hit;
            Debug.DrawRay(transform.position + Vector3.up * frontCheckRayOffset, targetDirection * frontCheckRayDistance, Color.red);
            if (Physics.Raycast(transform.position + Vector3.up * frontCheckRayOffset, targetDirection, out hit, frontCheckRayDistance))
            {
                colliderFront = true;
            }
            else
            {
                colliderFront = false;
            }
        }
    }

    private void RemoveVelocity()
    {
        //Vector3 horizontalVelocity = rb.velocity;
        //horizontalVelocity.y = 0;
        //rb.velocity = horizontalVelocity;
        rb.velocity = Vector3.zero;
    }
    #endregion

    #region Foot IK

    void MoveFootToIKPosition(AvatarIKGoal foot, 
        Vector3 positionIKHolder, Quaternion rotationIKHolder, ref float lastFootPositionY)
    {
        Vector3 targetIKPosition = anim.GetIKPosition(foot);

        if (positionIKHolder != Vector3.zero)
        {
            targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
            positionIKHolder = transform.InverseTransformPoint(positionIKHolder);

            float yVariable = Mathf.Lerp(lastFootPositionY, positionIKHolder.y, feetToIKPositionSpeed);
            targetIKPosition.y += yVariable;

            lastFootPositionY = yVariable;

            targetIKPosition = transform.TransformPoint(targetIKPosition);

            anim.SetIKRotation(foot, rotationIKHolder);
        }

        anim.SetIKPosition(foot, targetIKPosition);
    }

    void MovePelvisHeight()
    {
        if (rightFootIKPosition == Vector3.zero || leftFootIKPosition == Vector3.zero || lastPelvisPositionY == 0)
        {
            lastPelvisPositionY = anim.bodyPosition.y;
            return;
        }

        float lOffsetPosition = leftFootIKPosition.y - transform.position.y;
        float rOffsetPosition = rightFootIKPosition.y - transform.position.y;

        float totalOffset = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;

        Vector3 newPelvisPosition = anim.bodyPosition + Vector3.up * totalOffset;

        newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpAndDownSpeed);

        anim.bodyPosition = newPelvisPosition;

        lastPelvisPositionY = anim.bodyPosition.y;
    }

    void FootPositionSolver(Vector3 fromSkyPosition, ref Vector3 footIKPositions, ref Quaternion footIKRotations)
    {
        RaycastHit footOutHit;

        if (showSolverDebug)
        {
            Debug.DrawLine(fromSkyPosition, 
                fromSkyPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast), Color.yellow);
        }

        if (Physics.Raycast(fromSkyPosition, Vector3.down, 
            out footOutHit, raycastDownDistance + heightFromGroundRaycast, environmentLayer))
        {
            footIKPositions = fromSkyPosition;
            footIKPositions.y = footOutHit.point.y + pelvisOffset;
            footIKRotations = Quaternion.FromToRotation(Vector3.up, footOutHit.normal) * transform.rotation;

            return;
        }

        footIKPositions = Vector3.zero;
    }

    void AdjustFootTarget(ref Vector3 footPositions, HumanBodyBones foot)
    {
        footPositions = anim.GetBoneTransform(foot).position;
        footPositions.y = transform.position.y + heightFromGroundRaycast;
    }

    #endregion

    #region public functions

    public void EnableCanMove()
    {
        canMove = true;        
    }

    public void DisableCanMove()
    {
        canMove = false;        
    }

    public void DoAttack()
    {
        canMove = false;
        anim.SetBool("ContinueAttack", false);
    }

    public void StopAttack()
    {
        anim.SetBool("Attack", false);
        anim.SetBool("ContinueAttack", false);
        canMove = true;
    }

    public void ContinueOrStopAttack()
    {
        if (!anim.GetBool("ContinueAttack"))
        {
            StopAttack();
        }
    }

    #endregion
}
