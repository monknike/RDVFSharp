﻿using System;
using RDVFSharp.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDVFSharp.Commands
{
    public class Tackle : Action
    {
        public override async Task ExecuteCommand(string character, IEnumerable<string> args, string channel)
        {
            var attacker = Plugin.GetCurrentBattlefield(channel).GetActor();
            var target = Plugin.GetCurrentBattlefield(channel).GetTarget();
            var others = Plugin.GetCurrentBattlefield(channel).Fighters.Where(x => x.Name != attacker.Name);
            var otherothers = Plugin.GetCurrentBattlefield(channel).Fighters.Where(x => x.Name != target.Name);



                    if (!target.IsRestrained && target.IsRestraining == 0 && attacker.IsRestraining == 0 && !attacker.IsRestrained && ((attacker.IsGrabbable != target.IsGrabbable) || (attacker.IsGrabbable == 0)))
                    {
                        base.ExecuteCommand(character, args, channel);
                    }
                    else
                    {
                        Plugin.FChatClient.SendMessageInChannel("You can't use Tackle when you already are in grappling range, or on someone that's being grappled by/grappling someone else.", channel);
                    }
        }
    }
}
