﻿using UnityEngine;

using System.Collections;

using Assets.Script.Tools;
using Assets.Scripts.Tools;



public class MovementBehaviour : MonoBehaviour
{
    public GameObject _targetGameObject;
    private GameObject character;
    private Vector3 _characterVelocity;
    private Vector3 targetVelocity;
    private Vector3 characterAcceleration;
    public float MaxSpeed;
    private float originalMaxSpeed;
    public float maxAngularVelocity;
    public float maxAcceleration;
    public float maxAngularAcceleration;
    private float angularAcceleration;
    public float ArrivalRadius; //inner satisfaction radius
    public float SlowDownRadius;
    public float slowDownRotationRadius;
    private bool hasArrived = false;
    private float resetTimer = 3f;
    private float timer = 3f;
    public float TurnSmoothing;
    public float maxPredictionTime; //max prediction time for pursue, and evade
    public float satisfactionRotation;
    public float slowDownOrientation;
    public float timeToTarget; //time to target
    public float angularVelocity;
    public float characterAngularVelocity;
    public string outputInfo;

    public float maxRotation;
    public GameObject holder;

    public delegate Vector3 SeekTargetDelegate(GameObject target, float timeStep);

    public SeekTargetDelegate seekTargetDelegate;

    private Separation seperation; 


    #region constructors and setters

    public MovementBehaviour(GameObject character, float maxspeed, float maxAngularvelocity, float maxAcceleration,
        float maxAngularAcceleration, float turnSmoothing)
    {

        this.character = character;
        this.MaxSpeed = maxspeed;
        this.maxAngularAcceleration = maxAngularAcceleration;
        this.maxAngularVelocity = maxAngularvelocity;
        this.maxAcceleration = maxAcceleration;
        seekTargetDelegate += SteeringSeek;
        this.TurnSmoothing = turnSmoothing;
    }

    public GameObject TargetGameObject
    {
        get { return _targetGameObject; }
        set { _targetGameObject = value; }
    }

    public Vector3 CharacterVelocity
    {
        get { return _characterVelocity; }
        set { _characterVelocity = value; }
    }

    public void InstatiateMovementBehaviour(GameObject character, float maxspeed, float maxAngularvelocity, float maxAcceleration,
        float maxAngularAcceleration, float turnSmoothing, float timeToTarget)
    {

        this.character = character;
        this.MaxSpeed = maxspeed;
        this.maxAngularAcceleration = maxAngularAcceleration;
        this.maxAngularVelocity = maxAngularvelocity;
        this.maxAcceleration = maxAcceleration;
        seekTargetDelegate += SteeringSeek;
        this.TurnSmoothing = turnSmoothing;
        this.timeToTarget = timeToTarget;
        resetTimer = 3.5f;
        originalMaxSpeed = maxspeed;
    }

    #endregion

    #region unity function

    private void Start()
    {
        seperation = GetComponent<Separation>();
    }

    #endregion
    #region steering

    /// <summary>
    /// this will return a velocity, will always set the velocity.y to 0
    /// </summary>
    /// <param name="target"></param>
    /// <param name="timeStep"></param>
    /// <returns></returns>
    public Vector3 SteeringSeek(GameObject target, float timeStep)
    {
        if (!hasArrived)
        {

            characterAcceleration = (target.transform.position - character.transform.position);

            characterAcceleration.y = 0;
            characterAcceleration = Vector3.Normalize(characterAcceleration) * maxAcceleration;
            _characterVelocity = _characterVelocity + (characterAcceleration * timeStep);
            //current velocity + desiredAcceleration* time
            _characterVelocity = AdditionalVector3Tools.Limit(_characterVelocity, MaxSpeed);
            //FixRotation();
        }
        return _characterVelocity;


    }

    public void SteeringSeek(Transform target)
    {
        //get the direction of the target and normalize it
        characterAcceleration = (target.transform.position - character.transform.position).normalized;
        characterAcceleration *= maxAcceleration;

        _characterVelocity = _characterVelocity + (characterAcceleration * Time.fixedDeltaTime);

        _characterVelocity = AdditionalVector3Tools.Limit(_characterVelocity, MaxSpeed);
        //FixRotation();
        flattenYtoZero();
        character.rigidbody.velocity = _characterVelocity;

    }

    public void SteeringFlee(Transform target)
    {
        //get the direction of the target and normalize it
        characterAcceleration = (character.transform.position - target.transform.position).normalized;
        characterAcceleration *= maxAcceleration;

        _characterVelocity = _characterVelocity + (characterAcceleration * Time.fixedDeltaTime);

        _characterVelocity = AdditionalVector3Tools.Limit(_characterVelocity, MaxSpeed);
        //FixRotation();
        flattenYtoZero();
        character.rigidbody.velocity = _characterVelocity;

    }

    public Vector3 SteeringSeek(Vector3 targetPos, float timeStep)
    {
        if (!hasArrived)
        {
            //   FixRotation();
            characterAcceleration = (targetPos - character.transform.position);
            characterAcceleration.y = 0;
            characterAcceleration = Vector3.Normalize(characterAcceleration) * maxAcceleration;
            _characterVelocity = _characterVelocity + (characterAcceleration * timeStep);
            //current velocity + desiredAcceleration* time
            _characterVelocity = AdditionalVector3Tools.Limit(_characterVelocity, MaxSpeed);

        }
        return _characterVelocity;


    }

    public void SteeringArrive()
    {

        Vector3 targetVelocity = _targetGameObject.transform.position - character.transform.position;
        Vector3 direction = _targetGameObject.transform.position - character.transform.position;
        float targetspeed = 0;
        float dist = targetVelocity.magnitude;
        if (dist < ArrivalRadius)
        {
              print("setting to zero");
            character.rigidbody.velocity = Vector3.zero;
            return;

        }
         if (dist > SlowDownRadius) //if we're outside the target radius, go at full speed
        {
           
            targetspeed = MaxSpeed;
            //CharacterVelocity = desiredVel.normalized*MaxSpeed;
        }
        else //he's actually inside the slow radius
        {
         //   print(name+ " is in steering arrive and slowing down");
            targetspeed = MaxSpeed * dist / SlowDownRadius;
            // CharacterVelocity = desiredVel.normalized*MaxSpeed/SlowDownRadius;
        }
        targetVelocity = direction.normalized;
        targetVelocity = targetVelocity * targetspeed;
        //targetVelocity = character.rigidbody.velocity.normalized*targetspeed;
        characterAcceleration = targetVelocity - character.rigidbody.velocity;
        characterAcceleration /= timeToTarget;

        if (characterAcceleration.magnitude > maxAcceleration)
        {
            characterAcceleration = characterAcceleration.normalized * maxAcceleration;
        }

        _characterVelocity = targetVelocity + characterAcceleration * Time.fixedDeltaTime;
        if (_characterVelocity.magnitude > MaxSpeed)
            _characterVelocity = _characterVelocity.normalized * MaxSpeed;
        flattenYtoZero();
        outputInfo = name + " velocity =" + _characterVelocity + "\n Character Acceleration= " + characterAcceleration
            + "\n Target velocity " + targetVelocity + "\n Target speed " + targetspeed + "\n Distance to target " + dist + "\n slow down radius" + SlowDownRadius
            + "\n arrival radius" + ArrivalRadius + "\n t2t" + timeToTarget + "\n current pos: " + gameObject.transform.position +
   "\n targetPos = " + TargetGameObject.transform.position; ;
        flattenYtoZero();
        rigidbody.velocity = _characterVelocity;
        // character.rigidbody.velocity
    }

    /// <summary>
    /// Implementation of align according to AI for game by Ian Millington and John Funge
    /// </summary>
    /// <param name="t"></param>
    public void Align(Transform t)
    {

        float rotation = 0;
        Transform target;

        target = t.transform;


        /*        Vector2 targetForward = new Vector2(target.transform.forward.x, target.transform.forward.y);
                Vector2 charForward = new Vector2(character.transform.forward.x, character.transform.forward.y);
                rotation = Vector2.Angle(targetForward, charForward);*/

        rotation = target.rotation.eulerAngles.y - character.transform.rotation.eulerAngles.y;
        //map between -180 and 180
        rotation = CalculateTools.mapAngleToRange(rotation);
        float rotationSize = Mathf.Abs(rotation);
        // print("rotation size is "+ rotationSize);
        float targetRotation = 0;
        //are we there yet?
        if (rotationSize < 8f)
        {
          //  Vector3 directionVector3 = (target.transform.position - character.transform.position).normalized;
         //   float angle = Mathf.Atan2(directionVector3.x, directionVector3.z) * Mathf.Rad2Deg;
         //   Quaternion rotationQuaternion = Quaternion.Euler(new Vector3(0, angle, 0));
        //    print("might need to change this part if the rotation is off");
         //   character.transform.rotation = rotationQuaternion;
            return;
        }
     //   print("aligning");
        //if we're outside the slow radius, then use max rotation
        if (rotationSize > slowDownRotationRadius)
        {
            targetRotation = maxRotation;
        }
        //else calculate a scaled rotation
        else
        {
            targetRotation = maxRotation * rotationSize / slowDownRotationRadius;
        }

        //the final target rotation combines speed and direction
        targetRotation *= rotation / rotationSize;

        //acceleration tries to get to the target rotation 
        angularAcceleration = targetRotation - character.transform.rotation.eulerAngles.y;
        angularAcceleration /= timeToTarget;
        
        //check if acceleration is too great
        float angularAccelCheck = Mathf.Abs(angularAcceleration);
        if (angularAccelCheck > maxAngularAcceleration)
        {
//            print("angular accel check is bigger");
            angularAcceleration /= angularAccelCheck;
            angularAcceleration *= maxAngularAcceleration;
        }

        //set the angle of the character
        float targetAngle = character.transform.rotation.eulerAngles.y + angularAcceleration * Time.fixedDeltaTime;
        Quaternion rotQuaternion = Quaternion.Euler(new Vector3(0, targetAngle, 0));

        character.transform.rotation = rotQuaternion;

    }


    /// <summary>
    /// Implementation of Face according to AI for game by Ian Millington and John Funge
    /// </summary>
    /// <param name="t"></param>
    /// <summary>
    /// will simply flatten the character's velocity to zero
    /// </summary>
    public void Face(Transform t)
    {
        print("in Face() and aligning face" +
           "");
        //calculate the target to delegate to Align
        Vector3 direction = t.position - character.transform.position;
        //if direction is marginally small, do nothing
        if (direction.magnitude < 0.1f)
            return;
        //put the target into the holder
        holder.transform.position = t.position;
        float targetAngle = Mathf.Atan2(direction.x, direction.z)*Mathf.Rad2Deg;

        //float targetAngle = Mathf.Atan2(direction.z, direction.x) *-Mathf.Rad2Deg + 90;
        Quaternion rotQuaternion = Quaternion.Euler(new Vector3(0, targetAngle, 0));
        holder.transform.rotation = rotQuaternion;
        

     
        Align( holder.transform.transform);
    }

    public void LookWhereYoureGoing()
    {
        //calculate the target to o delegate to align

        //if the target velocity is marginally small, return 
        if (character.rigidbody.velocity.sqrMagnitude < 0.1f)
            return;
        

        //otherwise set the target based on the velocity


        //set the holder's position directly in front of the character
        holder.transform.position = character.transform.forward.normalized * 2f;
        float targetAngle = Mathf.Atan2(character.rigidbody.velocity.x, character.rigidbody.velocity.z) * Mathf.Rad2Deg;
        Quaternion rotQuaternion = Quaternion.Euler(new Vector3(0, targetAngle, 0));
        holder.transform.rotation = rotQuaternion;
        Align(holder.transform.transform);

    }
    private void flattenYtoZero()
    {
        _characterVelocity.y = 0;
    }

    /// <summary>
    /// this will return a velocity that is trying to steer away from the target. 
    /// will always set velocity.y to 0
    /// </summary>
    /// <param name="target"></param>
    /// <param name="timeStep"></param>
    /// <returns></returns>
    public Vector3 SteeringFlee(GameObject target, float timeStep)
    {
        // FixRotation();
        characterAcceleration = (character.transform.position - target.transform.position);

        characterAcceleration.y = 0;
        characterAcceleration = Vector3.Normalize(characterAcceleration) * maxAcceleration;
        _characterVelocity = _characterVelocity + (characterAcceleration * timeStep);
        _characterVelocity = AdditionalVector3Tools.Limit(_characterVelocity, MaxSpeed);
        return _characterVelocity;
    }

    public Vector3 SteeringFlee(Vector3 target, float timeStep)
    {
        // FixRotation();
        characterAcceleration = (character.transform.position - target);

        characterAcceleration.y = 0;
        characterAcceleration = Vector3.Normalize(characterAcceleration) * maxAcceleration;
        _characterVelocity = _characterVelocity + (characterAcceleration * timeStep);
        _characterVelocity = AdditionalVector3Tools.Limit(_characterVelocity, MaxSpeed);
        return _characterVelocity;
    }

    public void SteeringFlee()
    {
        characterAcceleration = (character.transform.position - _targetGameObject.transform.position);

        characterAcceleration.y = 0;
        characterAcceleration = Vector3.Normalize(characterAcceleration) * maxAcceleration;
     

        _characterVelocity = _characterVelocity + (characterAcceleration * Time.fixedDeltaTime);
        _characterVelocity = AdditionalVector3Tools.Limit(_characterVelocity, MaxSpeed);
        flattenYtoZero();
        rigidbody.velocity = _characterVelocity;

    }
    public Vector3 Pursuit(MovementBehaviour target, float timeStep, float timeInterval)
    {
        Vector3 directionVector3 = target.character.transform.position - character.transform.position;
        float distance = Vector3.Magnitude(directionVector3);
        float predictionTime; //prediction time
        if (MaxSpeed < distance)
            predictionTime = maxPredictionTime;
        else
        {
            predictionTime = distance / MaxSpeed;
        }
        //delegate to seek
        return SteeringSeek(target.character, timeStep);

    }
    /// <summary>
    /// Pursuit implementation of Artificial intelligence by Ian Millington and John Funge
    /// </summary>
    public void Pursuit()
    {
        Vector3 directionVector3 = _targetGameObject.transform.position - character.transform.position;
        print(_targetGameObject.name + " is being persued by  "+name);
        float distance = directionVector3.magnitude;

        //find our current speed
        float speed = character.rigidbody.velocity.magnitude;
        //check if the speed is too small to give a reasonable prediction time
        float predictionTime; //prediction time
        if (speed <= distance/maxPredictionTime)
            predictionTime = maxPredictionTime;

        //else calculate the prediction time
        else
            predictionTime = distance/speed;

        holder.transform.position = _targetGameObject.transform.position;
        holder.transform.position += _targetGameObject.rigidbody.velocity *predictionTime;
        holder.transform.rotation = _targetGameObject.transform.rotation;
        //delegate to seek
        //delegate to seek
        SteeringSeek(holder.transform);


    }
    /// <summary>
    /// Pursuit implementation of Artificial intelligence by Ian Millington and John Funge
    /// </summary>
    public void Evade()
    {
        Vector3 directionVector3 = _targetGameObject.transform.position - character.transform.position;
        float distance = directionVector3.magnitude;

        //find our current speed
       // float speed = character.rigidbody.velocity.magnitude;
        float speed = CharacterVelocity.magnitude;
        //check if the speed is too small to give a reasonable prediction time
        float predictionTime; //prediction time
        if (speed <= distance / maxPredictionTime)
            predictionTime = maxPredictionTime;

        //else calculate the prediction time
        else
            predictionTime = distance / speed;

        holder.transform.position = _targetGameObject.transform.position;
        holder.transform.position += _targetGameObject.rigidbody.velocity * predictionTime;
        holder.transform.rotation = _targetGameObject.transform.rotation;
        //delegate to seek
        //delegate to seek
        SteeringFlee(holder.transform);

    }



    public Vector3 SteeringArrive(GameObject target, float timeStep)
    {

        float dist = (target.transform.position - character.transform.position).magnitude;

        //in the inner radius, stop
        if (dist < ArrivalRadius)
        {
            _characterVelocity = Vector3.zero;
            hasArrived = true;

        }
        else if (dist < SlowDownRadius)
        {
            Vector3 desiredVel = target.transform.position - character.transform.position;
            float distance = desiredVel.magnitude;
            float mag = AdditionalVector3Tools.map(distance, 0, 50, 0, MaxSpeed);
            //CharacterVelocity = mag*desiredVel*timeStep;
            _characterVelocity = Vector3.Lerp(_characterVelocity, Vector3.zero, timeStep);

            hasArrived = true;
        }
        else if ((target.transform.position - character.transform.position).magnitude > SlowDownRadius)
        {
            hasArrived = false;
        }
        return _characterVelocity;


    }



    public void ReynoldsWander(float dist, float timeStep)
    {
        //decrement timer by one time frame
        timer -= Time.fixedDeltaTime;
        //  StartCoroutine(RandomizeDirection(TimeBetweenWanders,dist));
        if (timer < 0)
        {

            _characterVelocity = RandomDirection(6f, timeStep);
            timer = Random.Range(1, resetTimer*2f);
        }
        //smoothen rotation
        SmoothRotation(_characterVelocity, timeStep);
        _characterVelocity.y = 0;
        character.rigidbody.velocity = _characterVelocity;

    }
    void SmoothRotation(Vector3 tarDirection, float timeStep)
    {
        Vector3 targetDirection = tarDirection;

        // Rotation based on this new vector assuming that up is the global y axis.
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

        // Incremental rotation towards target rotation from the player's rotation.
        Quaternion newRotation = Quaternion.Lerp(character.rigidbody.rotation, targetRotation, TurnSmoothing * timeStep);

        character.rigidbody.MoveRotation(newRotation);
    }

    /// <summary>
    /// function helps with reynolds wander. This function will randomize the direction 
    /// </summary>
    /// <param name="dist"></param>
    /// <returns></returns>
    public Vector3 RandomDirection(float dist, float timeStep)
    {
        //get the orientation of the current character
        float _z = Mathf.Cos(character.transform.rotation.eulerAngles.y * Mathf.Deg2Rad);
        float _x = Mathf.Sin(character.transform.rotation.eulerAngles.y * Mathf.Deg2Rad);

        //place the target position in a front line in front of the character
        Vector3 target = character.transform.position;
        target.x += _x;
        target.z += _z;
        target = Vector3.Normalize(target) * dist;


        //get a random point inside a unit circle and scale it
        Vector2 randCircle = UnityEngine.Random.insideUnitCircle * 10f;
        //place a circle at the target's point, which will be the center of the velocity vector
        randCircle.x += target.x;
        randCircle.y += target.z;


        //calculate the new velocity, that is find the velocity from the middle of the circle to the chosen point. 
        _characterVelocity.x = randCircle.x - target.x;
        _characterVelocity.z = randCircle.y - target.z;
        _characterVelocity = Vector3.Normalize(_characterVelocity) * MaxSpeed / 2;

        return _characterVelocity;


    }

    IEnumerator RandomizeDirection(float waitTime, float dist)
    {
        yield return new WaitForSeconds(waitTime);
        Vector2 randCircle = UnityEngine.Random.insideUnitCircle * 5f;
        Vector3 target = character.transform.position.normalized * dist;
        randCircle.x += target.x;
        randCircle.y += target.z;
        target = Vector3.Normalize(target) * MaxSpeed;
        _characterVelocity = character.transform.position - target;
        _characterVelocity = Vector3.Normalize(_characterVelocity) * MaxSpeed;

        // FixRotation();
    }



    private void DrawCircle(Vector3 target, float r)
    {

        float theta_scale = 0.1f; //set lower to add more points
        int size = (int)((2.0 * Mathf.PI) / theta_scale);

        LineRenderer lineRenderer = character.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
        lineRenderer.SetColors(Color.black, Color.black);
        lineRenderer.SetWidth(0.2f, 0.2f);
        lineRenderer.SetVertexCount(size);

        int i = 0;
        for (float theta = 0; theta < 2 * Mathf.PI; theta += 0.1f)
        {
            float _x = r * Mathf.Cos(theta);
            float _z = r * Mathf.Sin(theta);
            Vector3 pos = new Vector3(_x, 0, _z);
            lineRenderer.SetPosition(i, pos);
            i += 1;
        }

    }

    /// <summary>
    /// helper function that fixes the rotation of the object to face in the right direction
    /// </summary>
    private void FixRotation()
    {
        if (_characterVelocity.sqrMagnitude > 0f)
            character.transform.rotation = Quaternion.LookRotation(_characterVelocity.normalized, Vector3.up);
        //
    }

    private void drawCircle(float r, int segments, LineRenderer line)
    {
        float _x = 0;
        float _y = transform.position.y + 3f;
        float _z = 0;

        float theta = 0f;

        for (int i = 0; i < segments + 1; i++)
        {
            _x = r * Mathf.Sin(Mathf.Deg2Rad * theta) + transform.position.x;
            _z = r * Mathf.Cos(Mathf.Deg2Rad * theta) + transform.position.z;
            theta += (360f / segments);
            line.SetPosition(i, new Vector3(_x, _y, _z));
        }
    }


    /*public float Align(Transform target)
    {
        if (target.rotation.eulerAngles.y != character.transform.rotation.eulerAngles.y)
        {
            float rotation = target.transform.rotation.eulerAngles.y - transform.rotation.eulerAngles.y;
            //if we are within the range of satisfaction, stop rotating and return the targets rotation
            if (Mathf.Abs(target.rotation.eulerAngles.y - transform.rotation.eulerAngles.y) < satisfactionRotation)
                return target.rotation.eulerAngles.y;
            rotation = AdditionalVector3Tools.mapAngleToRange(rotation);

            //rotation sign

            float sign = Mathf.Sign(rotation);
            //now we compute the goal angular velocity

            float goalVelocity = (sign * maxAngularVelocity) * rotation / (sign * slowDownOrientation);

            //current character velocity

            float currCharVelocity = Vector3.Magnitude(rigidbody.velocity);

            //compute the angular acceleration

            float angularAcceleration = (goalVelocity - currCharVelocity) / timeToTarget;


            if (Mathf.Abs(angularAcceleration) > Mathf.Abs(maxAngularAcceleration))
                angularAcceleration = maxAngularAcceleration * sign;

            //ensure angular velocity sign is the same as rotation
            if (Mathf.Sign(maxAngularVelocity) != sign)
                angularVelocity *= sign;
            angularVelocity += angularAcceleration * Time.fixedDeltaTime;

            if (Mathf.Abs(angularVelocity) > maxAngularVelocity)
                angularVelocity = Mathf.Abs(maxAngularVelocity) * sign;




            float newAngle = character.transform.rotation.eulerAngles.y + Time.fixedDeltaTime * angularVelocity;
            return newAngle;
        }
        else return character.transform.rotation.eulerAngles.y;

    }*/
    


    
    /// <summary>
    /// faces the character away from the target
    /// </summary>
    /// <param name="target"></param>
    /// <param name="timeStep"></param>
    public void FaceAway(Transform target, float timeStep)
    {
        Vector3 direction = target.position - character.transform.position;
        if (direction.magnitude < 0.01f && direction.magnitude > 0.01f)
            return;


        float targetOrientation = Mathf.Atan2(-direction.x, direction.z) * Mathf.Rad2Deg;
        Align2(targetOrientation, timeStep);

    }

    public void FaceAway(Transform t)
    {
        print(name +" in FaceAway() and aligning face" +
          "");
        //calculate the target to delegate to Align
        Vector3 direction = character.transform.position -t.position;
        //if direction is marginally small, do nothing
        if (direction.sqrMagnitude < 0.1f)
        {
            print("small margin"); return;
        }
            
        //put the target into the holder
        holder.transform.position = t.position;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Quaternion rotQuaternion = Quaternion.Euler(new Vector3(0, targetAngle, 0));
        holder.transform.rotation = rotQuaternion;



        Align(holder.transform.transform);
    }

    public void LookWhereYoureGoing(float timeStep)
    {
        if (character.rigidbody.velocity.magnitude < 0.01f && character.rigidbody.velocity.magnitude > -0.01f)
            return;
        else
        {
            float orientation = Mathf.Atan2(-character.rigidbody.velocity.x, character.rigidbody.velocity.z);
            Align2(orientation, timeStep);
        }

    }


    void Align(float targetOrientation, float timeStep)
    {



        float rotation = targetOrientation - character.transform.rotation.eulerAngles.y;
        rotation = AdditionalVector3Tools.mapAngleToRange(rotation);
        Vector3 charRotation = character.transform.rotation.eulerAngles;

        //if we are within the range of satisfaction, stop rotating and return the targets rotation
        if (Mathf.Abs(Mathf.Abs(targetOrientation) - Mathf.Abs(transform.rotation.eulerAngles.y)) < satisfactionRotation)
        {
            charRotation.y = targetOrientation;
            character.transform.rotation = Quaternion.Euler(charRotation);
            return;
        }


        //rotation sign

        float sign = Mathf.Sign(rotation);
        //now we compute the goal angular velocity

        float goalVelocity = (sign * maxAngularVelocity) * rotation / (sign * slowDownOrientation);

        //current character velocity

        float currCharVelocity = Vector3.Magnitude(character.rigidbody.velocity);

        //compute the angular acceleration

        float angularAcceleration = (goalVelocity - currCharVelocity) / timeToTarget;


        if (Mathf.Abs(angularAcceleration) > Mathf.Abs(maxAngularAcceleration))
        {

            angularAcceleration = maxAngularAcceleration * sign;
        }

        //ensure angular velocity sign is the same as rotation
        if (Mathf.Sign(maxAngularVelocity) != sign)
        {

            angularVelocity *= sign;
        }
        angularVelocity += angularAcceleration * timeStep;

        if (Mathf.Abs(angularVelocity) > maxAngularVelocity)
        {

            angularVelocity = Mathf.Abs(maxAngularVelocity) * sign;
        }




        charRotation.y = character.transform.rotation.eulerAngles.y + angularVelocity * timeStep;
        character.transform.rotation = Quaternion.Euler(charRotation);




    }

    public void Align2(float targetOrientation, float timeStep)
    {
        float rotation = targetOrientation - character.transform.rotation.eulerAngles.y;
        rotation = AdditionalVector3Tools.mapAngleToRange(rotation);
        Vector3 charAngles = character.transform.rotation.eulerAngles;
        float rotationSize = Mathf.Abs(Mathf.Abs(targetOrientation) - Mathf.Abs(transform.rotation.eulerAngles.y));
        //if we are within the range of satisfaction, stop rotating and return the targets rotation
        if (rotation < slowDownOrientation)
        {
            charAngles.y = targetOrientation;
            character.transform.rotation = Quaternion.Euler(charAngles);
            return;
        }
        float sign = Mathf.Sign(rotation);
        float goalVelocity = (sign * maxAngularVelocity) * (rotation * sign) / (sign * slowDownOrientation);
        float angularAcceleration = (goalVelocity - characterAngularVelocity) / timeToTarget;
        float angle = charAngles.y;
        if (Mathf.Abs(angularAcceleration) < Mathf.Abs(maxAngularAcceleration))
        {
            characterAngularVelocity = characterAngularVelocity + angularAcceleration * timeStep;
        }
        else
        {
            characterAngularVelocity = (sign) * angularAcceleration;
        }
        if (characterAngularVelocity < maxAngularVelocity)
        {
            charAngles.y = angle + characterAngularVelocity * timeStep;
            character.transform.rotation = Quaternion.Euler(charAngles);
            return;
        }

    }

   




    public Vector3 Evade(MovementBehaviour target)
    {

        return Vector3.one;

    }

    #endregion


    #region kinematic

    /// <summary>
    /// implmenetation of the kinematic arrive algorithm.
    /// move towards the target at max velocity and stop immediately at a satisfaction radius
    /// </summary>
    public void KinematicArrive()
    {
/*        print("start to debug");
        if (Input.GetKey(KeyCode.A))
            print("start to debug");*/
        _characterVelocity = TargetGameObject.transform.position - character.transform.position;
        float mag = _characterVelocity.magnitude;
        float currentVelocity = 0;
        if (mag > ArrivalRadius)
        {

            currentVelocity = Mathf.Min(MaxSpeed, mag / timeToTarget);
        }
        else
        {
            currentVelocity = 0;
        }

        outputInfo = "Character Velocity: " + _characterVelocity + "\n Arrival radius is " + ArrivalRadius + "\n Current speed is" + currentVelocity
            + "\n distance to target " + TargetGameObject.name + " is " + mag+"\n current pos: "+ gameObject.transform.position+ 
   "\n targetPos = "+TargetGameObject.transform.position;
        character.rigidbody.velocity = _characterVelocity.normalized * currentVelocity;
        // KinematicSeek();
    }

    /// <summary>
    /// 
    /// </summary>
    public void KinematicSeek()
    {
        _characterVelocity = TargetGameObject.transform.position - character.transform.position;
        character.rigidbody.velocity = _characterVelocity.normalized * MaxSpeed;
        outputInfo = "Character Velocity: " + _characterVelocity;

    }

    public void KinematicFlee()
    {
        _characterVelocity = character.transform.position - TargetGameObject.transform.position;
        character.rigidbody.velocity = _characterVelocity.normalized * MaxSpeed;
    }
    /// <summary>
    /// linearly interpolate the rotation to face the target
    /// </summary>
    public void InterpolateRotate()
    {
        /* Vector3 currentOrientation = character.transform.rotation.eulerAngles;
         Vector3 directionVector3 = (TargetGameObject.transform.position - character.transform.position).normalized;
        // print(gameObject.name + " is trying to point to " + TargetGameObject.transform.position + " and its direction is  " + directionVector3.x);
         float angle = Mathf.Atan2(directionVector3.z, directionVector3.x)* Mathf.Rad2Deg;
       // currentOrientation.y = Mathf.Lerp(currentOrientation.y, angle, Time.deltaTime * 5f);
         currentOrientation.y = angle;
       //  print(TargetGameObject.name + " and the angle " + angle);
         character.transform.rotation = Quaternion.Euler(currentOrientation);*/
       // Vector3 aimingDirection = _targetGameObject.transform.position - transform.position;

        //var heading = _targetGameObject.transform.position - transform.position;
/*
        var heading = CharacterVelocity;
        var newHeading = new Vector2(heading.x, heading.z).normalized;
        var currentHeading = new Vector2(transform.forward.x, transform.forward.z).normalized;
        float angle = Mathf.Atan2(newHeading.x, newHeading.y) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(new Vector3(0, angle, 0));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.fixedDeltaTime * 2.0f);


*/


        Vector3 directionVector3 = (_targetGameObject.transform.position) - transform.position;
        //Vector3 directionVector3 = _characterVelocity;
        float angle = Mathf.Atan2(directionVector3.x, directionVector3.z) * Mathf.Rad2Deg;
        Quaternion rotationQuaternion = Quaternion.Euler(new Vector3(0, angle, 0));

        transform.rotation = Quaternion.Lerp(transform.rotation, rotationQuaternion, Time.deltaTime * 6f);

    }
    public void InterpolateRotateWithEnemy()
    {
        /* Vector3 currentOrientation = character.transform.rotation.eulerAngles;
         Vector3 directionVector3 = (TargetGameObject.transform.position - character.transform.position).normalized;
        // print(gameObject.name + " is trying to point to " + TargetGameObject.transform.position + " and its direction is  " + directionVector3.x);
         float angle = Mathf.Atan2(directionVector3.z, directionVector3.x)* Mathf.Rad2Deg;
       // currentOrientation.y = Mathf.Lerp(currentOrientation.y, angle, Time.deltaTime * 5f);
         currentOrientation.y = angle;
       //  print(TargetGameObject.name + " and the angle " + angle);
         character.transform.rotation = Quaternion.Euler(currentOrientation);*/
        // Vector3 aimingDirection = _targetGameObject.transform.position - transform.position;

        //var heading = _targetGameObject.transform.position - transform.position;
        /*
                var heading = CharacterVelocity;
                var newHeading = new Vector2(heading.x, heading.z).normalized;
                var currentHeading = new Vector2(transform.forward.x, transform.forward.z).normalized;
                float angle = Mathf.Atan2(newHeading.x, newHeading.y) * Mathf.Rad2Deg;
                Quaternion rot = Quaternion.Euler(new Vector3(0, angle, 0));
                transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.fixedDeltaTime * 2.0f);


        */


        Vector3 directionVector3 = transform.position - (_targetGameObject.transform.position);
        //Vector3 directionVector3 = _characterVelocity;
        float angle = Mathf.Atan2(directionVector3.x, directionVector3.z) * Mathf.Rad2Deg;
        Quaternion rotationQuaternion = Quaternion.Euler(new Vector3(0, angle, 0));

        transform.rotation = Quaternion.Lerp(transform.rotation, rotationQuaternion, Time.deltaTime *6f);

    }

    public void Stop()
    {
        CharacterVelocity = Vector3.zero;
        rigidbody.velocity = CharacterVelocity;
    }

    #endregion
}
