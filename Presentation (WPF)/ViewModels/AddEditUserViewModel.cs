using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Enums;
using DVLD.Contracts.Person;
using DVLD.Contracts.User;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class AddEditUserViewModel : ObservableObject
{
    private readonly IUsersApiClient _usersApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly ICurrentUserSession _currentUserSession;
    private readonly IAuthApiClient _authApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private string _currentUsername = string.Empty;
    [ObservableProperty] private string _currentFullName = string.Empty;
    [ObservableProperty] private PersonResponse? _person;
    [ObservableProperty] private OperationMode _mode;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private int _selectedFilterIndex;
    [ObservableProperty] private int _userId;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string? _userIdDisplay = "???";
    [ObservableProperty]
    private string _userNameValidationMessage =
        "3-20 chars, start with a letter, numbers & _ allowed.";
    [ObservableProperty] private string _userNameValidationColor = "Gray";
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private bool _isConfirmPasswordVisible;
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _canGoToNextTab;
    [ObservableProperty] private Visibility _passwordVisibility = Visibility.Visible;
    [ObservableProperty] private Visibility _confirmPasswordVisibility = Visibility.Visible;

    public event Action<bool>? SaveCompleted;

    public AddEditUserViewModel(
        IUsersApiClient usersApiClient,
        IPeopleApiClient peopleApiClient,
        ICurrentUserSession currentUserSession,
        IAuthApiClient authApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _usersApiClient = usersApiClient ?? throw new ArgumentNullException(nameof(usersApiClient));
        _peopleApiClient = peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _currentUserSession = currentUserSession ?? throw new ArgumentNullException(nameof(currentUserSession));
        _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));

        CurrentUsername = _currentUserSession.Username;
        CurrentFullName = _currentUserSession.FullName;
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextTab))]
    private void GoToNextTab()
    {
        if (SelectedTabIndex < 1)
            SelectedTabIndex++;
    }

    public async Task InitializeAsync(int? userId)
    {
        try
        {
            CanGoToNextTab = false;
            GoToNextTabCommand.NotifyCanExecuteChanged();

            if (userId is > 0)
            {
                Mode = OperationMode.Edit;
                UserId = userId.Value;
                UserIdDisplay = userId.Value.ToString();

                PasswordVisibility = Visibility.Collapsed;
                ConfirmPasswordVisibility = Visibility.Collapsed;
                Password = string.Empty;
                ConfirmPassword = string.Empty;

                var userResult = await _usersApiClient.GetByIdAsync(userId.Value);

                if (userResult.IsFailure)
                {
                    _notifications.ShowFailure(userResult, "User Not Found");
                    return;
                }

                if (userResult.Value is null)
                {
                    _userNotifications.ShowWarning(
                        "User data could not be loaded.",
                        "User Not Found");
                    return;
                }

                var user = userResult.Value;
                UserName = user.UserName;
                IsActive = user.IsActive;

                var personResult = await _peopleApiClient.GetByIdAsync(user.PersonId);

                if (personResult.IsFailure)
                {
                    _notifications.ShowFailure(personResult, "Person Not Found");
                    return;
                }

                if (personResult.Value is null)
                {
                    _userNotifications.ShowWarning(
                        "The person associated with this user could not be found.",
                        "Person Not Found");
                    return;
                }

                Person = personResult.Value;
                CanGoToNextTab = true;
                GoToNextTabCommand.NotifyCanExecuteChanged();
                return;
            }

            Mode = OperationMode.Add;
            UserId = 0;
            UserIdDisplay = null;
            Person = null;
            FilterText = string.Empty;
            SelectedFilterIndex = 0;
            UserName = string.Empty;
            PasswordVisibility = Visibility.Visible;
            ConfirmPasswordVisibility = Visibility.Visible;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            IsActive = true;
            SelectedTabIndex = 0;
            CanGoToNextTab = false;
            UserNameValidationMessage =
                "3-20 chars, start with a letter, numbers & _ allowed.";
            UserNameValidationColor = "Gray";
            GoToNextTabCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Initialization Error");
        }
    }

    public async Task InitializeCurrentProfileAsync()
    {
        try
        {
            CanGoToNextTab = false;
            GoToNextTabCommand.NotifyCanExecuteChanged();

            Mode = OperationMode.Edit;
            PasswordVisibility = Visibility.Collapsed;
            ConfirmPasswordVisibility = Visibility.Collapsed;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            SelectedTabIndex = 0;

            var result = await _authApiClient.GetProfileAsync();

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Profile Error");
                return;
            }

            if (result.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Your profile could not be loaded.",
                    "Profile Error");
                return;
            }

            var profile = result.Value;

            UserId = profile.UserId;
            UserIdDisplay = profile.UserId.ToString();
            UserName = profile.UserName;
            IsActive = profile.IsActive;

            Person = new PersonResponse
            {
                PersonId = profile.PersonId,
                NationalNo = profile.NationalNo,
                FirstName = profile.FirstName,
                SecondName = profile.SecondName,
                ThirdName = profile.ThirdName,
                LastName = profile.LastName,
                FullName = profile.FullName,
                DateOfBirth = profile.DateOfBirth,
                Gender = (Gender)profile.Gender,
                Address = profile.Address,
                Phone = profile.Phone,
                Email = profile.Email,
                NationalityCountryID = profile.NationalityCountryID,
                CountryName = profile.CountryName,
                ImagePath = profile.ImagePath
            };

            CanGoToNextTab = true;
            GoToNextTabCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Profile Error");
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            _userNotifications.ShowWarning(
                "Please enter a Person ID or National Number.",
                "Search");
            return;
        }

        CanGoToNextTab = false;
        GoToNextTabCommand.NotifyCanExecuteChanged();

        try
        {
            PersonResponse? person;

            if (SelectedFilterIndex == 0)
            {
                if (!int.TryParse(FilterText.Trim(), out var personId))
                {
                    _userNotifications.ShowWarning(
                        "Please enter a valid Person ID.",
                        "Invalid ID");
                    return;
                }

                var result = await _peopleApiClient.GetByIdAsync(personId);

                if (result.IsFailure)
                {
                    _notifications.ShowFailure(result, "Person Not Found");
                    return;
                }

                if (result.Value is null)
                {
                    _userNotifications.ShowWarning(
                        "Person data could not be loaded.",
                        "Person Not Found");
                    return;
                }

                person = result.Value;
            }
            else
            {
                var result = await _peopleApiClient.GetByNationalNoAsync(
                    FilterText.Trim());

                if (result.IsFailure)
                {
                    _notifications.ShowFailure(result, "Person Not Found");
                    return;
                }

                if (result.Value is null)
                {
                    _userNotifications.ShowWarning(
                        "Person data could not be loaded.",
                        "Person Not Found");
                    return;
                }

                person = result.Value;
            }

            var existingUserResult =
                await _usersApiClient.GetByPersonIdAsync(person.PersonId);

            if (existingUserResult.IsSuccess &&
                existingUserResult.Value is not null)
            {
                var existingUser = existingUserResult.Value;

                if (Mode == OperationMode.Add ||
                    existingUser.UserId != UserId)
                {
                    _userNotifications.ShowInfo(
                        $"This person is already associated with the user account '{existingUser.UserName}'.",
                        "User Already Exists");
                    return;
                }

                UserName = existingUser.UserName;
                IsActive = existingUser.IsActive;
            }

            Person = person;
            CanGoToNextTab = true;
            GoToNextTabCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Search Error");
        }
    }

    [RelayCommand]
    private void AddPerson()
    {
        var vm = App.ServiceProvider
            .GetRequiredService<AddEditPersonViewModel>();

        var window = new AddEditPersonWin(vm)
        {
            Owner = Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        try
        {
            if (Person is null)
            {
                _userNotifications.ShowWarning(
                    "You must search for and select a person first.",
                    "Validation Error");
                return;
            }

            var isEdit = Mode == OperationMode.Edit;

            if (!isEdit && string.IsNullOrWhiteSpace(Password))
            {
                _userNotifications.ShowWarning(
                    "Password is required.",
                    "Validation Error");
                return;
            }

            if ((!isEdit || !string.IsNullOrWhiteSpace(Password)) &&
                Password != ConfirmPassword)
            {
                _userNotifications.ShowWarning(
                    "The entered passwords do not match.",
                    "Validation Error");
                return;
            }

            if (!isEdit)
            {
                var request = new CreateUserRequest(
                    Person.PersonId,
                    UserName.Trim(),
                    Password,
                    IsActive);

                var existingUserResult =
                    await _usersApiClient.GetByUsernameAsync(request.UserName);

                if (existingUserResult.IsFailure &&
                    existingUserResult.StatusCode is not System.Net.HttpStatusCode.NotFound)
                {
                    _notifications.ShowFailure(
                        existingUserResult,
                        "Validation Error");
                    return;
                }

                if (existingUserResult.IsSuccess &&
                    existingUserResult.Value is not null)
                {
                    _userNotifications.ShowWarning(
                        "This username is already taken. Please choose another.",
                        "Validation Error");
                    return;
                }

                var result = await _usersApiClient.CreateAsync(request);

                if (result.IsFailure)
                {
                    _notifications.ShowFailure(
                        result,
                        "Save Failed");
                    SaveCompleted?.Invoke(false);
                    return;
                }

                UserId = result.Value;
                UserIdDisplay = result.Value.ToString();
                Mode = OperationMode.Edit;
                PasswordVisibility = Visibility.Collapsed;
                ConfirmPasswordVisibility = Visibility.Collapsed;
                Password = string.Empty;
                ConfirmPassword = string.Empty;

                _userNotifications.ShowInfo(
                    $"The user account has been created successfully.\n\nUser ID: {UserId}",
                    "Operation Completed");

                SaveCompleted?.Invoke(true);
                return;
            }

            var updateRequest = new UpdateUserRequest(
                Person.PersonId,
                UserName.Trim(),
                IsActive);

            var existingUserByNameResult =
                await _usersApiClient.GetByUsernameAsync(
                    updateRequest.UserName);

            if (existingUserByNameResult.IsFailure &&
                existingUserByNameResult.StatusCode is not System.Net.HttpStatusCode.NotFound)
            {
                _notifications.ShowFailure(
                    existingUserByNameResult,
                    "Validation Error");
                return;
            }

            if (existingUserByNameResult.IsSuccess &&
                existingUserByNameResult.Value is not null &&
                existingUserByNameResult.Value.UserId != UserId)
            {
                _userNotifications.ShowWarning(
                    "This username is already taken by another user. Please choose another.",
                    "Validation Error");
                return;
            }

            var updateResult =
                await _usersApiClient.UpdateAsync(UserId, updateRequest);

            if (updateResult.IsFailure)
            {
                _notifications.ShowFailure(
                    updateResult,
                    "Save Failed");
                SaveCompleted?.Invoke(false);
                return;
            }

            Password = string.Empty;
            ConfirmPassword = string.Empty;

            _userNotifications.ShowInfo(
                "The user account has been updated successfully.",
                "Operation Completed");

            SaveCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Save Error");

            SaveCompleted?.Invoke(false);
        }
    }

    [RelayCommand]
    private void Cancel() => MainWindow.Navigation.GoBack();

    [RelayCommand]
    private void TogglePasswordVisibility() =>
        IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility() =>
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;

    partial void OnUserNameChanged(string value)
    {
        var sanitized = value?.Trim().ToLowerInvariant() ?? string.Empty;

        if (_userName != sanitized)
        {
            UserName = sanitized;
            return;
        }

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            UserNameValidationMessage = "Username is required.";
            UserNameValidationColor = "Red";
            return;
        }

        UserNameValidationMessage =
            "Username format will be validated by the server.";
        UserNameValidationColor = "Gray";
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}