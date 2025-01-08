using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using yatt.Tasks.Api.Models; // Add this line to include the WorkItem and WorkItemGroup classes

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

var app = builder.Build();

// WorkItem Group Endpoints
app.MapPost("/work-item-groups", async (AppDbContext dbContext, WorkItemGroup workItemGroup) =>
{
    dbContext.WorkItemGroups.Add(workItemGroup);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/work-item-groups/{workItemGroup.Id}", workItemGroup);
})
.WithOpenApi();

app.MapGet("/work-item-groups", async (AppDbContext dbContext) =>
{
    var workItemGroups = await dbContext.WorkItemGroups
        .Select(g => new
        {
            g.Id,
            g.Name,
            WorkItemCount = g.WorkItems.Count,
        })
        .ToListAsync();
    return Results.Ok(workItemGroups);
})
.WithOpenApi();

app.MapGet("/work-item-groups/{id}", async (AppDbContext dbContext, int id) =>
{
    var workItemGroup = await dbContext.WorkItemGroups
        .Include(g => g.WorkItems)
        .FirstOrDefaultAsync(g => g.Id == id);

    if (workItemGroup == null) return Results.NotFound();
    return Results.Ok(workItemGroup);
})
.WithOpenApi();

app.MapPut("/work-item-groups/{id}", async (AppDbContext dbContext, int id, WorkItemGroup updatedGroup) =>
{
    var group = await dbContext.WorkItemGroups.FindAsync(id);
    if (group == null) return Results.NotFound();

    group.Name = updatedGroup.Name;
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapDelete("/work-item-groups/{id}", async (AppDbContext dbContext, int id) =>
{
    var group = await dbContext.WorkItemGroups
        .Include(g => g.WorkItems)
        .FirstOrDefaultAsync(g => g.Id == id);

    if (group == null) return Results.NotFound();

    group.IsDeleted = true;
    foreach (var workItem in group.WorkItems)
    {
        workItem.IsDeleted = true;
    }

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

// WorkItem Endpoints
app.MapPost("/work-items", async (AppDbContext dbContext, WorkItem workItem) =>
{
    dbContext.WorkItems.Add(workItem);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/work-items/{workItem.Id}", workItem);
})
.WithOpenApi();

app.MapGet("/work-item-groups/{groupId}/work-items", async (AppDbContext dbContext, int groupId, int page = 1, int pageSize = 10, string? sortBy = null, string sortOrder = "asc") =>
{
    var query = dbContext.WorkItems.Where(t => t.WorkItemGroupId == groupId);

    if (!string.IsNullOrEmpty(sortBy))
    {
        query = sortOrder.ToLower() switch
        {
            "desc" => query.OrderByDescending(t => EF.Property<object>(t, sortBy)),
            _ => query.OrderBy(t => EF.Property<object>(t, sortBy)),
        };
    }

    var totalWorkItems = await query.CountAsync();
    var workItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return Results.Ok(new { totalWorkItems, workItems });
})
.WithOpenApi();

app.MapGet("/work-items/{id}", async (AppDbContext dbContext, int id) =>
{
    var workItem = await dbContext.WorkItems.FindAsync(id);
    if (workItem == null) return Results.NotFound();
    return Results.Ok(workItem);
})
.WithOpenApi();

app.MapPut("/work-items/{id}", async (AppDbContext dbContext, int id, WorkItem updatedWorkItem) =>
{
    var workItem = await dbContext.WorkItems.FindAsync(id);
    if (workItem == null) return Results.NotFound();

    workItem.Title = updatedWorkItem.Title;
    workItem.State = updatedWorkItem.State;
    workItem.AssigneeId = updatedWorkItem.AssigneeId;
    workItem.CustomProperties = updatedWorkItem.CustomProperties;

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapDelete("/work-items/{id}", async (AppDbContext dbContext, int id) =>
{
    var workItem = await dbContext.WorkItems.FindAsync(id);
    if (workItem == null) return Results.NotFound();

    workItem.IsDeleted = true;
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithOpenApi();

app.MapPut("/work-items/{workItemId}/assign/{userId}", async (AppDbContext dbContext, int workItemId, int userId) =>
{
    var workItem = await dbContext.WorkItems.FindAsync(workItemId);
    if (workItem == null) return Results.NotFound();

    var user = await dbContext.Users.FindAsync(userId);
    if (user == null || !user.IsActive) return Results.BadRequest("Invalid or inactive user.");

    workItem.AssigneeId = userId;
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

app.MapGet("/work-items/assignees", async (AppDbContext dbContext, int workItemGroupId) =>
{
    var workItemGroup = await dbContext.WorkItemGroups
        .Include(tg => tg.Organization)
        .Include(tg => tg.CoWorkers)
        .FirstOrDefaultAsync(tg => tg.Id == workItemGroupId);

    if (workItemGroup == null) return Results.NotFound();

    var possibleAssignees = await dbContext.Users
        .Where(u => u.IsActive && (workItemGroup.CoWorkers.Contains(u) || workItemGroup.Organization.Members.Contains(u)))
        .ToListAsync();

    return Results.Ok(possibleAssignees);
})
.WithOpenApi();

app.Run();