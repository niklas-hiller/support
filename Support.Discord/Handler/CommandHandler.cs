﻿using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLog;
using Support.Discord.Enums;
using Support.Discord.Exceptions;
using Support.Discord.Services;
using Support.Shared.Enums;

namespace Support.Discord.Handler
{
    internal static class CommandHandler
    {
        private static readonly DiscordSocketClient client = Program.client;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task InitializeCommands()
        {
            List<ApplicationCommandProperties> applicationCommandProperties = new();
            try
            {
                var command00 = new SlashCommandBuilder()
                    .WithName("create-project")
                    .WithDescription("Initiates a project creation process.")
                    .AddOption("project-name", ApplicationCommandOptionType.String, "The name for the project", isRequired: true)
                    .AddOption("sync", ApplicationCommandOptionType.Boolean, "If you directly want to sync the project with current channel", isRequired: true);
                applicationCommandProperties.Add(command00.Build());

                var command01 = new SlashCommandBuilder()
                    .WithName("delete-project")
                    .WithDescription("Deletes a project by project id.")
                    .AddOption("project-id", ApplicationCommandOptionType.String, "The project you with to delete", isRequired: true);
                applicationCommandProperties.Add(command01.Build());

                var command03 = new SlashCommandBuilder()
                    .WithName("sync")
                    .WithDescription("Syncs all activities regarding a project with the current channel.")
                    .AddOption("project-id", ApplicationCommandOptionType.String, "The project you wish to sync with current channel", isRequired: true);
                applicationCommandProperties.Add(command03.Build());

                var command04 = new SlashCommandBuilder()
                    .WithName("unsync")
                    .WithDescription("Unsyncs a project if any syncs exist.")
                    .AddOption("project-id", ApplicationCommandOptionType.String, "The project you wish to unsync", isRequired: true);
                applicationCommandProperties.Add(command04.Build());

                var command05 = new SlashCommandBuilder()
                    .WithName("create-ticket")
                    .WithDescription("Creates a new ticket.")
                    .AddOption("project-id", ApplicationCommandOptionType.String, "The project of the ticket you want to create", isRequired: true)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("type")
                        .WithDescription("The type of ticket you want to create")
                        .WithRequired(true)
                        .AddChoice(ETicketType.Bug.ToString(), ETicketType.Bug.ToString())
                        .AddChoice(ETicketType.Request.ToString(), ETicketType.Request.ToString())
                        .WithType(ApplicationCommandOptionType.String)
                    );
                applicationCommandProperties.Add(command05.Build());

                var command06 = new SlashCommandBuilder()
                    .WithName("update-ticket")
                    .WithDescription("Updates a ticket.")
                    .AddOption("ticket", ApplicationCommandOptionType.String, "The ticket id you wish to update", isRequired: true)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("status")
                        .WithDescription("The status you wish to update the ticket to")
                        .WithRequired(true)
                        .AddChoice(ETicketStatus.Open.ToString(), ETicketStatus.Open.ToString())
                        .AddChoice(ETicketStatus.In_Progress.ToString().Replace("_", " "), ETicketStatus.In_Progress.ToString().Replace("_", ""))
                        .AddChoice(ETicketStatus.Done.ToString(), ETicketStatus.Done.ToString())
                        .AddChoice(ETicketStatus.Declined.ToString(), ETicketStatus.Declined.ToString())
                        .WithType(ApplicationCommandOptionType.String)
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("priority")
                        .WithDescription("The priority you wish to update the ticket to")
                        .WithRequired(true)
                        .AddChoice(ETicketPriority.Trivial.ToString(), ETicketPriority.Trivial.ToString())
                        .AddChoice(ETicketPriority.Minor.ToString(), ETicketPriority.Minor.ToString())
                        .AddChoice(ETicketPriority.Lowest.ToString(), ETicketPriority.Lowest.ToString())
                        .AddChoice(ETicketPriority.Low.ToString(), ETicketPriority.Low.ToString())
                        .AddChoice(ETicketPriority.Medium.ToString(), ETicketPriority.Medium.ToString())
                        .AddChoice(ETicketPriority.High.ToString(), ETicketPriority.High.ToString())
                        .AddChoice(ETicketPriority.Highest.ToString(), ETicketPriority.Highest.ToString())
                        .AddChoice(ETicketPriority.Major.ToString(), ETicketPriority.Major.ToString())
                        .AddChoice(ETicketPriority.Critical.ToString(), ETicketPriority.Critical.ToString())
                        .AddChoice(ETicketPriority.Blocker.ToString(), ETicketPriority.Blocker.ToString())
                        .WithType(ApplicationCommandOptionType.String)
                    );
                applicationCommandProperties.Add(command06.Build());

                var command07 = new SlashCommandBuilder()
                    .WithName("force-unwatch")
                    .WithDescription("Forces a ticket unwatch. Only recommended if normal select menu does not work!")
                    .AddOption("ticket", ApplicationCommandOptionType.String, "The ticket id you wish to unwatch", isRequired: true);
                applicationCommandProperties.Add(command07.Build());

                await client.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties.ToArray());
                logger.Info("Successfully updated all commands");

                // With global commands we don't need the guild.
                // await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                logger.Info(json);
            }
        }

        public static async Task HandleCommandRules(SocketSlashCommand command, ECommandRules rule)
        {
            switch (rule)
            {
                case ECommandRules.NO_DM:
                    if (command.Channel.GetChannelType() == ChannelType.DM)
                    {
                        throw new RuleException(ECommandRules.NO_DM);
                    }
                    break;
                case ECommandRules.REQUIRES_INITIALIZE:
                    if (!SupportService.HasProject((ulong)command.GuildId))
                    {
                        throw new RuleException(ECommandRules.REQUIRES_INITIALIZE);
                    }
                    break;
            }
        }

        public static async Task HandleCommand(SocketSlashCommand command)
        {
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            logger.Info($"User executed {command.Data.Name}");
            try
            {
                switch (command.Data.Name)
                {
                    case "create-project":
                        await HandleCommandRules(command, ECommandRules.NO_DM);

                        await HandleCreateProjectCommand(command);
                        break;
                    case "delete-project":
                        await HandleCommandRules(command, ECommandRules.NO_DM);

                        await HandleDeleteProjectCommand(command);
                        break;
                    case "sync":
                        await HandleCommandRules(command, ECommandRules.NO_DM);

                        await HandleSynchronizeProjectCommand(command);
                        break;
                    case "unsync":
                        await HandleCommandRules(command, ECommandRules.NO_DM);

                        await HandleUnsynchronizeProjectCommand(command);
                        break;
                    case "create-ticket":
                        await HandleCommandRules(command, ECommandRules.NO_DM);

                        await HandleCreateTicketCommand(command);
                        break;
                    case "update-ticket":
                        await HandleCommandRules(command, ECommandRules.NO_DM);

                        await HandleUpdateTicketCommand(command);
                        break;
                    case "force-unwatch":
                        await HandleForceUnwatchCommand(command);
                        break;
                }
            }
            catch (RuleException ex)
            {
                await command.RespondAsync(ex.ToString(), ephemeral: true);
            }

        }

        private static async Task HandleCreateProjectCommand(SocketSlashCommand command)
        => await SupportService.CreateProjectCommand(command);

        private static async Task HandleDeleteProjectCommand(SocketSlashCommand command)
        => await SupportService.DeleteProjectCommand(command);

        private static async Task HandleSynchronizeProjectCommand(SocketSlashCommand command)
        => await SupportService.SynchronizeProjectCommand(command);

        private static async Task HandleUnsynchronizeProjectCommand(SocketSlashCommand command)
        => await SupportService.UnsynchronizeProjectCommand(command);

        private static async Task HandleCreateTicketCommand(SocketSlashCommand command)
        => await SupportService.CreateTicketCommand(command);

        private static async Task HandleUpdateTicketCommand(SocketSlashCommand command)
        => await SupportService.UpdateTicketCommand(command);

        private static async Task HandleForceUnwatchCommand(SocketSlashCommand command)
        => await SupportService.ForceUnwatchCommand(command);

    }
}
