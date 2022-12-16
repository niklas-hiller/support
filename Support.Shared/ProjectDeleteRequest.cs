namespace Support.Shared
{
    public class ProjectDeleteRequest
    {
        public string RequestId { get; set; }
        public string ProjectId { get; set; }

        public ProjectDeleteRequest() { }

        public ProjectDeleteRequest(string id)
        {
            this.RequestId = Guid.NewGuid().ToString();
            this.ProjectId = id;
        }
    }
}
