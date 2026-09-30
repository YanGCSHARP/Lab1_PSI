using ClosedXML.Excel;
using ExamTicketUI;
using Xunit;

namespace ExamTicketUI.Tests
{
    /// <summary>
    /// Каждый тест работает со своим временным файлом, чтобы тесты не
    /// мешали друг другу и не трогали настоящий results.xlsx.
    /// </summary>
    public sealed class ResultsJournalTests : IDisposable
    {
        private static readonly Student Ivanov = new("Иванов", "Пётр");
        private static readonly Student Petrov = new("Петров", "Иван");

        private static readonly IReadOnlyList<Ticket> Tickets = Enumerable.Range(1, 20)
            .Select(number => new Ticket(number, new[] { "А", "Б", "В" }))
            .ToArray();

        private readonly string _tempFilePath;
        private readonly ResultsJournal _journal;

        public ResultsJournalTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"results_test_{Guid.NewGuid():N}.xlsx");
            _journal = new ResultsJournal(_tempFilePath);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        private List<string[]> ReadRows()
        {
            using var workbook = new XLWorkbook(_tempFilePath);
            var sheet = workbook.Worksheet(1);
            var rows = new List<string[]>();
            for (var row = 1; row <= sheet.LastRowUsed()!.RowNumber(); row++)
            {
                rows.Add(Enumerable.Range(1, 6).Select(column => sheet.Cell(row, column).GetString()).ToArray());
            }

            return rows;
        }

        [Fact]
        public void EnsureFileExists_CreatesFileWithHeaderRow()
        {
            _journal.EnsureFileExists();

            var header = Assert.Single(ReadRows());
            Assert.Equal(
                new[] { "Группа", "Фамилия", "Имя", "Номер билета", "Дата и время", "Повтор (да/нет)" },
                header);
        }

        [Fact]
        public void Assign_NewStudent_GetsTicketFromListAndIsNotRepeat()
        {
            var assignment = _journal.Assign("ИС-21", Ivanov, Tickets, new Random(1));

            Assert.False(assignment.IsRepeat);
            Assert.InRange(assignment.TicketNumber, 1, Tickets.Count);

            var rows = ReadRows();
            Assert.Equal(2, rows.Count);
            Assert.Equal("ИС-21", rows[1][0]);
            Assert.Equal("Иванов", rows[1][1]);
            Assert.Equal("Пётр", rows[1][2]);
            Assert.Equal(assignment.TicketNumber.ToString(), rows[1][3]);
            Assert.False(string.IsNullOrEmpty(rows[1][4]));
            Assert.Equal("нет", rows[1][5]);
        }

        [Fact]
        public void Assign_SameStudentAgain_ReturnsSameTicket()
        {
            var random = new Random(7);
            var first = _journal.Assign("ИС-21", Ivanov, Tickets, random);

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var repeat = _journal.Assign("ИС-21", Ivanov, Tickets, random);

                Assert.True(repeat.IsRepeat);
                Assert.Equal(first.TicketNumber, repeat.TicketNumber);
            }
        }

        [Fact]
        public void Assign_Repeat_AppendsNewRowAndKeepsExistingRows()
        {
            var random = new Random(3);
            var first = _journal.Assign("ИС-21", Ivanov, Tickets, random);
            _journal.Assign("ИС-21", Petrov, Tickets, random);
            var before = ReadRows();

            _journal.Assign("ИС-21", Ivanov, Tickets, random);

            var after = ReadRows();
            Assert.Equal(before.Count + 1, after.Count);
            Assert.Equal(before, after.Take(before.Count));

            var last = after[^1];
            Assert.Equal("Иванов", last[1]);
            Assert.Equal(first.TicketNumber.ToString(), last[3]);
            Assert.Equal("да", last[5]);
        }

        [Fact]
        public void Assign_SameNameInAnotherGroup_IsDifferentStudent()
        {
            var random = new Random(5);
            _journal.Assign("ИС-21", Ivanov, Tickets, random);

            var other = _journal.Assign("ИС-22", Ivanov, Tickets, random);

            Assert.False(other.IsRepeat);
        }

        [Fact]
        public void Assign_PicksOnlyExistingTicketNumbers()
        {
            // Билет 2 пропущен при разборе — его номер выпасть не должен.
            var tickets = new[] { new Ticket(1, new[] { "А", "Б", "В" }), new Ticket(3, new[] { "Г", "Д", "Е" }) };
            var random = new Random(11);

            for (var index = 0; index < 30; index++)
            {
                var assignment = _journal.Assign("ИС-21", new Student($"Студент{index}", "Имя"), tickets, random);

                Assert.Contains(assignment.TicketNumber, new[] { 1, 3 });
            }
        }

        [Fact]
        public void Assign_ThrowsIOException_WhenFileIsLocked()
        {
            _journal.EnsureFileExists();

            using (new FileStream(_tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.ThrowsAny<IOException>(() => _journal.Assign("ИС-21", Ivanov, Tickets, new Random(1)));
            }

            // После снятия блокировки запись проходит, данные не потеряны.
            _journal.Assign("ИС-21", Ivanov, Tickets, new Random(1));
            Assert.Equal(2, ReadRows().Count);
        }
    }
}
