USE CloudCoreDB;

-- Both passwords are "password123" but hashed with BCrypt
INSERT INTO users (username, email, passwordHash) VALUES
    ('admin', 'admin@cloudcore.com', '$2a$11$9O8XWLCR8O3.6R7fKYhGRe.FYQVcEb8NUPD3tOcMHzBaW8LpI4Y7i'),
    ('user', 'user@cloudcore.com', '$2a$11$9O8XWLCR8O3.6R7fKYhGRe.FYQVcEb8NUPD3tOcMHzBaW8LpI4Y7i');

-- Sample files for the admin user (user_id = 1)
INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('admin-config.html', 'file', NULL, 1, 'test.html', 138, 'text/html', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('admin-photos', 'folder', NULL, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('admin-image.png', 'file', 2, 1, 'photos/image.png', 2048, 'image/png', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('admin-documents', 'folder', NULL, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('admin-resume.pdf', 'file', 4, 1, 'documents/resume.pdf', 51200, 'application/pdf', FALSE);

-- Sample files for the regular user (user_id = 2)
INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('user-documents', 'folder', NULL, 2, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('user-resume.pdf', 'file', 6, 2, 'documents/resume.pdf', 51200, 'application/pdf', FALSE);