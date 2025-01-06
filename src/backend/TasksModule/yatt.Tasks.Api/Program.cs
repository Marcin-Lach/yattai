var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// In-memory database for demo purposes
var taskGroups = new List<TaskGroup>();
var tasks = new List<Task>();
var userPreferences = new Dictionary<string, UserPreferences>();
var users = new List<User>();

// Task Groups Endpoints
app.MapPost("/task-groups", (TaskGroup newGroup) =>
{
    newGroup.Id = Guid.NewGuid();
    taskGroups.Add(newGroup);
    return Results.Created($"/task-groups/{newGroup.Id}", newGroup);
});

app.MapGet("/task-groups", () =>
{
    return Results.Ok(taskGroups.Select(g => new
    {
        g.Id,
        g.Name,
        TaskCount = tasks.Count(t => t.GroupId == g.Id),
        g.LastUpdated
    }));
});

app.MapGet("/task-groups/{id}", (Guid id) =>
{
    var group = taskGroups.FirstOrDefault(g => g.Id == id);
    return group is not null ? Results.Ok(group) : Results.NotFound();
});

app.MapPut("/task-groups/{id}", (Guid id, TaskGroup updatedGroup) =>
{
    var group = taskGroups.FirstOrDefault(g => g.Id == id);
    if (group is null) return Results.NotFound();

    group.Name = updatedGroup.Name;
    group.LastUpdated = DateTime.UtcNow;
    return Results.Ok(group);
});

app.MapDelete("/task-groups/{id}", (Guid id) =>
{
    var group = taskGroups.FirstOrDefault(g => g.Id == id);
    if (group is null) return Results.NotFound();

    taskGroups.Remove(group);
    tasks.RemoveAll(t => t.GroupId == id);
    return Results.NoContent();
});

// Tasks Endpoints
app.MapPost("/tasks", (Task newTask) =>
{
    newTask.Id = Guid.NewGuid();
    newTask.CreatedAt = DateTime.UtcNow;
    tasks.Add(newTask);
    return Results.Created($"/tasks/{newTask.Id}", newTask);
});

app.MapGet("/task-groups/{groupId}/tasks", (Guid groupId, int page = 1, int pageSize = 20, string? sortBy = null, string? sortOrder = "asc") =>
{
    var groupTasks = tasks.Where(t => t.GroupId == groupId);

    if (!string.IsNullOrEmpty(sortBy))
    {
        groupTasks = sortBy switch
        {
            "CustomProperties.DueDate" => sortOrder == "desc" 
                ? groupTasks.OrderByDescending(t => t.CustomProperties["DueDate"]) 
                : groupTasks.OrderBy(t => t.CustomProperties["DueDate"]),
            "CustomProperties.Priority" => sortOrder == "desc" 
                ? groupTasks.OrderByDescending(t => t.CustomProperties["Priority"]) 
                : groupTasks.OrderBy(t => t.CustomProperties["Priority"]),
            _ => sortOrder == "desc"
                ? groupTasks.OrderByDescending(t => EF.Property<object>(t, sortBy))
                : groupTasks.OrderBy(t => EF.Property<object>(t, sortBy))
        };
    }

    var pagedTasks = groupTasks
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return Results.Ok(pagedTasks);
});

app.MapGet("/tasks/{id}", (Guid id) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

app.MapPut("/tasks/{id}", (Guid id, Task updatedTask) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) return Results.NotFound();

    task.Title = updatedTask.Title;
    task.State = updatedTask.State;
    task.AssigneeId = updatedTask.AssigneeId;
    task.CustomProperties = updatedTask.CustomProperties;
    task.LastUpdated = DateTime.UtcNow;
    return Results.Ok(task);
});

app.MapDelete("/tasks/{id}", (Guid id) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) return Results.NotFound();

    tasks.Remove(task);
    return Results.NoContent();
});

app.MapGet("/tasks/assignees", () =>
{
    var activeUsers = users.Where(u => u.IsActive).Select(u => new
    {
        u.Id,
        u.Name,
        u.Email
    });

    return Results.Ok(activeUsers);
});

app.MapPut("/tasks/{taskId}/assign/{userId}", (Guid taskId, Guid userId) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == taskId);
    if (task is null) return Results.NotFound("Task not found.");

    var user = users.FirstOrDefault(u => u.Id == userId && u.IsActive);
    if (user is null) return Results.NotFound("User not found or inactive.");

    task.AssigneeId = userId;
    task.LastUpdated = DateTime.UtcNow;

    return Results.Ok(task);
});

// User Preferences Endpoints
app.MapGet("/user-preferences/{userId}", (string userId) =>
{
    userPreferences.TryGetValue(userId, out var preferences);
    return preferences is not null ? Results.Ok(preferences) : Results.NotFound();
});

app.MapPut("/user-preferences/{userId}", (string userId, UserPreferences updatedPreferences) =>
{
    userPreferences[userId] = updatedPreferences;
    return Results.Ok(updatedPreferences);
});

// User Endpoints
app.MapPost("/users", (User newUser) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Name) || string.IsNullOrWhiteSpace(newUser.Email))
    {
        return Results.BadRequest("Name and Email are required.");
    }

    newUser.Id = Guid.NewGuid();
    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
});

app.MapGet("/users", () =>
{
    return Results.Ok(users.Where(u => u.IsActive)); // Only return active users
});

app.MapGet("/users/{id}", (Guid id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPut("/users/{id}", (Guid id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    user.IsActive = updatedUser.IsActive;
    return Results.Ok(user);
});

app.MapDelete("/users/{id}", (Guid id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    user.IsActive = false; // Soft delete by marking as inactive
    return Results.NoContent();
});

app.Run();

// Models
public class TaskGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class Task
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Title { get; set; }
    public string State { get; set; }
    public Guid? AssigneeId { get; set; } // User ID
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

public class UserPreferences
{
    public List<string> ColumnOrder { get; set; } = new();
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true; // To deactivate users if needed
}