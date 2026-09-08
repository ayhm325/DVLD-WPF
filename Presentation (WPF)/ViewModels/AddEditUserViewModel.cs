using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Enums;
using DVLD.Contracts.Person;
using DVLD.Contracts.User;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class AddEditUserViewModel : ObservableObject
{
    private readonly IUsersApiClient _usersApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly ICurrentUserSession _currentUserSession;

    public AddEditUserViewModel(
        IUsersApiClient usersApiClient,
        IPeopleApiClient peopleApiClient,
        ICurrentUserSession currentUserSession)
    {
        _usersApiClient = usersApiClient
            ?? throw new ArgumentNullException(nameof(usersApiClient));

        _peopleApiClient = peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        _currentUserSession = currentUserSession
            ?? throw new ArgumentNullException(nameof(currentUserSession));

        CurrentUsername = _currentUserSession.Username;
        CurrentFullName = _currentUserSession.FullName;
    }

    public event Action<bool>? SaveCompleted;

    // CURRENT USER

    [ObservableProperty]
    private string _currentUsername = string.Empty;

    [ObservableProperty]
    private string _currentFullName = string.Empty;

    // PERSON

    [ObservableProperty]
    private PersonResponse? _person;

    // MODE

    [ObservableProperty]
    private OperationMode _mode;

    // SEARCH

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private int _selectedFilterIndex;

    // USER DATA

    [ObservableProperty]
    private int _userId;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    // DISPLAY

    [ObservableProperty]
    private string? _userIdDisplay = "???";

    [ObservableProperty]
    private string _userNameValidationMessage =
        "3-20 chars, start with a letter, numbers & _ allowed.";

    [ObservableProperty]
    private string _userNameValidationColor = "Gray";

    // PASSWORD VISIBILITY

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible;

    // TABS

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _canGoToNextTab;

    [RelayCommand(CanExecute = nameof(CanGoToNextTab))]
    private void GoToNextTab()
    {
        if (SelectedTabIndex < 1)
            SelectedTabIndex++;
    }

    // UI VISIBILITY

    [ObservableProperty]
    private Visibility _passwordVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _confirmPasswordVisibility = Visibility.Visible;

    // INITIALIZATION

    public async Task InitializeAsync(int? userId)
    {
        try
        {
            CanGoToNextTab = false;
            GoToNextTabCommand.NotifyCanExecuteChanged();

            // EDIT MODE

            if (userId.HasValue && userId.Value > 0)
            {
                Mode = OperationMode.Edit;
                UserId = userId.Value;
                UserIdDisplay = userId.Value.ToString();

                PasswordVisibility = Visibility.Collapsed;
                ConfirmPasswordVisibility = Visibility.Collapsed;

                Password = string.Empty;
                ConfirmPassword = string.Empty;

                var userResult =
                    await _usersApiClient.GetByIdAsync(userId.Value);

                if (userResult.IsFailure || userResult.Value is null)
                {
                    MessageBox.Show(
                        userResult.Error ?? "User data could not be loaded.",
                        "User Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var user = userResult.Value;

                UserName = user.UserName;
                IsActive = user.IsActive;

                var personResult =
                    await _peopleApiClient.GetByIdAsync(user.PersonId);

                if (personResult.IsFailure || personResult.Value is null)
                {
                    MessageBox.Show(
                        personResult.Error ??
                        "The person associated with this user could not be found.",
                        "Person Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                Person = personResult.Value;

                CanGoToNextTab = true;
                GoToNextTabCommand.NotifyCanExecuteChanged();

                return;
            }

            // ADD MODE

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
            MessageBox.Show(
                $"An error occurred while initializing the user form.\n\n{ex.Message}",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // SEARCH PERSON

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            MessageBox.Show(
                "Please enter a Person ID or National Number.",
                "Search",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        CanGoToNextTab = false;
        GoToNextTabCommand.NotifyCanExecuteChanged();

        try
        {
            PersonResponse? person;

            // BY PERSON ID

            if (SelectedFilterIndex == 0)
            {
                if (!int.TryParse(
                        FilterText.Trim(),
                        out int personId))
                {
                    MessageBox.Show(
                        "Please enter a valid Person ID.",
                        "Invalid ID",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var personResult =
                    await _peopleApiClient.GetByIdAsync(personId);

                if (personResult.IsFailure ||
                    personResult.Value is null)
                {
                    MessageBox.Show(
                        personResult.Error ??
                        "Person data could not be loaded.",
                        "Person Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                person = personResult.Value;
            }
            // BY NATIONAL NUMBER

            else
            {
                var personResult =
                    await _peopleApiClient.GetByNationalNoAsync(
                        FilterText.Trim());

                if (personResult.IsFailure ||
                    personResult.Value is null)
                {
                    MessageBox.Show(
                        personResult.Error ??
                        "Person data could not be loaded.",
                        "Person Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                person = personResult.Value;
            }

            // CHECK IF PERSON ALREADY HAS USER

            var existingUserResult =
                await _usersApiClient.GetByPersonIdAsync(
                    person.PersonId);

            if (existingUserResult.IsSuccess &&
                existingUserResult.Value is not null)
            {
                var existingUser = existingUserResult.Value;

                if (Mode == OperationMode.Add)
                {
                    MessageBox.Show(
                        $"This person is already associated with the user account '{existingUser.UserName}'.",
                        "User Already Exists",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                if (existingUser.UserId != UserId)
                {
                    MessageBox.Show(
                        $"This person is already associated with the user account '{existingUser.UserName}'.",
                        "User Already Exists",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                Person = person;
                UserName = existingUser.UserName;
                IsActive = existingUser.IsActive;
            }
            else
            {
                Person = person;
            }

            CanGoToNextTab = true;
            GoToNextTabCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error during person search: {ex}");

            MessageBox.Show(
                $"An error occurred while searching for the person.\n\n{ex.Message}",
                "Search Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ADD PERSON

    [RelayCommand]
    private void AddPerson()
    {
        var vm =
            App.ServiceProvider
                .GetRequiredService<AddEditPersonViewModel>();

        var win = new AddEditPersonWin(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        win.ShowDialog();
    }

    // SAVE USER

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        try
        {
            if (Person is null)
            {
                MessageBox.Show(
                    "You must search for and select a person first.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            bool isEdit = Mode == OperationMode.Edit;

            // PASSWORD CONFIRMATION

            if (!isEdit)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show(
                        "Password is required.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (Password != ConfirmPassword)
                {
                    MessageBox.Show(
                        "The entered passwords do not match.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }
            else if (!string.IsNullOrWhiteSpace(Password) &&
                     Password != ConfirmPassword)
            {
                MessageBox.Show(
                    "The entered passwords do not match.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // CREATE

            if (!isEdit)
            {
                var request = new CreateUserRequest(
                    Person.PersonId,
                    UserName.Trim(),
                    Password,
                    IsActive);

                var existingUserResult =
                    await _usersApiClient.GetByUsernameAsync(
                        request.UserName);

                if (existingUserResult.IsSuccess &&
                    existingUserResult.Value is not null)
                {
                    MessageBox.Show(
                        "This username is already taken. Please choose another.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var createResult =
                    await _usersApiClient.CreateAsync(request);

                if (createResult.IsFailure)
                {
                    MessageBox.Show(
                        createResult.Error,
                        "Save Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    SaveCompleted?.Invoke(false);
                    return;
                }

                UserId = createResult.Value;
                UserIdDisplay = createResult.Value.ToString();

                Mode = OperationMode.Edit;

                PasswordVisibility = Visibility.Collapsed;
                ConfirmPasswordVisibility = Visibility.Collapsed;

                Password = string.Empty;
                ConfirmPassword = string.Empty;

                MessageBox.Show(
                    $"The user account has been created successfully.\n\nUser ID: {UserId}",
                    "Operation Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                SaveCompleted?.Invoke(true);

                return;
            }

            // UPDATE

            var updateRequest = new UpdateUserRequest(
                Person.PersonId,
                UserName.Trim(),
                IsActive);

            var existingUserByNameResult =
                await _usersApiClient.GetByUsernameAsync(
                    updateRequest.UserName);

            if (existingUserByNameResult.IsSuccess &&
                existingUserByNameResult.Value is not null &&
                existingUserByNameResult.Value.UserId != UserId)
            {
                MessageBox.Show(
                    "This username is already taken by another user. Please choose another.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var updateResult =
                await _usersApiClient.UpdateAsync(
                    UserId,
                    updateRequest);

            if (updateResult.IsFailure)
            {
                MessageBox.Show(
                    updateResult.Error,
                    "Save Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                SaveCompleted?.Invoke(false);
                return;
            }

            Password = string.Empty;
            ConfirmPassword = string.Empty;

            MessageBox.Show(
                "The user account has been updated successfully.",
                "Operation Completed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            SaveCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error while saving user: {ex}");

            MessageBox.Show(
                $"An unexpected error occurred while saving the user.\n\n{ex.Message}",
                "Save Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SaveCompleted?.Invoke(false);
        }
    }

    // CANCEL

    [RelayCommand]
    private void Cancel()
    {
        MainWindow.Navigation.GoBack();
    }

    // PASSWORD VISIBILITY

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    // USERNAME CHANGED

    partial void OnUserNameChanged(string value)
    {
        var sanitized =
            value?.Trim().ToLowerInvariant() ?? string.Empty;

        if (_userName != sanitized)
        {
            UserName = sanitized;
            return;
        }

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            UserNameValidationMessage =
                "Username is required.";

            UserNameValidationColor = "Red";

            return;
        }

        UserNameValidationMessage =
            "Username format will be validated by the server.";

        UserNameValidationColor = "Gray";
    }
}