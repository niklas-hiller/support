using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using Support.Discord.Models;
using Support.Shared;
using NLog;

namespace Support.Discord.Services
{
    [Obsolete]
    internal static class UpdateService
    {
        private static readonly DiscordSocketClient client = Program.client;
        private static HubConnection hubConnection;
        private static readonly Session session = new Session() { GroupName = SessionGroups.Listener, Name = "Discord" };
        private static readonly BotConfiguration configuration = Program.configuration;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task ConnectHub()
        {
            if (hubConnection == null)
            {
                hubConnection = new HubConnectionBuilder()
                    .WithUrl("https://localhost:7290/Update")
                    .Build();

                hubConnection.On<UpdateEvent>(ServerBroadcasts.SendUpdateEvent, async updateEvent =>
                {
                    try
                    {
                        await ReceiveUpdateEvent(updateEvent);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Failed to receive update event");
                        logger.Error(ex);
                    }
                });

                await hubConnection.StartAsync();

                await hubConnection.SendAsync(
                    ServerBroadcasts.SessionConnected,
                    session
                );
            }
        }

        public static async Task CreateUpdateEvent(SocketModal modal, EUpdateEventType type)
        {
            List<SocketMessageComponentData> components =
                modal.Data.Components.ToList();

            string version = components
                .First(x => x.CustomId == "version").Value;
            string description = components
                .First(x => x.CustomId == "description").Value;
            DateTime releaseDate = DateTime.Now.AddDays(1);

            UpdateEvent updateEvent = new UpdateEvent(
                Type: type, Version: version, Description: description, ReleaseDate: releaseDate);

            await TransmitUpdateEvent(updateEvent);
            await modal.RespondAsync($"Successfully submitted your update event.", ephemeral: true);
        }

        private static async Task TransmitUpdateEvent(UpdateEvent updateEvent)
        {
            // Send ticket to hub
            await hubConnection.SendAsync(
                ServerBroadcasts.SendUpdateEvent,
                session,
                updateEvent
            );
        }

        private static async Task ReceiveUpdateEvent(UpdateEvent updateEvent)
        {
            string? updateType = null;
            switch (updateEvent.Type)
            {
                case EUpdateEventType.Hotfix:
                    updateType = "Hotfix";
                    break;
                case EUpdateEventType.Release:
                    updateType = "Release";
                    break;
            }
            if (updateType == null) return;
            var eventName = $"{updateType} {updateEvent.Version}";
            var eventDescription = updateEvent.Description == "" ? $"{updateType} Update to {updateEvent.Version}" : updateEvent.Description;

            var guild = client.GetGuild(configuration.RootGuildId);
            var guildEvents = await guild.GetEventsAsync();
            foreach (var guildEvent in guildEvents)
            {
                if (guildEvent.Name == eventName) return;
            }

            await guild.CreateEventAsync(
                name: eventName,
                startTime: updateEvent.ReleaseDate,
                endTime: updateEvent.ReleaseDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59),
                type: GuildScheduledEventType.External,
                description: eventDescription,
                location: "Official Server",
                coverImage: new Image("Images/Event.png"));
        }
    }
}
