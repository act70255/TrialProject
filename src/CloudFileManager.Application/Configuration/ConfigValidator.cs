namespace CloudFileManager.Application.Configuration;

public static class ConfigValidator
{
    private const string TraversalMode = "DFS_PRE_ORDER";
    private const string SiblingOrder = "CREATION_ORDER";
    private const string LogLevelInfo = "Info";
    private const string LogLevelDebug = "Debug";
    private const string XmlTargetConsole = "Console";
    private const string XmlTargetFile = "File";
    private const string DatabaseProviderSqlite = "Sqlite";
    private const string DatabaseProviderSqlServer = "SqlServer";

    private static readonly IReadOnlyList<ValidationRule> SharedRules =
    [
        new ValidationRule(ValidateVersion),
        new ValidationRule(ValidateTraversalMode),
        new ValidationRule(ValidateSiblingOrder),
        new ValidationRule(ValidateStorageRootPath),
        new ValidationRule(ValidateLoggingLevel),
        new ValidationRule(ValidateXmlTarget),
        new ValidationRule(ValidateXmlOutputPath),
        new ValidationRule(ValidateFileConflictPolicy),
        new ValidationRule(ValidateDirectoryDeletePolicy),
        new ValidationRule(ValidateMaxUploadSize)
    ];

    public static IReadOnlyList<ConfigValidationError> Validate(AppConfig config)
    {
        List<ConfigValidationError> errors = new();

        foreach (ValidationRule rule in SharedRules)
        {
            AddIfNotNull(errors, rule.Validate(config));
        }

        ValidateDatabase(config, errors);
        return errors;
    }

    private static ConfigValidationError? ValidateVersion(AppConfig config)
    {
        return string.IsNullOrWhiteSpace(config.ConfigVersion)
            ? new ConfigValidationError("CONF_VERSION_REQUIRED", "ConfigVersion", "ConfigVersion is required.")
            : null;
    }

    private static ConfigValidationError? ValidateTraversalMode(AppConfig config)
    {
        return string.Equals(config.Traversal.Mode, TraversalMode, StringComparison.OrdinalIgnoreCase)
            ? null
            : new ConfigValidationError("CONF_TRAVERSAL_MODE_INVALID", "Traversal.Mode", "Only DFS_PRE_ORDER is supported.");
    }

    private static ConfigValidationError? ValidateSiblingOrder(AppConfig config)
    {
        return string.Equals(config.Traversal.SiblingOrder, SiblingOrder, StringComparison.OrdinalIgnoreCase)
            ? null
            : new ConfigValidationError("CONF_SIBLING_ORDER_INVALID", "Traversal.SiblingOrder", "Only CREATION_ORDER is supported.");
    }

    private static ConfigValidationError? ValidateStorageRootPath(AppConfig config)
    {
        return string.IsNullOrWhiteSpace(config.Storage.StorageRootPath)
            ? new ConfigValidationError("CONF_STORAGE_ROOT_REQUIRED", "Storage.StorageRootPath", "Storage root path is required.")
            : null;
    }

    private static ConfigValidationError? ValidateLoggingLevel(AppConfig config)
    {
        bool isInfo = string.Equals(config.Logging.Level, LogLevelInfo, StringComparison.OrdinalIgnoreCase);
        bool isDebug = string.Equals(config.Logging.Level, LogLevelDebug, StringComparison.OrdinalIgnoreCase);
        return isInfo || isDebug
            ? null
            : new ConfigValidationError("CONF_LOG_LEVEL_INVALID", "Logging.Level", "Logging level must be Info or Debug.");
    }

    private static ConfigValidationError? ValidateXmlTarget(AppConfig config)
    {
        bool isConsole = string.Equals(config.Output.XmlTarget, XmlTargetConsole, StringComparison.OrdinalIgnoreCase);
        bool isFile = string.Equals(config.Output.XmlTarget, XmlTargetFile, StringComparison.OrdinalIgnoreCase);
        return isConsole || isFile
            ? null
            : new ConfigValidationError("CONF_XML_TARGET_INVALID", "Output.XmlTarget", "XmlTarget must be Console or File.");
    }

    private static ConfigValidationError? ValidateXmlOutputPath(AppConfig config)
    {
        bool isFile = string.Equals(config.Output.XmlTarget, XmlTargetFile, StringComparison.OrdinalIgnoreCase);
        return !isFile || !string.IsNullOrWhiteSpace(config.Output.XmlOutputPath)
            ? null
            : new ConfigValidationError("CONF_XML_PATH_REQUIRED", "Output.XmlOutputPath", "XmlOutputPath is required when XmlTarget is File.");
    }

    private static ConfigValidationError? ValidateFileConflictPolicy(AppConfig config)
    {
        bool conflictPolicyValid = Enum.TryParse(config.Management.FileConflictPolicy, ignoreCase: true, out FileConflictPolicyType _);
        return conflictPolicyValid
            ? null
            : new ConfigValidationError("CONF_MGMT_CONFLICT_POLICY_INVALID", "Management.FileConflictPolicy", "FileConflictPolicy must be Reject, Overwrite, or Rename.");
    }

    private static ConfigValidationError? ValidateDirectoryDeletePolicy(AppConfig config)
    {
        bool directoryDeletePolicyValid = Enum.TryParse(config.Management.DirectoryDeletePolicy, ignoreCase: true, out DirectoryDeletePolicyType _);
        return directoryDeletePolicyValid
            ? null
            : new ConfigValidationError("CONF_MGMT_DIR_DELETE_POLICY_INVALID", "Management.DirectoryDeletePolicy", "DirectoryDeletePolicy must be ForbidNonEmpty or RecursiveDelete.");
    }

    private static ConfigValidationError? ValidateMaxUploadSize(AppConfig config)
    {
        return config.Management.MaxUploadSizeBytes > 0
            ? null
            : new ConfigValidationError("CONF_MGMT_MAX_UPLOAD_INVALID", "Management.MaxUploadSizeBytes", "MaxUploadSizeBytes must be greater than zero.");
    }

    private static void ValidateDatabase(AppConfig config, List<ConfigValidationError> errors)
    {
        bool providerIsSqlite = string.Equals(config.Database.Provider, DatabaseProviderSqlite, StringComparison.OrdinalIgnoreCase);
        bool providerIsSqlServer = string.Equals(config.Database.Provider, DatabaseProviderSqlServer, StringComparison.OrdinalIgnoreCase);

        if (!providerIsSqlite && !providerIsSqlServer)
        {
            errors.Add(new ConfigValidationError("CONF_DB_PROVIDER_INVALID", "Database.Provider", "Provider must be Sqlite or SqlServer."));
            return;
        }

        if (providerIsSqlite && string.IsNullOrWhiteSpace(config.Database.ConnectionStrings.Sqlite))
        {
            errors.Add(new ConfigValidationError("CONF_DB_CONN_REQUIRED", "Database.ConnectionStrings.Sqlite", "Sqlite connection string is required."));
        }

        if (providerIsSqlServer && string.IsNullOrWhiteSpace(config.Database.ConnectionStrings.SqlServer))
        {
            errors.Add(new ConfigValidationError("CONF_DB_CONN_REQUIRED", "Database.ConnectionStrings.SqlServer", "SqlServer connection string is required."));
        }
    }

    private static void AddIfNotNull(List<ConfigValidationError> errors, ConfigValidationError? error)
    {
        if (error is not null)
        {
            errors.Add(error);
        }
    }

    private sealed record ValidationRule(Func<AppConfig, ConfigValidationError?> Validate);
}
