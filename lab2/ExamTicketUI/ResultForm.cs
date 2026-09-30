namespace ExamTicketUI
{
    /// <summary>
    /// Окно результата: номер билета и три вопроса. Закрывается по ESC
    /// (кнопка «Закрыть» назначена CancelButton), главное окно при этом
    /// остаётся открытым.
    /// </summary>
    internal sealed class ResultForm : Form
    {
        private const int ContentWidth = 520;

        public ResultForm(string group, Student student, Assignment assignment, Ticket? ticket)
        {
            Text = "Результат";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var layout = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                Dock = DockStyle.Fill
            };

            layout.Controls.Add(CreateLabel($"{student.FullName}, группа {group}", Font));
            layout.Controls.Add(CreateLabel(
                $"Ваш билет № {assignment.TicketNumber}",
                new Font(Font.FontFamily, Font.Size * 1.8f, FontStyle.Bold)));

            if (assignment.IsRepeat)
            {
                layout.Controls.Add(CreateLabel(
                    "Билет уже был выдан этому студенту ранее — номер сохранён.",
                    new Font(Font, FontStyle.Italic)));
            }

            if (ticket is null)
            {
                layout.Controls.Add(CreateLabel(
                    $"Вопросы билета № {assignment.TicketNumber} не найдены в {AppPaths.TicketsFileName}.",
                    Font));
            }
            else
            {
                for (var index = 0; index < ticket.Questions.Count; index++)
                {
                    layout.Controls.Add(CreateLabel($"{index + 1}. {ticket.Questions[index]}", Font));
                }
            }

            var closeButton = new Button
            {
                Text = "Закрыть (Esc)",
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(3, 12, 3, 3),
                DialogResult = DialogResult.Cancel
            };
            layout.Controls.Add(closeButton);

            Controls.Add(layout);
            CancelButton = closeButton;
            AcceptButton = closeButton;
        }

        private static Label CreateLabel(string text, Font font)
        {
            return new Label
            {
                Text = text,
                Font = font,
                AutoSize = true,
                MaximumSize = new Size(ContentWidth, 0),
                Margin = new Padding(3, 3, 3, 9)
            };
        }
    }
}
