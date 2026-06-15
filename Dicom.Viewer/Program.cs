using Dicom.Viewer;
using FellowOakDicom;
using FellowOakDicom.Imaging;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show($"{e.Exception.GetType().Name}: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "DICOM Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var log = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(log, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.ExceptionObject}\n\n");
        };

        new DicomSetupBuilder()
            .RegisterServices(s => s.AddFellowOakDicom()
                .AddImageManager<RawImageManager>()
                .AddTranscoderManager<FellowOakDicom.Imaging.NativeCodec.NativeTranscoderManager>())
            .Build();

        string? initialFile = args.Length == 1 && File.Exists(args[0]) ? args[0] : null;
        Application.Run(new DicomViewerForm(initialFile));
    }
}
