using Application.Common.Results;
using Application.DTOs;
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
        _userService = userService;
        _personService = personService;
        _currentUserService = currentUserService;

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
    private string _userNameValidationMessage = "3-20 chars, start with a letter, numbers & _ allowed.";

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
        Mode == OperationMode.Add ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ConfirmPasswordVisibility =>
        Mode == OperationMode.Add ? Visibility.Visible : Visibility.Collapsed;

    public event Action<bool>? SaveCompleted;

    #endregion

    #region Initialization

    public async Task InitializeAsync(int? userId)
    {
        CanGoToNextTab = false;
        GoToNextTabCommand.NotifyCanExecuteChanged();

        if (userId.HasValue && userId.Value > 0)
        {
            Mode = OperationMode.Edit;
            OnPropertyChanged(nameof(PasswordVisibility));
            OnPropertyChanged(nameof(ConfirmPasswordVisibility));

            UserId = userId.Value;
            UserIdDisplay = userId.Value.ToString();

            var userResult = await _userService.GetUserByIdAsync(userId.Value);

            if (userResult.IsSuccess)
            {
                var user = userResult.Value!;

                UserName = user.UserName;
                IsActive = user.IsActive;

                var personResult = await _personService.GetPersonByIdAsync(user.PersonId);

                if (personResult.IsSuccess)
                {
                    Person = personResult.Value!;

                    CanGoToNextTab = true;
                    GoToNextTabCommand.NotifyCanExecuteChanged();
                }
            }
        }
        else
        {
            Mode = OperationMode.Add;
            UserIdDisplay = null;
            Person = null;
            UserName = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
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

            if (SelectedFilterIndex == 0 && int.TryParse(FilterText, out int id))
                personResult = await _personService.GetPersonByIdAsync(id);
            else
                personResult = await _personService.GetPersonByNationalNoAsync(FilterText);


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


            var existingUserResult = await _userService.GetUserByPersonIdAsync(person.PersonId);

            // إذا كانت UserService أصبحت Result<T>
            if (existingUserResult.IsSuccess)
            {
                var existingUser = existingUserResult.Value!;

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
            System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");

            MessageBox.Show(
                    "An error occurred while searching. Please try again later.",
                    "Search Error",MessageBoxButton.OK,MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void AddPerson()
    {
        var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
        var win = new AddEditPersonWin(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        bool isEdit = Mode == OperationMode.Edit;
        bool userExists = isEdit ? await _userService.IsUserExistsByIdAsync(UserId) : true;

        var validationResult = UserValidator.ValidateUser(
            UserName,
            Password,
            ConfirmPassword,
            isEdit,
            userExists,
            Person);

        if (!validationResult.IsValid)
        {
            MessageBox.Show(
                string.Join("\n", validationResult.Errors),
                "Validation Errors",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (await _userService.IsUsernameTakenForAnotherUserAsync(UserName, UserId))
        {
            MessageBox.Show(
                "This username is already taken. Please choose another.",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (Person == null) return;

        var userDto = new CreateUserDto
        {
            UserId = UserId,
            PersonId = Person.PersonId,
            UserName = UserName,
            Password = Password,
            IsActive = IsActive
        };

        bool isSuccess;

        if (isEdit)
        {
            var updateResult = await _userService.UpdateUserAsync(UserId, userDto);

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

            isSuccess = true;
        }
        else
        {
            var addResult = await _userService.AddUserAsync(userDto);

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

            int newUserId = addResult.Value;

            UserId = newUserId;
            UserIdDisplay = newUserId.ToString();
            Mode = OperationMode.Edit;

            OnPropertyChanged(nameof(PasswordVisibility));
            OnPropertyChanged(nameof(ConfirmPasswordVisibility));

            isSuccess = true;
        }

        if (isSuccess)
        {
            string message = isEdit
                ? "The user account has been updated successfully."
                : "The user account has been created successfully.";

            MessageBox.Show(
                message,
                "Operation Completed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            SaveCompleted?.Invoke(true);
        }
        else
        {
            MessageBox.Show(
                "An unexpected error occurred while saving. Please try again.",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    #endregion

    #region Partial Methods

    partial void OnUserNameChanged(string value)
    {
        var sanitized = value?.ToLower().Trim() ?? string.Empty;

        if (_userName != sanitized)
        {
            UserName = sanitized;
            return;
        }

        var result = UserValidator.ValidateUsernameFormat(sanitized);

        if (result.IsValid)
        {
            UserNameValidationMessage = "✓ Username is valid.";
            UserNameValidationColor = "Green";
        }
        else
        {
            UserNameValidationMessage = result.Errors.FirstOrDefault() ?? "Invalid username";
            UserNameValidationColor = "Red";
        }
    }

    #endregion
}