using System;
using System.Windows.Forms;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;

namespace minecraft_windows_service_wrapper
{
    public class MainForm : Form
    {
        private readonly TextBox _serverDirBox = new TextBox { Left = 150, Top = 20, Width = 300 };
        private readonly TextBox _versionBox = new TextBox { Left = 150, Top = 60, Width = 100 };
        private readonly NumericUpDown _portBox = new NumericUpDown { Left = 150, Top = 100, Width = 100, Minimum = -1, Maximum = 65535, Value = 25565 };
        private readonly TextBox _javaHomeBox = new TextBox { Left = 150, Top = 140, Width = 300 };
        private readonly TextBox _jarFileBox = new TextBox { Left = 150, Top = 180, Width = 200 };
        private readonly Button _saveButton = new Button { Left = 20, Top = 230, Width = 100, Text = "Save" };
        private readonly Button _installButton = new Button { Left = 140, Top = 230, Width = 120, Text = "Add Service" };
        private readonly Button _removeButton = new Button { Left = 280, Top = 230, Width = 120, Text = "Remove Service" };

        private MinecraftServerOptions _options;

        public MainForm()
        {
            Text = "Minecraft Service Wrapper";
            Width = 500;
            Height = 320;

            Controls.AddRange(new Control[]
            {
                new Label { Left = 20, Top = 20, Text = "Server Directory", Width = 120 },
                _serverDirBox,
                new Label { Left = 20, Top = 60, Text = "Minecraft Version", Width = 120 },
                _versionBox,
                new Label { Left = 20, Top = 100, Text = "Port", Width = 120 },
                _portBox,
                new Label { Left = 20, Top = 140, Text = "Java Home", Width = 120 },
                _javaHomeBox,
                new Label { Left = 20, Top = 180, Text = "Jar File", Width = 120 },
                _jarFileBox,
                _saveButton,
                _installButton,
                _removeButton
            });

            _saveButton.Click += SaveButton_Click;
            _installButton.Click += InstallButton_Click;
            _removeButton.Click += RemoveButton_Click;

            LoadSettings();
        }

        private void LoadSettings()
        {
            _options = SettingsService.Load();
            _serverDirBox.Text = _options.ServerDirectory ?? string.Empty;
            _versionBox.Text = _options.MinecraftVersion?.ToString() ?? "";
            _portBox.Value = _options.Port == -1 ? 25565 : _options.Port;
            _javaHomeBox.Text = _options.JavaHome ?? string.Empty;
            _jarFileBox.Text = _options.JarFileName ?? "server.jar";
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!Version.TryParse(_versionBox.Text, out var version))
            {
                MessageBox.Show("Invalid Minecraft version.");
                return;
            }

            _options.ServerDirectory = _serverDirBox.Text;
            _options.MinecraftVersion = version;
            _options.Port = (int)_portBox.Value;
            _options.JavaHome = _javaHomeBox.Text;
            _options.JarFileName = _jarFileBox.Text;

            SettingsService.Save(_options);
            MessageBox.Show("Settings saved.");
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            WindowsServiceManager.Install("MinecraftService", Application.ExecutablePath);
            MessageBox.Show("Service installed.");
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            WindowsServiceManager.Remove("MinecraftService");
            MessageBox.Show("Service removed.");
        }
    }
}
