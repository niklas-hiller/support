namespace Support.Shared
{
    public class ProjectDeleteRequest
    {
        public RequestContext Context { get; set; }
        public string ProjectId { get; set; }

        public ProjectDeleteRequest() { }

        public ProjectDeleteRequest(string id, RequestContext context)
        {
            this.Context = context;
            this.ProjectId = id;
        }
    }
}
