using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContactEnnemyAttackState : EntityState
{
    private GameObject target;

    public ContactEnnemyAttackState(GameObject target)
    {
        this.target = target;
    }

    public override EntityState checkTransition()
    {
        if ((target.transform.position - automaton.transform.position).magnitude > automaton.gameObject.GetComponent<ContactEnnemySettings>().attackRange)
        {
            return new ContactEnnemyWalkState(target);
        }
        return null;
    }

    public override void fixedUpdateState()
    {

    }

    public override void onEnterState()
    {
        this.automaton.GetComponent<Animator>().SetInteger("AnimationState", (int)EntityAnimationState.CONTACT_ATTACKING);
    }

    public override void onExitState()
    {
    }

    public override void updateState()
    {
    }
}