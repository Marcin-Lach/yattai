namespace yatt.Tasks.Api.Models
{
    public class WorkItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string State { get; set; }
        public int? AssigneeId { get; set; }
        public int WorkItemGroupId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? CustomProperties { get; set; }
        public WorkItemGroup WorkItemGroup { get; set; }
    }
}