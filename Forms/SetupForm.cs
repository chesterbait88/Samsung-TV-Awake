using System;
using System.Windows.Forms;
using TVMonitorApp.Utils;

namespace TVMonitorApp.Forms
{
    public partial class SetupForm : Form
    {
        private readonly ConfigManager configManager;

        public SetupForm(ConfigManager configManager)
        {
            this.configManager = configManager;
            InitializeComponent();
            LoadExistingConfig();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties
            this.Text = "TV Monitor Setup";
            this.ClientSize = new System.Drawing.Size(600, 540); // Made form slightly taller
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Device monitor section
            Label deviceSectionLabel = new Label
            {
                Text = "Device Monitor Setup",
                Font = new System.Drawing.Font(this.Font.FontFamily, 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(460, 25)
            };
            
            Label ipLabel = new Label
            {
                Text = "TV IP Address:",
                Location = new System.Drawing.Point(20, 55),
                Size = new System.Drawing.Size(120, 25)
            };
            
            TextBox ipTextBox = new TextBox
            {
                Name = "ipTextBox",
                Location = new System.Drawing.Point(150, 55),
                Size = new System.Drawing.Size(300, 25)
            };
            
            Label intervalLabel = new Label
            {
                Text = "Check Interval (seconds):",
                Location = new System.Drawing.Point(20, 90),
                Size = new System.Drawing.Size(120, 25)
            };
            
            NumericUpDown intervalNumeric = new NumericUpDown
            {
                Name = "intervalNumeric",
                Location = new System.Drawing.Point(150, 90),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 5,
                Maximum = 300,
                Value = 60
            };

            Label timeoutLabel = new Label
            {
                Text = "Ping Timeout (ms):",
                Location = new System.Drawing.Point(20, 125),
                Size = new System.Drawing.Size(120, 25)
            };
            
            NumericUpDown timeoutNumeric = new NumericUpDown
            {
                Name = "timeoutNumeric",
                Location = new System.Drawing.Point(150, 125),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 500,
                Maximum = 5000,
                Increment = 100,
                Value = 1000
            };
            
            // SmartThings section
            Label smartThingsSectionLabel = new Label
            {
                Text = "SmartThings TV Setup",
                Font = new System.Drawing.Font(this.Font.FontFamily, 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, 170),
                Size = new System.Drawing.Size(460, 25)
            };
            
            Label instructionsLabel = new Label
            {
                Text = "To control your Samsung TV, you'll need to get your SmartThings\n" +
                      "access token and device ID. Visit the SmartThings Developer\n" +
                      "website to create a personal access token with proper permissions.",
                Location = new System.Drawing.Point(20, 200),
                Size = new System.Drawing.Size(460, 60)
            };
            
            Label tokenLabel = new Label
            {
                Text = "SmartThings Access Token:",
                Location = new System.Drawing.Point(20, 265),
                Size = new System.Drawing.Size(130, 25)
            };
            
            TextBox tokenTextBox = new TextBox
            {
                Name = "tokenTextBox",
                Location = new System.Drawing.Point(150, 265),
                Size = new System.Drawing.Size(300, 25)
            };
            
            Label deviceIdLabel = new Label
            {
                Text = "TV Device ID:",
                Location = new System.Drawing.Point(20, 300),
                Size = new System.Drawing.Size(130, 25)
            };
            
            TextBox deviceIdTextBox = new TextBox
            {
                Name = "deviceIdTextBox",
                Location = new System.Drawing.Point(150, 300),
                Size = new System.Drawing.Size(300, 25)
            };
            
            // Auto-start checkbox
            CheckBox autoStartCheckBox = new CheckBox
            {
                Name = "autoStartCheckBox",
                Text = "Start automatically with Windows",
                Location = new System.Drawing.Point(20, 345),
                Size = new System.Drawing.Size(300, 25),
                Checked = true
            };
            
            // Save and Cancel buttons
            Button saveButton = new Button
            {
                Name = "saveButton",
                Text = "Save",
                Location = new System.Drawing.Point(310, 385),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.OK
            };
            saveButton.Click += SaveButton_Click;
            
            Button cancelButton = new Button
            {
                Name = "cancelButton",
                Text = "Cancel",
                Location = new System.Drawing.Point(400, 385),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.Cancel
            };
            
            // Add controls to form
            this.Controls.AddRange(new Control[] {
                deviceSectionLabel, ipLabel, ipTextBox, 
                intervalLabel, intervalNumeric,
                timeoutLabel, timeoutNumeric,
                smartThingsSectionLabel, instructionsLabel,
                tokenLabel, tokenTextBox,
                deviceIdLabel, deviceIdTextBox,
                autoStartCheckBox,
                saveButton, cancelButton
            });
            
            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
            
            this.ResumeLayout(false);
        }

        private void LoadExistingConfig()
        {
            try
            {
                // Load existing settings if available
                if (this.Controls["ipTextBox"] is TextBox ipBox &&
                    this.Controls["intervalNumeric"] is NumericUpDown intervalBox &&
                    this.Controls["timeoutNumeric"] is NumericUpDown timeoutBox &&
                    this.Controls["tokenTextBox"] is TextBox tokenBox &&
                    this.Controls["deviceIdTextBox"] is TextBox deviceIdBox)
                {
                    if (configManager.HasSection("DeviceMonitor"))
                    {
                        ipBox.Text = configManager.GetValue("DeviceMonitor", "device_ip", "");
                        
                        string intervalStr = configManager.GetValue("DeviceMonitor", "check_interval", "60");
                        if (int.TryParse(intervalStr, out int interval))
                        {
                            intervalBox.Value = Math.Max(intervalBox.Minimum, Math.Min(intervalBox.Maximum, interval));
                        }

                        string timeoutStr = configManager.GetValue("DeviceMonitor", "ping_timeout", "1000");
                        if (int.TryParse(timeoutStr, out int timeout))
                        {
                            timeoutBox.Value = Math.Max(timeoutBox.Minimum, Math.Min(timeoutBox.Maximum, timeout));
                        }
                    }
                    
                    if (configManager.HasSection("SmartThings"))
                    {
                        tokenBox.Text = configManager.GetValue("SmartThings", "access_token", "");
                        deviceIdBox.Text = configManager.GetValue("SmartThings", "tv_device_id", "");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!(this.Controls["ipTextBox"] is TextBox ipBox &&
                    this.Controls["intervalNumeric"] is NumericUpDown intervalBox &&
                    this.Controls["timeoutNumeric"] is NumericUpDown timeoutBox &&
                    this.Controls["tokenTextBox"] is TextBox tokenBox &&
                    this.Controls["deviceIdTextBox"] is TextBox deviceIdBox &&
                    this.Controls["autoStartCheckBox"] is CheckBox autoStartBox))
                {
                    MessageBox.Show("Could not find all required controls.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Validate required fields
                if (string.IsNullOrWhiteSpace(ipBox.Text))
                {
                    MessageBox.Show("Please enter the TV IP address.", "Missing Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ipBox.Focus();
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(tokenBox.Text))
                {
                    MessageBox.Show("Please enter your SmartThings access token.", "Missing Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tokenBox.Focus();
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(deviceIdBox.Text))
                {
                    MessageBox.Show("Please enter the TV device ID.", "Missing Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    deviceIdBox.Focus();
                    return;
                }
                
                // Save configuration
                configManager.SetValue("DeviceMonitor", "device_ip", ipBox.Text);
                configManager.SetValue("DeviceMonitor", "check_interval", intervalBox.Value.ToString());
                configManager.SetValue("DeviceMonitor", "ping_timeout", timeoutBox.Value.ToString());
                configManager.SetValue("SmartThings", "access_token", tokenBox.Text);
                configManager.SetValue("SmartThings", "tv_device_id", deviceIdBox.Text);
                configManager.SaveConfiguration();
                
                // Set auto-start
                SetAutoStart(autoStartBox.Checked);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null)
                {
                    MessageBox.Show("Could not access Windows startup registry key.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (enable)
                {
                    string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue("TVMonitor", $"\"{appPath}\"");
                }
                else
                {
                    object? value = key.GetValue("TVMonitor");
                    if (value != null)
                    {
                        key.DeleteValue("TVMonitor");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting auto-start: {ex.Message}", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}