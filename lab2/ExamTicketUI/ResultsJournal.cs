using ClosedXML.Excel;

namespace ExamTicketUI
{
    /// <summary>
    /// Журнал результатов results.xlsx. Строки только дописываются в
    /// конец, существующие не изменяются.
    /// </summary>
    internal sealed class ResultsJournal
    {
        public const string DefaultFileName = "results.xlsx";
        public const string RepeatYes = "да";
        public const string RepeatNo = "нет";

        private static readonly string[] Headers =
        {
            "Группа", "Фамилия", "Имя", "Номер билета", "Дата и время", "Повтор (да/нет)"
        };

        private readonly string _fileName;

        public ResultsJournal(string fileName = DefaultFileName)
        {
            _fileName = fileName;
        }

        public string FileName => _fileName;

        public void EnsureFileExists()
        {
            if (File.Exists(_fileName))
            {
                return;
            }

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Results");
            WriteHeaders(sheet);
            workbook.SaveAs(_fileName);
        }

        /// <summary>
        /// Назначает билет и сразу сохраняет запись. Если студент уже
        /// есть в журнале — берётся номер из самой первой его записи
        /// (повтор «да»), иначе билет выбирается случайно из
        /// <paramref name="tickets"/> (повтор «нет»).
        /// Бросает IOException, если файл занят другой программой —
        /// вызывающий код решает, повторять ли попытку.
        /// </summary>
        public Assignment Assign(string group, Student student, IReadOnlyList<Ticket> tickets, Random random)
        {
            EnsureFileExists();

            using var workbook = new XLWorkbook(_fileName);
            var sheet = workbook.Worksheet(1);
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow == 0)
            {
                WriteHeaders(sheet);
                lastRow = 1;
            }

            var firstTicket = FindFirstTicket(sheet, lastRow, group, student);
            if (firstTicket is null && tickets.Count == 0)
            {
                throw new InvalidOperationException("Нет ни одного билета для выдачи.");
            }

            var assignment = firstTicket is null
                ? new Assignment(tickets[random.Next(tickets.Count)].Number, IsRepeat: false)
                : new Assignment(firstTicket.Value, IsRepeat: true);

            var nextRow = lastRow + 1;
            sheet.Cell(nextRow, 1).Value = group;
            sheet.Cell(nextRow, 2).Value = student.LastName;
            sheet.Cell(nextRow, 3).Value = student.FirstName;
            sheet.Cell(nextRow, 4).Value = assignment.TicketNumber;
            sheet.Cell(nextRow, 5).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sheet.Cell(nextRow, 6).Value = assignment.IsRepeat ? RepeatYes : RepeatNo;

            workbook.Save();
            return assignment;
        }

        private static int? FindFirstTicket(IXLWorksheet sheet, int lastRow, string group, Student student)
        {
            for (var row = 2; row <= lastRow; row++)
            {
                if (Same(sheet.Cell(row, 1).GetString(), group) &&
                    Same(sheet.Cell(row, 2).GetString(), student.LastName) &&
                    Same(sheet.Cell(row, 3).GetString(), student.FirstName) &&
                    sheet.Cell(row, 4).TryGetValue<int>(out var ticketNumber))
                {
                    return ticketNumber;
                }
            }

            return null;
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteHeaders(IXLWorksheet sheet)
        {
            for (var column = 0; column < Headers.Length; column++)
            {
                sheet.Cell(1, column + 1).Value = Headers[column];
            }
        }
    }
}
