// Лабораторная работа №2: генератор экзаменационных билетов с UI.
// Десктопное приложение (WinForms): группа и студент выбираются из
// students.xlsx, билеты читаются из tickets.docx, результат
// дописывается в results.xlsx.
//
// Зависимости (NuGet): ClosedXML, DocumentFormat.OpenXml
//
// Сборка и запуск (из корня репозитория):
//     dotnet run --project lab2/ExamTicketUI

namespace ExamTicketUI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Непредвиденная ошибка в обработчике UI не должна закрывать
            // приложение: пишем в лог и показываем сообщение.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                AppLog.Write($"Необработанная ошибка: {e.Exception}");
                MessageBox.Show(
                    e.Exception.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }
    }
}
