﻿namespace TextChat.Commands.RemoteAdmin.Mute
{
    using CommandSystem;
    using Exiled.API.Features;
    using Exiled.Permissions.Extensions;
    using Extensions;
    using Localizations;
    using System;
    using System.Linq;
    using static Database;
	using static TextChat;

    public class Add : ICommand
    {
        public string Command => "add";

        public string[] Aliases => new[] { "a" };

        public string Description => Language.AddMuteCommandDescription;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
			Player player = Player.Get(((CommandSender)sender).SenderId);

			if (!sender.CheckPermission("tc.mute"))
			{
				response = Language.CommandNotEnoughPermissionsError;
				return false;
			}

			if (arguments.Count < 2)
			{
				response = string.Format(Language.CommandNotEnoughParametersError, 2, Language.AddMuteCommandUsage);
				return false;
			}

			Player target = Player.Get(arguments.At(0));
			Collections.Chat.Player chatPlayer = arguments.At(0).GetChatPlayer();

			if (chatPlayer == null)
			{
				response = string.Format(Language.PlayerNotFoundError, arguments.At(0));
				return false;
			}

			if (!double.TryParse(arguments.At(1), out double duration) || duration < 1)
			{
				response = string.Format(Language.InvalidDurationError, arguments.At(1));
				return false;
			}

			string reason = string.Join(" ", arguments.Skip(2).Take(arguments.Count - 2));

			if (string.IsNullOrEmpty(reason))
			{
				response = Language.ReasonCannotBeEmptyError;
				return false;
			}

			if (chatPlayer.IsChatMuted())
			{
				response = string.Format(Language.PlayerIsAlreadyMutedError, chatPlayer.Name);
				return false;
			}

			LiteDatabase.GetCollection<Collections.Chat.Mute>().Insert(new Collections.Chat.Mute()
			{
				Target = chatPlayer,
				Issuer = player.GetChatPlayer(),
				Reason = reason,
				Duration = duration,
				Timestamp = DateTime.Now,
				Expire = DateTime.Now.AddMinutes(duration)
			});

			if (Instance.Config.ChatMutedBroadcast.Show)
			{
				target?.ClearBroadcasts();
				target?.Broadcast(Instance.Config.ChatMutedBroadcast.Duration, string.Format(Instance.Config.ChatMutedBroadcast.Content, duration, reason), Instance.Config.ChatMutedBroadcast.Type);
			}

			target?.SendConsoleMessage(string.Format(Language.AddMuteCommandSuccessPlayer, duration, reason), "red");

			response = string.Format(Language.AddMuteCommandSuccessModerator, chatPlayer.Name, duration, reason);
			return true;
		}
    }
}
