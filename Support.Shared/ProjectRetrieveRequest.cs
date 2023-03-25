namespace Support.Shared
{
    public class ProjectRetrieveRequest
    {
        public RequestContext Context { get; set; }
        public string ProjectId { get; set; }

        public ProjectRetrieveRequest() { }

        public ProjectRetrieveRequest(string id, RequestContext context)
        {
            this.Context = context;
            this.ProjectId = id;
        }
    }
}
