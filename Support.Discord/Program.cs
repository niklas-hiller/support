using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLog.Config;
using NLog;
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

            var guildCommand = new SlashCommandBuilder()
                .WithName("list-roles") // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
                .WithDescription("Lists all roles of a user.")  // Descriptions can have a max length of 100.
                .AddOption("user", ApplicationCommandOptionType.User, "The users whos roles you want to be listed", isRequired: true);

            var guildCommand2 = new SlashCommandBuilder()
                .WithName("initiate") // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
                .WithDescription("Initiates the support channel if it does not exist yet.");  // Descriptions can have a max length of 100.

            var guildCommand3 = new SlashCommandBuilder()
                .WithName("report-bug") // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
                .WithDescription("Creates a bug ticket.");  // Descriptions can have a max length of 100.

            var guildCommand4 = new SlashCommandBuilder()
                .WithName("report-request") // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
                .WithDescription("Creates a request ticket.");  // Descriptions can have a max length of 100.

            var guildCommand5 = new SlashCommandBuilder()
                .WithName("update-ticket") // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
                .WithDescription("Updates a ticket.")  // Descriptions can have a max length of 100. 
                .AddOption("ticket", ApplicationCommandOptionType.String, "The ticket id you wish to update", isRequired: true)
                .AddOption("status", ApplicationCommandOptionType.String, "The status you wish to update the ticket to", isRequired: true)
                .AddOption("priority", ApplicationCommandOptionType.String, "The priority you wish to update the ticket to", isRequired: true);

            // Let's do our global command
            //var globalCommand = new SlashCommandBuilder();
            //globalCommand.WithName("first-global-command");
            //globalCommand.WithDescription("This is my first global slash command");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await client.Rest.CreateGuildCommand(guildCommand.Build(), guildId);
                await client.Rest.CreateGuildCommand(guildCommand2.Build(), guildId);
                await client.Rest.CreateGuildCommand(guildCommand3.Build(), guildId);
                await client.Rest.CreateGuildCommand(guildCommand4.Build(), guildId);
                await client.Rest.CreateGuildCommand(guildCommand5.Build(), guildId);

                // With global commands we don't need the guild.
                //await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                logger.Info(json);
            }

            // await HandleRegularUpdateEvent();

        }

        //private async Task HandleRegularUpdateEvent()
        //{
        //    DateTime nextUpdate = GetNextWeekday(DateTime.Today, DayOfWeek.Tuesday);

        //    const ulong guildId = OfficialGuildId;

        //    var guild = client.GetGuild(guildId);

        //    var guildEvents = await guild.GetEventsAsync();

        //    foreach (var guildEvent in guildEvents)
        //    {
        //        if (guildEvent.Name == "Release 0.1.0") return;
        //    }

        //    var newGuildEvent = await guild.CreateEventAsync(
        //        name: "Release 0.1.0",
        //        startTime: nextUpdate,
        //        endTime: nextUpdate.AddDays(1).AddTicks(-1),
        //        type: GuildScheduledEventType.External,
        //        description: "Regular Update to 0.1.0",
        //        location: "Official Server",
        //        coverImage: new Image("images/cover.png"));
        //}

        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            switch (command.Data.Name)
            {
                case "list-roles":
                    await HandleListRoleCommand(command);
                    break;
                case "initiate":
                    await HandleInitiateCommand(command);
                    break;
                case "report-bug":
                    await HandleBugCommand(command);
                    break;
                case "report-request":
                    await HandleRequestCommand(command);
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
            await command.RespondAsync("Done");
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
                .AddTextInput("Name", "name", placeholder: "Please a short meaningful name for the ticket.", required: true)
                .AddTextInput("Description", "description", placeholder: "Please enter a description to the ticket.", style: TextInputStyle.Paragraph, required: true);

            await command.RespondWithModalAsync(modal.Build());
        }

        private async Task HandleRequestCommand(SocketSlashCommand command)
        {
            var modal = new ModalBuilder()
                .WithTitle("Create Ticket (Request)")
                .WithCustomId("request-modal")
                .AddTextInput("Name", "name", placeholder: "Please a short meaningful name for the ticket.", required: true)
                .AddTextInput("Description", "description", placeholder: "Please enter a description to the ticket.", style: TextInputStyle.Paragraph, required: true);

            await command.RespondWithModalAsync(modal.Build());
        }

        private async Task HandleListRoleCommand(SocketSlashCommand command)
        {
            // We need to extract the user parameter from the command. since we only have one option and it's required, we can just use the first option.
            var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

            // We remove the everyone role and select the mention of each role.
            var roleList = string.Join(",\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

            var embedBuiler = new EmbedBuilder()
                .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
                .WithTitle("Roles")
                .WithDescription(roleList)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            // Now, Let's respond with the embed.
            await command.RespondAsync(embed: embedBuiler.Build());
        }
    }
}