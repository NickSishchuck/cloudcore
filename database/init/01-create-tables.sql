CREATE DATABASE IF NOT EXISTS CloudCoreDB;
USE CloudCoreDB;
       
CREATE TABLE users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    passwordHash VARCHAR(255)
);

CREATE TABLE items (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(255) NOT NULL, -- presentation.pdf
    type ENUM('file', 'folder') NOT NULL,
    parent_id INT NULL,  -- folder123
    user_id INT NOT NULL,  -- user123
    file_path VARCHAR(500) NULL,  -- storage/users/user123/folder123/abcdef3478219.pdf
    file_size BIGINT NULL,  -- 5000 (5KB)
    mime_type VARCHAR(100) NULL, -- .pdf, .docx, etc.
    is_deleted BOOLEAN DEFAULT FALSE,
    INDEX idx_parent_user (parent_id, user_id),
    INDEX idx_user_type (user_id, type),
    INDEX idx_name (name),
    FOREIGN KEY (parent_id) REFERENCES items(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT chk_file_fields CHECK (
        (type = 'file' AND file_path IS NOT NULL AND file_size IS NOT NULL) OR
        (type = 'folder' AND file_path IS NULL AND file_size IS NULL)
    )
);
