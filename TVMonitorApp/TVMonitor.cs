using System;
using System.Management;
using System.Threading;
using TVMonitorApp.Utils;

namespace TVMonitorApp
{
    public class TVMonitor
    {
        private readonly ConfigManager configManager;
        private readonly Logger logger;
        private ManagementEventWatcher? displayWatcher;
        private System.Timers.Timer? presenceCheckTimer;
        private bool isMonitoring = false;
        private bool devicePresent = false;

        public TVMonitor(ConfigManager config)
        {
            configManager = config;
            logger = new Logger("TVMonitor");
            InitializeTimers();
            SetupDisplayWatcher();
        }

        private void InitializeTimers()
        {
            // Get check interval from config (default 60 seconds)
            int interval;
            if (!int.TryParse(configManager.GetValue("DeviceMonitor", "check_interval", "60"), out interval))
            {
                interval = 60;
            }

            presenceCheckTimer = new System.Timers.Timer(interval * 1000); // Convert to milliseconds
            presenceCheckTimer.Elapsed += OnPresenceCheckTimer;
            presenceCheckTimer.AutoReset = true;
        }

        private void SetupDisplayWatcher()
        {
            try
            {
                // WMI query for monitor state changes
                var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
                displayWatcher = new ManagementEventWatcher(query);
                displayWatcher.EventArrived += OnDisplayStateChanged;
            }
            catch (Exception ex)
            {
                logger.Log($"Error setting up display watcher: {ex.Message}");
            }
        }

        private void OnPresenceCheckTimer(object? sender, System.Timers.ElapsedEventArgs e)
        {
            CheckDevicePresence();
        }

        private void CheckDevicePresence()
        {
            try
            {
                string deviceIP = configManager.GetValue("DeviceMonitor", "device_ip");
                if (string.IsNullOrEmpty(deviceIP))
                {
                    logger.Log("Device IP not configured");
                    return;
                }

                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send(deviceIP, 1000); // 1 second timeout
                    bool wasPresent = devicePresent;
                    devicePresent = (reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success);

                    if (devicePresent != wasPresent)
                    {
                        logger.Log($"Device presence changed: {(devicePresent ? "Present" : "Not Found")}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error checking device presence: {ex.Message}");
                devicePresent = false;
            }
        }

        private void OnDisplayStateChanged(object sender, EventArrivedEventArgs e)
        {
            if (!isMonitoring || !devicePresent) return;

            try
            {
                // Get SmartThings configuration
                string token = configManager.GetValue("SmartThings", "access_token");
                string deviceId = configManager.GetValue("SmartThings", "tv_device_id");

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(deviceId))
                {
                    logger.Log("SmartThings configuration missing");
                    return;
                }

                // Send power on command to TV via SmartThings API
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    
                    var command = new
                    {
                        commands = new[]
                        {
                            new
                            {
                                component = "main",
                                capability = "switch",
                                command = "on"
                            }
                        }
                    };

                    var content = new System.Net.Http.StringContent(
                        System.Text.Json.JsonSerializer.Serialize(command),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    var response = client.PostAsync(
                        $"https://api.smartthings.com/v1/devices/{deviceId}/commands",
                        content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        logger.Log("Successfully sent power on command to TV");
                    }
                    else
                    {
                        logger.Log($"Failed to send power on command: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error handling display state change: {ex.Message}");
            }
        }

        public void Start()
        {
            if (!isMonitoring)
            {
                isMonitoring = true;
                displayWatcher?.Start();
                presenceCheckTimer?.Start();
                logger.Log("Monitoring started");
            }
        }

        public void Stop()
        {
            if (isMonitoring)
            {
                isMonitoring = false;
                displayWatcher?.Stop();
                presenceCheckTimer?.Stop();
                logger.Log("Monitoring stopped");
            }
        }

        public void Pause()
        {
            if (isMonitoring)
            {
                Stop();
                logger.Log("Monitoring paused");
            }
        }

        public void Resume()
        {
            if (!isMonitoring)
            {
                Start();
                logger.Log("Monitoring resumed");
            }
        }

        public void Restart()
        {
            Stop();
            Thread.Sleep(1000); // Short delay to ensure clean restart
            Start();
            logger.Log("Monitoring restarted");
        }
    }
}