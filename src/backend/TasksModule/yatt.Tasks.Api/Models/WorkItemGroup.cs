namespace yatt.Tasks.Api.Models
{
    public class WorkItemGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public ICollection<User> CoWorkers { get; set; } = new List<User>();
        public ICollection<WorkItem> WorkItems { get; set; }
    }
}