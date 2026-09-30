using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ExamTicketUI
{
    /// <summary>
    /// Одна строка документа. IsListItem — абзац оформлен в Word как
    /// элемент нумерованного списка (тогда номера «1.» в тексте нет).
    /// </summary>
    internal readonly record struct DocLine(string Text, bool IsListItem = false);

    /// <summary>
    /// Итог разбора: корректные билеты и список ошибок по пропущенным.
    /// </summary>
    internal sealed record TicketParseResult(IReadOnlyList<Ticket> Tickets, IReadOnlyList<string> Errors);

    /// <summary>
    /// Разбор tickets.docx. Заголовок билета — строка «Билет N»,
    /// вопросы — нумерованные абзацы «N. текст». Некорректные билеты
    /// пропускаются, причина попадает в Errors.
    /// </summary>
    internal static class TicketParser
    {
        public const int QuestionsPerTicket = 3;

        private static readonly Regex HeaderRegex = new(
            @"^Билет\s*№?\s*(\d+)\s*[.:]?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex HeaderLikeRegex = new(
            @"^Билет\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex QuestionRegex = new(@"^\d+\s*[.)]\s*(.+)$");

        public static TicketParseResult ParseFile(string path)
        {
            // FileShare.ReadWrite — чтобы билеты читались, даже если файл
            // в этот момент открыт в Word.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var document = WordprocessingDocument.Open(stream, false);

            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return new TicketParseResult(Array.Empty<Ticket>(), new[] { "Документ пуст." });
            }

            return Parse(ReadLines(body));
        }

        public static TicketParseResult Parse(IEnumerable<DocLine> lines)
        {
            var tickets = new List<Ticket>();
            var errors = new List<string>();

            // Состояние текущего блока: открыт ли он, распознан ли его
            // заголовок и какие вопросы уже набраны.
            var blockOpen = false;
            var headerValid = false;
            var number = 0;
            var questions = new List<string>();

            void Flush()
            {
                if (blockOpen && headerValid)
                {
                    if (questions.Count < QuestionsPerTicket)
                    {
                        errors.Add(
                            $"Билет {number} пропущен: вопросов {questions.Count}, нужно {QuestionsPerTicket}.");
                    }
                    else if (tickets.Any(ticket => ticket.Number == number))
                    {
                        errors.Add($"Билет {number} пропущен: такой номер уже встречался.");
                    }
                    else
                    {
                        if (questions.Count > QuestionsPerTicket)
                        {
                            errors.Add(
                                $"Билет {number}: вопросов {questions.Count}, " +
                                $"взяты первые {QuestionsPerTicket}.");
                        }

                        tickets.Add(new Ticket(number, questions.Take(QuestionsPerTicket).ToArray()));
                    }
                }

                blockOpen = false;
                questions = new List<string>();
            }

            foreach (var line in lines)
            {
                var text = line.Text.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                var header = HeaderRegex.Match(text);
                if (header.Success && int.TryParse(header.Groups[1].Value, out var parsedNumber))
                {
                    Flush();
                    blockOpen = true;
                    headerValid = true;
                    number = parsedNumber;
                    continue;
                }

                if (HeaderLikeRegex.IsMatch(text))
                {
                    // Блок всё равно открываем, чтобы его вопросы не
                    // приписались предыдущему билету.
                    Flush();
                    blockOpen = true;
                    headerValid = false;
                    errors.Add($"Заголовок билета не распознан: «{text}». Билет пропущен.");
                    continue;
                }

                if (!blockOpen || !headerValid)
                {
                    continue;
                }

                var question = QuestionRegex.Match(text);
                if (question.Success)
                {
                    questions.Add(question.Groups[1].Value.Trim());
                }
                else if (line.IsListItem)
                {
                    questions.Add(text);
                }
            }

            Flush();
            return new TicketParseResult(tickets, errors);
        }

        private static IEnumerable<DocLine> ReadLines(Body body)
        {
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var isListItem = paragraph.ParagraphProperties?.NumberingProperties is not null;

                // Мягкий перенос (Shift+Enter) внутри абзаца считаем
                // началом новой строки.
                var text = new StringBuilder();
                foreach (var element in paragraph.Descendants())
                {
                    switch (element)
                    {
                        case Text run:
                            text.Append(run.Text);
                            break;
                        case Break:
                        case CarriageReturn:
                            text.Append('\n');
                            break;
                        case TabChar:
                            text.Append(' ');
                            break;
                    }
                }

                foreach (var line in text.ToString().Split('\n'))
                {
                    yield return new DocLine(line, isListItem);
                }
            }
        }
    }
}
