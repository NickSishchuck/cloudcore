USE CloudCoreDB;

INSERT INTO users (username, email, password) VALUES
    ('testuser', 'test@example.com', 'password123');

INSERT INTO users (`id`, `username`, `email`, `password`) VALUES 
    ('2', 'test2', 'test2', 'test2');

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('test.html', 'file', NULL, 1, 'test.html', 138, 'text/html', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('photos', 'folder', NULL, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('image.png', 'file', 2, 1, 'photos/image.png', 2048, 'image/png', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('documents', 'folder', NULL, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('resume.pdf', 'file', 4, 1, 'documents/resume.pdf', 51200, 'application/pdf', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('documents', 'folder', NULL, 2, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('resume.pdf', 'file', 4, 2, 'documents/resume.pdf', 51200, 'application/pdf', FALSE);