namespace CloudFileManager.Application.Configuration;

public enum FileConflictPolicyType
{
    Reject,
    Overwrite,
    Rename
}

public enum DirectoryDeletePolicyType
{
    ForbidNonEmpty,
    RecursiveDelete
}

public static class ManagementPolicyParser
{
    public static FileConflictPolicyType ParseFileConflictPolicy(string rawPolicy)
    {
        if (Enum.TryParse(rawPolicy, ignoreCase: true, out FileConflictPolicyType parsed))
        {
            return parsed;
        }

        return FileConflictPolicyType.Reject;
    }

    public static DirectoryDeletePolicyType ParseDirectoryDeletePolicy(string rawPolicy)
    {
        if (Enum.TryParse(rawPolicy, ignoreCase: true, out DirectoryDeletePolicyType parsed))
        {
            return parsed;
        }

        return DirectoryDeletePolicyType.ForbidNonEmpty;
    }
}
