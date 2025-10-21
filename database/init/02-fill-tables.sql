USE CloudCoreDB;
--
-- INSERT INTO users (username, email, password_hash) VALUES
--     ('admin', 'admin@cloudcore.com', 'password123'),
--     ('user', 'user@cloudcore.com', 'password123');
--
INSERT INTO users (username, email, password_hash, is_email_verified, subscription_plan, subscription_status, personal_storage_used_mb, teamspaces_owned) VALUES
    ('admin', 'admin@cloudcore.com', 'password123', true, 'enterprise', 'active', 0, 0),
    ('user', 'user@cloudcore.com', 'password123', true, 'premium', 'active', 0, 0);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('test.html', 'file', NULL, 1, 'test.html', 138, 'text/html', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('photos', 'folder', NULL, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('image.png', 'file', 2, 1, 'photos/image.png', 2048, 'image/png', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('documents', 'folder', NULL, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('coursework', 'folder', 4, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('coursework1.pdf', 'file', 5, 1, 'documents/coursework/coursework1.pdf', 51200, 'application/pdf', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('coursework1', 'folder', 5, 1, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('coursework.pdf', 'file', 7, 1, 'documents/coursework/coursework1/coursework.pdf', 51200, 'application/pdf', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('coursework2', 'folder', 7, 1, NULL, NULL, NULL, FALSE);
    
INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('coursework.pdf', 'file', 9, 1, 'documents/coursework/coursework1/coursework2/coursework.pdf', 51200, 'application/pdf', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('resume.pdf', 'file', 4, 1, 'documents/resume.pdf', 51200, 'application/pdf', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('documents', 'folder', NULL, 2, NULL, NULL, NULL, FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('resume.pdf', 'file', 12, 2, 'documents/resume.pdf', 51200, 'application/pdf', FALSE);

INSERT INTO items (name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted) VALUES
    ('folder1', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder2', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder3', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder4', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder5', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder6', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder7', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder8', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder9', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder10', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder11', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder12', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder13', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder14', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder15', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder16', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder17', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder18', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder19', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder20', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder21', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder22', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder23', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder24', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder25', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder26', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder27', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder28', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder29', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder30', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder31', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder32', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder33', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder34', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder35', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder36', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder37', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder38', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder39', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder40', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder41', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder42', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder43', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder44', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder45', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder46', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder47', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder48', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder49', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder50', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder51', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder52', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder53', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder54', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder55', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder56', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder57', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder58', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder59', 'folder', NULL, 1, NULL, NULL, NULL, FALSE),
    ('folder60', 'folder', NULL, 1, NULL, NULL, NULL, FALSE);



