using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Form1204;

/// <summary>
/// Главная форма: загрузка CSV, фильтр по аудитории, таблица с цветовой разметкой.
/// </summary>
public class MainForm : Form
{
    // ── цвета тегов ──────────────────────────────────────────────────────────
    private static readonly Color ColorOk       = Color.FromArgb(0xD8, 0xFF, 0xD8);
    private static readonly Color ColorNotFound = Color.FromArgb(0xFF, 0xD8, 0xD8);
    private static readonly Color ColorWrongAud = Color.FromArgb(0xFF, 0xF4, 0xB3);
    private static readonly Color ColorLong15   = Color.FromArgb(0xFF, 0xE0, 0x8A);
    private static readonly Color ColorLong30   = Color.FromArgb(0xFF, 0x9D, 0x9D);

    // ── контролы ─────────────────────────────────────────────────────────────
    private readonly Label    _lblInfo      = new();
    private readonly Label    _lblCsv       = new() { Text = "CSV не найден" };
    private readonly Label    _lblOk        = new() { Text = "OK: 0" };
    private readonly Label    _lblErr       = new() { Text = "Ошибок: 0" };
    private readonly Label    _lblLong      = new() { Text = "Долгих выходов: 0" };
    private readonly ComboBox _cmbAud       = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ListView _list         = new();

    // ── данные ───────────────────────────────────────────────────────────────
    private List<Dictionary<string, string>> _csvRows = [];
    private readonly Dictionary<string, int> _totalMinutes = [];

    public MainForm()
    {
        Text          = $"Проверка формы 12-04 | {DbHelper.Server} | {DbHelper.Database}";
        ClientSize    = new Size(1400, 800);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
    }

    // ── построение интерфейса ────────────────────────────────────────────────

    private void BuildLayout()
    {
        var top = new FlowLayoutPanel
        {
            Dock      = DockStyle.Top,
            AutoSize  = true,
            FlowDirection = FlowDirection.TopDown,
            Padding   = new Padding(8, 4, 8, 4)
        };

        _lblInfo.Text      = "Изображение: - | Страница: - | Суммарно: 0 мин";
        _lblInfo.AutoSize  = true;

        var btnLoad = new Button { Text = "Загрузить последний CSV", AutoSize = true };
        btnLoad.Click += (_, _) => LoadLastCsv();

        var btnChangeDb = new Button { Text = "Сменить БД", AutoSize = true };
        btnChangeDb.Click += (_, _) => ChangeDb();

        _lblCsv.AutoSize  = true;
        _lblOk.AutoSize   = true;
        _lblErr.AutoSize  = true;
        _lblLong.AutoSize = true;

        _cmbAud.Width = 200;
        _cmbAud.SelectedIndexChanged += (_, _) => ShowAuditory();

        top.Controls.AddRange([
            _lblInfo, btnLoad, btnChangeDb, _lblCsv,
            _cmbAud, _lblOk, _lblErr, _lblLong
        ]);

        Controls.Add(top);

        BuildListView();
    }

    private void BuildListView()
    {
        _list.Dock        = DockStyle.Fill;
        _list.View        = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines   = true;
        _list.VirtualMode = false;
        _list.OwnerDraw   = true;

        var columns = new (string Key, string Header, int Width, HorizontalAlignment Align)[]
        {
            ("image",   "Изобр.",   80,  HorizontalAlignment.Center),
            ("page",    "Стр",      50,  HorizontalAlignment.Center),
            ("pos",     "Поз",      50,  HorizontalAlignment.Center),
            ("barcode", "Штрихкод", 180, HorizontalAlignment.Left),
            ("fio",     "ФИО",      350, HorizontalAlignment.Left),
            ("out",     "Выход",    80,  HorizontalAlignment.Center),
            ("in",      "Вход",     80,  HorizontalAlignment.Center),
            ("minutes", "Мин",      70,  HorizontalAlignment.Center),
            ("db_aud",  "Ауд.БД",   80,  HorizontalAlignment.Center),
            ("status",  "Статус",   150, HorizontalAlignment.Left),
        };

        foreach (var (_, header, width, align) in columns)
            _list.Columns.Add(header, width, align);

        // цветовая разметка строк
        _list.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        _list.DrawItem         += (_, e) => e.DrawDefault = true;
        _list.DrawSubItem      += OnDrawSubItem;

        _list.DoubleClick      += OnDoubleClick;
        _list.SelectedIndexChanged += OnSelectionChanged;

        Controls.Add(_list);
    }

    // ── цветовая разметка ────────────────────────────────────────────────────

    private static Color TagColor(RowTag tag) => tag switch
    {
        RowTag.Ok       => ColorOk,
        RowTag.NotFound => ColorNotFound,
        RowTag.WrongAud => ColorWrongAud,
        RowTag.Long15   => ColorLong15,
        RowTag.Long30   => ColorLong30,
        _               => SystemColors.Window
    };

    private void OnDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item?.Tag is not RowTag tag)
        {
            e.DrawDefault = true;
            return;
        }

        using var brush = new SolidBrush(TagColor(tag));
        e.Graphics.FillRectangle(brush, e.Bounds);

        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
        if (e.Header?.TextAlign == HorizontalAlignment.Center)
            flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;

        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem?.Text ?? "",
            _list.Font,
            e.Bounds,
            SystemColors.WindowText,
            flags);
    }

    // ── обработчики событий ──────────────────────────────────────────────────

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0) return;

        var barcode = _list.SelectedItems[0].SubItems[3].Text;
        Clipboard.SetText(barcode);
        Text = $"Скопировано: {barcode}";
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0) return;

        var item    = _list.SelectedItems[0];
        var imageNo = item.SubItems[0].Text;
        var page    = item.SubItems[1].Text;
        var barcode = item.SubItems[3].Text;
        var total   = _totalMinutes.GetValueOrDefault(barcode, 0);

        _lblInfo.Text = $"Изображение: {imageNo} | Страница: {page} | Суммарно: {total} мин";
    }

    // ── логика загрузки ──────────────────────────────────────────────────────

    private void LoadLastCsv()
    {
        var path = CsvLoader.FindLatestCsv();
        if (path is null)
        {
            MessageBox.Show("CSV не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _csvRows = CsvLoader.Load(path);
        _lblCsv.Text = path;

        var auds = _csvRows
            .Select(r => r.GetValueOrDefault("Аудитория", ""))
            .Where(a => a.Length > 0)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        _cmbAud.Items.Clear();
        _cmbAud.Items.AddRange(auds.Cast<object>().ToArray());

        if (auds.Count > 0)
        {
            _cmbAud.SelectedIndex = 0;
            ShowAuditory();
        }
    }

    private void ShowAuditory()
    {
        _list.Items.Clear();
        _totalMinutes.Clear();

        var aud = _cmbAud.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(aud)) return;

        try
        {
            using var conn = DbHelper.Connect();
            conn.Open();

            var pages = _csvRows.Where(r => r.GetValueOrDefault("Аудитория") == aud);

            foreach (var row in pages)
            {
                var page    = row.GetValueOrDefault("Страница", "");
                var imageNo = row.GetValueOrDefault("Номер_изображения", "");

                // собираем номера позиций из заголовков «Бланк_регистрации_NN»
                var positions = row.Keys
                    .Select(k => Regex.Match(k, @"^Бланк_регистрации_(\d+)$"))
                    .Where(m => m.Success)
                    .Select(m => int.Parse(m.Groups[1].Value))
                    .ToList();

                if (positions.Count == 0) continue;

                for (int i = 1; i <= positions.Max(); i++)
                {
                    var key     = $"Бланк_регистрации_{i:D2}";
                    var barcode = row.GetValueOrDefault(key, "").Trim();
                    if (barcode.Length == 0) continue;

                    var person = DbHelper.GetPersonByBarcode(conn, barcode);

                    string fio    = "";
                    string dbAud  = "";
                    string status = "НЕ НАЙДЕН";

                    if (person is not null)
                    {
                        fio   = person.Fio;
                        dbAud = person.Aud;
                        status = dbAud.PadLeft(4, '0') == aud ? "OK" : "ДРУГАЯ АУДИТОРИЯ";
                    }

                    var outTime = row.GetValueOrDefault($"Время_выхода_{i:D2}", "");
                    var inTime  = row.GetValueOrDefault($"Время_входа_{i:D2}", "");
                    var minutes = DbHelper.CalcMinutes(outTime, inTime);

                    _totalMinutes.TryAdd(barcode, 0);
                    _totalMinutes[barcode] += minutes;

                    RowTag tag;
                    if (minutes >= 30)
                        tag = RowTag.Long30;
                    else if (minutes >= 15)
                        tag = RowTag.Long15;
                    else if (status == "OK")
                        tag = RowTag.Ok;
                    else if (status == "ДРУГАЯ АУДИТОРИЯ")
                        tag = RowTag.WrongAud;
                    else
                        tag = RowTag.NotFound;

                    var lvi = new ListViewItem(imageNo) { Tag = tag };
                    lvi.SubItems.AddRange([
                        page, $"{i:D2}", barcode, fio,
                        outTime, inTime, minutes.ToString(),
                        dbAud, status
                    ]);

                    _list.Items.Add(lvi);
                }
            }

            UpdateCounters();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateCounters()
    {
        int ok = 0, err = 0, longOut = 0;

        foreach (ListViewItem item in _list.Items)
        {
            if (item.Tag is not RowTag tag) continue;
            switch (tag)
            {
                case RowTag.Ok:       ok++;      break;
                case RowTag.NotFound:
                case RowTag.WrongAud: err++;     break;
                case RowTag.Long15:
                case RowTag.Long30:   longOut++; break;
            }
        }

        _lblOk.Text   = $"OK: {ok}";
        _lblErr.Text  = $"Ошибок: {err}";
        _lblLong.Text = $"Долгих выходов: {longOut}";
    }

    private void ChangeDb()
    {
        Hide();
        var login = new LoginForm();
        login.FormClosed += (_, _) => Close();
        login.Show();
    }
}
