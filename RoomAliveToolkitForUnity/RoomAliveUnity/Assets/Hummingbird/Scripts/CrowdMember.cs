using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class CrowdMember : MonoBehaviour
{
    public Rigidbody controller;
    public float cohesionWeight = 0.1f;
    public float alignmentWeight = 0.1f;
    public float avoidanceWeight = 0.1f;
    public float obstacleAvoidanceForceWeight = 0.1f;
    public float seekWeight;
    public float avoidancWeight;

    public CrowdManager crowdManager;
    public List<GameObject> agents = new List<GameObject>();
    public float neighborRadius; // set in the inspector.
    public float avoidanceRadius = 5f; // [m]
    //public FibonacciRays fibonacciRays;
    int userMask;
    public float movementScalingFactor; // set in the inspector.
    public float maxSpeed; // set in the inspector.

    public float moveForce = 2.0f;


    [Header("Seek")]
    public float SeekWeight = 0;              // 0
    [Header("Arrival")]
    public float ArrivalSlowingDistance =2;  // 2
    //public float ArrivalMaxSpeed = 0.2f;         // 0.2

    public Transform tartget;

    //public GameObject predator;

    private void Start()
    {

        userMask = 1 << LayerMask.NameToLayer("user");
        GameObject parentObject = this.gameObject.transform.parent.gameObject; // parentObject is CrowdManager.
        agents = crowdManager.crowds;

        controller = this.gameObject.transform.GetComponent<Rigidbody>();


        //fibonacciRays = new FibonacciRays();
    }
    List<GameObject> GetNeighbors(List<GameObject> agents, float neighborRadius) 
    {
        List<GameObject> neighborAgents = new List<GameObject>(); // 비어있는 리스트 생성.

        foreach (GameObject agent in agents)
        {
            if (Vector3.Distance(this.gameObject.transform.position, agent.transform.position) < neighborRadius)
            {
                if (agent != this.gameObject)
                {
                    neighborAgents.Add(agent);
                }
            }
        }
        return neighborAgents;
    }
    // 'Cohesion' function of the 'Crowd Member' class
    Vector3 CohesionVector()
    {
        Vector3 currentVelocity = new Vector3();
        float agentSmoothTime = 0.5f;
        Vector3 cohMove = Vector3.zero;
        if (GetNeighbors(agents, neighborRadius).Count == 0)
        {
            return Vector3.zero;
        }
        foreach (GameObject a in GetNeighbors(agents, neighborRadius))
        {
            cohMove += a.transform.position;
        }
        cohMove /= GetNeighbors(agents, neighborRadius).Count;
        cohMove -= this.transform.position;
        cohMove = Vector3.SmoothDamp(this.transform.forward, cohMove, ref currentVelocity, agentSmoothTime);
        return cohMove;
    }

    // 'Alignment' function of the 'Crowd Member' class
    Vector3 AlignmentVector()
    {
        Vector3 alignMove = Vector3.zero;
        if (GetNeighbors(agents, neighborRadius).Count == 0)
        {
            return this.transform.forward;
        }
        foreach (GameObject a in GetNeighbors(agents, neighborRadius))
        {
            alignMove += a.transform.forward;
        }
        alignMove /= GetNeighbors(agents, neighborRadius).Count;
        return alignMove;
    }

    // 'Avoidance' function of the 'Crowd Member' class
    Vector3 AvoidanceVector()
    {
        Vector3 avoidMove = Vector3.zero;
        int nAvoid = 0;
        if (GetNeighbors(agents, neighborRadius).Count == 0)
        {
            return Vector3.zero;
        }
        foreach (GameObject a in GetNeighbors(agents, neighborRadius))
        {
            float distance = Vector3.Distance(this.transform.position, a.transform.position);
            if (distance < avoidanceRadius)
            {
                avoidMove += this.transform.position - a.transform.position;
                nAvoid++;
            }
        }
        if (nAvoid > 0)
        {
            avoidMove /= nAvoid;
        }
        return avoidMove;
    }

    public int numViewDirections = 10;
    Vector3[] directions;
    Vector3[] FibonacciRays()
    {
        //numViewDirections = total_Butterfly;
        directions = new Vector3[numViewDirections];
        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < numViewDirections; i++)
        {
            float t = (float)i / numViewDirections;
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth); ;
            float z = Mathf.Cos(inclination);
            directions[i] = new Vector3(x, y, z);
        }
        return directions;
    }
    // 'Obstacle avoidance' function of the 'Crowd Member' class
    Vector3 AttractVectorToUsers()
    {
        Vector3[] rayDirections = FibonacciRays();
        //Debug.Log("FibonacciRays  -> rayDirections   :   " + rayDirections[0]+"  "+ rayDirections[1] +"  "+ rayDirections[2] + "  " + rayDirections[3]);
        float raySphereRadius = 0.2f;
        float obstCollisionAvoidDst = 0.2f;
        
        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 dir = this.transform.TransformDirection(rayDirections[i]);
            Ray ray = new Ray(this.transform.position, dir);
            if (!Physics.SphereCast(ray, raySphereRadius, obstCollisionAvoidDst, userMask))
            // SphereCast(Ray ray, float radius, float maxDistance, int layerMask);
            {
                //Debug.Log(i + " AttractVectorToUsers   :   " + dir);
                return dir;
            }
        }
        return Vector3.zero;
    }

    // 'Move' function of the 'Crowd Member' class
    void Move()
    {
        Vector3 move = Vector3.zero;
        int numOfBehavs = 6;
        Vector3[] behaviors = new Vector3[numOfBehavs];



        behaviors[0] = CohesionVector();
        behaviors[1] = AlignmentVector();
        behaviors[2] = AvoidanceVector();
        behaviors[3] = AttractVectorToUsers(); // ObstacleAvoidanceForce -> AttractVectorToUsers : 자기랑 가까운 유저에게 접근한다.
        //behaviors[4] = AvoidPredator();
        //behaviors[5] = Forage();
        behaviors[4] = Seek(tartget);
        behaviors[5] = Arrival(tartget, ArrivalSlowingDistance, maxSpeed);


        float[] weights = new float[numOfBehavs];
        weights[0] = cohesionWeight;
        weights[1] = alignmentWeight;
        weights[2] = avoidanceWeight;
        weights[3] = obstacleAvoidanceForceWeight;

        weights[4] = seekWeight;
        weights[5] = avoidancWeight;
        for (int i = 0; i < behaviors.Length; i++)
        {
            Vector3 partialMove = behaviors[i] * weights[i];
            if (partialMove != Vector3.zero)
            {
                if (partialMove.sqrMagnitude > weights[i] * weights[i])
                {
                    partialMove.Normalize();
                    partialMove *= weights[i];
                }
                move += partialMove;
            }
        }
        move *= movementScalingFactor; 
        if (move.sqrMagnitude > maxSpeed) 
        {
            move = move.normalized * maxSpeed;
        }



        //Debug.Log("CohesionVector() :  " + CohesionVector() +
        //    " AlignmentVector() :  " + AlignmentVector() +
        //    " AvoidanceVector() :  " + AvoidanceVector() +
        //    " AttractVectorToUsers() :  " + AttractVectorToUsers() +
        //    " move : " + move);

        this.controller.AddForce(move * moveForce);

    }
    // Update is called once per frame

    private void FixedUpdate()
    {

        Move();
    }
    public Vector3 Seek(Transform target)
    {

        var desiredVelocity = (target.position - transform.position);
        return desiredVelocity;
    }


    public Vector3 Arrival(Transform target, float slowingDistance, float maxSpeed)
    {
        var desiredVelocity = Vector3.zero;
        if (slowingDistance < 0.0001f) return desiredVelocity;

        var targetOffset = target.position - transform.position;
        var distance = Vector3.Distance(target.position, transform.position);
        var rampedSpeed = maxSpeed * (distance / slowingDistance);
        var clippedSpeed = Mathf.Min(rampedSpeed, maxSpeed);
        if (distance > 0)
        {
            desiredVelocity = (clippedSpeed / distance) * targetOffset;
        }
        return desiredVelocity;
    }



    ////Predator avoidance and foraging functions -> 이거 안씀
    //public Vector3 AvoidPredator()
    //{
    //    if (Vector3.Distance(this.transform.position, predator.transform.position) <=  100)
    //    {
    //        Debug.Log("AVOID!!!!!!");
    //        return this.transform.position - predator.transform.position;
    //    }

    //    else
    //    {
    //        return Vector3.zero;
    //    }
    //}

    //public Vector3 Forage()
    //{
    //    return (GameObject.FindGameObjectsWithTag("flower_plant")
    //      .OrderBy(t => (t.transform.position - this.transform.position).sqrMagnitude).FirstOrDefault()
    //      .transform.position - this.transform.position).normalized;
    //}
 

}
