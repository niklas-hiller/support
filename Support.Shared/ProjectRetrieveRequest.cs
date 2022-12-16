namespace Support.Shared
{
    public class ProjectRetrieveRequest
    {
        public string RequestId { get; set; }
        public string ProjectId { get; set; }

        public ProjectRetrieveRequest() { }

        public ProjectRetrieveRequest(string id)
        {
            this.RequestId = Guid.NewGuid().ToString();
            this.ProjectId = id;
        }
    }
}
