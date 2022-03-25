﻿using RDVFSharp.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RDVFSharp.FightingLogic.Actions
{
    class FightActionFumble : BaseFightAction
    {
        public override bool Execute(int roll, Battlefield battlefield, Fighter initiatingActor, Fighter targetedActor)
        {
            var attacker = initiatingActor;
            var target = battlefield.GetTarget();

            if (target.IsEvading > 0)
            {//Evasion bonus from move/teleport. Lasts 1 turn. We didn't make an attack and now it resets to 0.
                target.IsEvading = 0;
            }
            if (attacker.IsAggressive > 0)
            {//Only applies to 1 action, so we reset it now.
                attacker.IsAggressive = 0;
            }

            attacker.IsExposed += 2;//Fumbling exposes you.

            battlefield.WindowController.Hit.Add(" FUMBLE! ");

            // Fumbles make you lose a turn, unless your opponent fumbled on their previous one in which case nobody should lose a turn and we just clear the fumbled status on them.
            // Reminder: if fumbled is true for you, your opponent's next normal action will stun you.
            if (!target.Fumbled)
            {
                attacker.Fumbled = true;
                battlefield.WindowController.Hint.Add(attacker.Name + " loses the next action and is Exposed!");
            }
            else
            {
                target.Fumbled = false;
                battlefield.WindowController.Hint.Add("Both fighter fumbled and lost an action so it evens out, but you should still emote the fumble.");
            }

            return false;
        }
    }
}
