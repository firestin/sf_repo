using Microsoft.Data.SqlClient;

namespace Form1204;

/// <summary>
/// Форма входа: ввод сервера/логина/пароля → выбор базы данных → открытие MainForm.
/// </summary>
public class LoginForm : Form
{
    private readonly TextBox _txtServer   = new();
    private readonly TextBox _txtUser     = new();
    private readonly TextBox _txtPassword = new();
    private readonly Button  _btnConnect  = new();

    public LoginForm()
    {
        Text            = "Подключение к SQL";
        ClientSize      = new Size(450, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            Padding     = new Padding(20, 10, 20, 10),
            AutoSize    = true
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtServer.Text = "ERBD02";
        _txtUser.Text   = "sa";
        _txtPassword.UseSystemPasswordChar = true;

        _btnConnect.Text    = "Подключиться";
        _btnConnect.Dock    = DockStyle.Fill;
        _btnConnect.Height  = 35;
        _btnConnect.Click  += (_, _) => TryConnect();

        foreach (var (label, ctrl) in new (string, Control)[]
        {
            ("Сервер",  _txtServer),
            ("Логин",   _txtUser),
            ("Пароль",  _txtPassword)
        })
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 8, 0, 2) });
            ctrl.Dock = DockStyle.Fill;
            ctrl.KeyDown += (_, e) => { if (e.KeyCode == Keys.Return) TryConnect(); };
            layout.Controls.Add(ctrl);
        }

        layout.Controls.Add(_btnConnect);
        Controls.Add(layout);

        _txtPassword.Focus();
        AcceptButton = _btnConnect;
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
            Dock         = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin       = new Padding(20, 20, 20, 10)
        };
        combo.Items.AddRange(dbs.Cast<object>().ToArray());
        combo.SelectedIndex = 0;

        var btn = new Button
        {
            Text   = "Открыть",
            Dock   = DockStyle.Bottom,
            Height = 35
        };

        btn.Click += (_, _) =>
        {
            DbHelper.Database = combo.SelectedItem!.ToString()!;
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
            main.FormClosed += (_, _) => Close();
            main.Show();
        }
    }
}
