using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCastSpellState : EntityState
{
    private Spell spell;
    private EntityState caller;

    public PlayerCastSpellState(Spell spell, EntityState caller)
    {
        this.spell = spell;
        this.caller = caller;
    }

    public override EntityState checkTransition()
    {
        return caller;
    }

    public override void fixedUpdateState()
    {
    }

    public override void onEnterState()
    {
        spell.cast(automaton);
    }

    public override void onExitState()
    {
    }

    public override void updateState()
    {
    }
}
