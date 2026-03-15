using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Management;

namespace TaskManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    public MainWindow()
    {
        InitializeComponent();
        const sbyte REFRESH_RATE = 1;
        GetCpuName();
        GetCpuSpeed();

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(REFRESH_RATE)
        };
        timer.Tick += GetProcessFromSystem!;
        timer.Tick += GetCpuPercentage!;
        timer.Start();

    }

    public void GetProcessFromSystem(object sender, EventArgs e)
    {
        Process[] processes = Process.GetProcesses();
        var orderedProcesses = processes.OrderByDescending(process => process.WorkingSet64).Select((process) =>
                      new { process.ProcessName, process.Id, WorkingSet64 = process.WorkingSet64 / 1048576 });

        ProcessGrid.ItemsSource = orderedProcesses;
    }

    public void KillButton(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            dynamic? data = button?.DataContext;

            Process.GetProcessById(data?.Id).Kill();
        }
        catch (Exception error)
        {
            MessageBox.Show("Error: " + error.Message);
        }
    }

    public void GetCpuPercentage(object sender, EventArgs e)
    {
        int percentage = (int)_cpuCounter.NextValue();
        CpuPercentage.Text = percentage.ToString() + '%';
    }

    public void GetCpuName()
    {
        ManagementObjectSearcher searcher = new("SELECT * FROM Win32_Processor");
        ManagementObjectCollection cpuCollection = searcher.Get();

        foreach (ManagementObject mo in cpuCollection.Cast<ManagementObject>())
        {
            CpuName.Text = mo["Name"].ToString();
        }
    }

    public void GetCpuSpeed()
    {
        string rk = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0")!.GetValue("~MHz")!.ToString()!;
        CpuSpeed.Text = rk + " " + "Mhz";
    }
}