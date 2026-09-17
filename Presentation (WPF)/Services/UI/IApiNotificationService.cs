using Presentation.Services.Results;

namespace Presentation.Services.UI;

public interface IApiNotificationService
{
    bool ShowFailure(ApiResult result, string title = "Error");
}