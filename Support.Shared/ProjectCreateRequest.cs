namespace Support.Shared
{
    public class ProjectCreateRequest
    {
        public RequestContext Context { get; set; }
        public string Name { get; set; }

        public ProjectCreateRequest() { }

        public ProjectCreateRequest(string name, RequestContext context)
        {
            this.Context = context;
            this.Name = name;
        }
    }
}
