using System;
using System.IO;
using System.Text;

namespace TVMonitorApp.Utils
{
    public class Logger
    {
        private readonly string logName;
        private readonly string logFilePath;
        private readonly object lockObj = new object();

        public Logger(string name)
        {
            logName = name;
            string appPath = AppDomain.CurrentDomain.GetData("APP_PATH") as string ?? AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(appPath, "Logs");
            
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            logFilePath = Path.Combine(logDirectory, $"{DateTime.Now.ToString("yyyy-MM-dd")}.log");
        }

        public void Log(string message)
        {
            string formattedMessage = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [{logName}] {message}";
            
            try
            {
                // Write to console in debug mode
                #if DEBUG
                Console.WriteLine(formattedMessage);
                #endif
                
                // Write to log file
                lock (lockObj)
                {
                    File.AppendAllText(logFilePath, formattedMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // If we can't log to file, at least try to write to console
                Console.WriteLine($"Error writing to log: {ex.Message}");
                Console.WriteLine(formattedMessage);
            }
        }
    }
}