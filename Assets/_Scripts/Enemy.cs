using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {
    // may need gameobjects for each enemy/teammate. Loop to circulate through components? if one has flag get him, if teammate down revive. Go get flag
    public GameObject target;
    public GameObject enemy1;
    public GameObject enemy2;
    public GameObject gameOverCanvas;
    public Enemy teamMate;
    public Flag flag;
    public EnemyHome home;
    private Controller controller;
    private GameObject childComponent;

    public bool kinematic = false; // click "k", change movement behavior to Kinematic
    public bool steering = false;  // click "s", change movement behavior to Steering
    public bool a = false;         // click "a", change movement to A.i and A.ii from instructions
    public bool b = false;         // click "b", change movement to B.i and B.ii from instructions
    public bool c = false;         // click "c", change movement to C.i and C.ii from instructions
    public bool frozen;            // if true, have teammate come unfreeze
    public bool hasFlag;           // if he has te flag;
    bool getFlag = false;          // if true, means character is currently going after flag
    bool hasTarget;                // if character going after one of enemies
    bool targetCaught;             // true if caught enemy in their zone
    bool stopSteeringArrive;       // // if true, stop steering arrive/ If false, dont stop.

    int flagGetter; // holds number which decides who is getting flag

    // parameters for Arrive/Flee
    float speed = 60.0f;         // basic movements speed
    float nearSpeed = 40.0f;     // speed when outside largest radius
    float arrivingSpeed = 30.0f; // speed when approaching smallest radius of target
    float farRadius = 200.0f;    // largest radius
    float midRadius = 150.0f;    // medium radius
    float arrivalRadius = 60.0f; // smallest radius
    float clamptoFloor = 0.5f;   // parameter to keep character to floor

    // parameters for Steering Arive
    public Vector3 velocity;
    public int maxAcceleration = 2; //6
    public float slowRadius = 5.0f;
    public float timeToTarget = 0.1f;
    public float steerSpeed = 80.0f;

    // parameters for Align
    Vector3 goalFacing;             // if he has somewhere he needs to be looking
    float rotationSpeedRads = 1.0f; // speed at which he rotates
    Quaternion lookWhereYoureGoing; // look in direction of where he's going

    // parameters for Wander
    Vector3 wayPoint;    // vector used to create different waypoints
    float range = 50.0f; // range in which waypoints will be created
    bool wanderOn = true;
    bool wanderMove = true;

    Vector3 targetSpeed; // speed of target being pursued 
    float minDistance = 5.0f;

    void Start()
    {
        childComponent = transform.GetChild(0).gameObject; // get first child object
        Renderer rend = childComponent.GetComponent<Renderer>();
        rend.material.shader = Shader.Find("Specular");
        rend.material.SetColor("_SpecColor", Color.red);

        Wander();

        controller = GameObject.Find("Controller").GetComponent<Controller>();
        flagGetter = controller.GetEnemyDecider();

        flag = GameObject.Find("Flag2").GetComponent<Flag>();
        home = GameObject.Find("EnemyHome").GetComponent<EnemyHome>();
        if (gameObject.tag == "E1")
            teamMate = GameObject.Find("E2").GetComponent<Enemy>();
        if (gameObject.tag == "E2")
            teamMate = GameObject.Find("E1").GetComponent<Enemy>();
    }

    void Update()
    {
        // clamp him to the floor. For some reason when he moves it floats upwards without this.
        transform.position = new Vector3(transform.position.x, clamptoFloor, transform.position.z);

        Movement();
        if (kinematic == true) // if "k" was clicked to use kinematic movements
            KinematicMovements();
        if (steering == true) // if "s" was clicked to use steering movements
            SteeringMovements();

        // constantly checking to see if they go out of bounds
        OutOfBoundsCheck();
        // if not going for flag, check if enemies enter zone
        checkApproachingEnemies();
        // if teammate is frozen and you have caught enemy in ur zone, save teammate
        if (targetCaught == true && teamMate.frozen == true)
        {
            target = teamMate.gameObject;
            SaveFrozenTeammates();
        }

        if (wanderMove == true && frozen != true)
            transform.position += transform.TransformDirection(Vector3.forward) * nearSpeed * Time.deltaTime;
        if ((transform.position - wayPoint).magnitude < 3 && wanderOn == true)
        {
            Wander();
        }
        if (target != null && (a == true || b == true)) // if character has target, stop wandering
            wanderOn = false;

        if (controller.GetEnemyDecider() == 1 && gameObject.tag == "E1") // if 1, make first enemy get the flag
        {
            if (getFlag == false)
            {
                getFlag = true; // character going for flag
                target = flag.gameObject;  // assign their target as the flag
            }
        }
        if(controller.GetEnemyDecider() == 2 && gameObject.tag == "E2") // if 2, make second enemy get the flag
        {
            if (getFlag == false)
            {
                getFlag = true; // character going for flag
                target = flag.gameObject;  // assign their target as the flag
            }
        }
    }

    void Movement()
    {
        if (Input.GetKey("k")) // change movement to kinematic
        {
            kinematic = true;
            steering = false;
        }
        if (Input.GetKey("s")) // change movement to steering
        {
            steering = true;
            kinematic = false;
        }

        if (Input.GetKey("a") && (target == flag.gameObject || target == teamMate)) // if you want to see Ai and Aii instruction set
        {
            Debug.Log("here");
            a = true;
            b = false;
            c = false;
            wanderOn = false;
            wanderMove = false;
            GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f); // reset velocity to stop character from continuing previous movement
                                                                                // (flee to arrive, kept moving backward while rotating to target)
        }

        if (Input.GetKey("b") && (target == flag.gameObject || target == teamMate)) // if you want to see Bi and Bii instruction set
        {
            float distance = (target.transform.position - transform.position).magnitude;
            a = false;
            b = true;
            c = false;
            wanderOn = false;
            wanderMove = false;
            if (distance <= midRadius)
                GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f);
        }

        if (Input.GetKey("c") && target != null) // if you want to see Ci and Cii instruction set
        {
            a = false;
            b = false;
            c = true;
            wanderOn = false;
            wanderMove = false;
            GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f);
        }
    }

    void KinematicMovements()
    {
        if (a == true) // if running A instructions
        {
            float distance = (target.transform.position - transform.position).magnitude;     // get distance to target
            Vector3 direction = (target.transform.position - transform.position).normalized; // get direction to face target

            if (distance > arrivalRadius) // if character is far enough, align before approaching target
            {
                Align();
                float facing = Vector3.Dot(direction, transform.forward);
                if (facing >= 0.9f) // if direction greater than or equal to 1, means character is aligned with target and can approach it.
                {
                    Arrive();
                }
            }
            else // if right next to target, just move right to it
            {
                Arrive();
            }
        }

        if (b == true) // if running B instructions
        {
            float distance = (target.transform.position - transform.position).magnitude;     // get distance to target
            Vector3 direction = (target.transform.position - transform.position).normalized; // get direction to face target

            if (distance > arrivalRadius) // if character is far enough, align before approaching target
            {
                Align();
                float facing = Vector3.Dot(direction, transform.forward);
                if (facing >= 0.9f) // if direction greater than or equal to 1, means character is aligned with target and can approach it.
                {
                    Arrive();
                }
            }
            if (distance <= midRadius) // if in middle range of target, just move right to it
            {
                Align();
                Arrive();
            }
        }

        if (c == true && target != null) // if running C instructions
        {
            float distance = (target.transform.position - transform.position).magnitude;     // get distance to target
            Vector3 direction = (target.transform.position - transform.position).normalized; // get direction to face target

            if (distance < arrivalRadius) // if small distance from target, simply move away
            {
                Flee();
            }

            if (distance > arrivalRadius) // turn around and then flee
            {
                FleeAlign();
                float facing = Vector3.Dot(direction, transform.forward);
                if (facing <= -1) // if direction less than or equal to 1, means character is facing in opposite direction of target and can flee.
                {
                    Flee();
                }
            }
        }
    }

    void SteeringMovements()
    {
        if (a == true)
        {
            float distance = (target.transform.position - transform.position).magnitude;     // get distance to target
            Vector3 direction = (target.transform.position - transform.position).normalized; // get direction to face target

            if (distance > arrivalRadius) // if character is far enough, align before approaching target
            {
                Align();
                float facing = Vector3.Dot(direction, transform.forward);
                if (facing >= 0.9f) // if direction greater than or equal to 1, means character is aligned with target and can approach it.
                {
                    SteeringArrive();
                }
            }
            else // if right next to target, just move right to it
            {
                SteeringArrive();
            }
        }

        if (b == true)
        {
            float distance = (target.transform.position - transform.position).magnitude;     // get distance to target
            Vector3 direction = (target.transform.position - transform.position).normalized; // get direction to face target

            if (distance > arrivalRadius) // if character is far enough, align before approaching target
            {
                Align();
                float facing = Vector3.Dot(direction, transform.forward);
                if (facing >= 0.9f) // if direction greater than or equal to 1, means character is aligned with target and can approach it.
                {
                    SteeringArrive();
                }
            }
            if (distance <= midRadius) // if in middle range of target, just move right to it
            {
                Align();
                SteeringArrive();
            }
        }

        if (c == true && target != null)
        {
            FleeAlign();
            Flee();
        }
    }

    void Arrive()
    {
        Vector3 direction = (target.transform.position - transform.position).normalized; // get direction of target

        if (Vector3.Distance(target.transform.position, transform.position) > farRadius) // far, move faster
        {
            Vector3 newVelocity = speed * direction.normalized;
            GetComponent<Rigidbody>().velocity = newVelocity;
        }
        if (Vector3.Distance(target.transform.position, transform.position) < farRadius) // near, slow down
        {
            Vector3 newVelocity = nearSpeed * direction.normalized;
            GetComponent<Rigidbody>().velocity = newVelocity;
        }

        if (Vector3.Distance(target.transform.position, transform.position) < arrivalRadius) // arrived, stop moving
        {
            Vector3 newVelocity = arrivingSpeed * direction.normalized;
            GetComponent<Rigidbody>().velocity = newVelocity;
        }
    }

    void SteeringArrive()
    {
        if (stopSteeringArrive == false)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            float distance = (target.transform.position - transform.position).magnitude;

            float targetSpeed = 0;
            if (distance > slowRadius)
                targetSpeed = steerSpeed;
            else
                targetSpeed = steerSpeed * distance / slowRadius;

            Vector3 targetVelocity = direction;
            targetVelocity.Normalize();
            targetVelocity *= targetSpeed;

            Vector3 linear = targetVelocity - velocity;
            linear /= timeToTarget;

            if (linear.magnitude > maxAcceleration)
            {
                linear.Normalize();
                linear *= maxAcceleration;
            }

            transform.position += velocity * Time.deltaTime;
            velocity += linear * Time.deltaTime;

            if (velocity.magnitude > steerSpeed)
            {
                velocity.Normalize();
                velocity *= speed;
            }
        }

        /* Vector3 direction = (target.transform.position - transform.position).normalized;
        float distance = (target.transform.position - transform.position).magnitude;
        float decelerationFactor = distance / 4.5f;

        float newSpeed = speed * decelerationFactor;
        GetComponent<Rigidbody>().velocity = direction * newSpeed * Time.deltaTime;*/
    }

    void Pursue()
    {
        float predictionCap = 10.0f;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        float distance = (target.transform.position - transform.position).magnitude;

        float prediction = 0.0f;
        if (nearSpeed <= distance / predictionCap)
            prediction = predictionCap;
        else
            prediction = distance / nearSpeed;
        Align();
        Arrive();
        /* float moveAhead = 20.0f;
        targetSpeed = target.GetComponent<Rigidbody>().velocity;
        Vector3 futureTargetPosition = target.transform.position + (targetSpeed * moveAhead); // estimating target future position
        Vector3 direction = futureTargetPosition - transform.position;
        Align();
        GetComponent<Rigidbody>().velocity = direction * 25.0f * Time.deltaTime;*/
    }

    void Align()
    {
        goalFacing = (target.transform.position - transform.position).normalized;
        lookWhereYoureGoing = Quaternion.LookRotation(goalFacing, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationSpeedRads);
    }

    void Flee()
    {
        if (target != null)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            GetComponent<Rigidbody>().velocity = -arrivingSpeed * direction.normalized;
        }
    }

    void FleeAlign()
    {
        if (target != null)
        {
            goalFacing = (target.transform.position - transform.position).normalized;
            lookWhereYoureGoing = Quaternion.LookRotation(-goalFacing, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationSpeedRads);
        }
    }

    void Wander()
    {
        wayPoint = new Vector3(Random.Range(transform.position.x + range, transform.position.x - range),
             transform.position.y,
             Random.Range(transform.position.z + range, transform.position.z - range));
        wayPoint.y = transform.position.y;
        transform.LookAt(wayPoint);
        //Debug.Log(wayPoint + " and " + (transform.position - wayPoint).magnitude);

    }

    void OutOfBoundsCheck()
    {

        if (transform.position.z < 0.5f) // past left edge
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 598);
            Wander();
        }
        if (transform.position.z > 600) // past right edge
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 1);
            Wander();
        }
        if (transform.position.x < 0.5f) // past top edge
        {
            transform.position = new Vector3(498, transform.position.y, transform.position.z);
            Wander();
        }
        if (transform.position.x > 499) // past bottom edge
        {
            transform.position = new Vector3(1, transform.position.y, transform.position.z);
            Wander();
        }
    }

    void checkApproachingEnemies()
    {
        Vector3 enemyPosition1 = enemy1.transform.position;  // position of enemy 1
        Vector3 enemyPosition2 = enemy2.transform.position;  // position of enemy 2

        
            if (getFlag == false && enemyPosition1.z > 300) // if ai is not going for flag and enemy 1 crosses into his zone
            {
                target = enemy1;
                Pursue();
                // hasTarget = true;
            }
        

            if (getFlag == false && enemyPosition2.z > 300) // if ai is not going for flag and enemy 2 crosses into his zone
            {
                target = enemy2;
                Pursue();
                // hasTarget = true;
            }
    }

    void SaveFrozenTeammates()
    {
        wanderMove = false;
        wanderOn = false;
        Align();
        Arrive();
    }

    public void SetWanderBool(bool wander)
    {
        wanderMove = wander;
        wanderOn = wander;
    }

    public void SetTargetToNull()
    {
        target = null;
    }

    public GameObject GetTarget()
    {
        return target;
    }

    public bool GetFrozen()
    {
        return frozen;
    }

    void OnCollisionEnter(Collision collision)
    {
        Enemy teamMember = collision.gameObject.GetComponent<Enemy>();
        AI1 enemies = collision.gameObject.GetComponent<AI1>();
        Flag f = collision.gameObject.GetComponent<Flag>();
        EnemyHome win = collision.gameObject.GetComponent<EnemyHome>();

        if (f)
        {
            hasFlag = true;
            target = home.gameObject;
            Align();
        }

        if(win && hasFlag == true)
        {
            gameOverCanvas.SetActive(true);
        }

        if (enemies && transform.position.z <= 299)
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f);
            frozen = true;
            stopSteeringArrive = true;
            if(hasFlag == true)
            {
                hasFlag = false;
                flag.returnToHomeSpot();
            }
        }

        if(enemies && transform.position.z > 300)
        {
            if (target != flag.gameObject)
            {  
                Wander();
                targetCaught = true;
                Debug.Log("enemy target: " + target); 
            }
        }

        if(teamMember && frozen == true)
        {
            frozen = false;                                                              // unfreeze teammate
            stopSteeringArrive = false;                                                  // allow steering arrive to move characters
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;           // unfreeze  
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation; // keep rotation frozen

            teamMember.SetTargetToNull();   // remove teammates target
            teamMember.SetWanderBool(true); // set wandering to true, make him wander again
        }
    }
}

/*
 * maybe need to adjust speed variables. Game may be too slow.
 * make target == null when hit by another player -- > check if he continues to wander
 * make marker to go to when retreiving flag, end game
 * 
 * 
 */