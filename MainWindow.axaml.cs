using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;

namespace PumpSimulator
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Transaction> unprocessedTransactions;
        private ComboBox pumpComboBox;
        private ComboBox nozzleComboBox;
        private TextBox attendantTextBox;
        private ComboBox fuelTypeComboBox;
        private NumericUpDown litersNumericUpDown;
        private Button submitButton;
        private TextBlock totalUnprocessedTextBlock;
        private TextBlock errorTextBlock;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            unprocessedTransactions = new ObservableCollection<Transaction>();
            this.Loaded += MainWindow_Loaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            pumpComboBox = this.FindControl<ComboBox>("PumpComboBox");
            nozzleComboBox = this.FindControl<ComboBox>("NozzleComboBox");
            attendantTextBox = this.FindControl<TextBox>("AttendantTextBox");
            fuelTypeComboBox = this.FindControl<ComboBox>("FuelTypeComboBox");
            litersNumericUpDown = this.FindControl<NumericUpDown>("LitersNumericUpDown");
            submitButton = this.FindControl<Button>("SubmitButton");
            totalUnprocessedTextBlock = this.FindControl<TextBlock>("TotalUnprocessedTextBlock");
            errorTextBlock = this.FindControl<TextBlock>("ErrorTextBlock");

            if (pumpComboBox != null)
            {
                pumpComboBox.ItemsSource = await FetchPumpsAsync();
                pumpComboBox.SelectionChanged += PumpComboBox_SelectionChanged;
            }

            if (fuelTypeComboBox != null)
                fuelTypeComboBox.ItemsSource = await FetchFuelTypesAsync();

            if (submitButton != null)
                submitButton.Click += SubmitButton_Click;

            UpdateTotalUnprocessed();
        }

        private async void PumpComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pumpComboBox.SelectedItem is Pump selectedPump)
            {
                nozzleComboBox.ItemsSource = await FetchNozzlesForPumpAsync(selectedPump.Id);
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (pumpComboBox == null || pumpComboBox.SelectedItem == null)
            {
                SetErrorMessage("Please select a pump.");
                return;
            }

            if (nozzleComboBox == null || nozzleComboBox.SelectedItem == null)
            {
                SetErrorMessage("Please select a nozzle.");
                return;
            }

            if (fuelTypeComboBox == null || fuelTypeComboBox.SelectedItem == null)
            {
                SetErrorMessage("Please select a fuel type.");
                return;
            }

            var selectedPump = pumpComboBox.SelectedItem as Pump;
            var selectedNozzle = nozzleComboBox.SelectedItem as NozzleItem;
            var selectedFuelType = fuelTypeComboBox.SelectedItem as FuelType;

            if (selectedPump == null || selectedNozzle == null || selectedFuelType == null)
            {
                SetErrorMessage("Invalid selection. Please try again.");
                return;
            }

            var transaction = new Transaction
            {
                Pump = selectedPump.Id,
                Nozzle = selectedNozzle.Id,
                Attendant = attendantTextBox?.Text,
                FuelType = selectedFuelType.Id,
                Volume = (float)(litersNumericUpDown?.Value ?? 0),
                TotalCost = CalculateTotalCost(selectedFuelType)
            };

            unprocessedTransactions.Add(transaction);
            UpdateTotalUnprocessed();

            var success = await LogTransactionAsync(transaction);
            if (!success)
            {
                SetErrorMessage("Failed to log transaction. Please try again.");
            }
            else
            {
                SetErrorMessage(""); // Clear error message on success
            }

            ClearInputs();
        }

        private float CalculateTotalCost(FuelType selectedFuelType)
        {
            var liters = (float)(litersNumericUpDown?.Value ?? 0);
            return liters * selectedFuelType.FuelPrice;
        }

        private async Task<bool> LogTransactionAsync(Transaction transaction)
        {
            var jsonObject = new
            {
                pump = transaction.Pump,
                nozzle = transaction.Nozzle,
                attendant = transaction.Attendant,
                fuel_type = transaction.FuelType,
                volume = transaction.Volume,
                total_cost = transaction.TotalCost
            };

            var jsonContent = JsonConvert.SerializeObject(jsonObject);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8000/backoffice/");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                try
                {
                    var response = await client.PostAsync("api/transactions/", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Transaction logged successfully.");
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"API response: {response.StatusCode}, {errorContent}");
                        SetErrorMessage($"Failed to log transaction: {errorContent}");
                        return false;
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"HTTP Request Exception: {e.Message}");
                    SetErrorMessage($"Network error: {e.Message}");
                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unexpected error: {e.Message}");
                    SetErrorMessage($"An unexpected error occurred: {e.Message}");
                    return false;
                }
            }
        }

        private void UpdateTotalUnprocessed()
        {
            totalUnprocessedTextBlock.Text = unprocessedTransactions.Count.ToString();
        }

        private void ClearInputs()
        {
            if (pumpComboBox != null) pumpComboBox.SelectedIndex = -1;
            if (nozzleComboBox != null) nozzleComboBox.SelectedIndex = -1;
            if (attendantTextBox != null) attendantTextBox.Text = "";
            if (fuelTypeComboBox != null) fuelTypeComboBox.SelectedIndex = -1;
            if (litersNumericUpDown != null) litersNumericUpDown.Value = 0;
        }

        private async Task<List<Pump>> FetchPumpsAsync()
        {
            try
            {
                var json = await FetchDataAsync("api/pumps/");
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("Received empty response from API for pumps");
                    return new List<Pump>();
                }

                var pumpData = JsonConvert.DeserializeObject<List<dynamic>>(json);
                if (pumpData == null)
                {
                    Console.WriteLine("Failed to deserialize JSON response for pumps");
                    return new List<Pump>();
                }

                return pumpData.Select(p => new Pump
                {
                    Id = p.id != null ? Convert.ToInt32(p.id) : 0,
                    PumpNumber = p.pump_number != null ? Convert.ToInt32(p.pump_number) : 0,
                    TankInfo = p.tank_info != null ? Convert.ToInt32(p.tank_info) : 0
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FetchPumpsAsync: {ex.Message}");
                return new List<Pump>();
            }
        }

        private async Task<List<NozzleItem>> FetchNozzlesForPumpAsync(int pumpId)
        {
            try
            {
                var json = await FetchDataAsync($"api/nozzles/?pump={pumpId}");
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"Received empty response from API for nozzles of pump {pumpId}");
                    return new List<NozzleItem>();
                }

                var nozzleData = JsonConvert.DeserializeObject<List<dynamic>>(json);
                if (nozzleData == null)
                {
                    Console.WriteLine($"Failed to deserialize JSON response for nozzles of pump {pumpId}");
                    return new List<NozzleItem>();
                }

                return nozzleData.Select(n => new NozzleItem
                {
                    Id = n.id != null ? Convert.ToInt32(n.id) : 0,
                    NozzleName = n.nozzle_name != null ? Convert.ToString(n.nozzle_name) : string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FetchNozzlesForPumpAsync: {ex.Message}");
                return new List<NozzleItem>();
            }
        }

        private async Task<List<FuelType>> FetchFuelTypesAsync()
        {
            try
            {
                var json = await FetchDataAsync("api/fuel-types/");
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("Received empty response from API for fuel types");
                    return new List<FuelType>();
                }

                var fuelTypeData = JsonConvert.DeserializeObject<List<dynamic>>(json);
                if (fuelTypeData == null)
                {
                    Console.WriteLine("Failed to deserialize JSON response for fuel types");
                    return new List<FuelType>();
                }

                return fuelTypeData.Select(f => new FuelType
                {
                    Id = f.id != null ? Convert.ToInt32(f.id) : 0,
                    FuelTypeName = f.fuel_type != null ? Convert.ToString(f.fuel_type) : string.Empty,
                    FuelPrice = f.fuel_price != null ? Convert.ToSingle(f.fuel_price) : 0.0f
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FetchFuelTypesAsync: {ex.Message}");
                return new List<FuelType>();
            }
        }

        private async Task<string> FetchDataAsync(string endpoint)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8000/backoffice/");
                var response = await client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw API response for {endpoint}: {content}");
                    return content;
                }
                Console.WriteLine($"API request failed for {endpoint}. Status code: {response.StatusCode}");
                return null;
            }
        }

        private void SetErrorMessage(string message)
        {
            if (errorTextBlock != null)
            {
                errorTextBlock.Text = message;
            }
            else
            {
                Console.WriteLine($"Error: {message}");
            }
        }
    }

    public class Pump
    {
        public int Id { get; set; }
        public int PumpNumber { get; set; }
        public int TankInfo { get; set; }
    }

    public class NozzleItem
    {
        public int Id { get; set; }
        public string NozzleName { get; set; }
    }

    public class FuelType
    {
        public int Id { get; set; }
        public string FuelTypeName { get; set; }
        public float FuelPrice { get; set; }
    }

    public class Transaction
    {
        public int Pump { get; set; }
        public int Nozzle { get; set; }
        public string Attendant { get; set; }
        public int FuelType { get; set; }
        public float Volume { get; set; }
        public float TotalCost { get; set; }
    }
} 