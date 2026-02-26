using System.Windows;
using System.Threading;

namespace EasySave.WPF
{
    public partial class App : System.Windows.Application
    {
        private Mutex? _singleInstanceMutex;
        private bool _ownsMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "Global\\EasySave_SingleInstance", out createdNew);
            _ownsMutex = createdNew;
            if (!createdNew)
            {
                System.Windows.MessageBox.Show("Another instance is already running.", "EasySave", MessageBoxButton.OK, MessageBoxImage.Information);
                // Shutdown immediately
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            // Release and dispose only if we own the mutex
            if (_ownsMutex)
            {
                try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
            }
            _singleInstanceMutex?.Dispose();
        }
    }
}
