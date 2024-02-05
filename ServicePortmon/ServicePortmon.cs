using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Net.Sockets;
using System.Timers;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;
using System.Configuration.Install;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;
using System.Threading;

namespace ServicePortmon
{
    [RunInstaller(true)]
    public class ServiceInstaller : Installer
    {
        public ServiceInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            // Set the account under which the service will run (LocalSystem, NetworkService, etc.)
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

            // Set the service installer properties
            serviceInstaller.DisplayName = "MID ServicePortmon";
            serviceInstaller.ServiceName = "ServicePortmon"; // Make sure it matches your service name

            // Add the installers to the installer collection
            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }

    public partial class ServicePortmon : ServiceBase
    {
        private System.Timers.Timer timer;
        private string serverAddress;
        private int[] portsToCheck;
        private string serviceToRestart;
        private string logFilePath = @"C:\Tasks\ServicePortmon.log";
        public ServicePortmon()
        {
            this.ServiceName = "ServicePortmon";
        }

        protected override void OnStart(string[] args)
        {
            // Load configuration from config.xml
            LoadConfig();

            // Set up a timer to periodically check ports
            timer = new System.Timers.Timer(300000); // 5 minutes
            timer.Elapsed += CheckPorts;
            timer.Start();

            Log($"Service started");

            // Initial port check
            CheckPorts(null, null);
            
        }

        protected override void OnStop()
        {
            // Stop the timer
            timer.Stop();

            Log($"Service stopped");

            // Clean up resources (optional)

        }

        private void LoadConfig()
        {
            try
            {
                string configFile = @"C:\Tasks\config.xml"; // Specify the full path to your config.xml file

                var config = System.Xml.Linq.XDocument.Load(configFile);

                serverAddress = config.Root.Element("ServerAddress").Value;
                portsToCheck = config.Root.Element("PortstoCheck").Value.Split(',').Select(int.Parse).ToArray();
                serviceToRestart = config.Root.Element("ServiceToRestart").Value;

                Log($"Config loaded");

            }
            catch (Exception ex)
            {
                Log("Error loading configuration: " + ex.Message);
                throw;
            }
        }


        private void CheckPorts(object sender, ElapsedEventArgs e)
        {
            bool anyPortClosed = false;
            Log("Checking ports");
            foreach (int port in portsToCheck)
            {
                if (!IsPortOpen(serverAddress, port))
                {
                    // Port is not open, set the flag to indicate that at least one port is closed
                    anyPortClosed = true;

                    // Log the event
                    Log($"Port {port} is not reachable.");
                }
            }

            if (anyPortClosed)
            {
                // At least one port is closed, restart the specified service
                RestartService(serviceToRestart);
                Log($"Restarted {serviceToRestart}.");

            }
            else
            {
                // All ports are reachable, log the status and wait for the next interval
                Log("All ports are reachable.");
            }
        }

        private bool IsPortOpen(string host, int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(host, port);
                    return true;

                }
            }
            catch
            {
                return false;
            }
        }

        private void RestartService(string serviceToRestart)
        {
            try
            {
                ServiceController service = new ServiceController(serviceToRestart);

                // Check if the service is running
                if (service.Status == ServiceControllerStatus.Running)
                {
                    Log($"Restarting service: {serviceToRestart}");
                    service.Stop();
                    Log($"Waiting for service to stop: {serviceToRestart}");
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(5));
                    Log($"Service stopped successfully: {serviceToRestart}");

                    Log($"Starting service: {serviceToRestart}");
                    service.Start();
                    Log($"Waiting for service to start: {serviceToRestart}");
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(5));
                    Log($"Service started successfully: {serviceToRestart}");
                }
                else
                {
                    // The service is not running; start it
                    Log($"Starting service: {serviceToRestart}");
                    service.Start();
                    Log($"Waiting for service to start: {serviceToRestart}");
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(5));
                    Log($"Service started successfully: {serviceToRestart}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error restarting service {serviceToRestart}: {ex.Message}");
                // Handle any exceptions that may occur during service restart
            }
        }

        private void Log(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
                }
            }
            catch (Exception ex)
            {
                // Handle log file errors
                Console.WriteLine("Error writing to log file: " + ex.Message);
            }
        }

    }
}
