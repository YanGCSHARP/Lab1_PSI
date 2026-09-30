using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTicketUI;
using Xunit;

namespace ExamTicketUI.Tests
{
    public sealed class TicketParserTests : IDisposable
    {
        private readonly string _tempFilePath;

        public TicketParserTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"tickets_test_{Guid.NewGuid():N}.docx");
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        private static IEnumerable<DocLine> Lines(params string[] lines)
        {
            return lines.Select(line => new DocLine(line));
        }

        private void WriteDocx(params string[] paragraphs)
        {
            using var document = WordprocessingDocument.Create(_tempFilePath, WordprocessingDocumentType.Document);
            var body = new Body();
            foreach (var paragraph in paragraphs)
            {
                body.AppendChild(new Paragraph(new Run(new Text(paragraph))));
            }

            document.AddMainDocumentPart().Document = new Document(body);
        }

        [Fact]
        public void ParseFile_ReadsTicketsAndQuestionsFromWord()
        {
            WriteDocx(
                "Билет 1",
                "1. Вопрос первый",
                "2. Вопрос второй",
                "3. Вопрос третий",
                "",
                "Билет 2",
                "1. Четвёртый",
                "2. Пятый",
                "3. Шестой");

            var result = TicketParser.ParseFile(_tempFilePath);

            Assert.Empty(result.Errors);
            Assert.Equal(new[] { 1, 2 }, result.Tickets.Select(ticket => ticket.Number));
            Assert.Equal(
                new[] { "Вопрос первый", "Вопрос второй", "Вопрос третий" },
                result.Tickets[0].Questions);
            Assert.Equal(new[] { "Четвёртый", "Пятый", "Шестой" }, result.Tickets[1].Questions);
        }

        [Fact]
        public void Parse_SkipsTicketWithFewerThanThreeQuestions()
        {
            var result = TicketParser.Parse(Lines(
                "Билет 1", "1. А", "2. Б",
                "Билет 2", "1. В", "2. Г", "3. Д"));

            var ticket = Assert.Single(result.Tickets);
            Assert.Equal(2, ticket.Number);
            var error = Assert.Single(result.Errors);
            Assert.Contains("Билет 1", error);
        }

        [Fact]
        public void Parse_SkipsTicketWithUnrecognizedHeader()
        {
            var result = TicketParser.Parse(Lines(
                "Билет 1", "1. А", "2. Б", "3. В",
                "Билет номер два", "1. Г", "2. Д", "3. Е",
                "Билет 3", "1. Ж", "2. З", "3. И"));

            Assert.Equal(new[] { 1, 3 }, result.Tickets.Select(ticket => ticket.Number));
            // Вопросы нераспознанного билета не приписались билету 1.
            Assert.Equal(new[] { "А", "Б", "В" }, result.Tickets[0].Questions);
            Assert.Single(result.Errors);
        }

        [Fact]
        public void Parse_SkipsDuplicateTicketNumber()
        {
            var result = TicketParser.Parse(Lines(
                "Билет 1", "1. А", "2. Б", "3. В",
                "Билет 1", "1. Г", "2. Д", "3. Е"));

            var ticket = Assert.Single(result.Tickets);
            Assert.Equal(new[] { "А", "Б", "В" }, ticket.Questions);
            Assert.Single(result.Errors);
        }

        [Fact]
        public void Parse_AcceptsWordListItemsWithoutNumberInText()
        {
            var result = TicketParser.Parse(new[]
            {
                new DocLine("Билет 5"),
                new DocLine("Первый", IsListItem: true),
                new DocLine("Второй", IsListItem: true),
                new DocLine("Третий", IsListItem: true)
            });

            var ticket = Assert.Single(result.Tickets);
            Assert.Equal(5, ticket.Number);
            Assert.Equal(new[] { "Первый", "Второй", "Третий" }, ticket.Questions);
        }

        [Fact]
        public void Parse_IgnoresTextOutsideTickets()
        {
            var result = TicketParser.Parse(Lines(
                "Экзаменационные билеты", "1. Не вопрос",
                "Билет 1", "1. А", "2. Б", "3. В"));

            var ticket = Assert.Single(result.Tickets);
            Assert.Equal(new[] { "А", "Б", "В" }, ticket.Questions);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Parse_ReturnsNoTickets_ForEmptyInput()
        {
            var result = TicketParser.Parse(Lines());

            Assert.Empty(result.Tickets);
            Assert.Empty(result.Errors);
        }
    }
}
