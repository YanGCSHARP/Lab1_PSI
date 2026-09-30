using ClosedXML.Excel;

namespace ExamTicketUI
{
    /// <summary>
    /// Список студентов из students.xlsx: один лист — одна группа,
    /// имя листа = имя группы. Строка 1 — заголовок, данные со строки 2:
    /// колонка A — фамилия, колонка B — имя.
    /// </summary>
    internal sealed class StudentRepository
    {
        private readonly List<string> _groups = new();
        private readonly Dictionary<string, List<Student>> _studentsByGroup = new();

        private StudentRepository()
        {
        }

        public IReadOnlyList<string> Groups => _groups;

        /// <summary>
        /// Студенты группы в порядке следования в листе. Для пустой или
        /// неизвестной группы возвращает пустой список, а не исключение.
        /// </summary>
        public IReadOnlyList<Student> GetStudents(string? group)
        {
            if (group is not null && _studentsByGroup.TryGetValue(group, out var students))
            {
                return students;
            }

            return Array.Empty<Student>();
        }

        public static StudentRepository Load(string path)
        {
            // FileShare.ReadWrite — чтобы список читался, даже если файл
            // в этот момент открыт в Excel.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(stream);

            var repository = new StudentRepository();
            foreach (var sheet in workbook.Worksheets)
            {
                var students = new List<Student>();
                var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
                for (var row = 2; row <= lastRow; row++)
                {
                    var lastName = sheet.Cell(row, 1).GetString().Trim();
                    var firstName = sheet.Cell(row, 2).GetString().Trim();
                    if (lastName.Length == 0 && firstName.Length == 0)
                    {
                        continue;
                    }

                    students.Add(new Student(lastName, firstName));
                }

                repository._groups.Add(sheet.Name);
                repository._studentsByGroup[sheet.Name] = students;
            }

            return repository;
        }
    }
}
