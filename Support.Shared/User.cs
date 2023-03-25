namespace Support.Shared
{
    public class User
    {
        public string Id { get; set; }
        public List<Project> Projects { get; set; } = new List<Project>();

        public User()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
