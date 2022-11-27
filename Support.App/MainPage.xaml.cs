using Support.Shared;
using Support.Shared.Enums;

namespace Support.App
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        List<string> status = TicketStatus.AsList();
        List<string> priority = TicketPriority.AsList();
        List<string> types = TicketType.AsList();

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            Ticket ticket = new Ticket();
            ticket.Id = Guid.NewGuid().ToString();
            ticket.ProjectId = Guid.NewGuid().ToString();
            ticket.Title = "Q doesn't work";
            ticket.Author = "Niklas#0786";
            ticket.CreatedAt = DateTimeOffset.Now;
            ticket.LastUpdatedAt = DateTimeOffset.Now;
            ticket.Status = ETicketStatus.Open;
            ticket.Priority = ETicketPriority.Minor;
            ticket.Type = ETicketType.Bug;
            ticket.CustomFields = new Dictionary<string, string>();
            ticket.CustomFields.Add("Environment", "Version 0.21.5");
            ticket.CustomFields.Add("Steps to reproduce", "- Select Hero\r\n- Skill Q Ability\r\n- Use Q Ability");
            ticket.CustomFields.Add("Current Behaviour", "Casts W Ability");
            ticket.CustomFields.Add("Expected Behaviour", "Casts Q Ability");

            TicketIdProjectImage.Source = "project.png";
            TicketId.Text = ticket.Id;

            TicketNameTypeImage.Source = $"{TicketType.AsString(ticket.Type).ToLower()}.png";
            TicketName.Text = ticket.Title;
            
            TicketStatusPicker.ItemsSource = status;
            TicketStatusPicker.SelectedItem = TicketStatus.AsString(ticket.Status);

            TicketProjectPicker.ItemsSource = new List<string>() { ticket.ProjectId };
            TicketProjectPicker.SelectedItem = ticket.ProjectId;
            TicketProjectImage.Source = "project.png";

            TicketTypePicker.ItemsSource = types;
            TicketTypePicker.SelectedItem = TicketType.AsString(ticket.Type);
            TicketTypeImage.Source = $"{TicketType.AsString(ticket.Type).ToLower()}.png";

            TicketPriorityPicker.ItemsSource = priority;
            TicketPriorityPicker.SelectedItem = TicketPriority.AsString(ticket.Priority);
            TicketPriorityImage.Source = $"{TicketPriority.AsString(ticket.Priority).ToLower()}.png";

            TicketAuthorLabel.Text = ticket.Author;
            TicketAuthorImage.Source = "user.png";

            TicketFields.ItemsSource = ticket.CustomFields;

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}