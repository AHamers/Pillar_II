using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ContactEnnemyWalkState : EntityState
{
    private GameObject target;

    public ContactEnnemyWalkState(GameObject target)
    {
        this.target = target;
    }
    public override EntityState checkTransition()
    {
        if ((target.transform.position - automaton.transform.position).magnitude < automaton.gameObject.GetComponent<ContactEnnemySettings>().minDistanceToTarget)
        {
            return new ContactEnnemyAttackState(target);
        }
        if ((target.transform.position - automaton.transform.position).magnitude > automaton.gameObject.GetComponent<ContactEnnemySettings>().walkRunDistance)
        {
            return new ContactEnnemyRunState(target);
        }
        return null;
    }

    public override void fixedUpdateState()
    {
        float previousYRotation = automaton.gameObject.transform.rotation.eulerAngles.y;
        NavMeshAgent nma = this.automaton.gameObject.GetComponent<NavMeshAgent>();
        NavMeshPath path = new NavMeshPath();
        bool hasFoundPath = nma.CalculatePath(target.transform.position, path);
        if (hasFoundPath)
        {
            automaton.transform.LookAt(path.corners[1]);
            automaton.GetComponent<NavMeshAgent>().Move((path.corners[1] - automaton.transform.position).normalized * automaton.GetComponent<ContactEnnemySettings>().walkingSpeed * Time.deltaTime);
        }

        float directionalRoll = previousYRotation - automaton.gameObject.transform.rotation.eulerAngles.y;
        if(directionalRoll > 180)
            directionalRoll -= 360;
        if (directionalRoll < -180)
            directionalRoll += 360;
        this.automaton.GetComponent<Animator>().SetFloat("DirectionalRoll", directionalRoll);
    }

    public override void onEnterState()
    {
        this.automaton.GetComponent<Animator>().SetInteger("AnimationState", (int)EntityAnimationState.WALKING);
        this.automaton.GetComponent<NavMeshAgent>().destination = target.transform.position;
    }

    public override void onExitState()
    {
    }

    public override void updateState()
    {
    }
}
