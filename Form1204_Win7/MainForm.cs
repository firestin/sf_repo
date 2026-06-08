using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Form1204
{
    public class MainForm : Form
    {
        private static readonly Color ColorOk       = Color.FromArgb(0xD8, 0xFF, 0xD8);
        private static readonly Color ColorNotFound = Color.FromArgb(0xFF, 0xD8, 0xD8);
        private static readonly Color ColorWrongAud = Color.FromArgb(0xFF, 0xF4, 0xB3);
        private static readonly Color ColorLong15   = Color.FromArgb(0xFF, 0xE0, 0x8A);
        private static readonly Color ColorLong30   = Color.FromArgb(0xFF, 0x9D, 0x9D);

        private readonly Label    _lblInfo  = new Label();
        private readonly Label    _lblCsv   = new Label { Text = "CSV не найден" };
        private readonly Label    _lblOk    = new Label { Text = "OK: 0" };
        private readonly Label    _lblErr   = new Label { Text = "Ошибок: 0" };
        private readonly Label    _lblLong  = new Label { Text = "Долгих выходов: 0" };
        private readonly ComboBox _cmbAud   = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ListView _list     = new ListView();

        private List<Dictionary<string, string>> _csvRows = new List<Dictionary<string, string>>();
        private readonly Dictionary<string, int> _totalMinutes = new Dictionary<string, int>();

        public MainForm()
        {
            Text          = string.Format("Проверка формы 12-04 | {0} | {1}", DbHelper.Server, DbHelper.Database);
            ClientSize    = new Size(1400, 800);
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
        }

        private void BuildLayout()
        {
            var top = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                AutoSize      = true,
                FlowDirection = FlowDirection.TopDown,
                Padding       = new Padding(8, 4, 8, 4)
            };

            _lblInfo.Text    = "Изображение: - | Страница: - | Суммарно: 0 мин";
            _lblInfo.AutoSize = true;

            var btnLoad = new Button { Text = "Загрузить последний CSV", AutoSize = true };
            btnLoad.Click += (s, e) => LoadLastCsv();

            var btnChangeDb = new Button { Text = "Сменить БД", AutoSize = true };
            btnChangeDb.Click += (s, e) => ChangeDb();

            _lblCsv.AutoSize  = true;
            _lblOk.AutoSize   = true;
            _lblErr.AutoSize  = true;
            _lblLong.AutoSize = true;

            _cmbAud.Width = 200;
            _cmbAud.SelectedIndexChanged += (s, e) => ShowAuditory();

            top.Controls.Add(_lblInfo);
            top.Controls.Add(btnLoad);
            top.Controls.Add(btnChangeDb);
            top.Controls.Add(_lblCsv);
            top.Controls.Add(_cmbAud);
            top.Controls.Add(_lblOk);
            top.Controls.Add(_lblErr);
            top.Controls.Add(_lblLong);

            Controls.Add(top);
            BuildListView();
        }

        private void BuildListView()
        {
            _list.Dock         = DockStyle.Fill;
            _list.View         = View.Details;
            _list.FullRowSelect = true;
            _list.GridLines    = true;
            _list.OwnerDraw    = true;

            var columns = new (string header, int width, HorizontalAlignment align)[]
            {
                ("Изобр.",   80,  HorizontalAlignment.Center),
                ("Стр",      50,  HorizontalAlignment.Center),
                ("Поз",      50,  HorizontalAlignment.Center),
                ("Штрихкод", 180, HorizontalAlignment.Left),
                ("ФИО",      350, HorizontalAlignment.Left),
                ("Выход",    80,  HorizontalAlignment.Center),
                ("Вход",     80,  HorizontalAlignment.Center),
                ("Мин",      70,  HorizontalAlignment.Center),
                ("Ауд.БД",   80,  HorizontalAlignment.Center),
                ("Статус",   150, HorizontalAlignment.Left),
            };

            foreach (var col in columns)
                _list.Columns.Add(col.header, col.width, col.align);

            _list.DrawColumnHeader += (s, e) => e.DrawDefault = true;
            _list.DrawItem         += (s, e) => e.DrawDefault = true;
            _list.DrawSubItem      += OnDrawSubItem;

            _list.DoubleClick            += OnDoubleClick;
            _list.SelectedIndexChanged   += OnSelectionChanged;

            Controls.Add(_list);
        }

        private static Color TagColor(RowTag tag)
        {
            switch (tag)
            {
                case RowTag.Ok:       return ColorOk;
                case RowTag.NotFound: return ColorNotFound;
                case RowTag.WrongAud: return ColorWrongAud;
                case RowTag.Long15:   return ColorLong15;
                case RowTag.Long30:   return ColorLong30;
                default:              return SystemColors.Window;
            }
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item == null || !(e.Item.Tag is RowTag))
            {
                e.DrawDefault = true;
                return;
            }

            var tag = (RowTag)e.Item.Tag;

            using (var brush = new SolidBrush(TagColor(tag)))
                e.Graphics.FillRectangle(brush, e.Bounds);

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
            if (e.Header != null && e.Header.TextAlign == HorizontalAlignment.Center)
                flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem != null ? e.SubItem.Text : "",
                _list.Font,
                e.Bounds,
                SystemColors.WindowText,
                flags);
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) return;
            var barcode = _list.SelectedItems[0].SubItems[3].Text;
            Clipboard.SetText(barcode);
            Text = "Скопировано: " + barcode;
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) return;

            var item    = _list.SelectedItems[0];
            var imageNo = item.SubItems[0].Text;
            var page    = item.SubItems[1].Text;
            var barcode = item.SubItems[3].Text;

            int total = 0;
            _totalMinutes.TryGetValue(barcode, out total);

            _lblInfo.Text = string.Format("Изображение: {0} | Страница: {1} | Суммарно: {2} мин",
                imageNo, page, total);
        }

        private void LoadLastCsv()
        {
            var path = CsvLoader.FindLatestCsv();
            if (path == null)
            {
                MessageBox.Show("CSV не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _csvRows = CsvLoader.Load(path);
            _lblCsv.Text = path;

            var auds = _csvRows
                .Select(r => { string v; r.TryGetValue("Аудитория", out v); return v ?? ""; })
                .Where(a => a.Length > 0)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            _cmbAud.Items.Clear();
            _cmbAud.Items.AddRange(auds.ToArray());

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

            if (_cmbAud.SelectedItem == null) return;
            var aud = _cmbAud.SelectedItem.ToString();
            if (string.IsNullOrEmpty(aud)) return;

            try
            {
                using (var conn = DbHelper.Connect())
                {
                    conn.Open();

                    var pages = _csvRows.Where(r =>
                    {
                        string v;
                        r.TryGetValue("Аудитория", out v);
                        return v == aud;
                    });

                    foreach (var row in pages)
                    {
                        string page    = GetVal(row, "Страница");
                        string imageNo = GetVal(row, "Номер_изображения");

                        var positions = row.Keys
                            .Select(k => Regex.Match(k, @"^Бланк_регистрации_(\d+)$"))
                            .Where(m => m.Success)
                            .Select(m => int.Parse(m.Groups[1].Value))
                            .ToList();

                        if (positions.Count == 0) continue;

                        int maxPos = positions.Max();

                        for (int i = 1; i <= maxPos; i++)
                        {
                            var barcode = GetVal(row, string.Format("Бланк_регистрации_{0:D2}", i)).Trim();
                            if (barcode.Length == 0) continue;

                            var person = DbHelper.GetPersonByBarcode(conn, barcode);

                            string fio    = "";
                            string dbAud  = "";
                            string status = "НЕ НАЙДЕН";

                            if (person != null)
                            {
                                fio   = person.Fio;
                                dbAud = person.Aud;
                                status = dbAud.PadLeft(4, '0') == aud ? "OK" : "ДРУГАЯ АУДИТОРИЯ";
                            }

                            var outTime = GetVal(row, string.Format("Время_выхода_{0:D2}", i));
                            var inTime  = GetVal(row, string.Format("Время_входа_{0:D2}", i));
                            var minutes = DbHelper.CalcMinutes(outTime, inTime);

                            if (!_totalMinutes.ContainsKey(barcode))
                                _totalMinutes[barcode] = 0;
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
                            lvi.SubItems.AddRange(new[]
                            {
                                page,
                                string.Format("{0:D2}", i),
                                barcode,
                                fio,
                                outTime,
                                inTime,
                                minutes.ToString(),
                                dbAud,
                                status
                            });

                            _list.Items.Add(lvi);
                        }
                    }

                    UpdateCounters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetVal(Dictionary<string, string> row, string key)
        {
            string v;
            return row.TryGetValue(key, out v) ? v : "";
        }

        private void UpdateCounters()
        {
            int ok = 0, err = 0, longOut = 0;

            foreach (ListViewItem item in _list.Items)
            {
                if (!(item.Tag is RowTag)) continue;
                var tag = (RowTag)item.Tag;
                switch (tag)
                {
                    case RowTag.Ok:       ok++;      break;
                    case RowTag.NotFound:
                    case RowTag.WrongAud: err++;     break;
                    case RowTag.Long15:
                    case RowTag.Long30:   longOut++; break;
                }
            }

            _lblOk.Text   = "OK: " + ok;
            _lblErr.Text  = "Ошибок: " + err;
            _lblLong.Text = "Долгих выходов: " + longOut;
        }

        private void ChangeDb()
        {
            Hide();
            var login = new LoginForm();
            login.FormClosed += (s, e) => Close();
            login.Show();
        }
    }
}
