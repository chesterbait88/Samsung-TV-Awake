using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

        // State tracking variables
        private readonly SemaphoreSlim eventProcessingSemaphore = new SemaphoreSlim(1);
        private DateTime lastMonitorEvent = DateTime.MinValue;
        private readonly TimeSpan monitorEventThreshold = TimeSpan.FromSeconds(20);
        private bool processingEvent = false;
        private const int MaxRetries = 2;
        private const int RetryDelaySeconds = 5;
        private bool tvIsOn = false;

        // Rate limiting variables
        private readonly SemaphoreSlim rateLimitSemaphore = new SemaphoreSlim(1);
        private DateTime lastRequestTime = DateTime.MinValue;
        private const int MinRequestInterval = 5000; // 5 seconds between API requests

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

            presenceCheckTimer = new System.Timers.Timer(interval * 1000);
            presenceCheckTimer.Elapsed += OnPresenceCheckTimer;
            presenceCheckTimer.AutoReset = true;
        }

        private void SetupDisplayWatcher()
        {
            try
            {
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

        private bool TestNetworkAvailable()
        {
            try
            {
                // Check if we have any network interface that's up
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                return interfaces.Any(ni => 
                    ni.OperationalStatus == OperationalStatus.Up && 
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || 
                     ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet));
            }
            catch (Exception ex)
            {
                logger.Log($"Error checking network status: {ex.Message}");
                return false;
            }
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

                if (!TestNetworkAvailable())
                {
                    logger.Log("No active network connection available");
                    devicePresent = false;
                    return;
                }

                const int MaxPingAttempts = 3;
                int successfulPings = 0;

                using (var ping = new Ping())
                {
                    for (int attempt = 1; attempt <= MaxPingAttempts; attempt++)
                    {
                        try
                        {
                            var reply = ping.Send(deviceIP, 1000);
                            if (reply != null && reply.Status == IPStatus.Success)
                            {
                                successfulPings++;
                                logger.Log($"Ping attempt {attempt} successful");
                            }
                            else
                            {
                                logger.Log($"Ping attempt {attempt} failed (no response)");
                            }
                        }
                        catch (PingException)
                        {
                            logger.Log($"Ping attempt {attempt} failed (network unreachable)");
                        }
                        catch (SocketException)
                        {
                            logger.Log($"Ping attempt {attempt} failed (host unreachable)");
                        }
                        catch (Exception ex)
                        {
                            logger.Log($"Ping attempt {attempt} failed: {ex.Message}");
                        }

                        if (attempt < MaxPingAttempts)
                        {
                            Thread.Sleep(500); // Short delay between attempts
                        }
                    }
                }

                bool wasPresent = devicePresent;
                devicePresent = ((double)successfulPings / MaxPingAttempts) > 0.5;

                if (devicePresent != wasPresent)
                {
                    logger.Log($"Device presence changed: {(devicePresent ? "Present" : "Not Found")} ({successfulPings}/{MaxPingAttempts} successful pings)");
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error checking device presence: {ex.Message}");
                devicePresent = false;
            }
        }

        private async Task<bool> IsTVPoweredOn()
        {
            try
            {
                string token = configManager.GetValue("SmartThings", "access_token");
                string deviceId = configManager.GetValue("SmartThings", "tv_device_id");

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(deviceId))
                {
                    logger.Log("SmartThings configuration missing");
                    return false;
                }

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    
                    var response = await client.GetAsync(
                        $"https://api.smartthings.com/v1/devices/{deviceId}/status");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        using (var doc = System.Text.Json.JsonDocument.Parse(content))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("components", out var components) &&
                                components.TryGetProperty("main", out var main) &&
                                main.TryGetProperty("switch", out var switchComponent) &&
                                switchComponent.TryGetProperty("switch", out var switchState) &&
                                switchState.TryGetProperty("value", out var value))
                            {
                                tvIsOn = value.GetString() == "on";
                                logger.Log($"Current TV state: {(tvIsOn ? "ON" : "OFF")}");
                                return tvIsOn;
                            }
                        }
                    }
                    else
                    {
                        logger.Log($"Failed to get TV power state: {response.StatusCode}");
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Log($"Error response: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error checking TV power state: {ex.Message}");
            }
            
            return false;
        }

        private async Task<bool> SendPowerOnCommand()
        {
            try
            {
                // Acquire rate limit lock
                await rateLimitSemaphore.WaitAsync();

                string token = configManager.GetValue("SmartThings", "access_token");
                string deviceId = configManager.GetValue("SmartThings", "tv_device_id");

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(deviceId))
                {
                    logger.Log("SmartThings configuration missing");
                    return false;
                }

                // Check if TV is already on
                if (await IsTVPoweredOn())
                {
                    logger.Log("TV is already powered on, no action needed");
                    return true;
                }

                // Check if we need to wait due to rate limiting
                var timeSinceLastRequest = DateTime.Now - lastRequestTime;
                if (timeSinceLastRequest.TotalMilliseconds < MinRequestInterval)
                {
                    var delayTime = MinRequestInterval - (int)timeSinceLastRequest.TotalMilliseconds;
                    await Task.Delay(delayTime);
                }

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

                    logger.Log("Sending power on command to TV...");
                    var response = await client.PostAsync(
                        $"https://api.smartthings.com/v1/devices/{deviceId}/commands",
                        content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        logger.Log($"Successfully sent power on command to TV. Response: {responseContent}");
                        tvIsOn = true;
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Log($"Failed to send power on command: {response.StatusCode}");
                        logger.Log($"Error response: {errorContent}");
                        tvIsOn = false;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error sending power on command: {ex.Message}");
                tvIsOn = false;
                return false;
            }
            finally
            {
                lastRequestTime = DateTime.Now;
                rateLimitSemaphore.Release();
            }
        }

        private async void OnDisplayStateChanged(object sender, EventArrivedEventArgs e)
        {
            if (!isMonitoring || !devicePresent) return;

            var currentTime = DateTime.Now;

            // Check if we're within the debounce period
            if ((currentTime - lastMonitorEvent) < monitorEventThreshold)
            {
                logger.Log("Ignoring monitor event - too soon after last event");
                return;
            }

            // Check if we're already processing an event
            if (!await eventProcessingSemaphore.WaitAsync(0))
            {
                logger.Log("Ignoring monitor event - previous event still processing");
                return;
            }

            try
            {
                processingEvent = true;
                lastMonitorEvent = currentTime;

                logger.Log("Monitor wake event detected - checking TV status...");
                bool success = false;
                int attempt = 0;

                while (!success && attempt < MaxRetries)
                {
                    attempt++;
                    try
                    {
                        logger.Log($"Attempt {attempt} to check/update TV...");
                        success = await SendPowerOnCommand();

                        if (!success && attempt < MaxRetries)
                        {
                            logger.Log($"Waiting {RetryDelaySeconds} seconds before retry...");
                            await Task.Delay(RetryDelaySeconds * 1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Error on attempt {attempt}: {ex.Message}");
                        if (attempt < MaxRetries)
                        {
                            logger.Log($"Waiting {RetryDelaySeconds} seconds before retry...");
                            await Task.Delay(RetryDelaySeconds * 1000);
                        }
                    }
                }

                if (!success)
                {
                    logger.Log($"Failed to turn on TV after {MaxRetries} attempts");
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error in display state change handler: {ex.Message}");
            }
            finally
            {
                processingEvent = false;
                eventProcessingSemaphore.Release();
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