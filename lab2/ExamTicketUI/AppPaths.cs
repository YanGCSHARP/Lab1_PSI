namespace ExamTicketUI
{
    /// <summary>
    /// Определяет, где лежат входные файлы и куда писать журнал.
    /// </summary>
    internal static class AppPaths
    {
        public const string StudentsFileName = "students.xlsx";
        public const string TicketsFileName = "tickets.docx";
        public const string LogFileName = "app.log";

        /// <summary>
        /// Текущая папка, если входные файлы лежат в ней (запуск из
        /// консоли рядом с данными), иначе — папка с exe.
        /// </summary>
        public static string DataDirectory { get; } = ResolveDataDirectory();

        public static string StudentsFile => Path.Combine(DataDirectory, StudentsFileName);
        public static string TicketsFile => Path.Combine(DataDirectory, TicketsFileName);
        public static string ResultsFile => Path.Combine(DataDirectory, ResultsJournal.DefaultFileName);
        public static string LogFile => Path.Combine(DataDirectory, LogFileName);

        private static string ResolveDataDirectory()
        {
            var current = Environment.CurrentDirectory;
            if (File.Exists(Path.Combine(current, StudentsFileName)) ||
                File.Exists(Path.Combine(current, TicketsFileName)))
            {
                return current;
            }

            return AppContext.BaseDirectory;
        }
    }
}
