using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLog;
using Support.Discord.Enums;
using Support.Discord.Exceptions;
using Support.Discord.Models;
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
            #region Load commands.json
            List<DiscordCommand> commands = 
                JsonConvert.DeserializeObject<List<DiscordCommand>>(File.ReadAllText("commands.json")) 
                ?? new List<DiscordCommand>();
            #endregion

            #region Construct Commands
            ApplicationCommandProperties[] applicationCommandProperties = commands.Select(command =>
            {
                logger.Info($"Loading command {command.Name.LocalizedDefault()}...");
                try
                {
                    SlashCommandBuilder commandBuilder = new SlashCommandBuilder();
                    commandBuilder.WithName(command.Name.LocalizedDefault());
                    commandBuilder.WithNameLocalizations(command.Name.Localization);
                    commandBuilder.WithDescription(command.Description.LocalizedDefault());
                    commandBuilder.WithDescriptionLocalizations(command.Description.Localization);
                    command.Options.ForEach(option =>
                    {
                        SlashCommandOptionBuilder optionBuilder = new SlashCommandOptionBuilder();
                        optionBuilder.WithName(option.Name.LocalizedDefault());
                        optionBuilder.WithNameLocalizations(option.Name.Localization);
                        optionBuilder.WithDescription(option.Description.LocalizedDefault());
                        optionBuilder.WithDescriptionLocalizations(option.Description.Localization);
                        optionBuilder.WithRequired(option.Required);
                        optionBuilder.WithType(option.OptionType());
                        option.Choices.ForEach(choice =>
                        {
                            optionBuilder.AddChoice(
                                name: choice.Name.LocalizedDefault(),
                                value: choice.Value,
                                nameLocalizations: choice.Name.Localization);
                        });
                        commandBuilder.AddOption(optionBuilder);
                    });
                    logger.Info($"...Successful!");
                    return commandBuilder.Build();
                } 
                catch(Exception)
                {
                    logger.Error($"...Failed!");
                    throw;
                }
            }).Where(property => property != null).ToArray();
            #endregion

            #region Send Commands to Discord
            logger.Info($"Sending commands to Discord...");
            try
            {
                await client.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties);
                logger.Info("...Successful!");
            }
            catch (Exception)
            {
                logger.Error($"...Failed!");
                throw;
            }
            #endregion
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
