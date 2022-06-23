﻿using FChatSharpLib.Entities.Plugin.Commands;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using RDVFSharp.Helpers;

namespace RDVFSharp.Commands
{
    public class RoomCreate : BaseCommand<RDVFPlugin>
    {
        public static Dictionary<string, DateTime> CharacterCooldowns = new Dictionary<string, DateTime>();
        public static List<PayPerViewChannelInfo> CharacterRoomsIds = new List<PayPerViewChannelInfo>();
        private static int _counter = 1;

        public class PayPerViewChannelInfo
        {
            public int Id { get; set; }
            public string Channel { get; set; }
            public int EntryPrice { get; set; }
            public DateTime CreationTime { get; set; }
            public string ChannelName { get; set; }
            public string CreatorId { get; set; }
        }

        public async Task<List<string>> Execute(string characterCalling, IEnumerable<string> args)
        {
            var messages = new List<string>();

            if (CharacterCooldowns.ContainsKey(characterCalling))
            {
                if (CharacterCooldowns[characterCalling] > DateTime.Now)
                {
                    var message = $"Please wait before creating another room. (Cooldown: {(CharacterCooldowns[characterCalling] - DateTime.Now).FormatTimeSpan()} left)";
                    messages.Add($"{message}");
                    return messages;
                }
            }

            int entryPrice = 0;

            var room = new PayPerViewChannelInfo()
            {
                CreationTime = DateTime.Now,
                EntryPrice = entryPrice,
                ChannelName = $"RDVF - Private - {characterCalling}'s Room",
                CreatorId = characterCalling
            };
            Plugin.FChatClient.BotCreatedChannel += FChatClient_BotCreatedChannel;
            Plugin.FChatClient.CreateChannel(room.ChannelName);
            await Task.Delay(4000);
            if (!string.IsNullOrEmpty(_newChannelId))
            {
                room.Id = _counter;
                _counter++;
                room.Channel = _newChannelId;

                CharacterRoomsIds.Add(room);

                messages.Add($"A new channel titled '{room.ChannelName}' has been created for you ({room.Channel}).\n" +
                    $"An invite will be sent to you right away.\n" +
                    $"People can also get invited and join you by typing '!roomjoin {room.Id}'");

                await Task.Delay(2000);

                Plugin.FChatClient.InviteUserToChannel(characterCalling, room.Channel);
                Plugin.FChatClient.ModUser(characterCalling, room.Channel);
                Plugin.AddHandledChannel(room.Channel);
            }
            else
            {
                messages.Add("The bot couldn't create the channel. Contact Elise Pariat.");
            }

            return messages;
        }


        public async new void ExecutePrivateCommand(string characterCalling, IEnumerable<string> args)
        {
            var result = await Execute(characterCalling, args);
            foreach (var message in result)
            {
                Plugin.FChatClient.SendPrivateMessage(message, characterCalling);
            }
        }

        public override async Task ExecuteCommand(string characterCalling, IEnumerable<string> args, string channel)
        {
            var result = await Execute(characterCalling, args);
            foreach (var message in result)
            {
                Plugin.FChatClient.SendMessageInChannel($"{message}", channel);
            }
        }


        private string _newChannelId = "";

        private void FChatClient_BotCreatedChannel(object sender, FChatSharpLib.Entities.Events.Server.InitialChannelData e)
        {
            _newChannelId = e.channel;
            Console.WriteLine("New channel created.");
        }
    }
}