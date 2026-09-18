using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Person;
using DVLD.Contracts.User;
using DVLD.Contracts.Users;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Enums;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Net;
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
            SetCanGoToNextTab(false);

            if (userId is > 0)
            {
                await InitializeEditAsync(userId.Value);
                return;
            }

            InitializeAdd();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(GetExceptionMessage(ex), "Initialization Error");
        }
    }

    private async Task InitializeEditAsync(int userId)
    {
        Mode = OperationMode.Edit;
        UserId = userId;
        UserIdDisplay = userId.ToString();
        SetPasswordVisibility(false);
        Password = ConfirmPassword = string.Empty;

        var result = await _usersApiClient.GetDetailsAsync(userId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "User Details");
            return;
        }

        if (result.Value is null)
        {
            _userNotifications.ShowWarning(
                "User details could not be loaded.",
                "User Details");
            return;
        }

        var details = result.Value;

        UserName = details.UserName;
        IsActive = details.IsActive;
        Person = MapPerson(details);

        SetCanGoToNextTab(true);
    }

    private void InitializeAdd()
    {
        Mode = OperationMode.Add;
        UserId = 0;
        UserIdDisplay = null;
        Person = null;
        FilterText = string.Empty;
        SelectedFilterIndex = 0;
        UserName = string.Empty;
        Password = ConfirmPassword = string.Empty;
        IsActive = true;
        SelectedTabIndex = 0;
        UserNameValidationMessage =
            "3-20 chars, start with a letter, numbers & _ allowed.";
        UserNameValidationColor = "Gray";
        SetPasswordVisibility(true);
    }

    public async Task InitializeCurrentProfileAsync()
    {
        try
        {
            SetCanGoToNextTab(false);
            Mode = OperationMode.Edit;
            SetPasswordVisibility(false);
            Password = ConfirmPassword = string.Empty;
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

            SetCanGoToNextTab(true);
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(GetExceptionMessage(ex), "Profile Error");
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

        SetCanGoToNextTab(false);

        try
        {
            var person = await FindPersonAsync();

            if (person is null)
                return;

            var existingUserResult =
                await _usersApiClient.GetByPersonIdAsync(person.PersonId);

            if (existingUserResult.IsSuccess && existingUserResult.Value is not null)
            {
                var existingUser = existingUserResult.Value;

                if (Mode == OperationMode.Add || existingUser.UserId != UserId)
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
            SetCanGoToNextTab(true);
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(GetExceptionMessage(ex), "Search Error");
        }
    }

    private async Task<PersonResponse?> FindPersonAsync()
    {
        if (SelectedFilterIndex == 0)
        {
            if (!int.TryParse(FilterText.Trim(), out var personId))
            {
                _userNotifications.ShowWarning(
                    "Please enter a valid Person ID.",
                    "Invalid ID");
                return null;
            }

            var result = await _peopleApiClient.GetByIdAsync(personId);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Person Not Found");
                return null;
            }

            return HandlePersonResult(result.Value);
        }

        var nationalNoResult =
            await _peopleApiClient.GetByNationalNoAsync(FilterText.Trim());

        if (nationalNoResult.IsFailure)
        {
            _notifications.ShowFailure(nationalNoResult, "Person Not Found");
            return null;
        }

        return HandlePersonResult(nationalNoResult.Value);
    }

    private PersonResponse? HandlePersonResult(PersonResponse? person)
    {
        if (person is not null)
            return person;

        _userNotifications.ShowWarning(
            "Person data could not be loaded.",
            "Person Not Found");

        return null;
    }

    [RelayCommand]
    private void AddPerson()
    {
        var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
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
            if (!ValidateSave())
                return;

            if (Mode == OperationMode.Add)
                await CreateUserAsync();
            else
                await UpdateUserAsync();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(GetExceptionMessage(ex), "Save Error");
            SaveCompleted?.Invoke(false);
        }
    }

    private bool ValidateSave()
    {
        if (Person is null)
        {
            _userNotifications.ShowWarning(
                "You must search for and select a person first.",
                "Validation Error");
            return false;
        }

        var isEdit = Mode == OperationMode.Edit;

        if (!isEdit && string.IsNullOrWhiteSpace(Password))
        {
            _userNotifications.ShowWarning(
                "Password is required.",
                "Validation Error");
            return false;
        }

        if ((!isEdit || !string.IsNullOrWhiteSpace(Password)) &&
            Password != ConfirmPassword)
        {
            _userNotifications.ShowWarning(
                "The entered passwords do not match.",
                "Validation Error");
            return false;
        }

        return true;
    }

    private async Task CreateUserAsync()
    {
        var request = new CreateUserRequest(
            Person!.PersonId,
            UserName.Trim(),
            Password,
            IsActive);

        if (!await ValidateUsernameAsync(request.UserName))
            return;

        var result = await _usersApiClient.CreateAsync(request);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Save Failed");
            SaveCompleted?.Invoke(false);
            return;
        }

        UserId = result.Value;
        UserIdDisplay = result.Value.ToString();
        Mode = OperationMode.Edit;
        SetPasswordVisibility(false);
        Password = ConfirmPassword = string.Empty;

        _userNotifications.ShowInfo(
            $"The user account has been created successfully.\n\nUser ID: {UserId}",
            "Operation Completed");

        SaveCompleted?.Invoke(true);
    }

    private async Task UpdateUserAsync()
    {
        var request = new UpdateUserRequest(
            Person!.PersonId,
            UserName.Trim(),
            IsActive);

        if (!await ValidateUsernameAsync(request.UserName))
            return;

        var result = await _usersApiClient.UpdateAsync(UserId, request);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Save Failed");
            SaveCompleted?.Invoke(false);
            return;
        }

        Password = ConfirmPassword = string.Empty;

        _userNotifications.ShowInfo(
            "The user account has been updated successfully.",
            "Operation Completed");

        SaveCompleted?.Invoke(true);
    }

    private async Task<bool> ValidateUsernameAsync(string username)
    {
        var result = await _usersApiClient.GetByUsernameAsync(username);

        if (result.IsFailure && result.StatusCode != HttpStatusCode.NotFound)
        {
            _notifications.ShowFailure(result, "Validation Error");
            return false;
        }

        if (result.IsSuccess &&
            result.Value is not null &&
            (Mode == OperationMode.Add || result.Value.UserId != UserId))
        {
            _userNotifications.ShowWarning(
                Mode == OperationMode.Add
                    ? "This username is already taken. Please choose another."
                    : "This username is already taken by another user. Please choose another.",
                "Validation Error");
            return false;
        }

        return true;
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

    private void SetCanGoToNextTab(bool value)
    {
        CanGoToNextTab = value;
        GoToNextTabCommand.NotifyCanExecuteChanged();
    }

    private void SetPasswordVisibility(bool visible)
    {
        PasswordVisibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ConfirmPasswordVisibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private static PersonResponse MapPerson(UserDetailsResponse details) =>
        new()
        {
            PersonId = details.PersonId,
            NationalNo = details.NationalNo,
            FirstName = details.FirstName,
            SecondName = details.SecondName,
            ThirdName = details.ThirdName,
            LastName = details.LastName,
            FullName = string.Join(
                " ",
                new[] { details.FirstName, details.SecondName, details.ThirdName, details.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))),
            DateOfBirth = details.DateOfBirth,
            Gender = Enum.TryParse<Gender>(details.Gender, out var gender)
                ? gender
                : default,
            Address = details.Address,
            Phone = details.Phone,
            Email = details.Email,
            NationalityCountryID = details.NationalityCountryID,
            CountryName = details.CountryName,
            ImagePath = details.ImagePath
        };

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}