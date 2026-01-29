namespace Scrapile.Desktop.Services;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Service for controlling macOS dock icon visibility.
/// Uses Objective-C runtime to change the app's activation policy.
/// </summary>
public static class MacOSDockService
{
    private const long NSApplicationActivationPolicyRegular = 0;
    private const long NSApplicationActivationPolicyAccessory = 1;

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string className);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_long(IntPtr receiver, IntPtr selector, long arg);

    /// <summary>
    /// Shows the dock icon by setting activation policy to Regular.
    /// </summary>
    public static void ShowDockIcon()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        try
        {
            SetActivationPolicy(NSApplicationActivationPolicyRegular);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to show dock icon: {ex.Message}");
        }
    }

    /// <summary>
    /// Hides the dock icon by setting activation policy to Accessory.
    /// </summary>
    public static void HideDockIcon()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        try
        {
            SetActivationPolicy(NSApplicationActivationPolicyAccessory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to hide dock icon: {ex.Message}");
        }
    }

    private static void SetActivationPolicy(long policy)
    {
        var nsApplicationClass = objc_getClass("NSApplication");
        var sharedApplicationSelector = sel_registerName("sharedApplication");
        var setActivationPolicySelector = sel_registerName("setActivationPolicy:");

        var nsApp = objc_msgSend(nsApplicationClass, sharedApplicationSelector);
        objc_msgSend_long(nsApp, setActivationPolicySelector, policy);
    }
}
