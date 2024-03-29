﻿using Microsoft.AspNetCore.SignalR;
using Support.Shared;
using Support.Shared.Enums;
using System.Net.Sockets;

namespace Support.Server.Hubs;

internal class SupportHub : Hub
{
    private readonly ILogger logger;
    private static readonly Dictionary<string, Project> projects = new Dictionary<string, Project>();
    private static readonly List<Ticket> tickets = new List<Ticket>();
    private static readonly List<User> users = new List<User>();

    public SupportHub(ILogger<SupportHub> logger)
    {
        this.logger = logger;
    }

    public async Task SessionConnected(Session session)
    {
        // Add the session to the group name.
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);
        logger.LogInformation($"{session.Name} connected as {session.GroupName}");

        // await Clients.OthersInGroup(session.GroupName).SendAsync(ServerBroadcasts.SessionConnected, session);
    }

    /// <summary>
    /// Receives a project create request from client.
    /// Will create a new project with the given information of the request.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="projectCreate"></param>
    /// <returns>The created project</returns>
    public async Task CreateProject(Session session, ProjectCreateRequest projectCreate)
    {
        Project project = new Project(projectCreate.Name, projectCreate.Context.Agent);
        projects.Add(project.Id, project);
        logger.LogInformation($"{session.Name} created a new Project '{project.Name}' ({project.Id}) for User {project.Owner}");

        await Clients.Caller.SendAsync(ServerBroadcasts.SendProject, projectCreate.Context.Id, project);
    }

    /// <summary>
    /// Receives a project delete request from client.
    /// Will delete a existing project with the given information of the request.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="projectDelete"></param>
    /// <returns>The created project</returns>
    public async Task DeleteProject(Session session, ProjectDeleteRequest projectDelete)
    {
        Project project = projects[projectDelete.ProjectId];
        projects.Remove(project.Id);
        logger.LogInformation($"{session.Name} deleted a Project '{project.Name}' ({project.Id}) of User {project.Owner}");

        await Clients.Caller.SendAsync(ServerBroadcasts.SendProject, projectDelete.Context.Id, project);
    }

    public async Task RetrieveProject(Session session, ProjectRetrieveRequest projectRetrieve)
    {
        Project project = projects[projectRetrieve.ProjectId];
        logger.LogInformation($"{session.Name} retrieve a Project '{project.Name}' ({project.Id})");

        await Clients.Caller.SendAsync(ServerBroadcasts.SendProject, projectRetrieve.Context.Id, project);
    }

    /// <summary>
    /// Receives a ticket create request from client.
    /// Will create a new ticket with the informations contained in the ticket create request.
    /// Created ticket will be added to corresponding project.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="ticketCreate"></param>
    /// <returns>The created ticket</returns>
    public async Task TicketCreate(Session session, TicketCreateRequest ticketCreate)
    {
        logger.LogInformation($"Received new ticket from {session.Name}");

        Project project = projects[ticketCreate.ProjectId];
        Ticket ticket = new Ticket(ticketCreate);
        project.Tickets.Add(ticket);
        tickets.Add(ticket);

        logger.LogInformation($"Sending new ticket ({ticket.Id}) to all sessions in group '{SessionGroups.Listener}'");
        await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicket, ticket);
    }

    /// <summary>
    /// Receives Ticket Update request from client.
    /// Will try to update priority, status and last updated value of the ticket.
    /// If ticket is not found, will send a Server Error message to the caller.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="ticketUpdate"></param>
    /// <returns>The updated ticket</returns>
    public async Task TicketUpdate(Session session, TicketUpdateRequest ticketUpdate)
    {
        logger.LogInformation($"Received updated ticket ({ticketUpdate.Id}) from {session.Name}");

        try
        {
            Ticket ticket;
            ticket = tickets.First(x => x.Id == ticketUpdate.Id);

            ticket.Priority = ticketUpdate.Priority;
            ticket.Status = ticketUpdate.Status;
            ticket.LastUpdatedAt = DateTimeOffset.Now;

            logger.LogInformation($"Sending updated ticket ({ticket.Id}) to all sessions in group '{SessionGroups.Listener}'");
            await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicket, ticket);
        }
        catch (InvalidOperationException e)
        {
            var error = new ServerError(EServerError.TICKET_ID_NOT_FOUND, $"Couldn't find any ticket with the id {ticketUpdate.Id}");
            logger.LogError(error.Message);
            await Clients.Caller.SendAsync(ServerBroadcasts.SendServerError, error);
            return;
        }

    }
}