namespace TVMonitorApp
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            // Use this to detect application instances
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "TVMonitorAppMutex", out createdNew))
            {
                if (createdNew)
                {
                    Application.Run(new MainContext());
                }
                else
                {
                    MessageBox.Show("TV Monitor App is already running.", "TV Monitor App", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
