namespace Support.Shared
{
    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Ticket> Tickets { get; set; }
        public string Owner { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Project() { }

        public Project(string Name, string Owner)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = Name;
            this.Tickets = new List<Ticket>();
            this.Owner = Owner;
            this.CreatedAt = DateTimeOffset.Now;
        }

        public Project(string Id, string Name, List<Ticket> Tickets, string Owner, DateTimeOffset CreatedAt)
        {
            this.Id = Id;
            this.Name = Name;
            this.Tickets = Tickets;
            this.Owner = Owner;
            this.CreatedAt = CreatedAt;
        }
    }
}
