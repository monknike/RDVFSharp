﻿using RDVFSharp.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RDVFSharp.FightingLogic.Actions
{
    class FightActionRanged : BaseFightAction
    {
        public override bool Execute(int roll, Battlefield battlefield, Fighter initiatingActor, Fighter targetedActor)
        {
            var attacker = initiatingActor;
            var target = battlefield.GetTarget();
            var damage = Utils.RollDice(new List<int>() { 5, 5 }) - 1;
            damage *= 2;
            damage += (attacker.Strength + attacker.Dexterity);
            damage += Math.Min(attacker.Strength, attacker.Spellpower);
            var requiredStam = 10;
            var difficulty = 10; //Base difficulty, rolls greater than this amount will hit.
            var others = battlefield.Fighters.Where(x => x.Name != attacker.Name).OrderBy(x => new Random().Next()).ToList();
            var othersdeadcheck = others.Where(x => x.IsDead == false).OrderBy(x => new Random().Next()).ToList();
            var sametarget = othersdeadcheck.Where(x => x.CurrentTarget == attacker.CurrentTarget).OrderBy(x => new Random().Next()).ToList();


            difficulty += 2 * sametarget.Count;
            //If opponent fumbled on their previous action they should become stunned.
            if (target.Fumbled)
            {
                target.IsDazed = true;
                target.Fumbled = false;
            }

            if (attacker.IsRestrained) difficulty += 4; //Up the difficulty considerably if the attacker is restrained.
            if (attacker.IsFocused > 0 && !attacker.IsRestrained) difficulty -= (int)Math.Ceiling((double)attacker.IsFocused / 10); //Lower the difficulty considerably if the attacker is focused
            if (target.IsExposed > 0) difficulty -= 2; // If opponent left themself wide open after a failed strong attack, they'll be easier to hit.

            if (attacker.IsFocused > 0 && !attacker.IsRestrained) damage += (int)Math.Ceiling((double)attacker.IsFocused / 10); //Focus gives bonus damage.

            if (target.IsEvading > 0)
            {//Evasion bonus from move/teleport. Only applies to one attack, then is reset to 0.
                difficulty += (int)Math.Ceiling((double)target.IsEvading / 2);//Half effect on ranged attacks.
                damage -= (int)Math.Ceiling((double)target.IsEvading / 2);
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

            attacker.HitStamina(requiredStam); //Now that stamina has been checked, reduce the attacker's stamina by the appopriate amount.

            var attackTable = attacker.BuildActionTable(difficulty, target.Dexterity, attacker.Dexterity, target.Stamina, target.StaminaCap);
            //If target can dodge the atatcker has to roll higher than the dodge value. Otherwise they need to roll higher than the miss value. We display the relevant value in the output.
            battlefield.OutputController.Info.Add("Dice Roll Required: " + (attackTable.miss + 1));

            if (roll <= attackTable.miss)
            {   //Miss-- no effect.
                battlefield.OutputController.Hit.Add(" FAILED!");
                return false; //Failed attack, if we ever need to check that.
            }

            if (roll >= attackTable.crit && critCheck == true)
            { //Critical Hit-- increased damage/effect, typically 3x damage if there are no other bonuses.
                battlefield.OutputController.Hit.Add(" CRITICAL HIT! ");
                battlefield.OutputController.Hint.Add(attacker.Name + " hit somewhere that really hurts!");
                damage += 10;
            }
            else
            { //Normal hit.
                battlefield.OutputController.Hit.Add(" HIT! ");
            }

            //Deal all the actual damage/effects here.

            foreach (var opponent in battlefield.Fighters.Where(x => x.TeamColor != attacker.TeamColor))
            {
                if (attacker.IsGrabbable > 0 && opponent.IsGrabbable == attacker.IsGrabbable && !attacker.IsGrappling(target) && !target.IsGrappling(attacker))
                {
                    battlefield.OutputController.Hint.Add(attacker.Name + " managed to put some distance between them and " + opponent.Name + " and is now out of grabbing range.");
                }
            }


            //If you're being grappled and you hit the opponent that will make it a little easier to escape later on.
            if (attacker.IsRestrained) attacker.IsEscaping += (int)Math.Floor((double)damage / 5);
            attacker.IsGrabbable = 0;
            target.IsGrabbable = 0;
            damage = Math.Max(damage, 1);
            target.HitHp(damage);
            return true; //Successful attack, if we ever need to check that.
        }
    }
}
