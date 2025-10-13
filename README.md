# CloudCore - Cloud Storage API

A production-ready cloud storage platform API built with ASP.NET Core, featuring secure file management, team collaboration, and intelligent storage tracking.

[College term paper]

## 🚀 Overview

CloudCore is a comprehensive backend system for cloud file storage and collaboration, similar to Google Drive or Dropbox. It provides RESTful APIs for file/folder operations, team workspaces (teamspaces), user authentication, and storage quota management.

## 🎯 Key Features

### File Management
- **CRUD Operations**: Upload, download, rename, move, and delete files/folders
- **Bulk Operations**: Download multiple items as ZIP archives
- **Soft Delete**: Trash system with 30-day retention policy
- **Search**: Query items by name with pagination and sorting
- **Path Management**: Hierarchical folder structure with breadcrumb navigation

### Team Collaboration
- **Teamspaces**: Shared workspaces with separate storage pools
- **Permission System**: Granular access control (read/write/admin)
- **Member Management**: Invite users, manage roles, and track invitations
- **Collaborative Storage**: Independent storage quotas per teamspace

### Storage Management
- **Quota Tracking**: Real-time storage usage monitoring
- **Subscription Tiers**: Free, Premium, and Enterprise plans with different limits
- **Storage Calculation**: Efficient recursive folder size computation
- **Automatic Updates**: Storage usage updated on file operations

### Security
- **JWT Authentication**: Secure token-based authentication
- **Authorization Filter**: Automatic user validation on all endpoints
- **Path Traversal Protection**: Prevents unauthorized file system access
- **User Isolation**: Strict enforcement of user-owned resources

## 🧩 Core Components

### Controllers Layer
Handles HTTP requests and responses, delegates business logic to services:

- **`AuthController`**: User registration and login
- **`ItemController`**: Personal file/folder operations  
- **`TeamspaceController`**: Teamspace management and member operations
- **`TeamspaceItemController`**: File operations within teamspaces
- **`StorageController`**: Storage usage and quota information

### Service Layer
Implements business logic with single-responsibility interfaces:

#### Application Services (Orchestration)
- **`IItemApplication`**: Orchestrates file operations with validation
- **`ITeamspaceApplication`**: Coordinates teamspace item workflows

#### Domain Services (Business Logic)
- **`IItemManagerService`**: Prepares entities for operations (rename, move, delete)
- **`IValidationService`**: Validates names, files, permissions, and business rules
- **`ITeamspaceService`**: Core teamspace and membership logic
- **`IStorageTrackingService`**: Manages storage quota updates

#### Infrastructure Services
- **`IItemStorageService`**: Physical file system operations
- **`IZipArchiveService`**: ZIP archive creation for downloads
- **`IAuthService`**: Authentication and JWT generation
- **`ITrashCleanupService`**: Background job for expired item deletion

#### Data Access
- **`IItemRepository`**: Database queries with async enumerable support
- **`ISubscriptionService`**: User plan limits and restrictions

### Domain Models

**Core Entities:**
- `User`: Authentication and subscription information
- `Item`: Files and folders with hierarchical relationships
- `Teamspace`: Shared workspaces with storage limits
- `TeamspaceMember`: User roles within teamspaces

**Key Relationships:**
- Self-referencing hierarchy for folders
- User ownership of items
- Teamspace membership with permissions
- Soft delete tracking with timestamps

### 🛠️ Technology Stack

- Framework: ASP.NET Core 8.0
- Database: MySQL with Entity Framework Core 9.0
- ORM: Pomelo Entity Framework MySQL provider
- Authentication: JWT Bearer tokens (System.IdentityModel.Tokens.Jwt)
- Password Hashing: BCrypt.Net-Next
- Logging: Serilog with file rotation and console output
- API Documentation: Swagger
- Environment Config: DotNetEnv for .env file support
- Natural Sorting: NaturalSort.Extension for intuitive file ordering

### 📊 Database Schema
Key Tables

- users: Authentication, subscription plans, storage tracking
- items: Files and folders with parent-child relationships
- teamspaces: Shared workspaces with admin and limits
- teamspace_members: User roles within teamspaces

Optimized Indexes

- idx_parent_user: Fast hierarchical queries
- idx_user_type: Efficient user item filtering
- idx_teamspace_items: Quick teamspace item lookups
- idx_name: Name-based searches

### 🔐 Security Features

JWT Authentication:

- 7-day token expiration
- User ID, username, and email in claims
- Configurable secret key via environment

Authorization Filter:

- Validates JWT claims match route userId parameter
- Prevents users from accessing others' resources
- Applied globally to all controllers

Path Security:

- Prevents directory traversal attacks
- Validates all file paths within user storage
- Rejects paths containing .. or absolute paths

Soft Delete:

- 30-day retention before permanent deletion
- Allows data recovery
- Background cleanup service

CORS Configuration:

- AllowAll policy (configurable for production)
- Supports cross-origin requests


Global Error Handler:

- Catches all exceptions
- Returns user-friendly error messages
- Logs detailed error information



📈 Performance Optimizations

- Async Streams: Memory-efficient processing of large datasets
- Batch Operations: Bulk database updates with configurable batch sizes (500 default)
- Read Optimization: AsNoTracking() for read-only queries
- Natural Sorting: Efficient file name sorting with NaturalSort.Extension
- Pagination: Prevents loading excessive data in list endpoints
- Transaction Batching: Groups multiple operations for atomic commits
- Lazy Loading: Hierarchical queries load data on-demand
- Connection Pooling: DbContextFactory for efficient connection reuse

📝 API Reference
File Operations
```
GET    /user/{userId}/mydrive    
GET    /user/{userId}/mydrive?parentId={id} 
POST   /user/{userId}/mydrive/upload  
POST   /user/{userId}/mydrive/createfolder 
PUT    /user/{userId}/mydrive/{itemId}/rename    
POST   /user/{userId}/mydrive/{itemId}/move/{target} 
DELETE /user/{userId}/mydrive/{itemId}/delete     
PUT    /user/{userId}/mydrive/{itemId}/restore   
GET    /user/{userId}/mydrive/{fileId}/download 
GET    /user/{userId}/mydrive/{folderId}/downloadfolder 
POST   /user/{userId}/mydrive/download/multiple   
GET    /user/{userId}/mydrive/trash  
GET    /user/{userId}/mydrive/folder/path/{folderId}
```
Teamspace Management
```
POST   /user/{userId}/teamspaces                          # Create teamspace
GET    /user/{userId}/teamspaces                          # List user's teamspaces
GET    /user/{userId}/teamspaces/{id}                     # Get teamspace details
PUT    /user/{userId}/teamspaces/{id}                     # Update teamspace
DELETE /user/{userId}/teamspaces/{id}                     # Delete teamspace
POST   /user/{userId}/teamspaces/{id}/members             # Add member
GET    /user/{userId}/teamspaces/{id}/members             # List members
PUT    /user/{userId}/teamspaces/{id}/members/{userId}    # Update member role
DELETE /user/{userId}/teamspaces/{id}/members/{userId}    # Remove member
POST   /user/{userId}/teamspaces/{id}/leave               # Leave teamspace
```
Teamspace Files
```
GET    /user/{userId}/teamspaces/{id}/items              # List teamspace items
POST   /user/{userId}/teamspaces/{id}/items/upload       # Upload to teamspace
POST   /user/{userId}/teamspaces/{id}/items/createfolder # Create folder
PUT    /user/{userId}/teamspaces/{id}/items/{itemId}/rename    # Rename
DELETE /user/{userId}/teamspaces/{id}/items/{itemId}/delete    # Delete
PUT    /user/{userId}/teamspaces/{id}/items/{itemId}/restore   # Restore
GET    /user/{userId}/teamspaces/{id}/items/{fileId}/download  # Download
```
Storage Information
```
GET    /user/{userId}/storage/personal                    # Personal storage info
GET    /user/{userId}/storage/teamspace/{id}              # Teamspace storage info
POST   /user/{userId}/storage/personal/recalculate        # Recalculate personal
POST   /user/{userId}/storage/teamspace/{id}/recalculate  # Recalculate teamspace
```

🎓 Code Quality & Best Practices
Logging

- Structured logging with Serilog
- Log levels: Information, Warning, Error
- File rotation: Daily with 31-day retention
- Console and file sinks
- Contextual logging with user IDs and operation details

Error Handling

- Global middleware catches all exceptions
- Specific error codes for each failure type
- Transaction rollback on failures

Documentation

- Swagger UI with detailed endpoint descriptions
- Request/response examples
- Parameter descriptions and constraints

Validation

- Multi-layer validation (file, name, authorization)
- Descriptive error codes and messages
- Path traversal prevention
- File size and type restrictions
- Business rule enforcement

📦 Project Structure Details
#### Contracts (DTOs)

- Requests: Input models with data annotations
- Responses: Output models with computed properties
- Clear separation of concerns
- Validation attributes on request models

#### Services

- Interfaces: Define contracts
- Implementations: Concrete business logic
- Dependency injection for all services
- Scoped lifetime for database-dependent services

#### Domain

- Entities: EF Core models
- Navigation properties for relationships
- Computed properties (IsDeleted, etc.)
- Timestamps for audit trail

#### Common

- Errors: Centralized error code constants
- Validation: Reusable validation result models
- Models: Shared DTOs (e.g., TeamspaceLimits)

### 🔧 Configuration
#### Subscription Plans
Configured in ISubscriptionService:

- Free: 10GB personal, 5GB teamspace, 2 teamspaces, 5 members
- Premium: 20GB personal, 50GB teamspace, 10 teamspaces, 25 members
- Enterprise: 50GB personal, 500GB teamspace, unlimited teamspaces, 100 members

#### File Validation

- Max file size: 2GB
- Supported formats: 100+ file types
- Reserved names: Windows reserved names (CON, PRN, etc.)
- Invalid characters: < > : " | ? * \0 ,

#### Storage Paths

- Base path: /app/storage (configurable)
- User structure: /app/storage/users/user{id}/
- Relative paths stored in database
