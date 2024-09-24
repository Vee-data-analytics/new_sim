using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

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
                pumpComboBox.ItemsSource = await FetchPumpsAsync();

            if (nozzleComboBox != null)
                nozzleComboBox.ItemsSource = await FetchNozzlesAsync();

            if (fuelTypeComboBox != null)
                fuelTypeComboBox.ItemsSource = await FetchFuelTypesAsync();

            if (submitButton != null)
                submitButton.Click += SubmitButton_Click;

            UpdateTotalUnprocessed();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var transaction = new Transaction
            {
                PumpId = ((Pump)pumpComboBox?.SelectedItem)?.Id ?? 0,
                NozzleId = ((NozzleItem)nozzleComboBox?.SelectedItem)?.Id ?? 0,
                AttendantName = attendantTextBox?.Text,
                FuelTypeId = ((FuelType)fuelTypeComboBox?.SelectedItem)?.Id ?? 0,
                Volume = (float)(litersNumericUpDown?.Value ?? 0),
                TotalCost = CalculateTotalCost()
            };
        
            unprocessedTransactions.Add(transaction);
            UpdateTotalUnprocessed();
        
            // Log transaction to API
            var success = await LogTransactionAsync(transaction);
            if (!success)
            {
                errorTextBlock.Text = "Failed to log transaction. Please try again.";
            }
            else
            {
                errorTextBlock.Text = "";
            }
        
            ClearInputs();
        }
        
        private float CalculateTotalCost()
        {
            var selectedFuelType = (FuelType)fuelTypeComboBox?.SelectedItem;
            var liters = (float)(litersNumericUpDown?.Value ?? 0);
            return selectedFuelType != null ? liters * selectedFuelType.FuelPrice : 0;
        }
        
        private async Task<bool> LogTransactionAsync(Transaction transaction)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:8000/backoffice/");
                    var json = JsonConvert.SerializeObject(new
                    {
                        pump_id = transaction.PumpId,
                        nozzle_id = transaction.NozzleId,
                        attendant_name = transaction.AttendantName,
                        fuel_type_id = transaction.FuelTypeId,
                        volume = transaction.Volume,
                        total_cost = transaction.TotalCost
                    });
                    Console.WriteLine($"Sending transaction data: {json}");
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("api/transactions/", content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API response: {response.StatusCode}, Content: {responseContent}");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LogTransactionAsync: {ex.Message}");
                return false;
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

        private async Task<List<NozzleItem>> FetchNozzlesAsync()
        {
            try
            {
                var json = await FetchDataAsync("api/nozzles/");
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("Received empty response from API for nozzles");
                    return new List<NozzleItem>();
                }
        
                var nozzleData = JsonConvert.DeserializeObject<List<dynamic>>(json);
                if (nozzleData == null)
                {
                    Console.WriteLine("Failed to deserialize JSON response for nozzles");
                    return new List<NozzleItem>();
                }
        
                return nozzleData.Select(n => new NozzleItem
                {
                    Id = n.id != null ? Convert.ToInt32(n.id) : 0,
                    NozzleName = n.nozzle_name != null ? Convert.ToInt32(n.nozzle_name) : 0,
                    PumpId = n.pump != null ? Convert.ToInt32(n.pump) : 0,
                    AttendantId = n.attendant != null ? (int?)Convert.ToInt32(n.attendant) : null,
                    FuelTypeId = n.fuel_type != null ? (int?)Convert.ToInt32(n.fuel_type) : null,
                    Processed = n.processed != null ? Convert.ToBoolean(n.processed) : false
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FetchNozzlesAsync: {ex.Message}");
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
                    FuelPrice = 0.0f // We don't have this information in the current API response
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


        public class Pump
        {
            public int Id { get; set; }
            public int PumpNumber { get; set; }
            public int TankInfo { get; set; }
        }
        
        public class NozzleItem
        {
            public int Id { get; set; }
            public int NozzleName { get; set; }
            public int PumpId { get; set; }
            public int? AttendantId { get; set; }
            public int? FuelTypeId { get; set; }
            public bool Processed { get; set; }
        }
        
        public class FuelType
        {
            public int Id { get; set; }
            public string FuelTypeName { get; set; }
            public float FuelPrice { get; set; }
        }

        public class Transaction
        {
            public int PumpId { get; set; }
            public int NozzleId { get; set; }
            public string AttendantName { get; set; }
            public int FuelTypeId { get; set; }
            public float Volume { get; set; }
            public float TotalCost { get; set; }
        }
        
    }
}
        