INSERT INTO `CloudCoreDB`.`users` (`id`, `username`, `email`, `password`) VALUES ('1', 'test', 'test', 'test');
INSERT INTO `CloudCoreDB`.`items` (`id`, `name`, `type`, `user_id`, `file_path`, `file_size`, `mime_type`, `is_deleted`) VALUES ('1', 'test1', 'file', '1', 'test1.html', '1024', 'text/html', '0');
INSERT INTO `CloudCoreDB`.`items` (`id`, `name`, `type`, `user_id`, `is_deleted`) VALUES ('2', 'testfolder1', 'folder', '1', '0');
INSERT INTO `CloudCoreDB`.`items` (`id`, `name`, `type`, `parent_id`, `user_id`, `is_deleted`) VALUES ('3', 'testfolder2', 'folder', '2', '1', '0');
INSERT INTO `CloudCoreDB`.`items` (`id`, `name`, `type`, `parent_id`, `user_id`, `file_path`, `file_size`, `mime_type`) VALUES ('4', 'test2', 'file', '3', '1', 'test2.zip', '1024', 'application/zip');
