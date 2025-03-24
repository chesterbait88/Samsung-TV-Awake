using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TVMonitorApp.Forms;
using TVMonitorApp.Utils;

namespace TVMonitorApp
{
    public class MainContext : ApplicationContext
    {
        private NotifyIcon? trayIcon;
        private TVMonitor? monitor;
        private readonly ConfigManager configManager = new();
        private bool isPaused = false;

        public MainContext()
        {
            // Initialize configuration
            bool isFirstRun = configManager.LoadConfiguration();

            // Show setup form if this is the first run
            if (isFirstRun)
            {
                using (var setupForm = new SetupForm(configManager))
                {
                    if (setupForm.ShowDialog() != DialogResult.OK)
                    {
                        // User canceled setup, exit application
                        Application.Exit();
                        return;
                    }
                }
            }

            // Initialize monitoring
            monitor = new TVMonitor(configManager);

            // Load custom icon from resources
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            Icon appIcon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;

            // Initialize tray icon with custom icon
            trayIcon = new NotifyIcon()
            {
                Icon = appIcon,
                ContextMenuStrip = CreateContextMenu(),
                Visible = true,
                Text = "TV Monitor"
            };

            // Start monitoring
            monitor.Start();
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var statusItem = new ToolStripMenuItem("Status: Running");
            statusItem.Enabled = false;
            menu.Items.Add(statusItem);

            menu.Items.Add(new ToolStripSeparator());

            var pauseItem = new ToolStripMenuItem("Pause Monitoring");
            pauseItem.Click += (sender, e) => 
            {
                if (monitor == null) return;
                
                isPaused = !isPaused;
                if (isPaused)
                {
                    monitor.Pause();
                    pauseItem.Text = "Resume Monitoring";
                    statusItem.Text = "Status: Paused";
                }
                else
                {
                    monitor.Resume();
                    pauseItem.Text = "Pause Monitoring";
                    statusItem.Text = "Status: Running";
                }
            };
            menu.Items.Add(pauseItem);

            var editConfigItem = new ToolStripMenuItem("Edit Configuration");
            editConfigItem.Click += (sender, e) => 
            {
                // Open config file in Notepad
                System.Diagnostics.Process.Start("notepad.exe", configManager.ConfigFilePath);
                
                // Watch for the file to be saved
                string? configDir = System.IO.Path.GetDirectoryName(configManager.ConfigFilePath);
                if (configDir != null)
                {
                    var watcher = new System.IO.FileSystemWatcher(configDir)
                    {
                        Filter = System.IO.Path.GetFileName(configManager.ConfigFilePath),
                        NotifyFilter = System.IO.NotifyFilters.LastWrite
                    };
                    
                    watcher.Changed += (s, args) => 
                    {
                        watcher.Dispose();
                        
                        // Reload config and restart monitoring
                        configManager.LoadConfiguration();
                        if (!isPaused && monitor != null)
                        {
                            monitor.Restart();
                        }
                    };
                    
                    watcher.EnableRaisingEvents = true;
                }
            };
            menu.Items.Add(editConfigItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (sender, e) => Application.Exit();
            menu.Items.Add(exitItem);

            return menu;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up tray icon
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
                
                // Stop monitoring
                monitor?.Stop();
            }

            base.Dispose(disposing);
        }
    }
}
