using ClosedXML.Excel;
using ExamTicketUI;
using Xunit;

namespace ExamTicketUI.Tests
{
    public sealed class StudentRepositoryTests : IDisposable
    {
        private readonly string _tempFilePath;

        public StudentRepositoryTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"students_test_{Guid.NewGuid():N}.xlsx");

            using var workbook = new XLWorkbook();
            AddGroup(workbook, "ИС-21", ("Иванов", "Пётр"), ("Сидорова", "Анна"));
            AddGroup(workbook, "ИС-22", ("Петров", "Иван"));
            AddGroup(workbook, "Пустая");
            workbook.SaveAs(_tempFilePath);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        private static void AddGroup(XLWorkbook workbook, string name, params (string Last, string First)[] students)
        {
            var sheet = workbook.Worksheets.Add(name);
            sheet.Cell(1, 1).Value = "Фамилия";
            sheet.Cell(1, 2).Value = "Имя";
            for (var index = 0; index < students.Length; index++)
            {
                sheet.Cell(index + 2, 1).Value = students[index].Last;
                sheet.Cell(index + 2, 2).Value = students[index].First;
            }
        }

        [Fact]
        public void Load_GroupsAreSheetNames()
        {
            var repository = StudentRepository.Load(_tempFilePath);

            Assert.Equal(new[] { "ИС-21", "ИС-22", "Пустая" }, repository.Groups);
        }

        [Fact]
        public void GetStudents_ReturnsStudentsOfSelectedGroupOnly()
        {
            var repository = StudentRepository.Load(_tempFilePath);

            var students = repository.GetStudents("ИС-21");

            Assert.Equal(
                new[] { new Student("Иванов", "Пётр"), new Student("Сидорова", "Анна") },
                students);
            Assert.Equal("Иванов Пётр", students[0].ToString());
        }

        [Fact]
        public void GetStudents_ReturnsEmptyList_ForGroupWithoutStudents()
        {
            var repository = StudentRepository.Load(_tempFilePath);

            Assert.Empty(repository.GetStudents("Пустая"));
        }

        [Theory]
        [InlineData("Нет такой группы")]
        [InlineData(null)]
        public void GetStudents_ReturnsEmptyList_ForUnknownGroup(string? group)
        {
            var repository = StudentRepository.Load(_tempFilePath);

            Assert.Empty(repository.GetStudents(group));
        }
    }
}
