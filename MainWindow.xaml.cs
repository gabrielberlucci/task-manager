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
using System.Management;
using System.Windows.Markup;

namespace TaskManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    double _TotalRAM;



    public MainWindow()
    {
        InitializeComponent();
        const sbyte REFRESH_RATE = 1;
        _TotalRAM = Math.Round(double.Parse(GetRAMInfo("TotalVisibleMemorySize")) / Math.Pow(1024, 2));
        InitializeCpuStaticInfo();
        GetCpuCacheL1();
        GetTotalRAM(_TotalRAM);

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(REFRESH_RATE)
        };
        timer.Tick += GetProcessFromSystem!;
        timer.Tick += GetCpuPercentage!;
        timer.Tick += GetUsedRAM!;
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


    public void InitializeCpuStaticInfo()
    {
        ManagementObjectSearcher searcher = new("SELECT * FROM Win32_Processor");
        ManagementObjectCollection cpuCollection = searcher.Get();

        foreach (ManagementObject mo in cpuCollection.Cast<ManagementObject>())
        {
            CpuName.Text = mo["Name"].ToString();
            CpuSpeed.Text = mo["CurrentClockSpeed"] + " " + "MHz";
            float tempCpuSpeed = float.Parse(mo["MaxClockSpeed"].ToString()!) / 1000;
            BaseSpeedText.Text = tempCpuSpeed.ToString() + " " + "GHz";
            SocketsText.Text = mo["SocketDesignation"].ToString();
            CoresText.Text = mo["NumberOfCores"].ToString();
            LogicalProcText.Text = mo["NumberOfLogicalProcessors"].ToString();

            string virtualization = mo["VirtualizationFirmwareEnabled"].ToString()!;
            if (virtualization == "True")
            {
                VirtualizationText.Text = "Enabled";
            }
            else
            {
                VirtualizationText.Text = "Disabled";
            }

            float l2CacheTemp = float.Parse(mo["L2CacheSize"].ToString()!) / 1024;
            L2CacheText.Text = l2CacheTemp.ToString() + " " + "MB";

            float l3CacheTemp = float.Parse(mo["L3CacheSize"].ToString()!) / 10240;
            L3CacheText.Text = l3CacheTemp.ToString() + " " + "MB";
        }
    }

    public void GetCpuCacheL1()
    {
        ManagementObjectSearcher searcher = new("SELECT * FROM Win32_CacheMemory WHERE Level = 3");
        ManagementObjectCollection cacheCollection = searcher.Get();

        foreach (ManagementObject mo in cacheCollection.Cast<ManagementObject>())
        {
            L1CacheText.Text = mo["MaxCacheSize"].ToString()! + " " + "KB";
        }
    }

    public string GetRAMInfo(string property)
    {
        ManagementObjectSearcher searcher = new("SELECT * FROM Win32_OperatingSystem");
        ManagementObjectCollection cpuCollection = searcher.Get();

        string s = "";

        foreach (ManagementObject mo in cpuCollection.Cast<ManagementObject>())
        {
            s = mo[property].ToString()!;
        }

        return s;
    }

    public void GetTotalRAM(double ram)
    {
        RamAmount.Text = ram.ToString() + " " + "GB";
    }

    public void GetUsedRAM(object sender, EventArgs e)
    {
        double temp = _TotalRAM - double.Parse(GetRAMInfo("FreePhysicalMemory")) / Math.Pow(1024, 2);
        RamUsage.Text = temp.ToString("N2") + " " + "GB";
    }
}
