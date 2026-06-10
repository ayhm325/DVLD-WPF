using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Helpers;
using Presentation.Services;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

public partial class AddEditUserViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly IPersonService _personService;

    public AddEditUserViewModel(IUserService userService, IPersonService personService)
    {
        _userService = userService;
        _personService = personService;        
    }


    [ObservableProperty] private PersonDto? _person;
    [ObservableProperty] private OperationMode _mode;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private int _selectedFilterIndex = 0;
    [ObservableProperty] private int _userId;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string? _userIdDisplay = "???";

    [ObservableProperty] private string _userNameValidationMessage = "3-20 chars, start with a letter, numbers & _ allowed.";

    [ObservableProperty] private string _userNameValidationColor = "Gray";

    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private bool _isConfirmPasswordVisible;

    public ObservableCollection<Person> People { get; } = [];

    public Visibility PasswordVisibility =>
    Mode == OperationMode.Add ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ConfirmPasswordVisibility =>
    Mode == OperationMode.Add ? Visibility.Visible : Visibility.Collapsed;

    public event Action<bool>? SaveCompleted;




    // هذا الميثود يتم استدعاؤه من الصفحة عند تحميلها، ويحدد إذا كنا في وضع الإضافة أو التعديل بناءً على وجود UserId
    public async Task InitializeAsync(int? userId)
    {
        if (userId.HasValue && userId.Value > 0)
        {
            Mode = OperationMode.Edit;
            OnPropertyChanged(nameof(PasswordVisibility));
            OnPropertyChanged(nameof(ConfirmPasswordVisibility));
            UserId = userId.Value;
            UserIdDisplay = userId.Value.ToString();
            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user != null)
            {
                UserName = user.UserName;
                IsActive = user.IsActive;

                Person = await _personService.GetPersonByIdAsync(user.PersonId);                
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


    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(FilterText)) return;

        PersonDto? person = null;

        try
        {
            // 1. البحث عن الشخص
            if (SelectedFilterIndex == 0 && int.TryParse(FilterText, out int id))
                person = await _personService.GetPersonByIdAsync(id);
            else
                person = await _personService.GetPersonByNationalNoAsync(FilterText);

            if (person == null)
            {
                MessageBox.Show(
                    "No person was found with the specified criteria.",
                    "Person Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                // لا نغير أي شيء في الـ SelectedPerson أو الـ UserName
                return;
            }

            // 2. البحث عن مستخدم مرتبط بهذا الشخص
            var existingUser = await _userService.GetUserByPersonIdAsync(person.PersonId);

            // 3. التحقق المنطقي
            if (existingUser != null)
            {
                // إذا كان الشخص مرتبطاً بيوزر مختلف (سواء في Add أو Edit)
                if (existingUser.UserId != this.UserId)
                {
                    MessageBox.Show(
                        $"This person is already associated with the user account '{existingUser.UserName}'.",
                        "User Already Exists",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    // نكتفي بإظهار رسالة الخطأ ونخرج دون تغيير أي بيانات
                    return;
                }

                // إذا كان هو الشخص نفسه (في حالة Edit)
                Person = person;
                UserName = existingUser.UserName;
                IsActive = existingUser.IsActive;
            }
            else
            {
                // الشخص متاح (غير مرتبط بيوزر)
              Person = person;

                // هنا فقط نقوم بتحديث البيانات إذا كنا في حالة Add
                // في حالة Edit، نترك الـ UserName كما هو (أو نعدله ليناسب الشخص الجديد)
            }
        }
        catch (Exception ex)
        {
            // 1. تسجيل الخطأ في نافذة المخرجات (للمطور)
            System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");

            // 2. إظهار رسالة للمستخدم (لأن الخطأ قد يعني فشل الاتصال بقاعدة البيانات)
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
        var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
        //MainWindow.Navigation.Navigate(new AddEditPersonWin(vm));
        var win = new AddEditPersonWin(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        // 1. التحقق المبدئي من البيانات (Business Rules)
        bool isEdit = (Mode == OperationMode.Edit);
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
            MessageBox.Show(string.Join("\n", validationResult.Errors), "Validation Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 2. التحقق الخاص بقاعدة البيانات (Unique Constraint)
        if (await _userService.IsUsernameTakenForAnotherUserAsync(UserName, UserId))
        {
            MessageBox.Show("This username is already taken. Please choose another.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 3. التحقق من الشخص المحدد
        if (Person == null) return;



        // 4. تنفيذ عملية الحفظ
        var userDto = new CreateUserDto
        {
            UserId = this.UserId,
            PersonId = Person.PersonId,
            UserName = UserName,
            Password = Password,
            IsActive = IsActive
        };

        bool isSuccess = false;
        if (isEdit)
        {
            isSuccess = await _userService.UpdateUserAsync(UserId, userDto);
        }
        else
        {
            int newUserId = await _userService.AddUserAsync(userDto);
            if (newUserId > 0)
            {
                this.UserId = newUserId;
                this.UserIdDisplay = newUserId.ToString();
                Mode = OperationMode.Edit;
                isSuccess = true;
            }
        }

        // 5. النتيجة النهائية
        if (isSuccess)
        {
            string message = isEdit ? "The user account has been updated successfully." : "The user account has been created successfully.";
            MessageBox.Show(message, "Operation Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            SaveCompleted?.Invoke(true);
        }
        else
        {
            MessageBox.Show("An unexpected error occurred while saving. Please try again.", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        MainWindow.Navigation.GoBack();
    }







    // في مكان واحد فقط داخل الـ ViewModel:
    partial void OnUserNameChanged(string value)
    {
        // 1. التنظيف (التحويل للـ lowercase والـ trim)
        var sanitized = value?.ToLower().Trim() ?? string.Empty;

        // منع التكرار اللانهائي (لأننا نعدل الـ UserName داخل الميثود)
        if (_userName != sanitized)
        {
            UserName = sanitized;
            return;
        }

        // 2. التحقق اللحظي وتحديث الواجهة
        var result = UserValidator.ValidateUsernameFormat(sanitized);

        if (result.IsValid)
        {
            UserNameValidationMessage = "✓ Username is valid.";
            UserNameValidationColor = "Green";
        }
        else
        {
            // عرض أول خطأ فقط من قائمة الأخطاء ليكون التنبيه نظيفاً
            UserNameValidationMessage = result.Errors.FirstOrDefault() ?? "Invalid username";
            UserNameValidationColor = "Red";
        }
    }


    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        // استخدم اسم الخاصية (IsPasswordVisible) وليس الحقل (_isPasswordVisible)
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        // استخدم اسم الخاصية (IsConfirmPasswordVisible) وليس الحقل (_isConfirmPasswordVisible)
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }







}