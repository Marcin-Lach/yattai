# YATT (Yet Another Task Tracker)

Urgh... as the name suggests, this repository contains source code for a (yet another) task tracker. To make things more fun (for me), I will implement this with great help from LLMs, starting with free version of ChatGPT, but who knows where I will end up.

## Features

The initial prompt to ChatGPT was as follows:

```markdown
A comprehensive web-based task tracker application with advanced organization and customization features:
- Task grouping functionality
- Assignee system for task responsibility
- Customizable task states defined by assigned individuals
- User-defined task properties (e.g., due date, customer name, details)
- Task groups displayed as interactive tables
- Drag-and-drop column reordering with user-specific persistence
- Alphabetical sorting capability for tasks within each column
- Multi-user support with individualized column order preferences

UI/Style:
- Modular design with collapsible task groups and expandable task details
- Intuitive drag-and-drop interfaces for both tasks and columns
- Color-coded task states and priority levels for at-a-glance status assessment
```

## Endpoints

Following is the list of all available endpoints.

### **Endpoints for Task Groups**

| Endpoint                   | HTTP Method | Description                                   | Notes                                     |
|----------------------------|-------------|-----------------------------------------------|-------------------------------------------|
| `/task-groups`             | POST        | Add a new task group.                        | Adds a new task group to the system.      |
| `/task-groups`             | GET         | Get a list of all task groups with metadata. | Includes task count and last updated date.|
| `/task-groups/{id}`        | GET         | Get a specific task group by ID.             | Fetches details of a single task group.   |
| `/task-groups/{id}`        | PUT         | Update a task group.                         | Updates the name and metadata of a task group. |
| `/task-groups/{id}`        | DELETE      | Delete a task group (and associated tasks).  | Removes a group and all its associated tasks. |

---

### **Endpoints for Tasks**

| Endpoint                          | HTTP Method | Description                                | Notes                                                   |
|-----------------------------------|-------------|--------------------------------------------|---------------------------------------------------------|
| `/tasks`                          | POST        | Add a new task.                           | Adds a task with user-defined properties.               |
| `/task-groups/{groupId}/tasks`    | GET         | Get tasks for a task group with pagination and sorting. | Supports `page`, `pageSize`, `sortBy`, and `sortOrder`. |
| `/tasks/{id}`                     | GET         | Get a specific task by ID.                | Fetches details of a single task.                      |
| `/tasks/{id}`                     | PUT         | Update a task.                            | Allows updates to task title, state, assignee, and custom properties. |
| `/tasks/{id}`                     | DELETE      | Delete a task.                            | Removes a task from the system.                        |
| `/tasks/{taskId}/assign/{userId}` | PUT         | Assign a task to a specific user.         | Links a task to an active user by their `User.Id`.      |

---

### **Endpoints for Users**

| Endpoint                   | HTTP Method | Description                                | Notes                                                   |
|----------------------------|-------------|--------------------------------------------|---------------------------------------------------------|
| `/users`                   | POST        | Add a new user.                           | Requires `Name` and `Email`.                           |
| `/users`                   | GET         | Get all active users.                     | Only returns users marked as active.                   |
| `/users/{id}`              | GET         | Get a specific user by ID.                | Fetches details of a single user.                      |
| `/users/{id}`              | PUT         | Update a user.                            | Allows updates to name, email, and active status.       |
| `/users/{id}`              | DELETE      | Deactivate a user (soft delete).          | Marks the user as inactive instead of deleting them permanently. |
| `/tasks/assignees`         | GET         | Get all active users available to assign. | Lists users eligible to be assigned to tasks.          |

---

### **Endpoints for User Preferences**

| Endpoint                       | HTTP Method | Description                                | Notes                                                   |
|--------------------------------|-------------|--------------------------------------------|---------------------------------------------------------|
| `/user-preferences/{userId}`   | GET         | Get preferences for a specific user.      | Includes preferences like column order for task groups. |
| `/user-preferences/{userId}`   | PUT         | Update preferences for a specific user.   | Updates user-specific settings for UI and organization. |

---

### **Key Features**

- **Task Pagination and Sorting**:
  - The `/task-groups/{groupId}/tasks` endpoint supports **pagination** (`page`, `pageSize`) and **sorting** (`sortBy`, `sortOrder`) for efficient data retrieval.
  
- **Task Assignment**:
  - The `/tasks/{taskId}/assign/{userId}` endpoint enables linking tasks to users by their unique `User.Id`, ensuring accountability.

- **User Management**:
  - Users are soft-deleted by marking them as inactive, preserving task history and integrity.

- **Dynamic Properties**:
  - The `CustomProperties` field in tasks supports storing user-defined properties like `DueDate`, `Priority`, and more.