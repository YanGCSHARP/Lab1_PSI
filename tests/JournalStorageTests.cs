using System;
using System.IO;
using ClosedXML.Excel;
using ExamTicketGenerator;
using Xunit;

namespace ExamTicketGenerator.Tests
{
    /// <summary>
    /// Каждый тест работает со своим временным файлом, чтобы тесты не
    /// мешали друг другу и не трогали настоящий journal.xlsx.
    /// </summary>
    public sealed class JournalStorageTests : IDisposable
    {
        private readonly string _tempFilePath;

        public JournalStorageTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"journal_test_{Guid.NewGuid():N}.xlsx");
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [Fact]
        public void EnsureFileExists_CreatesFileWithHeaderRow()
        {
            var storage = new JournalStorage(_tempFilePath);

            storage.EnsureFileExists();

            Assert.True(File.Exists(_tempFilePath));
            using var workbook = new XLWorkbook(_tempFilePath);
            var sheet = workbook.Worksheet(1);
            Assert.Equal("Last name", sheet.Cell(1, 1).GetString());
            Assert.Equal("First name", sheet.Cell(1, 2).GetString());
            Assert.Equal("Номер билета", sheet.Cell(1, 3).GetString());
            Assert.Equal("Дата и время", sheet.Cell(1, 4).GetString());
        }

        [Fact]
        public void EnsureFileExists_DoesNotOverwriteExistingFile()
        {
            var storage = new JournalStorage(_tempFilePath);
            storage.EnsureFileExists();
            storage.AppendRecord("Existing", "Student", 5);

            // Повторный вызов не должен затронуть уже существующий файл.
            storage.EnsureFileExists();

            using var workbook = new XLWorkbook(_tempFilePath);
            var sheet = workbook.Worksheet(1);
            var lastRow = sheet.LastRowUsed()!.RowNumber();
            Assert.Equal(2, lastRow); // шапка + одна ранее добавленная запись
            Assert.Equal("Existing", sheet.Cell(2, 1).GetString());
        }

        [Fact]
        public void AppendRecord_AddsRowRightAfterHeader()
        {
            var storage = new JournalStorage(_tempFilePath);
            storage.EnsureFileExists();

            storage.AppendRecord("Ivanov", "Petr", 7);

            using var workbook = new XLWorkbook(_tempFilePath);
            var sheet = workbook.Worksheet(1);
            Assert.Equal(2, sheet.LastRowUsed()!.RowNumber());
            Assert.Equal("Ivanov", sheet.Cell(2, 1).GetString());
            Assert.Equal("Petr", sheet.Cell(2, 2).GetString());
            Assert.Equal(7, sheet.Cell(2, 3).GetValue<int>());
            Assert.False(string.IsNullOrEmpty(sheet.Cell(2, 4).GetString()));
        }

        [Fact]
        public void AppendRecord_KeepsPreviouslyWrittenRows()
        {
            var storage = new JournalStorage(_tempFilePath);
            storage.EnsureFileExists();

            storage.AppendRecord("Ivanov", "Petr", 7);
            storage.AppendRecord("Sidorov", "Ivan", 12);
            storage.AppendRecord("Petrova", "Anna", 1);

            using var workbook = new XLWorkbook(_tempFilePath);
            var sheet = workbook.Worksheet(1);
            Assert.Equal(4, sheet.LastRowUsed()!.RowNumber()); // шапка + 3 записи
            Assert.Equal("Ivanov", sheet.Cell(2, 1).GetString());
            Assert.Equal("Sidorov", sheet.Cell(3, 1).GetString());
            Assert.Equal("Petrova", sheet.Cell(4, 1).GetString());
        }
    }
}
