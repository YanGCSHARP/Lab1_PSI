namespace ExamTicketUI
{
    /// <summary>
    /// Студент из students.xlsx. ToString используется выпадающим
    /// списком, поэтому возвращает «Фамилия Имя».
    /// </summary>
    internal sealed record Student(string LastName, string FirstName)
    {
        public string FullName => $"{LastName} {FirstName}".Trim();

        public override string ToString() => FullName;
    }

    /// <summary>
    /// Билет из tickets.docx: номер и ровно три вопроса.
    /// </summary>
    internal sealed record Ticket(int Number, IReadOnlyList<string> Questions);

    /// <summary>
    /// Результат назначения билета студенту.
    /// </summary>
    internal sealed record Assignment(int TicketNumber, bool IsRepeat);
}
