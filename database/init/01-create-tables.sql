CREATE DATABASE IF NOT EXISTS CloudCoreDB;
USE CloudCoreDB;


CREATE TABLE users (
  id INT PRIMARY KEY AUTO_INCREMENT,
  username VARCHAR(50) NOT NULL UNIQUE,
  email VARCHAR(100) NOT NULL UNIQUE,
  password_hash VARCHAR(255),

    -- subscription state
  subscription_plan ENUM('free', 'premium', 'enterprise') DEFAULT 'free',
  subscription_status ENUM('active', 'expired', 'cancelled') DEFAULT 'active',
  subscription_expires_at TIMESTAMP NULL,
    
  personal_storage_used_mb BIGINT DEFAULT 0,
  teamspaces_owned INT DEFAULT 0
);


CREATE TABLE teamspaces (
   id INT PRIMARY KEY AUTO_INCREMENT,
   name VARCHAR(255) NOT NULL,
   description TEXT DEFAULT NULL,
   admin_user_id INT NOT NULL,

    -- Plan-based limits. Set on creation based on admin's plan
   storage_limit_mb BIGINT NOT NULL,
   member_limit INT NOT NULL,

    -- Current usage
   storage_used_mb BIGINT DEFAULT 0,
   member_count INT DEFAULT 1, -- Admin counts

   INDEX idx_admin_user (admin_user_id),
   FOREIGN KEY (admin_user_id) REFERENCES users(id) ON DELETE RESTRICT
);


CREATE TABLE items (
   id INT PRIMARY KEY AUTO_INCREMENT,
   name VARCHAR(255) NOT NULL,
   type ENUM('file', 'folder') NOT NULL,
   parent_id INT NULL,  -- Points to parent item
   user_id INT NOT NULL,  -- Owner of the file
   teamspace_id INT NULL,  -- Null = personal file, NOT NULL = teamspace

    -- metadata
   file_path VARCHAR(500) NULL,
   file_size BIGINT NULL,
   mime_type VARCHAR(100) NULL,

    -- Permissions and status
   access_level ENUM('private', 'team_read', 'team_write') DEFAULT 'private',
   is_deleted BOOLEAN DEFAULT FALSE,

   INDEX idx_parent_user (parent_id, user_id),
   INDEX idx_user_type (user_id, type, is_deleted),
   INDEX idx_teamspace_items (teamspace_id, is_deleted),
   INDEX idx_name (name),

   FOREIGN KEY (parent_id) REFERENCES items(id) ON DELETE CASCADE,
   FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
   FOREIGN KEY (teamspace_id) REFERENCES teamspaces(id) ON DELETE CASCADE,

   CONSTRAINT chk_file_fields CHECK (
     (type = 'file' AND file_path IS NOT NULL AND file_size IS NOT NULL) OR
     (type = 'folder' AND file_path IS NULL AND file_size IS NULL)
   )
);


CREATE TABLE teamspace_members (
   id INT PRIMARY KEY AUTO_INCREMENT,
   teamspace_id INT NOT NULL,
   user_id INT NOT NULL,
   permission_level ENUM('read', 'write', 'admin') DEFAULT 'read',

   invited_by INT DEFAULT NULL,

   UNIQUE KEY unique_membership (teamspace_id, user_id),
   FOREIGN KEY (teamspace_id) REFERENCES teamspaces(id) ON DELETE CASCADE,
   FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
   FOREIGN KEY (invited_by) REFERENCES users(id) ON DELETE SET NULL
);