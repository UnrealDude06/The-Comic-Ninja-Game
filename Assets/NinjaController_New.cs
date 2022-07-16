using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaController_New : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;
    public Animator anim;
    CapsuleCollider capsule_collider;

   [Header("Movement")]
    public float speed;
    public float jumpSpeed, jumpShortSpeed, gravity, turnSpeed, maxSpeed, friction, acceleration, groundSlopeAngle;
    public bool isGrounded, stopAccelerating, canControl;
    bool jump, jumpCancel;
    Vector3 input, movement, localMove, groundSlopeDir;
    public float turnSpeedHigh, turnSpeedLow;

    [Header("Health")]
    public float hp;

    [Header("Energy Recharge")]  
    public float energy;
    public float energyChargingRate, energyDischargeRate;
    public float energyMax,energyAttackDamage;



    [Header("Combat")]  
    public float attackTimer; 
    float timeStamp;
    public float cooldown   = 1.3f;
    public int combo, maxCombo;   
    bool isAttacking,isAttacking2,isAttacking3;   
    public LayerMask enemyLayer;
    public float AttackRadius,attackDamage,attackDamage2,attackDamage3;
    public bool rolling;

    [Header("Dashing")]
    public bool dash;
    public float dashSpeed;


    //hidden
    float horizontalAxis;
    float verticalAxis;
    float offset_distance,old_maxSpeed,old_acc,old_deacc;
    Quaternion slope;
    RaycastHit hit;


    // Start is called before the first frame update
    void Start()
    {
    capsule_collider = GetComponent<CapsuleCollider>();
    old_acc=  acceleration ;
    old_deacc = friction ;
    old_maxSpeed=   maxSpeed ;
    GetOGspeed();
    }

   void Update()
    {
        RaycastHit hit_ground;

        if (Physics.Raycast(transform.position, -transform.up, out hit_ground, 2f, LayerMask.GetMask("Default")))
        { //1 for the distance orignally
            if (hit_ground.collider != gameObject)
            {
                offset_distance = hit_ground.distance;
                Debug.DrawLine(transform.position, hit_ground.point, Color.cyan);
                if (offset_distance <= 5f)
                {
                    isGrounded = true;
                }

            }
        }
        else
        {
            isGrounded = false;
        }





        if (Input.GetButtonDown("Jump") && isGrounded)   // Player starts pressing the button
        { jump = true; }
        if (Input.GetButtonUp("Jump") && !isGrounded)     // Player stops pressing the button
        { jumpCancel = true; }


        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        input = Vector2.ClampMagnitude(input, 1);

        //camera forward and right vectors:
        var forward = Camera.main.transform.forward;
        var right = Camera.main.transform.right;

        //project forward and right vectors on the horizontal plane (y = 0)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();


        //this is the direction in the world space we want to move:
        movement = forward * input.y + right * input.x;



        localMove.x = (Input.GetAxisRaw("Horizontal") * right.x) + (Input.GetAxisRaw("Vertical") * forward.x) * speed;
        //Same with Z except dictates how he will move across the Z plane
        localMove.z = (Input.GetAxisRaw("Horizontal") * right.z) + (Input.GetAxisRaw("Vertical") * forward.z) * speed;





        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x) + Mathf.Abs(rb.velocity.z));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("Yvelocity", rb.velocity.y);
        
        anim.SetBool("isAttacking", isAttacking);
        anim.SetBool("isAttacking2", isAttacking2);
        anim.SetBool("isAttacking3", isAttacking3);
        anim.SetBool("canControl",canControl);
        anim.SetBool("Rolling",rolling);
        anim.SetBool("dash",dash);


        if (Physics.Raycast(transform.position, -(transform.up), out hit, 2.5f, LayerMask.GetMask("Default")))
        {
            Vector3 temp = Vector3.Cross(hit.normal, Vector3.down);
            groundSlopeDir = Vector3.Cross(temp, hit.normal);
            groundSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (isGrounded)
            {
                slope = Quaternion.FromToRotation(transform.up, hit.normal);
                transform.rotation = (slope) * transform.rotation;

            }
            else
            {
                Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), 0.1f * Time.deltaTime);
            }

        }
        if(dash ==false)
        {
            if(speed < 22)
            {
            MeleeAttack();
            }
            else
            {
                MeeleSttackRollSpeed();
            }
        }
        Dash();

        DoFastOnSlope();
        EnergyUsage();
        
        
    }

    void EnergyUsage()
    {
        if(Input.GetButton("Fire2") && energy >=0)
        {
            energy += energyChargingRate * Time.deltaTime;

        }
        //clamp energy to max
        energy = Mathf.Clamp(energy,0,energyMax);
    }


    
    void FixedUpdate()
    {

        rb.AddRelativeForce(Vector3.down * gravity * Time.deltaTime, ForceMode.Force);



        if (rb.velocity.magnitude > maxSpeed)
        {
            Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        }




        if (input.magnitude > 0)
        {
            Quaternion rot = Quaternion.LookRotation(movement);

            transform.rotation = Quaternion.Lerp(transform.rotation, rot, turnSpeed * Time.deltaTime);


        }

        float tS = speed / 5;

        turnSpeed = Mathf.Lerp(turnSpeedHigh, turnSpeedLow, tS);

        movement = transform.forward * speed;
        if (canControl)
        {
           
          
             

            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
         
        }

        //rb.AddForce ( movement * Time.deltaTime,ForceMode.VelocityChange);
        if (stopAccelerating == false)
        {
            if (input.magnitude != 0)
            {
                speed = Mathf.MoveTowards(speed, maxSpeed * input.magnitude, acceleration * Time.deltaTime);
            }
            //////////////////////////////////////////////////
            else
            {
                // apply deceleration unless slope is too steep and player is going downhill
                if (Vector3.Angle(hit.normal, Vector3.up) < 45f || rb.velocity.y > 0f)
                {
                    speed *= 1f - (  friction);
                }
            }
        }



        ///////////////////////////////////////////////////////////

        if (isGrounded)
        {
            //If the angle between the stick and the player's direction is larger than 150 degrees, incur a skid penalty.
            if (Vector3.Angle(transform.forward, localMove) >= 130 && speed != 0 && isGrounded)
            { SkidTurnaround(); }


        }



        // Normal jump (full speed)
        if (jump && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpSpeed, rb.velocity.z);
            jump = false;
        }
        // Cancel the jump when the button is no longer pressed
        if (jumpCancel)
        {
            if (rb.velocity.y > jumpShortSpeed)
                rb.velocity = new Vector3(rb.velocity.x, jumpShortSpeed, rb.velocity.z);
            jumpCancel = false;
        }

    }

void Dash()
{
    if(Input.GetButtonDown("Fire1") && !dash && !isGrounded)
    {
      

            dash =true;
           canControl = false;

        
        
    }
    if(dash)
    {
        canControl =  false;
         Vector3 fwd  = transform.forward * dashSpeed;
         fwd.y = rb.velocity.y;
            rb.velocity = fwd;
            if(isGrounded)
            {
                dash =false;
                 canControl = true;
            }
    }
}

void MeleeAttack()
     {
       
         if (timeStamp < Time.time && Input.GetButtonDown("Fire1") && combo < maxCombo)
         {
             combo++;
              energy -= energyDischargeRate ;
             Debug.Log("Attack " + combo);
             timeStamp = Time.time + cooldown;
         }
 
         if ((Time.time - timeStamp) > cooldown)
         {
             combo = 0;

           isAttacking= false;
           isAttacking2= false;
            isAttacking3= false;
            canControl = true;
         }
 
         if (combo == 1)
         {
            isAttacking=true;
            canControl = false;
           
         }
 
         if (combo == 2)
         {
            isAttacking2= true;
            isAttacking =false;
            canControl= false;
           
         }
 
         if (combo == 3)
         {
            isAttacking3= true;
            isAttacking2= false;
            canControl =false;
           
         }


         DoDamage();
     }


    void MeeleSttackRollSpeed()
    {
        if(Input.GetButtonDown("Fire1") && !rolling)
        {
            //roll
            speed --;
            rolling = true;

        }    
        if(rolling)
        {
            StartCoroutine(StopRoll(1f));
        combo = 1;
        DoDamage();
        }    


    }

    IEnumerator StopRoll(float time)
    {
        yield return new WaitForSeconds(time);
        rolling = false;
    }



     void DoDamage()
     {
         
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, AttackRadius,enemyLayer);
        if (hitColliders.Length != 0)
        {

        Debug.Log("Found something!");
        }

        if(hitColliders.Length !=0)
        {
            if((combo >0 && Input.GetButtonDown("Fire1")) || rolling)
            {
            foreach(Collider nearbyObject in hitColliders)
                {
                // Assuming that the enemy gameobject with the collider also holds the EnemyHealth script (!)
                EnemyScr enemy = nearbyObject.GetComponent<EnemyScr>();
                if(energy > 0 )
                {
                enemy.hp -=energyAttackDamage ;
                }
                else
                {
                enemy.hp -= attackDamage;
                }
                }
            }
        
     }
    }

     private void OnDrawGizmos()
    {
          Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRadius);
        Debug.DrawRay(transform.position,transform.right * 1f);
    }


    void DoFastOnSlope()
    {
        if(!(input.magnitude == 0))
            {
        if(groundSlopeAngle > 10 && transform.localEulerAngles.x > 13)
        {
            
            SetFastMovement(24,  4,  0.02f);
            }
       
            
        }
        else
        {
              if((input.magnitude == 0))
            {
                if(speed < 7)
                {
            SetDefaultMovement();
                }
            }
        }
        

    }







    void SkidTurnaround()
    {
        if (isGrounded)
        {
            transform.forward *= -1;
            float skidVelocity = (speed * -1);

            speed = skidVelocity / 2;
        }
    }


    void GetOGspeed()
    {
    old_maxSpeed = maxSpeed;
    old_acc = acceleration;
    old_deacc = friction;
    }
    void SetFastMovement(float new_maxSpeed,float new_acc,float new_deacc)
    {
            acceleration= new_acc;
            friction =new_deacc;
            maxSpeed = new_maxSpeed;

              if((input.magnitude == 0))
            {
            SetDefaultMovement();
            }
    }
void SetDefaultMovement()
    {
            acceleration= old_acc;
            friction =old_deacc;
            maxSpeed = old_maxSpeed;
    }
}
