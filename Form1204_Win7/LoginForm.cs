using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Form1204
{
    public class LoginForm : Form
    {
        private readonly TextBox _txtServer   = new TextBox();
        private readonly TextBox _txtUser     = new TextBox();
        private readonly TextBox _txtPassword = new TextBox();
        private readonly Button  _btnConnect  = new Button();

        public LoginForm()
        {
            Text            = "Подключение к SQL";
            ClientSize      = new Size(450, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;

            _txtServer.Text = "ERBD02";
            _txtUser.Text   = "sa";
            _txtPassword.UseSystemPasswordChar = true;

            _btnConnect.Text   = "Подключиться";
            _btnConnect.Height = 35;
            _btnConnect.Click += (s, e) => TryConnect();

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                Padding     = new Padding(20, 10, 20, 10),
                AutoSize    = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            foreach (var pair in new (string label, TextBox ctrl)[]
            {
                ("Сервер",  _txtServer),
                ("Логин",   _txtUser),
                ("Пароль",  _txtPassword)
            })
            {
                layout.Controls.Add(new Label { Text = pair.label, AutoSize = true, Margin = new Padding(0, 8, 0, 2) });
                pair.ctrl.Dock = DockStyle.Fill;
                pair.ctrl.KeyDown += (s, e) => { if (e.KeyCode == Keys.Return) TryConnect(); };
                layout.Controls.Add(pair.ctrl);
            }

            _btnConnect.Dock = DockStyle.Fill;
            layout.Controls.Add(_btnConnect);

            Controls.Add(layout);
            AcceptButton = _btnConnect;
            _txtPassword.Focus();
        }

        private void TryConnect()
        {
            DbHelper.Server   = _txtServer.Text.Trim();
            DbHelper.User     = _txtUser.Text.Trim();
            DbHelper.Password = _txtPassword.Text;

            try
            {
                var dbs = DbHelper.GetErbdDatabases();
                if (dbs.Count == 0)
                    throw new Exception("Базы erbd не найдены.");

                ShowDbSelect(dbs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка подключения",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowDbSelect(List<string> dbs)
        {
            var dlg = new Form
            {
                Text            = "Выбор базы данных",
                ClientSize      = new Size(450, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                StartPosition   = FormStartPosition.CenterParent
            };

            var combo = new ComboBox
            {
                Dock          = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            combo.Items.AddRange(dbs.ToArray());
            combo.SelectedIndex = 0;

            var btn = new Button
            {
                Text   = "Открыть",
                Dock   = DockStyle.Bottom,
                Height = 35
            };

            btn.Click += (s, e) =>
            {
                DbHelper.Database = combo.SelectedItem.ToString();
                dlg.DialogResult  = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.Add(combo);
            dlg.Controls.Add(btn);
            dlg.AcceptButton = btn;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Hide();
                var main = new MainForm();
                main.FormClosed += (s, e) => Close();
                main.Show();
            }
        }
    }
}
