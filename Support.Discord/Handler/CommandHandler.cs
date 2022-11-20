using Discord;
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
    public static class CommandHandler
    {
        private static readonly DiscordSocketClient client = Program.client;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task InitializeCommands()
        {
            List<ApplicationCommandProperties> applicationCommandProperties = new();
            try
            {
                var command0 = new SlashCommandBuilder()
                    .WithName("initiate")
                    .WithDescription("Initiates the project if it does not exist yet.")
                    .AddOption("project-name", ApplicationCommandOptionType.String, "The name of the project you initiate", isRequired: true);
                applicationCommandProperties.Add(command0.Build());

                var command1 = new SlashCommandBuilder()
                    .WithName("support-channel")
                    .WithDescription("Initiates the support channel if it does not exist yet.")
                    .AddOption(
                        name: "channel",
                        type: ApplicationCommandOptionType.Channel,
                        description: "The channel which should be used to display live tickets",
                        isRequired: true,
                        channelTypes: new List<ChannelType>() { ChannelType.Text }
                    );
                applicationCommandProperties.Add(command1.Build());

                var command2 = new SlashCommandBuilder()
                    .WithName("create-ticket")
                    .WithDescription("Creates a new ticket.")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("type")
                        .WithDescription("The type of ticket you want to create")
                        .WithRequired(true)
                        .AddChoice(ETicketType.Bug.ToString(), ETicketType.Bug.ToString())
                        .AddChoice(ETicketType.Request.ToString(), ETicketType.Request.ToString())
                        .WithType(ApplicationCommandOptionType.String)
                    );
                applicationCommandProperties.Add(command2.Build());

                var command3 = new SlashCommandBuilder()
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
                applicationCommandProperties.Add(command3.Build());

                var command4 = new SlashCommandBuilder()
                    .WithName("force-unwatch")
                    .WithDescription("Forces a ticket unwatch. Only recommended if normal select menu does not work!")
                    .AddOption("ticket", ApplicationCommandOptionType.String, "The ticket id you wish to unwatch", isRequired: true);
                applicationCommandProperties.Add(command4.Build());

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
                case ECommandRules.REQUIRES_SUPPORT_CHANNEL:
                    if (!SupportService.HasSupportChannel((ulong)command.GuildId))
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
                    case "initiate":
                        await HandleCommandRules(command, ECommandRules.NO_DM);

                        await HandleInitiateCommand(command);
                        break;
                    case "support-channel":
                        await HandleCommandRules(command, ECommandRules.NO_DM);
                        await HandleCommandRules(command, ECommandRules.REQUIRES_INITIALIZE);

                        await HandleSupportChannelCommand(command);
                        break;
                    case "create-ticket":
                        await HandleCommandRules(command, ECommandRules.NO_DM);
                        await HandleCommandRules(command, ECommandRules.REQUIRES_SUPPORT_CHANNEL);

                        ETicketType type = TicketType.FromString(
                            HelperService.GetDataObjectFromSlashCommand(command, "type").ToString());
                        switch (type)
                        {
                            case ETicketType.Bug:
                                await HandleBugCommand(command);
                                break;
                            case ETicketType.Request:
                                await HandleRequestCommand(command);
                                break;
                        }
                        break;
                    case "update-ticket":
                        await HandleCommandRules(command, ECommandRules.NO_DM);
                        await HandleCommandRules(command, ECommandRules.REQUIRES_SUPPORT_CHANNEL);

                        await HandleUpdateCommand(command);
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

        private static async Task HandleInitiateCommand(SocketSlashCommand command)
        {
            await SupportService.CreateProject(command);
        }

        private static async Task HandleSupportChannelCommand(SocketSlashCommand command)
        {
            await SupportService.RegisterSupportChannel(command);
        }

        private static async Task HandleUpdateCommand(SocketSlashCommand command)
        {
            var statusStr = HelperService.GetDataObjectFromSlashCommand(command, "status").ToString() ?? "";
            var status = TicketStatus.FromString(statusStr);

            var priorityStr = HelperService.GetDataObjectFromSlashCommand(command, "priority").ToString() ?? "";
            var priority = TicketPriority.FromString(priorityStr);

            var ticketId = HelperService.GetDataObjectFromSlashCommand(command, "ticket").ToString() ?? "0";

            try
            {
                await SupportService.InitiateTransmitUpdateTicket(ticketId, status, priority);
                await command.RespondAsync($"Successfully updated Ticket {ticketId}", ephemeral: true);
            }
            catch (KeyNotFoundException e)
            {
                await command.RespondAsync($"Couldn't updated Ticket {ticketId}. (Ticket Id does not exist)", ephemeral: true);
            }
        }

        private static async Task HandleBugCommand(SocketSlashCommand command)
        {
            var modal = new ModalBuilder()
                .WithTitle("Create Ticket (Bug)")
                .WithCustomId("bug-modal")
                .AddTextInput("Name", "name", placeholder: "Please enter a short meaningful name for the ticket.", required: true)
                .AddTextInput("Environment", "environment", placeholder: "The environment information during the bug occurence, i.e. Software Version.", style: TextInputStyle.Paragraph, required: true)
                .AddTextInput("Steps to reproduce", "steps", placeholder: "Steps to reproduce written down as bullet points.", style: TextInputStyle.Paragraph, required: true)
                .AddTextInput("Current Behaviour", "currentBehaviour", placeholder: "The behaviour that currently occurs.", style: TextInputStyle.Paragraph, required: true)
                .AddTextInput("Expected Behaviour", "expectedBehaviour", placeholder: "The behaviour you would expect to occur.", style: TextInputStyle.Paragraph, required: true);

            await command.RespondWithModalAsync(modal.Build());
        }

        private static async Task HandleRequestCommand(SocketSlashCommand command)
        {
            var modal = new ModalBuilder()
                .WithTitle("Create Ticket (Request)")
                .WithCustomId("request-modal")
                .AddTextInput("Name", "name", placeholder: "Please enter a short meaningful name for the ticket.", required: true)
                .AddTextInput("Description", "description", placeholder: "Please enter a description to the ticket.", style: TextInputStyle.Paragraph, required: true);

            await command.RespondWithModalAsync(modal.Build());
        }

        private static async Task HandleForceUnwatchCommand(SocketSlashCommand command)
        {
            string ticketId = HelperService.GetDataObjectFromSlashCommand(command, "ticket").ToString() ?? "0";
            try
            {
                bool success = SupportService.SetWatchTicket(ticketId, command.User.Id, false);
                if (success)
                {
                    await command.RespondAsync(
                        $"Successfully force removed ticket {ticketId} from your watchlist.\n" +
                        "Please keep in mind that the select menu of that ticket will still show 'Watching'.", ephemeral: true);
                }
                else
                {
                    await command.RespondAsync(
                        $"Failed to force removed ticket {ticketId} from your watchlist.\n" +
                        "Reason: You are not watching that ticket. Are you sure you entered the right id?", ephemeral: true);
                }

            }
            catch (KeyNotFoundException e)
            {
                await command.RespondAsync(
                    $"Failed to force removed ticket {ticketId} from your watchlist.\n" +
                    "Reason: A ticket with the given Id does not exist. Maybe the ticket does not exist anymore?", ephemeral: true);
            }

        }
    }
}
