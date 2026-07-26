using System.Windows.Forms;

namespace AutoBrowser.Services;

public interface INotificationService
{
    void Show(string title, string message, ToolTipIcon icon = ToolTipIcon.Info);
}