using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace FlexiPane.Samples.DefaultApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            Debug.WriteLine("[App] Application startup completed");
        }
        
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Debug.WriteLine($"[App] UNHANDLED EXCEPTION (AppDomain): {exception?.GetType().Name}");
            Debug.WriteLine($"[App] Message: {exception?.Message}");
            Debug.WriteLine($"[App] StackTrace: {exception?.StackTrace}");
            Debug.WriteLine($"[App] IsTerminating: {e.IsTerminating}");
            
            if (exception != null)
            {
                MessageBox.Show(
                    $"Fatal Error:\n\n{exception.Message}\n\nStack:\n{exception.StackTrace}", 
                    "Application Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
        
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"[App] UNHANDLED EXCEPTION (Dispatcher): {e.Exception.GetType().Name}");
            Debug.WriteLine($"[App] Message: {e.Exception.Message}");
            Debug.WriteLine($"[App] StackTrace: {e.Exception.StackTrace}");
            
            // Show error message
            MessageBox.Show(
                $"An error occurred:\n\n{e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}", 
                "Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            
            // Prevent the application from crashing
            e.Handled = true;
        }
    }

}
