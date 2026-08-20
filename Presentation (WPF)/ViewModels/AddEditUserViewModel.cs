using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Validators;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

public partial class AddEditUserViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly IPersonService _personService;
    private readonly ICurrentUserService _currentUserService;

    public AddEditUserViewModel(
        IUserService userService,
        IPersonService personService,
        ICurrentUserService currentUserService)
    {
        _userService = userService
            ?? throw new ArgumentNullException(nameof(userService));

        _personService = personService
            ?? throw new ArgumentNullException(nameof(personService));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        CurrentUsername = _currentUserService.Username;
        CurrentFullName = _currentUserService.FullName;
    }

    #region Properties

    [ObservableProperty]
    private string _currentUsername = string.Empty;

    [ObservableProperty]
    private string _currentFullName = string.Empty;

    [ObservableProperty]
    private PersonDto? _person;

    [ObservableProperty]
    private OperationMode _mode;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private int _selectedFilterIndex;

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

    [ObservableProperty]
    private string? _userIdDisplay = "???";

    [ObservableProperty]
    private string _userNameValidationMessage =
        "3-20 chars, start with a letter, numbers & _ allowed.";

    [ObservableProperty]
    private string _userNameValidationColor = "Gray";

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _canGoToNextTab;

    public ObservableCollection<Person> People { get; } = [];

    public Visibility PasswordVisibility =>
        Mode == OperationMode.Add
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility ConfirmPasswordVisibility =>
        Mode == OperationMode.Add
            ? Visibility.Visible
            : Visibility.Collapsed;

    public event Action<bool>? SaveCompleted;

    #endregion

    #region Initialization

    public async Task InitializeAsync(int? userId)
    {
        CanGoToNextTab = false;
        GoToNextTabCommand.NotifyCanExecuteChanged();

        if (userId.HasValue && userId.Value > 0)
        {
            // =========================
            // EDIT MODE
            // =========================

            Mode = OperationMode.Edit;

            OnPropertyChanged(nameof(PasswordVisibility));
            OnPropertyChanged(nameof(ConfirmPasswordVisibility));

            UserId = userId.Value;
            UserIdDisplay = userId.Value.ToString();

            var userResult =
                await _userService.GetUserByIdAsync(userId.Value);

            if (userResult.IsFailure)
            {
                MessageBox.Show(
                    userResult.Error,
                    "User Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var user = userResult.Value!;

            UserName = user.UserName;
            IsActive = user.IsActive;

            var personResult =
                await _personService.GetPersonByIdAsync(user.PersonId);

            if (personResult.IsSuccess)
            {
                Person = personResult.Value!;

                CanGoToNextTab = true;
                GoToNextTabCommand.NotifyCanExecuteChanged();
            }
        }
        else
        {
            // =========================
            // ADD MODE
            // =========================

            Mode = OperationMode.Add;

            UserId = 0;
            UserIdDisplay = null;

            Person = null;

            UserName = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;

            IsActive = true;

            OnPropertyChanged(nameof(PasswordVisibility));
            OnPropertyChanged(nameof(ConfirmPasswordVisibility));
        }
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanGoToNextTab))]
    private void GoToNextTab()
    {
        SelectedTabIndex = 1;
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            return;

        CanGoToNextTab = false;
        GoToNextTabCommand.NotifyCanExecuteChanged();

        try
        {
            Result<PersonDto> personResult;

            if (SelectedFilterIndex == 0 &&
                int.TryParse(FilterText, out int id))
            {
                personResult =
                    await _personService.GetPersonByIdAsync(id);
            }
            else
            {
                personResult =
                    await _personService
                        .GetPersonByNationalNoAsync(FilterText);
            }

            if (personResult.IsFailure)
            {
                MessageBox.Show(
                    personResult.Error,
                    "Person Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var person = personResult.Value!;

            var existingUserResult =
                await _userService
                    .GetUserByPersonIdAsync(person.PersonId);

            if (existingUserResult.IsSuccess)
            {
                var existingUser = existingUserResult.Value!;

                // If another user already owns this person
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
                $"Error during search: {ex}");

            MessageBox.Show(
                "An error occurred while searching. Please try again later.",
                "Search Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

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

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        // =========================
        // BASIC CHECK
        // =========================

        if (Person == null)
        {
            MessageBox.Show(
                "You must search for and select a person first.",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        bool isEdit = Mode == OperationMode.Edit;

        // =========================
        // CONFIRM PASSWORD
        // =========================
        // This is UI validation.
        // ConfirmPassword does not belong to the DTO.

        if (!isEdit)
        {
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
        else if (!string.IsNullOrWhiteSpace(Password))
        {
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

        // =========================
        // CREATE
        // =========================

        if (!isEdit)
        {
            var createDto = new CreateUserDto
            {
                PersonId = Person.PersonId,
                UserName = UserName,
                Password = Password,
                IsActive = IsActive
            };

            var validationResult =
                UserValidator.ValidateCreateUser(createDto);

            if (!validationResult.IsValid)
            {
                MessageBox.Show(
                    string.Join(
                        Environment.NewLine,
                        validationResult.Errors),
                    "Validation Errors",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Check duplicate username
            if (await _userService
                .IsUsernameTakenForAnotherUserAsync(
                    UserName,
                    0))
            {
                MessageBox.Show(
                    "This username is already taken. Please choose another.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var addResult =
                await _userService.AddUserAsync(createDto);

            if (addResult.IsFailure)
            {
                MessageBox.Show(
                    addResult.Error,
                    "Save Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                SaveCompleted?.Invoke(false);
                return;
            }

            // =========================
            // CREATED SUCCESSFULLY
            // =========================

            UserId = addResult.Value;
            UserIdDisplay = addResult.Value.ToString();

            Mode = OperationMode.Edit;

            Password = string.Empty;
            ConfirmPassword = string.Empty;

            OnPropertyChanged(nameof(PasswordVisibility));
            OnPropertyChanged(nameof(ConfirmPasswordVisibility));

            MessageBox.Show(
                "The user account has been created successfully.",
                "Operation Completed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            SaveCompleted?.Invoke(true);

            return;
        }

        // =========================
        // UPDATE
        // =========================

        var updateDto = new UpdateUserDto
        {
            PersonId = Person.PersonId,
            UserName = UserName,
            Password = string.IsNullOrWhiteSpace(Password)
                ? null
                : Password,
            IsActive = IsActive
        };

        var updateValidation =
            UserValidator.ValidateUpdateUser(updateDto);

        if (!updateValidation.IsValid)
        {
            MessageBox.Show(
                string.Join(
                    Environment.NewLine,
                    updateValidation.Errors),
                "Validation Errors",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Check duplicate username
        if (await _userService
            .IsUsernameTakenForAnotherUserAsync(
                UserName,
                UserId))
        {
            MessageBox.Show(
                "This username is already taken. Please choose another.",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var updateResult =
            await _userService.UpdateUserAsync(
                UserId,
                updateDto);

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

    [RelayCommand]
    private void Cancel()
    {
        MainWindow.Navigation.GoBack();
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible =
            !IsConfirmPasswordVisible;
    }

    #endregion

    #region Partial Methods

    partial void OnUserNameChanged(string value)
    {
        var sanitized =
            value?.ToLower().Trim() ?? string.Empty;

        if (_userName != sanitized)
        {
            UserName = sanitized;
            return;
        }

        var result =
            UserValidator.ValidateUsernameFormat(sanitized);

        if (result.IsValid)
        {
            UserNameValidationMessage =
                "✓ Username is valid.";

            UserNameValidationColor = "Green";
        }
        else
        {
            UserNameValidationMessage =
                result.Errors.FirstOrDefault()
                ?? "Invalid username";

            UserNameValidationColor = "Red";
        }
    }

    #endregion
}