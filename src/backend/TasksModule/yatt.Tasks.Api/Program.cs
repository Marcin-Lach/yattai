using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

var app = builder.Build();

// Task Group Endpoints
app.MapPost("/task-groups", async (AppDbContext dbContext, TaskGroup taskGroup) =>
{
    dbContext.TaskGroups.Add(taskGroup);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/task-groups/{taskGroup.Id}", taskGroup);
})
.WithOpenApi();

app.MapGet("/task-groups", async (AppDbContext dbContext) =>
{
    var taskGroups = await dbContext.TaskGroups
        .Select(g => new
        {
            g.Id,
            g.Name,
            TaskCount = g.Tasks.Count,
        })
        .ToListAsync();
    return Results.Ok(taskGroups);
})
.WithOpenApi();

app.MapGet("/task-groups/{id}", async (AppDbContext dbContext, int id) =>
{
    var taskGroup = await dbContext.TaskGroups
        .Include(g => g.Tasks)
        .FirstOrDefaultAsync(g => g.Id == id);

    if (taskGroup == null) return Results.NotFound();
    return Results.Ok(taskGroup);
})
.WithOpenApi();

app.MapPut("/task-groups/{id}", async (AppDbContext dbContext, int id, TaskGroup updatedGroup) =>
{
    var group = await dbContext.TaskGroups.FindAsync(id);
    if (group == null) return Results.NotFound();

    group.Name = updatedGroup.Name;
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapDelete("/task-groups/{id}", async (AppDbContext dbContext, int id) =>
{
    var group = await dbContext.TaskGroups
        .Include(g => g.Tasks)
        .FirstOrDefaultAsync(g => g.Id == id);

    if (group == null) return Results.NotFound();

    group.IsDeleted = true;
    foreach (var task in group.Tasks)
    {
        task.IsDeleted = true;
    }

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

// Task Endpoints
app.MapPost("/tasks", async (AppDbContext dbContext, Task task) =>
{
    dbContext.Tasks.Add(task);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/tasks/{task.Id}", task);
})
.WithOpenApi();

app.MapGet("/task-groups/{groupId}/tasks", async (AppDbContext dbContext, int groupId, int page = 1, int pageSize = 10, string? sortBy = null, string sortOrder = "asc") =>
{
    var query = dbContext.Tasks.Where(t => t.TaskGroupId == groupId);

    if (!string.IsNullOrEmpty(sortBy))
    {
        query = sortOrder.ToLower() switch
        {
            "desc" => query.OrderByDescending(t => EF.Property<object>(t, sortBy)),
            _ => query.OrderBy(t => EF.Property<object>(t, sortBy)),
        };
    }

    var totalTasks = await query.CountAsync();
    var tasks = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return Results.Ok(new { totalTasks, tasks });
})
.WithOpenApi();

app.MapGet("/tasks/{id}", async (AppDbContext dbContext, int id) =>
{
    var task = await dbContext.Tasks.FindAsync(id);
    if (task == null) return Results.NotFound();
    return Results.Ok(task);
})
.WithOpenApi();

app.MapPut("/tasks/{id}", async (AppDbContext dbContext, int id, Task updatedTask) =>
{
    var task = await dbContext.Tasks.FindAsync(id);
    if (task == null) return Results.NotFound();

    task.Title = updatedTask.Title;
    task.State = updatedTask.State;
    task.AssigneeId = updatedTask.AssigneeId;
    task.CustomProperties = updatedTask.CustomProperties;

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapDelete("/tasks/{id}", async (AppDbContext dbContext, int id) =>
{
    var task = await dbContext.Tasks.FindAsync(id);
    if (task == null) return Results.NotFound();

    task.IsDeleted = true;
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapPut("/tasks/{taskId}/assign/{userId}", async (AppDbContext dbContext, int taskId, int userId) =>
{
    var task = await dbContext.Tasks.FindAsync(taskId);
    if (task == null) return Results.NotFound();

    var user = await dbContext.Users.FindAsync(userId);
    if (user == null || !user.IsActive) return Results.BadRequest("Invalid or inactive user.");

    task.AssigneeId = userId;
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

// User Endpoints
app.MapPost("/users", async (AppDbContext dbContext, User user) =>
{
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
})
.WithOpenApi();

app.MapGet("/users", async (AppDbContext dbContext) =>
{
    var users = await dbContext.Users.Where(u => u.IsActive).ToListAsync();
    return Results.Ok(users);
})
.WithOpenApi();

app.MapGet("/users/{id}", async (AppDbContext dbContext, int id) =>
{
    var user = await dbContext.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    return Results.Ok(user);
})
.WithOpenApi();

app.MapPut("/users/{id}", async (AppDbContext dbContext, int id, User updatedUser) =>
{
    var user = await dbContext.Users.FindAsync(id);
    if (user == null) return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    user.IsActive = updatedUser.IsActive;

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapDelete("/users/{id}", async (AppDbContext dbContext, int id) =>
{
    var user = await dbContext.Users.FindAsync(id);
    if (user == null) return Results.NotFound();

    user.IsActive = false;
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapGet("/tasks/assignees", async (AppDbContext dbContext, int taskGroupId) =>
{
    var taskGroup = await dbContext.TaskGroups
        .Include(tg => tg.Organization)
        .Include(tg => tg.CoWorkers)
        .FirstOrDefaultAsync(tg => tg.Id == taskGroupId);

    if (taskGroup == null) return Results.NotFound();

    var possibleAssignees = await dbContext.Users
        .Where(u => u.IsActive && (taskGroup.CoWorkers.Contains(u) || taskGroup.Organization.Members.Contains(u)))
        .ToListAsync();

    return Results.Ok(possibleAssignees);
})
.WithOpenApi();

app.Run();

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskGroup> TaskGroups { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Organization> Organizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskGroup>().HasQueryFilter(g => !g.IsDeleted);
        modelBuilder.Entity<Task>().HasQueryFilter(t => !t.IsDeleted);

        modelBuilder.Entity<TaskGroup>()
            .HasMany(tg => tg.CoWorkers)
            .WithMany(u => u.TaskGroups);

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Members)
            .WithMany(u => u.Organizations);
    }
}
public class TaskGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; }
    public ICollection<User> CoWorkers { get; set; } = new List<User>();
    public ICollection<Task> Tasks { get; set; }
}

public class Task
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string State { get; set; }
    public int? AssigneeId { get; set; }
    public int TaskGroupId { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string? CustomProperties { get; set; }
    public TaskGroup TaskGroup { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TaskGroup> TaskGroups { get; set; } = new List<TaskGroup>();
    public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
}

public class Organization
{
    public ICollection<User> Members { get; set; }
}