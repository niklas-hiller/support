namespace Support.Shared
{
    public class ProjectCreateRequest
    {
        public string RequestId { get; set; }
        public string Name { get; set; }

        public ProjectCreateRequest() { }

        public ProjectCreateRequest(string name)
        {
            this.RequestId = Guid.NewGuid().ToString();
            this.Name = name;
        }
    }
}
