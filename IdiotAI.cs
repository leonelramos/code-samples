/**
    Author: Leonel Ramos
**/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class IdiotAI : MonoBehaviour
{

    enum State
    {
        OnNav,
        OnPlatform,
        Jumping,
        Landed,
        Dead,
        Won,
    }

    public float jumpDuration;
    public Transform target;
    public bool disableNavAgent = false;
    State currentState;
    NavMeshAgent agent;
    public Text deathScreen;
    public Text winScreen;
    public GameObject player=null; 
    private AudioSource deathAudio;
    private AudioSource winAudio;

    void Start()
    {
        deathScreen.enabled = false;
        winScreen.enabled = false;
        agent = GetComponent<NavMeshAgent>();
        deathAudio = (GetComponents<AudioSource>())[0];
        winAudio = (GetComponents<AudioSource>())[1];
        currentState = State.OnNav;
    }

    void Update()
    {
        // Only use Unity NavAgent pathfinding if in OnNav state
        if(currentState == State.OnNav)
        {
            agent.destination = target.position;
        }
        // Check if the Idiot has successfully cleared the level and won.
        if(Vector3.Distance(transform.position, target.position) < 2 && currentState != State.Won)
        {
            currentState = State.Won;
            winScreen.enabled = true;
            winAudio.Play(0);
        }
    }

    // If the Idiot has been hit by a hazard, start death code
    private void Die()
    {
        currentState = State.Dead;
        agent.enabled = false;
        (GetComponent<MeshRenderer>().material).color = Color.red;
        deathScreen.enabled = true;
        deathAudio.Play(0);
        StartCoroutine(DeathFall());
        if (player!=null) {
            player.SetActive(false);
        }
    }

    // Death animation routine
    private IEnumerator DeathFall()
    {
        Quaternion startRotation = transform.rotation;
        transform.Rotate(90, 0, 0);
        Quaternion endRotation = transform.rotation;
        transform.Rotate(-90, 0, 0);
        float fallSpeed = 1;
        float timeLeft = 1;
        while(timeLeft > 0)
        {
            timeLeft -= Time.deltaTime * fallSpeed;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, 1 - timeLeft/1);
            yield return null;
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState == State.Dead) return;
        if(other.gameObject.tag == "Hazard")
        {
            if(currentState != State.Dead) Die();

        }
        if(other.gameObject.tag == "Shover")
        {
            if(currentState != State.Dead) Die();

        }
        if(other.name == "PlatformTrigger" && (currentState == State.OnNav || currentState == State.OnPlatform))
        {
            Transform target = other.transform.GetChild(0);
            currentState = State.Jumping;
            agent.enabled = false;
            StartCoroutine(Jump(target, jumpDuration));
        }
        else if(other.name == "WalkwayTrigger")
        {
            transform.SetParent(null);
            Transform target = other.transform.GetChild(0);
            currentState = State.Jumping;
            agent.enabled = false;
            StartCoroutine(Jump(target, jumpDuration));
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentState == State.Dead) return;

        if (other.name == "PlatformTrigger" && currentState == State.Landed)
        {
            transform.SetParent(other.transform);
            currentState = State.OnPlatform;
            Transform target = other.transform.GetChild(1);
            StartCoroutine(MoveTo(target, 1));
        }
        if(other.name == "WalkwayTrigger" && currentState == State.Landed)
        {
            agent.enabled = true;
            currentState = State.OnNav;
        }
    }

    IEnumerator MoveTo(Transform target, float timeToPass)
    {
        float timeLeft = timeToPass;
        Vector3 start = transform.position;
        Vector3 end = target.position + agent.baseOffset * Vector3.up;
        
        Vector3 lastLerp = start;
        Vector3 currLerp;
        Vector3 moveAmount = Vector3.zero;

        while (timeLeft > 0)
        {

            timeLeft = timeLeft - Time.deltaTime;
            currLerp = Vector3.Lerp(start, end, (timeToPass - timeLeft) / timeToPass);
            moveAmount = currLerp - lastLerp;
            transform.position += moveAmount;
            lastLerp = currLerp;
            yield return null;
        }
    }

    IEnumerator Jump(Transform target, float timeToPass)
    {
        Vector3 start = transform.position;
        Vector3 end = target.position + agent.baseOffset * Vector3.up;

        ParabolicArc arc = new ParabolicArc(start, end, 1);

        float timeLeft = timeToPass;

        while (timeLeft > 0)
        {
            timeLeft = timeLeft - Time.deltaTime;

            transform.position =
                arc.Lerp(1 - timeLeft / timeToPass);

            yield return null;
        }
        currentState = State.Landed;
        transform.position = end;
    }
}