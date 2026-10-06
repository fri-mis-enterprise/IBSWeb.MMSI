# 7. Administration (Users & Roles)

User and role management is available in the **Admin** area. Requires **Admin** role.

## User Access Permissions

User Access records control which MSAP procedures each user can perform (e.g., CreateJobOrder, BillDispatchTickets, ViewMaritimeReport).

Managed via: **MSAP > User Access** (Admin role required)

## Role Management

![Role Management list](/msap/docs-images/admin/role-management.png)

**Path:** GENERAL > Role

- **Table columns:** Name
- **Actions:** Create New Role (via modal)
- Roles are used for coarse authorization (`[Authorize(Roles = "Admin")]`)

### Create a Role
1. Click **CREATE NEW ROLE**
2. Enter the role name (e.g., "Supervisor")
3. Submit

## User Management

![User Management list](/msap/docs-images/admin/user-management.png)

**Path:** GENERAL > User

- **Table columns:** Username, Full Name, Department, Role, Status, Created, Modified, Actions
- **Actions:** Create, Edit, Toggle Status, Reset Password

### Create a User
1. Click **CREATE NEW USER** (opens modal)
2. Fill in: Username, Full Name, Department, Role, Password
3. Submit

### Edit a User
1. Click the Edit icon on a user row
2. Modify fields in the modal
3. Submit

### Toggle User Status
- Click the Activate/Deactivate icon to enable or disable a user account

### Reset Password
1. Click the Reset Password icon
2. Enter the new password
3. Submit

## User Access (MSAP Permissions)

**Path:** MSAP > User Access (Admin role)

- Manages fine-grained access to MSAP procedures per user
- **Table columns:** User, Permissions
- Create and Edit assign which procedures a user can access

## Tips

- **Roles** are coarse (Admin/User); **User Access** handles per-procedure permissions
- Deactivating a user prevents login but preserves their audit trail
- Password reset does NOT require the old password
