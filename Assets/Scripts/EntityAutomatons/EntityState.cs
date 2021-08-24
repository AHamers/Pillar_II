using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityAnimationState
{
    IDLE = 0,
    WALKING = 1,
    RUNNING = 2,
    JUMPING = 3,
    CONTACT_ATTACKING = 4
}

public abstract class EntityState
{
    protected EntityAutomaton automaton;

    public abstract EntityState checkTransition();
    public abstract void updateState();
    public abstract void fixedUpdateState();
    public abstract void onEnterState();
    public abstract void onExitState();

    public void setAutomaton(EntityAutomaton automaton)
    {
        this.automaton = automaton;
    }
}
