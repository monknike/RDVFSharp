﻿using RDVFSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RDVFSharp.FightingLogic.Actions
{
    class FightActionTackle : BaseFightAction
    {
        public override bool Execute(int roll, Battlefield battlefield, Fighter initiatingActor, Fighter targetedActor)
        {
            var attacker = initiatingActor;
            var target = battlefield.GetTarget();
            var damage = Utils.RollDice(new List<int>() { 5, 5 }) - 1 + attacker.Strength;
            damage /= 2;
            var requiredStam = 10;
            var difficulty = 8; //Base difficulty, rolls greater than this amount will hit.
            var others = battlefield.Fighters.Where(x => x.Name != attacker.Name).OrderBy(x => new Random().Next()).ToList();
            var othersdeadcheck = others.Where(x => x.IsDead == false).OrderBy(x => new Random().Next()).ToList();
            var sametarget = othersdeadcheck.Where(x => x.CurrentTarget == attacker.CurrentTarget).OrderBy(x => new Random().Next()).ToList();


            difficulty += 2 * sametarget.Count;
            if (target.IsExposed > 0) difficulty -= 2; // If opponent left themself wide open after a failed strong attack, they'll be easier to hit.
            if (target.IsEvading > 0)
            {//Evasion bonus from move/teleport. Only applies to one attack, then is reset to 0.
                difficulty += target.IsEvading;
                damage -= target.IsEvading;
            }
            if (attacker.IsAggressive > 0)
            {//Apply attack bonus from move/teleport then reset it.
                difficulty -= attacker.IsAggressive;
                damage += attacker.IsAggressive;
                attacker.IsAggressive = 0;
            }
            if (attacker.IsEvading > 0)
            {//Apply attack bonus from move/teleport then reset it.
                attacker.IsEvading = 0;
            }
            var critCheck = true;
            if (attacker.Stamina < requiredStam)
            {   //Not enough stamina-- reduced effect
                critCheck = false;
                damage /= 2;
                attacker.HitHp(requiredStam - attacker.Stamina);
                difficulty += (int)Math.Ceiling((double)((requiredStam - attacker.Stamina) / requiredStam) * (20 - difficulty)); // Too tired? You're likely to miss.
                battlefield.OutputController.Hint.Add(attacker.Name + " did not have enough stamina, and took penalties to the attack.");
            }

            attacker.HitStamina(requiredStam); //Now that stamina has been checked, reduce the attacker's stamina by the appopriate amount. (We'll hit the attacker up for the rest on a miss or a dodge).

            var attackTable = attacker.BuildActionTable(difficulty, target.Dexterity, attacker.Dexterity, target.Stamina, target.StaminaCap);
            //If target can dodge the atatcker has to roll higher than the dodge value. Otherwise they need to roll higher than the miss value. We display the relevant value in the output.
            battlefield.OutputController.Info.Add("Dice Roll Required: " + (attackTable.miss + 1));

            if (roll <= attackTable.miss)
            {   //Miss-- no effect.
                battlefield.OutputController.Hit.Add(" FAILED!");
                attacker.IsExposed += 2; //If the fighter misses a big attack, it leaves them open and they have to recover balance which gives the opponent a chance to strike.
                battlefield.OutputController.Hint.Add(attacker.Name + " was left wide open by the failed attack and is now Exposed! " + target.Name + " has -2 difficulty to hit and can use Grab even if fighters are not in grappling range!");
                //If opponent fumbled on their previous action they should become stunned. Tackle is a special case because it stuns anyway if it hits, so we only do this on a miss.
                if (target.Fumbled)
                {
                    target.IsDazed = true;
                    target.Fumbled = false;
                }
                return false; //Failed attack, if we ever need to check that.
            }

            if (roll >= attackTable.crit && critCheck == true)
            { //Critical Hit-- increased damage/effect, typically 3x damage if there are no other bonuses.
                battlefield.OutputController.Hint.Add("Critical Hit! " + attacker.Name + " really drove that one home!");
                damage += 10;
            }

            battlefield.OutputController.Hit.Add(attacker.Name + " TACKLED " + target.Name + ". " + attacker.Name + " can take another action while their opponent is stunned!");

            //Deal all the actual damage/effects here.

            damage = Math.Max(damage, 0);
            if (damage > 0) target.HitHp(damage); //This is to prevent the game displayin that the attacker did 0 damage, which is the normal case.
            if (target.IsDazed) target.Fumbled = true;
            battlefield.Fighters.ForEach(f => f.IsDazed = (f != attacker)); // Set all as dazed
            if (target.IsDisoriented > 0) target.IsDisoriented += 2;
            if (target.IsExposed > 0) target.IsExposed = 0;

            if (attacker.IsGrabbable == 0 && target.IsGrabbable == 0)
            {
                attacker.IsGrabbable += 1;
                target.IsGrabbable += 1;
            }
            if (attacker.IsGrabbable == 0 && target.IsGrabbable == 1)
            {
                attacker.IsGrabbable += 1;
            }
            if (attacker.IsGrabbable == 1 && target.IsGrabbable == 0)
            {
                attacker.IsGrabbable += 1;
                target.IsGrabbable += 2;
            }
            if (attacker.IsGrabbable == 2 && target.IsGrabbable == 1)
            {
                attacker.IsGrabbable -= 1;
            }
            if (attacker.IsGrabbable == 1 && target.IsGrabbable == 2)
            {
                attacker.IsGrabbable += 1;
            }
            if (attacker.IsGrabbable == 2 && target.IsGrabbable == 0)
            {
                attacker.IsGrabbable -= 1;
                target.IsGrabbable += 1;
            }
            if (attacker.IsGrabbable == 0 && target.IsGrabbable == 2)
            {
                attacker.IsGrabbable += 2;
            }

            return true; //Successful attack, if we ever need to check that.
        }
    }
}
