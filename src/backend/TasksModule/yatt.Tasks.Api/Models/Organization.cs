namespace yatt.Tasks.Api.Models
{
    public class Organization
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<User> Members { get; set; } = new List<User>();
        public ICollection<WorkItemGroup> WorkItemGroups { get; set; } = new List<WorkItemGroup>();
    }
}