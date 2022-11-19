using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using Support.Discord.Models;
using Support.Discord.Services;
using Support.Shared;

namespace Support.Discord
{
    internal class Program
    {
        static Task Main(string[] args) => new Program().MainAsync();

        public static readonly DiscordSocketClient client = new DiscordSocketClient();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static readonly BotConfiguration configuration = JsonConvert.DeserializeObject<BotConfiguration>(File.ReadAllText("configuration.json"));

        public async Task MainAsync()
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");

            client.Log += Log;

            client.Ready += Client_Ready;

            client.SlashCommandExecuted += SlashCommandHandler;

            client.ButtonExecuted += ButtonHandler;

            client.ModalSubmitted += ModalHandler;

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<BotConfiguration>(File.ReadAllText("configuration.json")).Token;

            await client.LoginAsync(TokenType.Bot, configuration.Token);
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            logger.Info(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task Client_Ready()
        {
            await SupportService.ConnectHub();

            ulong guildId = configuration.RootGuildId;

            var guildCommand1 = new SlashCommandBuilder()
                .WithName("initiate") // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
                .WithDescription("Initiates the support channel if it does not exist yet.");  // Descriptions can have a max length of 100.

            var guildCommand2 = new SlashCommandBuilder()
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

            var guildCommand3 = new SlashCommandBuilder()
                .WithName("update-ticket") // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
                .WithDescription("Updates a ticket.")  // Descriptions can have a max length of 100. 
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
                    .WithType(ApplicationCommandOptionType.String)
                );

            // Let's do our global command
            // var globalCommand = new SlashCommandBuilder();
            // globalCommand.WithName("first-global-command");
            // globalCommand.WithDescription("This is my first global slash command");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await client.Rest.CreateGuildCommand(guildCommand1.Build(), guildId);
                await client.Rest.CreateGuildCommand(guildCommand2.Build(), guildId);
                await client.Rest.CreateGuildCommand(guildCommand3.Build(), guildId);

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

            // await HandleRegularUpdateEvent();

        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            logger.Info($"User executed {command.Data.Name}");
            switch (command.Data.Name)
            {
                case "initiate":
                    await HandleInitiateCommand(command);
                    break;
                case "create-ticket":
                    ETicketType type = TicketType.FromString(command.Data.Options.First(x => x.Name == "type").Value.ToString());
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
                    await HandleUpdateCommand(command);
                    break;
            }
        }

        public async Task HandleInitiateCommand(SocketSlashCommand command)
        {
            await SupportService.RegisterSupportChannel(command);
        }

        public async Task HandleUpdateCommand(SocketSlashCommand command)
        {
            var statusStr = command.Data.Options.First(x => x.Name == "status").Value.ToString() ?? "";
            var status = TicketStatus.FromString(statusStr);

            var priorityStr = command.Data.Options.First(x => x.Name == "priority").Value.ToString() ?? "";
            var priority = TicketPriority.FromString(priorityStr);

            var ticketId = command.Data.Options.First(x => x.Name == "ticket").Value.ToString() ?? "0";

            await SupportService.UpdateTicket(ticketId, status, priority);
            await command.RespondAsync($"Successfully updated Ticket {ticketId}", ephemeral: true);
        }

        public async Task ModalHandler(SocketModal modal)
        {
            switch (modal.Data.CustomId)
            {
                case "bug-modal":
                    await SupportService.CreateTicket(modal, ETicketType.Bug);
                    break;
                case "request-modal":
                    await SupportService.CreateTicket(modal, ETicketType.Request);
                    break;
            }
        }

        public async Task ButtonHandler(SocketMessageComponent component)
        {
            switch (component.Data.CustomId)
            {
                case "ticket-status-unknown":
                    await component.RespondAsync($"Ticket Status 'Unknown' is exception status when the server couldn't retrieve the status", ephemeral: true);
                    break;
                case "ticket-status-open":
                    await component.RespondAsync($"Ticket Status 'Open' means that the ticket was created, but is nobody is working on it.", ephemeral: true);
                    break;
                case "ticket-status-inprogress":
                    await component.RespondAsync($"Ticket Status 'In Progress' means that someone is working on the ticket.", ephemeral: true);
                    break;
                case "ticket-status-done":
                    await component.RespondAsync($"Ticket Status 'Done' means the content of the ticket was finished.", ephemeral: true);
                    break;
                case "ticket-status-declined":
                    await component.RespondAsync($"Ticket Status 'Declined' means the ticket will not be done.", ephemeral: true);
                    break;
                case "ticket-priority-unknown":
                    await component.RespondAsync($"Ticket Priority 'Unknown' means nobody assigned a priority to this ticket yet.", ephemeral: true);
                    break;
                case "ticket-priority-trivial":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-minor":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-lowest":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-low":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-medium":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-high":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-highest":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-major":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-critical":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-blocker":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
            }
        }

        private async Task HandleBugCommand(SocketSlashCommand command)
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

        private async Task HandleRequestCommand(SocketSlashCommand command)
        {
            var modal = new ModalBuilder()
                .WithTitle("Create Ticket (Request)")
                .WithCustomId("request-modal")
                .AddTextInput("Name", "name", placeholder: "Please enter a short meaningful name for the ticket.", required: true)
                .AddTextInput("Description", "description", placeholder: "Please enter a description to the ticket.", style: TextInputStyle.Paragraph, required: true);

            await command.RespondWithModalAsync(modal.Build());
        }
    }
}