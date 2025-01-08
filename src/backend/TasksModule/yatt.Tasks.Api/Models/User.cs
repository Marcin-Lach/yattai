namespace yatt.Tasks.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<WorkItemGroup> WorkItemGroups { get; set; } = new List<WorkItemGroup>();
        public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
    }
}