namespace CloudCore.Common.Errors
{
    public static class ErrorCodes
    {

        public const string INVALID_NAME = "INVALID_NAME";
        public const string NAME_TOO_LONG = "NAME_TOO_LONG";
        public const string NAME_ALREADY_EXISTS = "NAME_ALREADY_EXISTS";
        public const string INVALID_CHARECTER = "INVALID_CHARACTER";
        public const string RESERVED_NAME = "RESERVED_NAME";
        public const string INVALID_NAME_FORMAT = "INVALID_NAME_FORMAT";

        public const string ITEM_NOT_FOUND = "ITEM_NOT_FOUND";
        public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";
        public const string FOLDER_NOT_FOUND = "FOLDER_NOT_FOUND";
        public const string UNSUPPORTED_TYPE = "UNSUPPORTED_TYPE";
        public const string NO_ITEMS = "NO_ITEMS";
        public const string PARENT_FOLDER_DELETED = "PARENT_FOLDER_DELETED";
        public const string NULL_OR_EMPTY = "NULL_OR_EMPTY";
        public const string NOT_ALLOWED_SYMBOL = "NOT_ALLOWED_SYMBOL";



        public const string ARCHIVE_TOO_LARGE = "ARCHIVE_TOO_LARGE";
        public const string TOO_MANY_FILES = "TOO_MANY_FILES";

        public const string FILE_TOO_LARGE = "FILE_TOO_LARGE";
        public const string INVALID_FILE_TYPE = "INVALID_FILE_TYPE";
        public const string FILE_REQUIRED = "FILE_REQUIRED";
        public const string INVALID_TARGET = "INVALID_TARGET";
        public const string INVALID_OPERATION = "INVALID_OPERATION";
        public const string CIRCULAR_REFERENCE = "CIRCULAR_REFERENCE";


        public const string UPLOADED_SUCCESSFULLY = "UPLOADED_SUCCESSFULLY";
        public const string CREATED_SUCCESSFULLY = "CREATED_SUCCESSFULLY";
        public const string DELETED_SUCCESSFULLY = "DELETED_SUCCESSFULLY";
        public const string RESTORED_SUCCESSFULLY = "RESTORED_SUCCESSFULLY";
        public const string MOVED_SUCCESSFULLY = "MOVED_SUCCESSFULLY";

        public const string ACCESS_DENIED = "ACCESS_DENIED";
        public const string BAD_REQUEST = "BAD_REQUEST";
        public const string OPERATION_FAILED = "OPERATION_FAILED";
        public const string UNEXPECTED_ERROR = "UNEXPECTED_ERROR";
        public const string IO_ERROR = "IO_ERROR";

        public const string TEAMSPACE_NOT_FOUND = "TEAMSPACE_NOT_FOUND";
        public const string TEAMSPACE_ACCESS_DENIED = "TEAMSPACE_ACCESS_DENIED";
        public const string TEAMSPACE_LIMIT_REACHED = "TEAMSPACE_LIMIT_REACHED";

        public const string MEMBER_NOT_FOUND = "MEMBER_NOT_FOUND";
        public const string MEMBER_ALREADY_EXISTS = "MEMBER_ALREADY_EXISTS";
        public const string MEMBER_LIMIT_REACHED = "MEMBER_LIMIT_REACHED";
        public const string CANNOT_REMOVE_ADMIN = "CANNOT_REMOVE_ADMIN";
        public const string CANNOT_LEAVE_AS_ADMIN = "CANNOT_LEAVE_AS_ADMIN";
        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
        public const string TEAMSPACE_NAME_TAKEN = "TEAMSPACE_NAME_TAKEN";

        public const string INSUFFICIENT_PERMISSION = "INSUFFICIENT_PERMISSION";
        public const string INVALID_PERMISSION = "INVALID_PERMISSION";

        public const string STORAGE_LIMIT_EXCEEDED = "STORAGE_LIMIT_EXCEEDED";
    }
}
