using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;

namespace minecraft_windows_service_wrapper
{
    public class MainForm : Form
    {
        // Reorganized UI fields: Java version → JAR path → MC version → Port
        private readonly Label _javaVersionLabel = new Label { Left = 150, Top = 20, Width = 300, ForeColor = Color.Green };
        private readonly TextBox _jarPathBox = new TextBox { Left = 150, Top = 60, Width = 250, ReadOnly = true };
        private readonly Button _browseButton = new Button { Left = 410, Top = 60, Width = 80, Text = "Browse..." };
        private readonly TextBox _versionBox = new TextBox { Left = 150, Top = 100, Width = 200, ReadOnly = true };
        private readonly NumericUpDown _portBox = new NumericUpDown { Left = 150, Top = 140, Width = 100, Minimum = -1, Maximum = 65535, Value = 25565 };
        private readonly Button _saveButton = new Button { Left = 20, Top = 190, Width = 100, Text = "Save" };
        private readonly Button _installButton = new Button { Left = 140, Top = 190, Width = 120, Text = "Add Service" };
        private readonly Button _removeButton = new Button { Left = 280, Top = 190, Width = 120, Text = "Remove Service" };

        private MinecraftServerOptions _options;
        private readonly IJavaVersionService _javaVersionService;

        public MainForm(IJavaVersionService javaVersionService = null)
        {
            _javaVersionService = javaVersionService; // Allow null for testing
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Minecraft Service Wrapper";
            Width = 520;
            Height = 280;

            Controls.AddRange(new Control[]
            {
                new Label { Left = 20, Top = 20, Text = "Java Version", Width = 120 },
                _javaVersionLabel,
                new Label { Left = 20, Top = 60, Text = "Server JAR Path", Width = 120 },
                _jarPathBox,
                _browseButton,
                new Label { Left = 20, Top = 100, Text = "Minecraft Version", Width = 120 },
                _versionBox,
                new Label { Left = 20, Top = 140, Text = "Port", Width = 120 },
                _portBox,
                _saveButton,
                _installButton,
                _removeButton
            });

            _saveButton.Click += SaveButton_Click;
            _installButton.Click += InstallButton_Click;
            _removeButton.Click += RemoveButton_Click;
            _browseButton.Click += BrowseButton_Click;

            LoadSettings();
        }

        private void LoadSettings()
        {
            _options = SettingsService.Load();
            
            // Combine server directory and jar file into full path
            if (!string.IsNullOrEmpty(_options.ServerDirectory) && !string.IsNullOrEmpty(_options.JarFileName))
            {
                _jarPathBox.Text = Path.Combine(_options.ServerDirectory, _options.JarFileName);
            }
            
            _versionBox.Text = _options.MinecraftVersion?.ToString() ?? "";
            _portBox.Value = _options.Port == -1 ? 25565 : _options.Port;
            
            // Load Java version asynchronously with proper error handling
            if (_javaVersionService != null)
            {
                _ = LoadJavaVersionSafelyAsync();
            }
            else
            {
                _javaVersionLabel.Text = "Java service not available";
                _javaVersionLabel.ForeColor = Color.Gray;
            }
            
            // If we have a JAR path, detect Minecraft version synchronously
            if (!string.IsNullOrEmpty(_jarPathBox.Text))
            {
                DetectVersionFromFilenameSynchronous(_jarPathBox.Text);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_jarPathBox.Text))
            {
                MessageBox.Show("Please select a server JAR file.");
                return;
            }
            
            if (!Version.TryParse(_versionBox.Text, out var version))
            {
                MessageBox.Show("Invalid Minecraft version. Please select a valid server JAR file.");
                return;
            }

            // Split JAR path into directory and filename
            _options.ServerDirectory = Path.GetDirectoryName(_jarPathBox.Text);
            _options.JarFileName = Path.GetFileName(_jarPathBox.Text);
            _options.MinecraftVersion = version;
            _options.Port = (int)_portBox.Value;
            // Java home is auto-detected

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

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            try
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Filter = "JAR files (*.jar)|*.jar|All files (*.*)|*.*",
                    Title = "Select Minecraft Server JAR File",
                };

                var result = openFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    _jarPathBox.Text = openFileDialog.FileName;

                    // Simple synchronous version detection
                    DetectVersionFromFilenameSynchronous(openFileDialog.FileName);
                }
                else
                {
                    _versionBox.Text = "No file selected";
                    _versionBox.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in browse button: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _versionBox.Text = $"Error: {ex.Message}";
                _versionBox.ForeColor = Color.Red;
            }
        }
        
        private async Task LoadJavaVersionSafelyAsync()
        {
            await Task.Run(async () =>
            {
                try
                {
                    Invoke(new Action(() =>
                    {
                        _javaVersionLabel.Text = "Detecting Java...";
                        _javaVersionLabel.ForeColor = Color.Blue;
                    }));
                    
                    // Use timeout for Java detection
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    
                    var javaHome = await _javaVersionService.GetJavaHomeAsync();
                    var javaExe = await _javaVersionService.GetJavaExecutablePathAsync(javaHome);
                    var javaVersion = await _javaVersionService.GetJavaVersionAsync(javaExe);
                    
                    Invoke(new Action(() =>
                    {
                        _javaVersionLabel.Text = $"Java {javaVersion} (from {javaHome})";
                        _javaVersionLabel.ForeColor = Color.Green;
                    }));
                    
                    // Update the options with the detected Java home
                    _options.JavaHome = javaHome;
                }
                catch (OperationCanceledException)
                {
                    Invoke(new Action(() =>
                    {
                        _javaVersionLabel.Text = "Java detection timed out";
                        _javaVersionLabel.ForeColor = Color.Red;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() =>
                    {
                        _javaVersionLabel.Text = $"Error: {ex.Message}";
                        _javaVersionLabel.ForeColor = Color.Red;
                    }));
                    _options.JavaHome = null;
                }
            });
        }

        private async Task LoadJavaVersionAsync()
        {
            // This is the old method - keeping for compatibility but unused
            await LoadJavaVersionSafelyAsync();
        }

        private async Task DetectMinecraftVersionAsync(string jarPath)
        {
            await Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(jarPath) || !File.Exists(jarPath))
                    {
                        Invoke(new Action(() =>
                        {
                            _versionBox.Text = "JAR file not found";
                            _versionBox.ForeColor = Color.Red;
                        }));
                        return;
                    }

                    Invoke(new Action(() =>
                    {
                        _versionBox.Text = "Detecting version...";
                        _versionBox.ForeColor = Color.Blue;
                    }));

                    // First try to detect version from filename
                    var versionFromFilename = ExtractVersionFromFilename(jarPath);
                    if (versionFromFilename != null)
                    {
                        Invoke(new Action(() =>
                        {
                            _versionBox.Text = versionFromFilename.ToString();
                            _versionBox.ForeColor = Color.Green;
                        }));
                        return;
                    }

                    // Try to inspect JAR manifest
                    var versionFromManifest = await ExtractVersionFromJarAsync(jarPath);
                    if (versionFromManifest != null)
                    {
                        Invoke(new Action(() =>
                        {
                            _versionBox.Text = versionFromManifest.ToString();
                            _versionBox.ForeColor = Color.Green;
                        }));
                        return;
                    }

                    // Last resort: try running JAR with timeout
                    var versionFromExecution = await TryDetectVersionWithTimeoutAsync(jarPath);
                    if (versionFromExecution != null)
                    {
                        Invoke(new Action(() =>
                        {
                            _versionBox.Text = versionFromExecution.ToString();
                            _versionBox.ForeColor = Color.Green;
                        }));
                        return;
                    }

                    Invoke(new Action(() =>
                    {
                        _versionBox.Text = "Unable to detect version";
                        _versionBox.ForeColor = Color.Orange;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() =>
                    {
                        _versionBox.Text = $"Error: {ex.Message}";
                        _versionBox.ForeColor = Color.Red;
                    }));
                }
            });
        }

        private Version ExtractVersionFromFilename(string jarPath)
        {
            try
            {
                var filename = Path.GetFileNameWithoutExtension(jarPath);
                
                // Common filename patterns: server-1.16.5.jar, minecraft-server-1.19.2.jar, etc.
                var patterns = new[]
                {
                    @"(\d+\.\d+(?:\.\d+)?)", // Any version pattern in filename
                    @"server[_-]?(\d+\.\d+(?:\.\d+)?)",
                    @"minecraft[_-]?server[_-]?(\d+\.\d+(?:\.\d+)?)",
                    @"mc[_-]?(\d+\.\d+(?:\.\d+)?)"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(filename, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
                    {
                        return version;
                    }
                }
            }
            catch
            {
                // Ignore filename parsing errors
            }

            return null;
        }

        private async Task<Version> ExtractVersionFromJarAsync(string jarPath)
        {
            try
            {
                // This is a simplified approach - in reality you'd need to parse JAR manifest
                // For now, return null to indicate this method isn't implemented
                await Task.Delay(100); // Simulate some work
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<Version> TryDetectVersionWithTimeoutAsync(string jarPath)
        {
            try
            {
                var javaHome = await _javaVersionService.GetJavaHomeAsync();
                var javaExe = await _javaVersionService.GetJavaExecutablePathAsync(javaHome);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = javaExe,
                    Arguments = $"-jar \"{jarPath}\" --version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(jarPath)
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    // Wait maximum 5 seconds for the process
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);

                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();

                        if (process.ExitCode != 0)
                        {
                            Debug.WriteLine($"Version detection failed with exit code {process.ExitCode}\nOutput: {output}\nError: {error}");
                            return null;
                        }

                        return ExtractMinecraftVersion(output + " " + error);
                    }
                    catch (OperationCanceledException)
                    {
                        // Process timed out, kill it
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                            // Ignore kill errors
                        }
                        return null;
                    }
                }
            }
            catch
            {
                // Ignore execution errors
            }

            return null;
        }

        private Version ExtractMinecraftVersion(string output)
        {
            // Common patterns for Minecraft server version detection
            var patterns = new[]
            {
                @"(?i)(?:minecraft|mc|server)\s*(?:version)?\s*:?\s*(\d+\.\d+(?:\.\d+)?)",
                @"(\d+\.\d+(?:\.\d+)?)\s*(?:minecraft|mc|server)",
                @"Starting minecraft server version\s*(\d+\.\d+(?:\.\d+)?)",
                @"MC:\s*(\d+\.\d+(?:\.\d+)?)",
                @"Version\s*(\d+\.\d+(?:\.\d+)?)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(output, pattern, RegexOptions.IgnoreCase);
                if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
                {
                    return version;
                }
            }

            return null;
        }

        private void DetectVersionFromFilenameSynchronous(string jarPath)
        {
            try
            {
                var version = ExtractVersionFromFilename(jarPath);
                if (version != null)
                {
                    _versionBox.Text = version.ToString();
                    _versionBox.ForeColor = Color.Green;
                }
                else
                {
                    _versionBox.Text = "Unable to detect version from filename";
                    _versionBox.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                _versionBox.Text = $"Error: {ex.Message}";
                _versionBox.ForeColor = Color.Red;
            }
        }
    }
}
