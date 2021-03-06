using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player : Vulnerable
{
    private Rigidbody rigidBody;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] float movementSpeed = 9f;
    [SerializeField] float helicopterMovementSpeed = 3.0f;

    private PowerUpsEnum powerUp = PowerUpsEnum.NONE;

    float playerSpeedMultiplier;
    private float helicopterDescendingSpeed = -1.0f;

    private Animator animator;
    private float distToGround;
    private float horizontalAxisInput;
    private float verticalAxisInput;
    private bool jumpInput;
    private bool isUsingHelicopter;
    private bool isGrounded;

    private bool hitInput;
    private Gun fistShooter;
    private Gun fistShooterStrengthPowerUp;

    private new Collider collider;

    private float rotation = 80;
    private Vector3 mouseLookingAt;

	private GameObject raymanBody;

    private float recurrentHealthLost = 1;
    private float recurrentHealthLostTime = 1;

    private Material defaultMaterial;

    private void ReduceHealthByTime()
    {
        TakeDamage(recurrentHealthLost);
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        collider = GetComponent<Collider>();
        distToGround = collider.bounds.extents.y;
        fistShooter = GetComponent<Gun>();
		raymanBody = this.gameObject.transform.Find("rayman").gameObject.transform.Find("Body").gameObject;
        fistShooter = this.gameObject.transform.Find("FistShooter").GetComponent<Gun>();
        fistShooterStrengthPowerUp = this.gameObject.transform.Find("FistShooterStrengthPowerUp").GetComponent<Gun>();

        defaultMaterial = raymanBody.GetComponent<Renderer>().material;

        //TODO va a traer problemas cuando se recargue la escena?
        InvokeRepeating(nameof(ReduceHealthByTime), recurrentHealthLostTime, recurrentHealthLostTime);
	}

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        GetInputs();
        GetCircumstances();
        CalculateMovingSpeedAndApplyRotation();
        HandleMovementCases();
        HandleShoot();
        SetAnimatorParameters();
    }

    void HandleShoot()
    {
        if (hitInput)
        {
            Gun gun = powerUp == PowerUpsEnum.STRENGTH ? fistShooterStrengthPowerUp : fistShooter;
            if(gun != null)
            {
                StartCoroutine(AnimatePunch());
                gun.Attack(null);
            }
        }
    }

    void HandleMovementCases()
    {
        if (jumpInput && isGrounded)
        {
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, jumpSpeed, rigidBody.velocity.z);
        }

        if (isGrounded)
        {
            //audioSource.Stop();
            isUsingHelicopter = false;
			raymanBody.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(22, 0);
		}

        if(jumpInput)
        {
            if(isUsingHelicopter)
            {
                isUsingHelicopter = false;
				raymanBody.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(22, 0);
			}
            else if(!isGrounded && !isUsingHelicopter)
            {
                isUsingHelicopter = true;
				raymanBody.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(22, 100);
			}
        }

        if (isUsingHelicopter)
        {
            playerSpeedMultiplier = helicopterMovementSpeed;

            if(powerUp != PowerUpsEnum.HELICOPTER)
            {
                rigidBody.velocity = new Vector3(rigidBody.velocity.x, helicopterDescendingSpeed, rigidBody.velocity.z);
            }
            else
            {
                rigidBody.velocity = new Vector3(rigidBody.velocity.x, jumpSpeed, rigidBody.velocity.z);
            }
        }
        else
        {
            playerSpeedMultiplier = movementSpeed;
        }
    }

    void CalculateMovingSpeedAndApplyRotation()
    {
        Vector3 resultVelocity = new Vector3(horizontalAxisInput, 0, verticalAxisInput);
        resultVelocity.Normalize();
        resultVelocity = Quaternion.AngleAxis(rotation, Vector3.up) * resultVelocity;
        resultVelocity *= playerSpeedMultiplier;
        rigidBody.velocity = new Vector3(resultVelocity.x, rigidBody.velocity.y, resultVelocity.z);
        if (CurrentlyMoving())
        {
            transform.rotation = Quaternion.Euler(0,
                GetFlatVelocityAbsoluteAngle(new Vector3(horizontalAxisInput, 0, verticalAxisInput)),
                0);
        }
    }

    bool CurrentlyMoving()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;
    }

    float GetFlatVelocityAbsoluteAngle(Vector3 flatVelocity)
    {
        Vector3 forwardVector = new Vector3(0, 0, 1.0f);
        Vector3 standardizedFlatVelocity = Quaternion.AngleAxis(0.0f, Vector3.down) * flatVelocity;
        bool right = standardizedFlatVelocity.x >= 0.0f;
        float angle = Vector3.Angle(forwardVector, flatVelocity);
        return right ? angle + rotation : 360f - angle + rotation;
    }

    void SetAnimatorParameters()
    {
        animator.SetFloat("FlatSpeedAbsoluteValue", new Vector3(rigidBody.velocity.x, 0, rigidBody.velocity.z).magnitude);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("JumpPressed", jumpInput);
        animator.SetFloat("VerticalSpeedValue", rigidBody.velocity.y);
        animator.SetBool("IsUsingHelicopter", isUsingHelicopter);
    }

    void GetCircumstances()
    {
        isGrounded = IsGrounded();
    }

    void GetInputs()
    {
        horizontalAxisInput = Input.GetAxisRaw("Horizontal");
		verticalAxisInput = Input.GetAxisRaw("Vertical");
        jumpInput = Input.GetButtonDown("Jump");

        hitInput = Input.GetMouseButtonDown(0);
    }

	bool IsGrounded()
	{
        int layers = LayerMask.GetMask("Ground", "Enemies");
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.1f, layers);
	}

	public void SetRotation(float rotation)
    {
        this.rotation = rotation;
    }

    public void SetMouseLookingAt(Vector3 mouseLookingAt)
    {
        this.mouseLookingAt = mouseLookingAt;
    }

    protected override void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ApplyPowerUp(PowerUpsEnum powerUp, float duration, Material material)
    {
        CancelInvoke(nameof(UndoPowerUp));
        raymanBody.GetComponent<Renderer>().material = material;
        this.powerUp = powerUp;
        Invoke(nameof(UndoPowerUp), duration);
    }

    private void UndoPowerUp()
    {
        raymanBody.GetComponent<Renderer>().material = defaultMaterial;
        this.powerUp = PowerUpsEnum.NONE;
    }

    IEnumerator AnimatePunch()
    {
        animator.SetBool("isPunching", true);
        yield return new WaitForSeconds(0.4f);
        animator.SetBool("isPunching", false);
    }
}
