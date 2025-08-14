using System.Reflection;

namespace CarnageWatcher;

public partial class CarnageWatcher : Form
{
    private readonly FileSystemWatcher? _watcher;
    private readonly string? _fileToWatch;

    private System.Threading.Timer? _debounceTimer;
    private readonly Lock _debounceLock = new();

    private readonly Label _lblStatus;
    private readonly TextBox _txtLog;

    private void SetIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream("CarnageWatcher.carnage_reporter.ico")!;
        var icon = new Icon(stream);
        this.Icon = icon;

        var notify = new NotifyIcon
        {
            Icon = icon,
            Visible = true
        };
    }

    public CarnageWatcher()
    {
        InitializeComponent();
        SetIcon();

        _lblStatus = new Label
        {
            Name = "lblStatus",
            Text = "Status: Initializing",
            Dock = DockStyle.Top,
            Height = 25
        };

        _txtLog = new TextBox
        {
            Name = "txtLog",
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical
        };

        Controls.Add(_txtLog);
        Controls.Add(_lblStatus);

        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string carnageDirectory = Path.Combine(userFolder, "AppData", "LocalLow", "MCC", "Temporary");

        if (!Directory.Exists(carnageDirectory))
        {
            Log($"The folder was not found: {carnageDirectory}");
            SetStatus("Error");
            return;
        }

        var lastFile = Directory.GetFiles(carnageDirectory, "*.xml")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .FirstOrDefault();

        if (lastFile == null)
        {
            Log("No XML file was found in the Temporary MCC folder.");
            SetStatus("Error");
            return;
        }

        _fileToWatch = lastFile;

        _watcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(_fileToWatch)!,
            Filter = Path.GetFileName(_fileToWatch),
            NotifyFilter = NotifyFilters.LastWrite
        };

        _watcher.Changed += OnCarnageReportChanged;
        _watcher.Created += OnCarnageReportChanged;
        _watcher.EnableRaisingEvents = true;

        this.Load += LoadCarnageWatcher!;
    }

    private void LoadCarnageWatcher(object sender, EventArgs e)
    {
        Log("Watcher started. Monitoring: " + _fileToWatch);
        SetStatus("Watching");
    }

    private void OnCarnageReportChanged(object sender, FileSystemEventArgs e)
    {
        lock (_debounceLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Threading.Timer(_ =>
            {
                ProcessCarnageReport(e);
            }, null, 700, Timeout.Infinite);
        }
    }

    private async void ProcessCarnageReport(FileSystemEventArgs e)
    {
        try
        {
            SetStatus("Change Detected");
            Log($"Change detected: {e.FullPath}");

            await Task.Delay(500);

            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string tempFile = Path.Combine(Path.GetTempPath(), $"Carnage_{timeStamp}.xml");
            File.Copy(_fileToWatch!, tempFile, true);
            Log($"Copied to temp: {tempFile}");

            SetStatus("Uploading");

            var uploader = new DiscordWebhookUploader("https://discordapp.com/api/webhooks/1405342667923263651/_wN9N0WGp37Unw3sYodtIRJsJcHOYTbCWqyOstkISdSjTJk-dMFC4XbhVdH1BoMkwUZm");
            bool Success = await uploader.SendFileAsync(tempFile);

            if (Success)
            {
                Log("Upload Successful.");
                SetStatus("Watching.");
            }
            else
            {
                Log("Upload Failed!");
                SetStatus("Error");
            }
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
            SetStatus("Error");
        }
    }

    private void Log(string message)
    {
        Invoke(() =>
        {
            _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        });
    }

    private void SetStatus(string status)
    {
        Invoke(() =>
        {
            _lblStatus.Text = $"Status: {status}";
        });
    }
}
