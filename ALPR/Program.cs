namespace ALPR
{
    internal static class Program
    {
        /// <summary>
        /// Uygulamanýn ana giriþ noktasý.
        /// .NET 9 ve OpenCV optimize edilmiþ ALPR uygulamasý
        /// </summary>
        [STAThread]
        static void Main()
        {
            // High DPI ve font ayarlarý
            ApplicationConfiguration.Initialize();
            
            // Global exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            
            // Ana formu çalýþtýr
            Application.Run(new frmALPR());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Beklenmeyen bir hata oluþtu:\n{e.Exception.Message}", 
                "ALPR Hatasý", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
    }
}