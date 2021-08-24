using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContactEnnemyIdleState : EntityState
{
    public override EntityState checkTransition()
    {
        GameObject player = Globals.singleton.player;
        if ((player.transform.position - this.automaton.transform.position).magnitude < automaton.GetComponent<ContactEnnemySettings>().targetDetectionDistance)
        {
            return new ContactEnnemyRunState(Globals.singleton.player);
        }

        return null;
    }

    public override void fixedUpdateState()
    {
    }

    public override void onEnterState()
    {
        automaton.GetComponent<Animator>().SetInteger("AnimationState", (int)EntityAnimationState.IDLE);
    }

    public override void onExitState()
    {
    }

    public override void updateState()
    {
    }
}
