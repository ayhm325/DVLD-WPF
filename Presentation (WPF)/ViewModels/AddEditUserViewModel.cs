using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Validators;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views;
using Presentation.Views.Windows;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels;

public partial class AddEditUserViewModel : ObservableObject
{
    // DEPENDENCIES
    private readonly IUserService _userService;
    private readonly IPersonService _personService;
    private readonly ICurrentUserService _currentUserService;

    // CONSTRUCTOR
    public AddEditUserViewModel(IUserService userService, IPersonService personService, ICurrentUserService currentUserService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

        CurrentUsername = _currentUserService.Username;
        CurrentFullName = _currentUserService.FullName;
    }

    // EVENTS
    public event Action<bool>? SaveCompleted;

    // CURRENT USER
    [ObservableProperty] private string _currentUsername = string.Empty;
    [ObservableProperty] private string _currentFullName = string.Empty;

    // PERSON
    [ObservableProperty] private PersonDto? _person;

    // MODE
    [ObservableProperty] private OperationMode _mode;

    // SEARCH
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private int _selectedFilterIndex;

    // USER DATA
    [ObservableProperty] private int _userId;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    // DISPLAY
    [ObservableProperty] private string? _userIdDisplay = "???";
    [ObservableProperty] private string _userNameValidationMessage = "3-20 chars, start with a letter, numbers & _ allowed.";
    [ObservableProperty] private string _userNameValidationColor = "Gray";

    // PASSWORD VISIBILITY
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private bool _isConfirmPasswordVisible;

    // TABS
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _canGoToNextTab;

    // UI VISIBILITY
    public Visibility PasswordVisibility => Visibility.Visible;
    public Visibility ConfirmPasswordVisibility => Visibility.Visible;

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
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                OnPropertyChanged(nameof(PasswordVisibility));
                OnPropertyChanged(nameof(ConfirmPasswordVisibility));

                var userResult = await _userService.GetUserByIdAsync(userId.Value);
                if (userResult.IsFailure || userResult.Value is null)
                {
                    MessageBox.Show(userResult.Error ?? "User data could not be loaded.", "User Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = userResult.Value;
                UserName = user.UserName;
                IsActive = user.IsActive;

                var personResult = await _personService.GetPersonByIdAsync(user.PersonId);
                if (personResult.IsFailure || personResult.Value is null)
                {
                    MessageBox.Show(personResult.Error ?? "The person associated with this user could not be found.",
                        "Person Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            IsActive = true;
            SelectedTabIndex = 0;
            CanGoToNextTab = false;
            UserNameValidationMessage = "3-20 chars, start with a letter, numbers & _ allowed.";
            UserNameValidationColor = "Gray";
            OnPropertyChanged(nameof(PasswordVisibility));
            OnPropertyChanged(nameof(ConfirmPasswordVisibility));
            GoToNextTabCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while initializing the user form.\n\n{ex.Message}",
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // NEXT TAB
    [RelayCommand(CanExecute = nameof(CanGoToNextTab))]
    private void GoToNextTab() => SelectedTabIndex = 1;

    // SEARCH PERSON
    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            MessageBox.Show("Please enter a Person ID or National Number.", "Search",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CanGoToNextTab = false;
        GoToNextTabCommand.NotifyCanExecuteChanged();

        try
        {
            Result<PersonDto> personResult;

            // BY PERSON ID
            if (SelectedFilterIndex == 0)
            {
                if (!int.TryParse(FilterText.Trim(), out int personId))
                {
                    MessageBox.Show("Please enter a valid Person ID.", "Invalid ID",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                personResult = await _personService.GetPersonByIdAsync(personId);
            }
            // BY NATIONAL NUMBER
            else
            {
                personResult = await _personService.GetPersonByNationalNoAsync(FilterText.Trim());
            }

            if (personResult.IsFailure || personResult.Value is null)
            {
                MessageBox.Show(personResult.Error ?? "Person data could not be loaded.", "Person Not Found",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var person = personResult.Value;

            // CHECK IF PERSON ALREADY HAS USER
            var existingUserResult = await _userService.GetUserByPersonIdAsync(person.PersonId);

            if (existingUserResult.IsSuccess && existingUserResult.Value is not null)
            {
                var existingUser = existingUserResult.Value;

                if (Mode == OperationMode.Add)
                {
                    MessageBox.Show(
                        $"This person is already associated with the user account '{existingUser.UserName}'.",
                        "User Already Exists", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (existingUser.UserId != UserId)
                {
                    MessageBox.Show(
                        $"This person is already associated with the user account '{existingUser.UserName}'.",
                        "User Already Exists", MessageBoxButton.OK, MessageBoxImage.Information);
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
            System.Diagnostics.Debug.WriteLine($"Error during person search: {ex}");
            MessageBox.Show($"An error occurred while searching for the person.\n\n{ex.Message}",
                "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ADD PERSON
    [RelayCommand]
    private void AddPerson()
    {
        var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
        var win = new AddEditPersonWin(vm) { Owner = System.Windows.Application.Current.MainWindow };
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
                MessageBox.Show("You must search for and select a person first.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isEdit = Mode == OperationMode.Edit;

            // CONFIRM PASSWORD
            if (!isEdit)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("Password is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Password != ConfirmPassword)
                {
                    MessageBox.Show("The entered passwords do not match.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else if (!string.IsNullOrWhiteSpace(Password) && Password != ConfirmPassword)
            {
                MessageBox.Show("The entered passwords do not match.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // CREATE
            if (!isEdit)
            {
                var createDto = new CreateUserDto
                {
                    PersonId = Person.PersonId,
                    UserName = UserName.Trim(),
                    Password = Password,
                    IsActive = IsActive
                };

                var validationResult = UserValidator.ValidateCreateUser(createDto);
                if (validationResult.IsFailure)
                {
                    MessageBox.Show(validationResult.Error, "Validation Errors",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (await _userService.IsUsernameTakenForAnotherUserAsync(createDto.UserName, 0))
                {
                    MessageBox.Show("This username is already taken. Please choose another.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var addResult = await _userService.AddUserAsync(createDto);
                if (addResult.IsFailure)
                {
                    MessageBox.Show(addResult.Error, "Save Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    SaveCompleted?.Invoke(false);
                    return;
                }

                UserId = addResult.Value;
                UserIdDisplay = addResult.Value.ToString();
                Mode = OperationMode.Edit;
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                OnPropertyChanged(nameof(PasswordVisibility));
                OnPropertyChanged(nameof(ConfirmPasswordVisibility));

                MessageBox.Show($"The user account has been created successfully.\n\nUser ID: {UserId}",
                    "Operation Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                SaveCompleted?.Invoke(true);
                return;
            }

            // UPDATE
            var updateDto = new UpdateUserDto
            {
                PersonId = Person.PersonId,
                UserName = UserName.Trim(),
                Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
                IsActive = IsActive
            };

            var updateValidation = UserValidator.ValidateUpdateUser(updateDto);
            if (updateValidation.IsFailure)
            {
                MessageBox.Show(updateValidation.Error, "Validation Errors",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (await _userService.IsUsernameTakenForAnotherUserAsync(updateDto.UserName, UserId))
            {
                MessageBox.Show("This username is already taken by another user. Please choose another.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var updateResult = await _userService.UpdateUserAsync(UserId, updateDto);
            if (updateResult.IsFailure)
            {
                MessageBox.Show(updateResult.Error, "Save Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SaveCompleted?.Invoke(false);
                return;
            }

            Password = string.Empty;
            ConfirmPassword = string.Empty;

            MessageBox.Show("The user account has been updated successfully.", "Operation Completed",
                MessageBoxButton.OK, MessageBoxImage.Information);
            SaveCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error while saving user: {ex}");
            MessageBox.Show($"An unexpected error occurred while saving the user.\n\n{ex.Message}",
                "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            SaveCompleted?.Invoke(false);
        }
    }

    // CANCEL
    [RelayCommand]
    private void Cancel() => MainWindow.Navigation.GoBack();

    // TOGGLE PASSWORD
    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility() => IsConfirmPasswordVisible = !IsConfirmPasswordVisible;

    // USERNAME CHANGED
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

        var result = UserValidator.ValidateUsernameFormat(sanitized);
        if (result.IsSuccess)
        {
            UserNameValidationMessage = "✓ Username is valid.";
            UserNameValidationColor = "Green";
        }
        else
        {
            UserNameValidationMessage = result.Error ?? "Invalid username.";
            UserNameValidationColor = "Red";
        }
    }
}