using System.Windows;

namespace Presentation.Services.UI;

public interface IUserNotificationService
{
    void ShowError(string message, string title = "Error");
    void ShowWarning(string message, string title = "Warning");
    void ShowInfo(string message, string title = "Information");
    MessageBoxResult ShowConfirmation(string message, string title = "Confirm");
}