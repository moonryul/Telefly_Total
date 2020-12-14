using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Spped to rotate around the axis")]
    public float yawSpeed = 100f;

    [Tooltip("Tip of the beek")]
    public Transform beakTip;

    [Tooltip("Agent's Camera")]
    public Camera agentCamera;

    [Tooltip("Train or gameplay mode")]
    public bool trainingMode;

    new private Rigidbody rigidbody;
    private FlowerArea flowerArea;
    private Flower nearestFlower;
    private float smoothPitchChange = 0f;
    private float smoothYawChange = 0f;
    private const float MaxPitchAngle = 80f;
    private const float BeakTipRadius = 0.008f;
    private bool isFrozen = false;

   // int numberOfGoodSteps = 0;
   // int numberOfBadSteps = 0;

    public HumanHandAvatar humanHand;
    public GameObject humanHandAvatar; //owner of this agent


    /// <summary>
    /// nectar obtained in the episode
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    /// Initialize the agent
    /// </summary>
    ///
    public bool mUserExist;
    public float mNearRadius = 0.3f;      // 50cm
    public float mGoodAngelThreshold = 15.0f; // 15도
    public float mBadAngelThreshold = 45.0f; // 30도

    Vector3 flowerTargetPosition;


    int agentNottInGoalHandTime;
    int agentNottInGoalFowerTime;

    float circleSize = 1f;
    float circleSpeed = 1.5f;
    float timeValue;
    float circlePositionX;
    float circlePositionZ;

    ReciveIndex reciveIndex;
    public override void Initialize()
    {
        rigidbody = this.gameObject.GetComponent<Rigidbody>();
        flowerArea = this.gameObject.GetComponentInParent<FlowerArea>();

        reciveIndex = this.gameObject.GetComponent<ReciveIndex>();

        // if not training mode, no max steps, play forever
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    public override void OnEpisodeBegin() //정책망에 weight데이터는 random vector다. action -> observation
    {
        if (trainingMode)
        {
           // numberOfGoodSteps = 0;
            //numberOfBadSteps = 0;

            // reset flowers and one agent only
            flowerArea.ResetFlowers();

            float random = Random.Range(0.0f, 1.0f);

            Debug.Log("Episode start");

            if (random < 0.5f)
            {
                //Debug.Log("In this time Flower");
                mUserExist = false;
                humanHandAvatar.SetActive(false);
            }
            else
            {
                //Debug.Log("In this time UserExist");
                mUserExist = true;
                if (humanHandAvatar.transform.position.y < 0)
                {
                    humanHandAvatar.transform.position = new Vector3(humanHandAvatar.transform.position.x, Random.Range(0.8f, 3.0f), humanHandAvatar.transform.position.z);
                }

                humanHandAvatar.SetActive(true);
            }
        }

        if (reciveIndex.userDetect)
        {
            mUserExist = true;
            if (humanHandAvatar.transform.position.y < 0)
            {
                humanHandAvatar.transform.position = new Vector3(humanHandAvatar.transform.position.x, Random.Range(0.8f, 3.0f), humanHandAvatar.transform.position.z);
            }

            humanHandAvatar.SetActive(true);
        }
        //Debug.Log("In train mode");
        // reset nectar obtained
        NectarObtained = 0f;

        // zero out velocities for new episode
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // default to spawning i.f.o a flower
        bool inFrontOfFlower = true;

        if (trainingMode)
        {
            inFrontOfFlower = UnityEngine.Random.value > 0.5f;
        }

        // move to safe random position
        MoveToSafeRandomPosition(inFrontOfFlower);
        flowerArea.ResetFlower1();

        UpdateNearestFlower();

        if (mUserExist && trainingMode)
        {
            float random = Random.Range(0.0f, 1.0f);
            if (random < 0.5f)
            {
                Vector3 toHand = (humanHandAvatar.transform.position - this.gameObject.transform.position).normalized;
                Quaternion r = Quaternion.LookRotation(toHand); // 꽃과 동일한 로직이 아니다.
                this.gameObject.transform.rotation = r;
            }
            flowerArea.OnModeUser();
            nearestFlower = null;
        }
        //flowerArea.ResetFlower1();
        // Recalculate nearest flower


    }

    /// <summary>
    /// Called when action is received from player or neural-net
    /// index 0: move vector x (+1 right, -1 left, 0 same)
    /// index 1: move vector y (+1 up, -1 down, 0 same)
    /// index 2: move vector z (+1 forward, -1 backward, 0 same)
    /// index 3: pitch angle (+1 up, -1 down)
    /// index 4: yaw angle (+1 right, -1 left) 
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction) //policy  정책망으로부터 
    {
        if (isFrozen) return;
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        // add force
        rigidbody.AddForce(move * moveForce);
        // rotation
        // current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;
        // calculate pitch and yaw
        float pitchChange = vectorAction[3];
        float yawchange = vectorAction[4];
        // smooth rotation
        smoothPitchChange = Mathf.MoveTowards(
            smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(
            smoothYawChange, yawchange, 2f * Time.fixedDeltaTime);
        // new pitch and new
        // clamp pitch to avoid flipping
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, +MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        // apply rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

    }

    /// <summary>
    /// Collect vecotr observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    { // 여기서 수집한 observation vector는 다음 action값을 수집할 때 다음 input값으로 선택된다.
        //Debug.Log(" UserExist  " + mUserExist);
        // Debug.Log("In CollectObservations ");
        sensor.AddObservation(mUserExist);//1

        if (humanHandAvatar != null && humanHandAvatar.activeSelf == true)
        {

            Vector3 toHand = humanHandAvatar.transform.position - this.gameObject.transform.position;

            Vector3 targetDir = (toHand).normalized;
            float dot = Vector3.Dot(transform.forward, targetDir);

            //내적을 이용한 각 계산하기
            // thetha = cos^-1( a dot b / |a||b|)
            float AngleBetweenDegree = Mathf.Acos(dot) * Mathf.Rad2Deg;
            //Debug.Log("1  :   "+ AngleBetweenDegree + "  "+ map(AngleBetweenDegree,0,180,-1,1));
            float angle = 1 + (-1 + 1) * ((AngleBetweenDegree - 0) / (180 - 0));
            if (float.IsNaN(angle))
            {
                sensor.AddObservation(new float[1]); //1

            }
            else
            {
                sensor.AddObservation(angle);

            }

            sensor.AddObservation(this.gameObject.transform.localRotation.normalized); //4
            // pointing to nearest flower 3
            sensor.AddObservation(toHand.normalized); // 3
            // dot product observation - beak tip in front of flower?
            // +1 -> infront, -1 -> behind 1
            sensor.AddObservation(toHand.magnitude / FlowerArea.AreaDiameter);//1
        }
        else
        {
            sensor.AddObservation(new float[9]);
        }



        if (nearestFlower != null)
        {
            // Observe the local rotation  4
            sensor.AddObservation(this.gameObject.transform.localRotation.normalized);
            Vector3 toFlower = nearestFlower.FlowerCenterVector - beakTip.position;
            Vector3 targetDir = (toFlower).normalized;
            float dot = Vector3.Dot(transform.forward, targetDir);

            //내적을 이용한 각 계산하기
            // thetha = cos^-1( a dot b / |a||b|)
            float AngleBetweenDegree = Mathf.Acos(dot) * Mathf.Rad2Deg;
            // pointing to nearest flower  3
            sensor.AddObservation(toFlower.normalized);
            //// dot product observation - beak tip in front of flower?
            //// +1 -> infront, -1 -> behind  1
            //sensor.AddObservation(
            //    Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));
            //// beak tip point to flower  1
            //sensor.AddObservation(
            //    Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));


            float angle = 1 + (-1 + 1) * ((AngleBetweenDegree - 0) / (180 - 0));
            if (float.IsNaN(angle))
            {
                sensor.AddObservation(new float[1]); //1

            }
            else
            {
                sensor.AddObservation(angle);

            }
            // relative distance from beek tip to flower  1
            sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);//1
            // 10 total observations

        }
        else
        {
            sensor.AddObservation(new float[9]);

        }


    }// End public override void CollectObservations(VectorSensor sensor)


    /// <summary>
    /// no neural net, use it
    /// </summary>
    /// <param name="actionsOut">output action array</param>
    public override void Heuristic(float[] actionsOut)
    {
        // create placeholder
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // keyboard input to control
        // forward / backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;
        // left / right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;
        // up / down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;
        // pitch up / down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;
        // yaw left / down
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        // combine and normalize
        Vector3 combined = (forward + left + up).normalized;
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;


    }
    /// <summary>
    /// prevent from moving
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported in training");
        isFrozen = true;
        rigidbody.Sleep();
    }
    /// <summary>
    /// resume movement
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported in training");
        isFrozen = false;
        rigidbody.WakeUp();
    }


    Vector3 circlePosition;
    /// <summary>
    /// Move the agent to safe random position, no collision
    /// or flower with beek
    /// </summary>
    /// <param name="inFrontOfFlower"></param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // loop until safe position
        while (!safePositionFound && attemptsRemaining > 0)
        {
            --attemptsRemaining;
            if (inFrontOfFlower)
            {
                Flower randomFlower = flowerArea.Flowers[
                    UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];
                // position in front of flower
                float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);
                potentialPosition = randomFlower.transform.position
                    + randomFlower.FlowerUpVector * distanceFromFlower;

                // Point beek at flower
                Vector3 toFlower = randomFlower.FlowerCenterVector - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);

            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);
                float radius = UnityEngine.Random.Range(2f, 7f);
                Quaternion direction = Quaternion.Euler(
                    0, UnityEngine.Random.Range(-180f, 180f), 0f);
                potentialPosition = flowerArea.transform.position
                    + Vector3.up * height + direction * Vector3.forward * radius;
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }


            // agent collision
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);
            // safe position
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not found a safe position");

        // set position, rotation
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
        Vector3 RandomV = new Vector3(Random.Range(-1, 2), Random.Range(0, 2), Random.Range(-1, 2));
        circlePosition = potentialPosition + RandomV;
        humanHandAvatar.transform.position = potentialPosition + RandomV;
    }


    /// <summary>
    /// update the nearest flower to agent
    /// </summary>
    private void UpdateNearestFlower()
    {

        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                // no current nearest flower
                nearestFlower = flower;
                //Debug.Log("flower111,,,");
            }
            else if (flower.HasNectar)
            {
                //Debug.Log("flower222,,,");
                // calculate distance to this flower and current nearest
                float distanceToFlower = Vector3.Distance(
                    flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(
                    nearestFlower.transform.position, beakTip.position);
                // current flower empty, update nearest flower
                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                   // Debug.Log("flower,,,");
                }

            }
        }
    }
    /// <summary>
    /// Called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }
    /// <summary>
    /// Called when the agent's collider stays a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }
    /// <summary>
    /// Enter or stay in a trigger collider
    /// </summary>
    /// <param name="collider"></param>
    ///
    float reward;

    private void OnTriggerExit(Collider other)
    {
        if (trainingMode && other.CompareTag("hand"))
        {
            Debug.Log("OnTriggerExit - agent");
            //numberOfBadSteps++;
            AddReward(-0.014f);

            //if (numberOfBadSteps > this.MaxStep * 0.7f)
            //{
            //    Debug.Log("Failed the Goal");
            //    SetReward(-1.0f);  // agent has failed to chieve the goal
            //    EndEpisode();
            //}
        }
    }
    ///

    private void TriggerEnterOrStay(Collider collider)
    {



        if (collider.CompareTag("hand"))
        {
            agentNottInGoalHandTime = 0;
            Debug.Log("Enter in hand   ");
            AddReward(0.02f);
            //numberOfGoodSteps++;


            ////check If Number Of GoodSteps is greater than 70% max of the episode
            //if (trainingMode && numberOfGoodSteps > this.MaxStep * 0.55f)
            //{

            //    Debug.Log("hand - Sucess the Goal");
            //    SetReward(1.0f);
            //    EndEpisode();
            //}

        }


        // check if colliding with nectar
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);
            // check if closest is close to tip
            Flower flower = flowerArea.GetFlowerFromNectar(collider);
            // Attemp to take 0.01 nectar
            float nectarReceived = flower.Feed(0.01f);
            // nectar obtained
            NectarObtained += nectarReceived;
            if (trainingMode)
            {
                agentNottInGoalFowerTime = 0;

                // calculate reward for getting nectar
                // Mathf.Clamp01 -> Clamps value between 0 and 1 and returns value
                //float bonus = 0.02f * Mathf.Clamp01(
                //    Vector3.Dot(transform.forward.normalized,
                //        -nearestFlower.FlowerUpVector.normalized));
                //AddReward(0.0015f);
                if (!flower.HasNectar)
                {
                    AddReward(0.001f); // experiment, balance reward
                }
                else
                {
                    AddReward(0.014f);
                }

                //numberOfGoodSteps++;
                //if (numberOfGoodSteps > this.MaxStep * 0.25f)
                //{

                //    Debug.Log("Sucess eatting to flower");
                //    SetReward(1);
                //    EndEpisode();
                //}



                Debug.Log("eatting flower");

            }
            // if flower empty, update nearest flower
            if (!flower.HasNectar)
            {
                UpdateNearestFlower();
            }
            //reward = GetCumulativeReward();
            //Debug.Log("reward :  " + reward);
        }






    }



    /// <summary>
    /// when collides with something solid
    /// </summary>
    /// <param name="collision">collision info</param>
    private void OnCollisionEnter(Collision collision)
    {

        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // boundary negative reward
            Debug.Log("crush boundary");
            AddReward(-0.5f); // discourage getting outside
        }
    }
    /// <summary>
    /// call every frame
    /// </summary>
    private void Update()
    {
        if (humanHandAvatar.activeSelf == true && humanHandAvatar != null)
        {
            Debug.DrawLine(beakTip.position, humanHandAvatar.transform.position, Color.blue);
            //CheckIfOntherRightTrack(humanHandAvatar);

        }
        // Beektip to flower-line debug
        if (nearestFlower != null)
        {

            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterVector, Color.green);
        }
    }
    /// <summary>
    /// called every 0.02 seconds
    /// </summary>
    private void FixedUpdate()
    {


        if (mUserExist)
        {
            humanHandAvatar.SetActive(true);
            nearestFlower = null;
            
        }
        else
        {
            humanHandAvatar.SetActive(false);

        }


        // Debug.Log($"GetCumulativeReward :   {GetCumulativeReward()},  stepCount :   { this.StepCount }, " +
        //    $"numberOfGoodSteps : {numberOfGoodSteps}, numberOfBadSteps : {numberOfBadSteps}");
        if (trainingMode)
        {
            AddReward(-1f / MaxStep);
            if (mUserExist)
            {
                if (timeValue > 999)
                {
                    timeValue = 0;
                }
                else
                {
                    timeValue += 0.1f;
                }

                circlePositionX = circlePosition.x + Mathf.Sin(Time.deltaTime * timeValue * circleSpeed) * circleSize;
                circlePositionZ = circlePosition.z + Mathf.Cos(Time.deltaTime * timeValue * circleSpeed) * circleSize;
                //zPos += forwardSpeed * Time.deltaTime;

                //circleSize += circleGrowSpeed;
                humanHandAvatar.transform.position = new Vector3(circlePositionX, circlePosition.y, circlePositionZ);

                CheckIfInRightDirection(humanHandAvatar, mNearRadius);
            }
            else
            {
                CheckIfInRightDirection1();

            }
        }





        // avoid stolen nearest flower
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }else if(nearestFlower == null && mUserExist == false)
        {
            UpdateNearestFlower();
        }
    }


    //100스텝중에 반 이상이 범위 안에 들어오면 성공 - 안들어오면 실패
    //30% 범위 이내면 실패 SetReward(-1);  70% 이상 범위안에 있으면 성공 SetReward(1);
    //성공도 실패도 아닌데 방향이 맞으면 AddReward(0.01) 방향이 틀리면 0 reward

    //sucessfull steps  max step의 70프로면 성공
    //failed steps  max step 70프로 실패

    // 
    // 유저의 손이 있다면 손을 따라가는게 목표, 유저의 손이 없다면 꽃으로 가는게 목표
    // 유저의 손이 있을 경우 - 손으로 가는게 목표 - 손을 향해 방향을 맞게 가면(전방 120도 방향)  addreward(0.01)
    // 유저의 손이 없을 경우 - 꽂으로 가는게 목표 - 꽃을 향해 방향을 맞게 가면(전방 120도 방향)  addreward(0.01)




    public void CheckIfInRightDirection(GameObject targetObj, float nearRadius)// 방향이 올바르게 따라 잘 가고 있는지 체크해서 옮바르게 가면 보상 그렇지 않으면 처벌
    { //forwarddirection과 vectTarget 각도가 15도 이하면 righthDirection



        Vector3 vecToTarget = targetObj.transform.position - this.gameObject.transform.position;



        float distF = Vector3.Distance(targetObj.transform.position, this.gameObject.transform.position);


        if (distF < nearRadius) //접근했을 때 reward는를 trigger함수에서 적용했음
        {
            return;
        }
        //check if the agent has been in the near radius

        //float Dot = Vector3.Dot(this.gameObject.transform.forward, vecToTarget); //Dot = abs(a)*abs(b)*cos th(두백터간의 각)
        //                                                                         //cos th =Dot/(abs(a) * abs(b))

        //float AngleBetween = Mathf.Acos(Dot / (this.gameObject.transform.forward.magnitude * vecToTarget.magnitude));


        //float dot = Vector3.Dot( this.gameObject.transform.up , vecToTarget); //Dot = abs(a)*abs(b)*cos th(두백터간의 각)
        //cos th =Dot/(abs(a) * abs(b))


        //float AngleBetween = Mathf.Acos(dot) * Mathf.Rad2Deg;

        Vector3 targetDir = (vecToTarget).normalized;
        float dot = Vector3.Dot(transform.forward, targetDir);

        //내적을 이용한 각 계산하기
        // thetha = cos^-1( a dot b / |a||b|)
        float AngleBetweenDegree = Mathf.Acos(dot) * Mathf.Rad2Deg;


        //Debug.Log(AngleBetweenDegree);





        if (AngleBetweenDegree < mGoodAngelThreshold )
        {
            Debug.Log("hand - Good try   ");
            //AddReward(0.001f);
            AddReward(0.008f/ (distF*2)); //(손과 타겟의 거리가 0.3001m), --> 0.0133 보상 // 손과 타겟의 거리가 5m이면 0.0008 보상을 받음
            //agentNottInGoalHandTime++;
            //if (agentNottInGoalHandTime > this.MaxStep * 0.3f)
            //{

            //    Debug.Log("hand -Failed... only good try");
            //    SetReward(-1.0f);
            //    EndEpisode();
            //}
        }
        else if (AngleBetweenDegree > mBadAngelThreshold )
        {
            Debug.Log("hand - bad try   ");
            //AddReward(-0.001f);
            AddReward(-0.01f);//
            //numberOfBadSteps++;
            //if (numberOfBadSteps > this.MaxStep * 0.7f)
            //{
            //    Debug.Log("hand - Failed... do nothing ");
            //    SetReward(-1.0f);  // agent has failed to chieve the goal
            //    EndEpisode();
            //}

        }//otherwise the situation is need bad or good. increment no reward.

    }













    public void CheckIfInRightDirection1()// 방향이 올바르게 따라 잘 가고 있는지 체크해서 옮바르게 가면 보상 그렇지 않으면 처벌
    { //forwarddirection과 vectTarget 각도가 15도 이하면 righthDirection

        flowerTargetPosition = new Vector3(nearestFlower.FlowerCenterVector.x, nearestFlower.FlowerCenterVector.y, nearestFlower.FlowerCenterVector.z);

        Vector3 targetObj = flowerTargetPosition;

        Vector3 vecToTarget = targetObj - beakTip.transform.position;



        float distF = Vector3.Distance(targetObj, beakTip.transform.position);


        float upOrDown = Vector3.Dot(vecToTarget, -humanHandAvatar.transform.up);




        if (distF < mNearRadius)
        {
            return;
        }


        Vector3 targetDir = (vecToTarget).normalized;
        float dot = Vector3.Dot(transform.forward, targetDir);

        //내적을 이용한 각 계산하기
        // thetha = cos^-1( a dot b / |a||b|)
        float AngleBetweenDegree = Mathf.Acos(dot) * Mathf.Rad2Deg;


        //Debug.Log(AngleBetweenDegree);





        if (AngleBetweenDegree < mGoodAngelThreshold && distF<1f)
        {
            Debug.Log("Flower - Good try  ");
            //AddReward(0.008f / (distF * 2));
            AddReward(0.008f / (distF * 2));
            //agentNottInGoalFowerTime++;
            //if (agentNottInGoalFowerTime > this.MaxStep * 0.3f)
            //{

            //    Debug.Log("Flower -Failed... only good try");
            //    SetReward(-1.0f);
            //    EndEpisode();
            //}
        }
        if (AngleBetweenDegree > mBadAngelThreshold)
        {
            Debug.Log("Flower - bad try   ");
            AddReward(-0.01f);
            //numberOfBadSteps++;
            //if (numberOfBadSteps > this.MaxStep * 0.7f)
            //{
            //    Debug.Log("Flower -Failed... do nothing");
            //    SetReward(-1.0f);  // agent has failed to chieve the goal
            //    EndEpisode();
            //}

        }//otherwise the situation is need bad or good. increment no reward.

    }

    float map(float value, float istart, float istop, float ostart, float ostop)
    {
        return ostart + (ostop - ostart) * ((value - istart) / (istop - istart));
    }

}
