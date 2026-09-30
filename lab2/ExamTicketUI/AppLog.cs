namespace ExamTicketUI
{
    /// <summary>
    /// Простейший лог в текстовый файл app.log. Сбой записи лога не
    /// должен ронять приложение, поэтому ошибки здесь глотаются.
    /// </summary>
    internal static class AppLog
    {
        public static void Write(string message)
        {
            try
            {
                File.AppendAllText(
                    AppPaths.LogFile,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch (Exception)
            {
            }
        }
    }
}
