using System;
using System.Drawing;
using System.Windows.Forms;
using Serilog;

namespace AutoBrowser.Services;

public class NotificationService : INotificationService
{
    public void Show(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var notifyIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""),
                Visible = true
            };
            notifyIcon.ShowBalloonTip(3000, title, message, icon);

            _ = Task.Delay(4000).ContinueWith(_ =>
            {
                try
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to dispose notification icon");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to show notification: {Title} - {Message}", title, message);
        }
    }
}