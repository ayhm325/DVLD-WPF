using Presentation.Services.Results;
using System.Net;

namespace Presentation.Services.UI;

public sealed class ApiNotificationService(
    IUserNotificationService notificationService) : IApiNotificationService
{
    private readonly IUserNotificationService _notifications = notificationService;

    public bool ShowFailure(ApiResult result, string title = "Error")
    {
        if (result.IsSuccess)
            return false;

        switch (result.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                _notifications.ShowWarning(
                    "Your session has expired. Please sign in again.",
                    "Authentication Required");
                return true;

            case HttpStatusCode.Forbidden:
                _notifications.ShowWarning(
                    "You are not authorized to perform this operation.",
                    "Access Denied");
                return true;

            default:
                _notifications.ShowError(result.Error, title);
                return true;
        }
    }
}