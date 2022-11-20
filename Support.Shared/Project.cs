namespace Support.Shared
{
    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Ticket> Tickets { get; set; }

        public Project() { }

        public Project(string Name)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = Name;
            this.Tickets = new List<Ticket>();
        }

        public Project(string Id, string Name, List<Ticket> Tickets)
        {
            this.Id = Id;
            this.Name = Name;
            this.Tickets = Tickets;
        }
    }
}
