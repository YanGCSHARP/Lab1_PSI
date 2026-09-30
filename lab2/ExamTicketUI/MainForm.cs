namespace ExamTicketUI
{
    /// <summary>
    /// Главное окно: выбор группы и студента, кнопка генерации билета.
    /// </summary>
    internal sealed class MainForm : Form
    {
        private const int FieldWidth = 320;

        private readonly ComboBox _groupBox;
        private readonly ComboBox _studentBox;
        private readonly Button _generateButton;
        private readonly Button _reloadButton;
        private readonly Label _statusLabel;

        private readonly ResultsJournal _journal = new(AppPaths.ResultsFile);
        private readonly Random _random = new();

        private StudentRepository? _students;
        private IReadOnlyList<Ticket> _tickets = Array.Empty<Ticket>();

        public MainForm()
        {
            Text = "Генератор экзаменационных билетов";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _groupBox = CreateComboBox();
            _studentBox = CreateComboBox();
            _generateButton = new Button
            {
                Text = "Сгенерировать билет",
                AutoSize = true,
                Enabled = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(0, 4, 0, 4),
                Margin = new Padding(3, 12, 3, 3)
            };
            _reloadButton = new Button
            {
                Text = "Перечитать файлы",
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _statusLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(FieldWidth, 0),
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(3, 12, 3, 3)
            };

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(CreateCaption("Группа:"), 0, 0);
            layout.Controls.Add(_groupBox, 1, 0);
            layout.Controls.Add(CreateCaption("Студент:"), 0, 1);
            layout.Controls.Add(_studentBox, 1, 1);
            layout.Controls.Add(_generateButton, 1, 2);
            layout.Controls.Add(_reloadButton, 1, 3);
            layout.Controls.Add(_statusLabel, 1, 4);
            Controls.Add(layout);

            _groupBox.SelectedIndexChanged += (_, _) => FillStudents();
            _studentBox.SelectedIndexChanged += (_, _) => UpdateGenerateButton();
            _generateButton.Click += (_, _) => GenerateTicket();
            _reloadButton.Click += (_, _) => LoadData();

            // Shown, а не конструктор: сообщения о проблемах с файлами
            // должны появляться поверх уже видимого окна.
            Shown += (_, _) => LoadData();
        }

        private static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = FieldWidth,
                Margin = new Padding(3, 3, 3, 9)
            };
        }

        private static Label CreateCaption(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 0, 9, 6)
            };
        }

        /// <summary>
        /// Читает students.xlsx и tickets.docx, создаёт results.xlsx.
        /// Любая проблема с файлами превращается в сообщение, а не в
        /// падение приложения.
        /// </summary>
        private void LoadData()
        {
            var problems = new List<string>();

            _students = LoadStudents(problems);
            _tickets = LoadTickets(problems, out var skipped);

            try
            {
                _journal.EnsureFileExists();
            }
            catch (Exception ex)
            {
                problems.Add($"Не удалось создать файл журнала {_journal.FileName}: {ex.Message}");
            }

            _groupBox.Items.Clear();
            _studentBox.Items.Clear();
            if (_students is not null)
            {
                _groupBox.Items.AddRange(_students.Groups.Cast<object>().ToArray());
            }

            UpdateGenerateButton();

            _statusLabel.Text =
                $"Групп: {_groupBox.Items.Count}, билетов: {_tickets.Count}" +
                (skipped > 0 ? $"\nЗамечаний при разборе билетов: {skipped} (см. {AppPaths.LogFileName})" : string.Empty) +
                $"\nПапка с данными: {AppPaths.DataDirectory}";

            if (problems.Count > 0)
            {
                foreach (var problem in problems)
                {
                    AppLog.Write(problem);
                }

                MessageBox.Show(
                    this,
                    string.Join(Environment.NewLine + Environment.NewLine, problems) +
                    Environment.NewLine + Environment.NewLine +
                    "Исправьте файлы и нажмите «Перечитать файлы».",
                    "Проблема с исходными файлами",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static StudentRepository? LoadStudents(List<string> problems)
        {
            if (!File.Exists(AppPaths.StudentsFile))
            {
                problems.Add($"Не найден файл со списком студентов: {AppPaths.StudentsFile}");
                return null;
            }

            try
            {
                return StudentRepository.Load(AppPaths.StudentsFile);
            }
            catch (Exception ex)
            {
                problems.Add($"Не удалось прочитать {AppPaths.StudentsFile}: {ex.Message}");
                return null;
            }
        }

        private static IReadOnlyList<Ticket> LoadTickets(List<string> problems, out int skipped)
        {
            skipped = 0;
            if (!File.Exists(AppPaths.TicketsFile))
            {
                problems.Add($"Не найден файл с билетами: {AppPaths.TicketsFile}");
                return Array.Empty<Ticket>();
            }

            try
            {
                var result = TicketParser.ParseFile(AppPaths.TicketsFile);
                foreach (var error in result.Errors)
                {
                    AppLog.Write($"{AppPaths.TicketsFileName}: {error}");
                }

                skipped = result.Errors.Count;
                if (result.Tickets.Count == 0)
                {
                    problems.Add($"В файле {AppPaths.TicketsFile} нет ни одного корректного билета.");
                }

                return result.Tickets;
            }
            catch (Exception ex)
            {
                problems.Add($"Не удалось прочитать {AppPaths.TicketsFile}: {ex.Message}");
                return Array.Empty<Ticket>();
            }
        }

        private void FillStudents()
        {
            _studentBox.Items.Clear();

            var students = _students?.GetStudents(_groupBox.SelectedItem as string);
            if (students is not null)
            {
                _studentBox.Items.AddRange(students.Cast<object>().ToArray());
            }

            UpdateGenerateButton();
        }

        private void UpdateGenerateButton()
        {
            _generateButton.Enabled =
                _groupBox.SelectedItem is not null &&
                _studentBox.SelectedItem is not null &&
                _tickets.Count > 0;
        }

        private void GenerateTicket()
        {
            if (_groupBox.SelectedItem is not string group || _studentBox.SelectedItem is not Student student)
            {
                return;
            }

            var assignment = AssignWithRetry(group, student);
            if (assignment is null)
            {
                return;
            }

            var ticket = _tickets.FirstOrDefault(item => item.Number == assignment.TicketNumber);
            using (var resultForm = new ResultForm(group, student, assignment, ticket))
            {
                resultForm.ShowDialog(this);
            }

            // Возврат к выбору: готовы к следующему студенту.
            _studentBox.SelectedIndex = -1;
            UpdateGenerateButton();
        }

        /// <summary>
        /// Запись в журнал происходит сразу при генерации. Если файл
        /// занят (открыт в Excel), предлагает закрыть его и повторить.
        /// Возвращает null, если пользователь отказался от повтора.
        /// </summary>
        private Assignment? AssignWithRetry(string group, Student student)
        {
            while (true)
            {
                try
                {
                    return _journal.Assign(group, student, _tickets, _random);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    var answer = MessageBox.Show(
                        this,
                        $"Не удаётся записать в файл {_journal.FileName} — похоже, он открыт " +
                        "в Excel или другой программе." + Environment.NewLine + Environment.NewLine +
                        "Закройте файл и нажмите «Повтор».",
                        "Файл журнала занят",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Warning);
                    if (answer != DialogResult.Retry)
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Write($"Ошибка записи в журнал: {ex}");
                    MessageBox.Show(
                        this,
                        $"Не удалось записать результат в {_journal.FileName}: {ex.Message}",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return null;
                }
            }
        }
    }
}
