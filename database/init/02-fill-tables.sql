USE CloudCoreDB;

INSERT INTO users (username, email, password) VALUES
    ('testuser', 'test@example.com', 'password123');

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('test.html', 'file', NULL, 1, 'test.html', 138, 'text/html', FALSE);
