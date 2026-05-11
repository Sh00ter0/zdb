namespace Client.Data
{
    public static class DbPath
    {
        public static string GetDatabasePath(string configuredPath = "Data\\api-security.db")
        {
            var databasePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, configuredPath);

            var dataDir = Path.GetDirectoryName(databasePath);

            if (!string.IsNullOrWhiteSpace(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            return databasePath;
        }
    }
}
