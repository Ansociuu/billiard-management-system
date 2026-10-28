using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace billiard_management_system
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Contains("--test-db"))
            {
                var result = await DatabaseHelper.TestConnectionAsync();
                File.WriteAllText("db_test_result.txt", $"Success: {result.Success}\r\nMessage: {result.Message}\r\nTimestamp: {DateTime.Now}");
                Console.WriteLine($"DB Test: {result.Success} - {result.Message}");
                Shutdown(result.Success ? 0 : 1);
                return;
            }

            base.OnStartup(e);
        }
    }
}
