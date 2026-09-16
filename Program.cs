// Генератор экзаменационных билетов.
// Консольное приложение: запрашивает Ф.И. студента, генерирует номер
// билета (1..20) и дописывает запись в Excel-файл journal.xlsx.
//
// Зависимости (NuGet): ClosedXML
//     dotnet add package ClosedXML
//
// Сборка и запуск:
//     dotnet run
//
// Примечание по ESC:
//     Перед вводом фамилии считывается одна клавиша через
//     Console.ReadKey(intercept: true). Если это ESC — приложение
//     завершается. Если это любой другой символ — он используется как
//     первый символ фамилии, дальнейший ввод читается обычным
//     Console.ReadLine().

using System;
using System.IO;
using ClosedXML.Excel;

namespace ExamTicketGenerator
{
    /// <summary>
    /// Отвечает за чтение и запись journal.xlsx.
    /// </summary>
    internal sealed class JournalStorage
    {
        public const string DefaultFileName = "journal.xlsx";

        private static readonly string[] Headers =
        {
            "Last name", "First name", "Номер билета", "Дата и время"
        };

        private readonly string _fileName;

        public JournalStorage(string fileName = DefaultFileName)
        {
            _fileName = fileName;
        }

        public void EnsureFileExists()
        {
            if (File.Exists(_fileName))
            {
                return;
            }

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Journal");
            for (var column = 0; column < Headers.Length; column++)
            {
                sheet.Cell(1, column + 1).Value = Headers[column];
            }

            workbook.SaveAs(_fileName);
        }

        /// <summary>
        /// Дописывает одну запись и сразу сохраняет файл. Если файл
        /// занят другим процессом (например, открыт в Excel), просит
        /// пользователя закрыть его и повторяет попытку, не завершаясь
        /// с ошибкой.
        /// </summary>
        public void AppendRecord(string lastName, string firstName, int ticketNumber)
        {
            while (true)
            {
                try
                {
                    using var workbook = new XLWorkbook(_fileName);
                    var sheet = workbook.Worksheet(1);
                    var nextRow = sheet.LastRowUsed()!.RowNumber() + 1;

                    sheet.Cell(nextRow, 1).Value = lastName;
                    sheet.Cell(nextRow, 2).Value = firstName;
                    sheet.Cell(nextRow, 3).Value = ticketNumber;
                    sheet.Cell(nextRow, 4).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    workbook.Save();
                    return;
                }
                catch (IOException)
                {
                    Console.WriteLine(
                        $"Не удаётся записать в файл {_fileName} — похоже, он открыт " +
                        "в Excel или другой программе. Закройте файл и нажмите Enter, " +
                        "чтобы повторить попытку.");
                    Console.ReadLine();
                }
            }
        }
    }

    /// <summary>
    /// Чистая логика проверки ввода фамилии/имени — без обращения к
    /// консоли, поэтому легко покрывается юнит-тестами.
    /// </summary>
    internal static class NameValidator
    {
        /// <summary>
        /// Обрезает пробелы по краям. Возвращает null, если после
        /// обрезки строка пустая (значит ввод не принят).
        /// </summary>
        public static string? Normalize(string? raw)
        {
            var trimmed = (raw ?? string.Empty).Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }

    /// <summary>
    /// Чистая логика генерации номера билета — без обращения к
    /// консоли, поэтому легко покрывается юнит-тестами.
    /// </summary>
    internal static class TicketNumberGenerator
    {
        public const int MinTicket = 1;
        public const int MaxTicket = 20;

        public static int Generate(Random random)
        {
            return random.Next(MinTicket, MaxTicket + 1);
        }
    }

    /// <summary>
    /// Данные одного студента, введённые в консоли.
    /// </summary>
    internal readonly struct StudentEntry
    {
        public StudentEntry(string lastName, string firstName)
        {
            LastName = lastName;
            FirstName = firstName;
        }

        public string LastName { get; }
        public string FirstName { get; }
    }

    /// <summary>
    /// Отвечает за консольный ввод с проверкой ESC и пустых строк.
    /// </summary>
    internal sealed class StudentInputReader
    {
        /// <summary>
        /// Считывает фамилию и имя. Возвращает null, если пользователь
        /// нажал ESC в момент ожидания ввода фамилии.
        /// </summary>
        public StudentEntry? ReadNext()
        {
            var lastName = ReadLastName();
            if (lastName is null)
            {
                return null;
            }

            var firstName = ReadNonEmptyLine("First name: ", "Имя не может быть пустым, повторите ввод.");
            return new StudentEntry(lastName, firstName);
        }

        private static string? ReadLastName()
        {
            while (true)
            {
                Console.Write("Last name: ");

                var firstKey = Console.ReadKey(intercept: true);
                if (firstKey.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return null;
                }

                Console.Write(firstKey.KeyChar);
                var rest = Console.ReadLine() ?? string.Empty;
                var lastName = NameValidator.Normalize(firstKey.KeyChar + rest);

                if (lastName is null)
                {
                    Console.WriteLine("Фамилия не может быть пустой, повторите ввод.");
                    continue;
                }

                return lastName;
            }
        }

        private static string ReadNonEmptyLine(string prompt, string emptyMessage)
        {
            while (true)
            {
                Console.Write(prompt);
                var value = NameValidator.Normalize(Console.ReadLine());
                if (value is null)
                {
                    Console.WriteLine(emptyMessage);
                    continue;
                }

                return value;
            }
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            var storage = new JournalStorage();
            var inputReader = new StudentInputReader();
            var random = new Random();

            storage.EnsureFileExists();
            Console.WriteLine("Для выхода нажмите ESC.");

            while (true)
            {
                var student = inputReader.ReadNext();
                if (student is null)
                {
                    Console.WriteLine("Работа приложения завершена.");
                    break;
                }

                var ticketNumber = TicketNumberGenerator.Generate(random);
                Console.WriteLine($"Билет № {ticketNumber}");

                storage.AppendRecord(student.Value.LastName, student.Value.FirstName, ticketNumber);
            }
        }
    }
}