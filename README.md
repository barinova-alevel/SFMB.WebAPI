# Self-Finance-Manager-Backend-WebApi 

.NET Core Backend for application of managing self finance (REST API).

## Features

1. **CRUD for list of types for income and expenses**
2. **CRUD for financial operations**
3. **Daily report** (input: date; result: total income for date, total expenses for date, list of operation for date)
4. **Date period report** (input: start date, end date; result: total income for period, total expenses for period, list of operation for period)
5. **User Authentication & Authorization** with JWT tokens
6. **Role-based access control** (Admin and User roles)

## Authentication & Authorization

### Overview

The application implements a secure authentication and authorization system using:
- **ASP.NET Core Identity** for user management
- **JWT (JSON Web Tokens)** for stateless authentication
- **Role-based authorization** (Admin and User roles)

### User Roles

#### User Role
- Can register and login
- Can create, read, update, and delete their **own** operations
- Can create, read, update, and delete their **own** operation types
- Can generate daily and period reports for their **own** data only
- Cannot see other users' data

#### Admin Role
- Has all User permissions
- Can view **all** operations and operation types from all users
- Can generate reports across **all** users' data
- Can delete users (except themselves)
- Can view list of all users

### Default Users

The system seeds three default users on startup:

| Email | Password | Role | Name |
|-------|----------|------|------|
| admin@sfmb.com | Admin123! | Admin | Admin User |
| user1@sfmb.com | User123! | User | John Doe |
| user2@sfmb.com | User123! | User | Jane Smith |
| demo@goldfish.app | `DemoUser__Password` | User | Demo User |

### Database Changes

The following changes were made to support authentication:

1. **Identity Tables**: Added ASP.NET Core Identity tables for user management
   - AspNetUsers
   - AspNetRoles
   - AspNetUserRoles
   - AspNetUserClaims
   - AspNetRoleClaims
   - AspNetUserLogins
   - AspNetUserTokens

2. **User Ownership**: Added `UserId` foreign key to:
   - `Operations` table
   - `OperationTypes` table

3. **ApplicationUser Entity**: Extended IdentityUser with:
   - FirstName
   - LastName
   - CreatedAt timestamp

### API Endpoints

#### Authentication Endpoints (No authentication required)

```
POST /api/Auth/login
POST /api/Auth/register
GET  /api/Auth/demo
```

`GET /api/Auth/demo` returns the demo account email and password for the one-click login button. It responds with 404 until `DemoUser__Password` is set. The password is not stored in source control. `DemoUser__Email` is optional and defaults to `demo@goldfish.app`.

#### Operation Endpoints (Requires authentication)

```
GET    /api/Operations          # Get all operations (filtered by user, or all if admin)
GET    /api/Operations/{id}     # Get specific operation (only owned or all if admin)
POST   /api/Operations          # Create new operation (auto-assigned to current user)
PUT    /api/Operations/{id}     # Update operation (only owned or all if admin)
DELETE /api/Operations/{id}     # Delete operation (only owned or all if admin)
```

#### Operation Type Endpoints (Requires authentication)

```
GET    /api/OperationTypes      # Get all operation types (filtered by user, or all if admin)
GET    /api/OperationTypes/{id} # Get specific operation type (only owned or all if admin)
POST   /api/OperationTypes      # Create new operation type (auto-assigned to current user)
PUT    /api/OperationTypes/{id} # Update operation type (only owned or all if admin)
DELETE /api/OperationTypes/{id} # Delete operation type (only owned or all if admin)
```

#### Report Endpoints (Requires authentication)

```
GET /api/DailyReport/report/daily?date=YYYY-MM-DD           # Daily report (filtered by user, or all if admin)
GET /api/PeriodReport/report/period?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD  # Period report (filtered by user, or all if admin)
```

#### User Management Endpoints (Admin only)

```
GET    /api/UserManagement        # Get all users
DELETE /api/UserManagement/{userId}  # Delete a user
```

### How to Use Authentication

#### 1. Login or Register

**Login:**
```bash
curl -X POST "https://your-api-url/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user1@sfmb.com",
    "password": "User123!"
  }'
```

**Register:**
```bash
curl -X POST "https://your-api-url/api/Auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "SecurePassword123!",
    "firstName": "New",
    "lastName": "User"
  }'
```

Both endpoints return:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user1@sfmb.com",
  "userId": "user-id-here",
  "role": "User"
}
```

#### 2. Use the JWT Token

Include the token in the `Authorization` header for all subsequent requests:

```bash
curl -X GET "https://your-api-url/api/Operations" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

#### 3. Using Swagger UI

1. Navigate to `/swagger` endpoint
2. Click the **Authorize** button (green lock icon)
3. Enter: `Bearer YOUR_JWT_TOKEN_HERE`
4. Click **Authorize**
5. Now all API calls from Swagger will include the token

### Configuration

JWT settings are configured in `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "REPLACE_WITH_SECURE_RANDOM_KEY_AT_LEAST_32_CHARACTERS",
    "Issuer": "SFMBApi",
    "Audience": "SFMBApi",
    "ExpiryInHours": 24
  }
}
```

**Important for Production:**
- **CRITICAL**: Replace the `Jwt:Key` placeholder with a secure random string before deployment
- The key must be at least 32 characters long
- Use a cryptographically secure random generator to create the key
- Store sensitive configuration in environment variables or Azure Key Vault, not in appsettings.json
- Set appropriate token expiry times
- Never commit production keys to version control

### Environment Variables

For DigitalOcean deployment, set these environment variables:

```
DATABASE_URL=your-postgres-connection-string
Jwt__Key=your-secure-jwt-key
Jwt__Issuer=SFMBApi
Jwt__Audience=SFMBApi
Jwt__ExpiryInHours=24
DemoUser__Password=the-demo-account-password
DemoUser__Email=demo@goldfish.app
```

### Security Features

1. **Password Requirements:**
   - Minimum 6 characters
   - Must contain uppercase letter
   - Must contain lowercase letter
   - Must contain digit
   - Non-alphanumeric not required

2. **JWT Token Security:**
   - Tokens expire after configured time (default 24 hours)
   - HTTPS recommended for all API calls
   - Tokens contain user ID and role claims

3. **Authorization Checks:**
   - All endpoints (except Auth) require valid JWT token
   - User role can only access their own data
   - Admin role can access all data
   - Admins cannot delete themselves

### Database Migration

To apply the authentication migration:

```bash
cd SFMB.DAL
dotnet ef migrations add AddIdentityAndUserOwnership --startup-project ../WebApiForAz
dotnet ef database update --startup-project ../WebApiForAz
```

### Testing Authentication

You can test the authentication flow:

1. **Register a new user** via `/api/Auth/register`
2. **Login** with the credentials to get a JWT token
3. **Create operations and operation types** - they will be automatically assigned to your user
4. **Try to access another user's data** - you should get 404 (not found)
5. **Login as admin** and verify you can see all users' data
6. **Test admin user management** endpoints

## Technical Stack

- **.NET 9.0**
- **ASP.NET Core Identity** for authentication
- **JWT Bearer tokens** for authorization
- **Entity Framework Core** with PostgreSQL
- **Swagger/OpenAPI** for API documentation

## Project Structure

```
WebApiForAz/
├── WebApiForAz/           # API Controllers and Program.cs
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── OperationsController.cs
│   │   ├── OperationTypesController.cs
│   │   ├── DailyReportController.cs
│   │   ├── PeriodReportController.cs
│   │   └── UserManagementController.cs
│   └── Middleware/
├── SFMB.BL/              # Business Logic Layer
│   ├── Services/
│   │   └── AuthService.cs
│   └── Dtos/
│       ├── LoginDto.cs
│       ├── RegisterDto.cs
│       └── AuthResponseDto.cs
├── SFMB.DAL/             # Data Access Layer
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   ├── Operation.cs
│   │   └── OperationType.cs
│   ├── Repositories/
│   └── DbSeeder.cs       # Seeds default users and roles
└── FSMB.UnitTests/       # Unit Tests
```

## Contributing

When adding new features that involve user data, ensure:
1. Add `UserId` to new entities
2. Filter queries by `UserId` for regular users
3. Allow admins to access all data
4. Add `[Authorize]` attribute to controllers
5. Test both User and Admin role access
