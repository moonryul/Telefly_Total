using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Predator : MonoBehaviour
{

    public float predtorVisionRadius = 5.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MoveDirection();
    }

    // Predator main functionality -> 이거 안씀
    List<Transform> radiusMemmory = new List<Transform>();

    Vector3 MoveDirection()
    {
        List<Transform> agentWithinRadius = new List<Transform>();

        foreach (GameObject agent in GameObject.FindGameObjectsWithTag("agent"))
        {
            if (Vector3.Distance(this.transform.position, agent.transform.position) <= predtorVisionRadius)
            {
                agentWithinRadius.Add(agent.transform);
                if (!radiusMemmory.Contains(agent.transform)) radiusMemmory.Add(agent.transform);
            }
            else
            {
                if (radiusMemmory.Contains(agent.transform)) radiusMemmory.Remove(agent.transform);
            }
        }

        if (agentWithinRadius.Count < 2)
        {
            radiusMemmory.Clear();
            return (GameObject.FindGameObjectsWithTag("agent")
              .OrderBy(t => (t.transform.position - this.transform.position).sqrMagnitude).FirstOrDefault()
              .transform.position - this.transform.position).normalized;
        }
        else
        {
            if (radiusMemmory.Count > 0) return (radiusMemmory[radiusMemmory.Count - 1].position
              - this.transform.position).normalized;
            else return Vector3.zero;
        }
    }
}
