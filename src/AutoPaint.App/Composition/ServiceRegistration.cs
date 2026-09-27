using AutoPaint.App.Bridge;
using AutoPaint.App.Overlays;
using AutoPaint.App.Painting;
using AutoPaint.App.Updates;
using AutoPaint.Core.Config;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Licensing;
using AutoPaint.Core.Painting;
using AutoPaint.Core.Platform;
using AutoPaint.Platform.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPaint.App.Composition;

/// <summary>Composition root. Único lugar que amarra Core + Platform.Windows + App.</summary>
public static class ServiceRegistration
{
    public static ServiceProvider Build()
    {
        var s = new ServiceCollection();

        // Core (puro)
        s.AddSingleton<IDelayService, InterruptibleDelayService>();
        s.AddSingleton<ILicenseGate, AlwaysAllowedLicenseGate>();
        s.AddSingleton<PlanBuilder>();
        s.AddSingleton<PaintOrderPlanner>();
        s.AddSingleton<ImageToPlanService>();

        // Platform.Windows
        s.AddSingleton<Win32ScreenGeometry>();
        s.AddSingleton<IScreenGeometry>(sp => sp.GetRequiredService<Win32ScreenGeometry>());
        s.AddSingleton<IScreenReader, GdiScreenReader>();
        s.AddSingleton<IImageRasterizer, GdiImageRasterizer>();
        s.AddSingleton<IInputSender, WindowsInputSender>();
        s.AddSingleton<IHotkeyService, LowLevelHotkeyService>();
        s.AddSingleton<IConfigStore, JsonConfigStore>();

        // Motor de pintura
        s.AddTransient<IPaintEngine, PaintEngine>();

        // App / ponte
        s.AddSingleton<UpdateService>();
        s.AddSingleton<BridgeEventHub>();
        s.AddSingleton<CalibrationOverlayService>();
        s.AddSingleton<PaintOverlayController>();
        s.AddSingleton<PaintSession>();
        s.AddSingleton<BridgeRouter>();
        s.AddSingleton<MainWindow>();

        return s.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
