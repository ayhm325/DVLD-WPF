using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Presentation.Views.Windows;

public partial class DriverLicenseInfoWin : Window, INotifyPropertyChanged
{
    private readonly int _licenseId;
    private readonly ILicenseQueryService _licenseQueryService;


    // =========================================================
    // LICENSE DATA
    // =========================================================

    private DriverLicenseInfoDto? _licenseData;

    public DriverLicenseInfoDto? LicenseData
    {
        get => _licenseData;

        set
        {
            _licenseData = value;
            OnPropertyChanged();
        }
    }


    // =========================================================
    // COMMAND
    // =========================================================

    public ICommand CloseCommand { get; }


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public DriverLicenseInfoWin(
        int licenseId)
    {
        InitializeComponent();

        _licenseId =
            licenseId;

        _licenseQueryService =
            App.ServiceProvider
                .GetRequiredService<ILicenseQueryService>();

        DataContext =
            this;

        CloseCommand =
            new RelayCommand(
                _ => Close());

        Loaded +=
            DriverLicenseInfoWin_Loaded;
    }


    // =========================================================
    // LOADED
    // =========================================================

    private async void DriverLicenseInfoWin_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var result =
                await _licenseQueryService
                    .GetLicenseDetailsByIdAsync(
                        _licenseId);


            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error);

                return;
            }


            LicenseData =
                result.Value;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error: {ex.Message}");
        }
    }


    // =========================================================
    // PROPERTY CHANGED
    // =========================================================

    public event PropertyChangedEventHandler?
        PropertyChanged;


    protected void OnPropertyChanged(
        [CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(name));
    }


    // =========================================================
    // CLOSE
    // =========================================================

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}


// =============================================================
// RELAY COMMAND
// =============================================================

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;

    private readonly Func<object?, bool>?
        _canExecute;


    public RelayCommand(
        Action<object?> execute,
        Func<object?, bool>? canExecute = null)
    {
        _execute =
            execute
            ?? throw new ArgumentNullException(
                nameof(execute));

        _canExecute =
            canExecute;
    }


    public bool CanExecute(
        object? parameter)
    {
        return _canExecute?.Invoke(parameter)
            ?? true;
    }


    public void Execute(
        object? parameter)
    {
        _execute(parameter);
    }


    public event EventHandler?
        CanExecuteChanged;


    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}