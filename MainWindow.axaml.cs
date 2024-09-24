using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
                Pump = pumpComboBox?.SelectedItem?.ToString(),
                Nozzle = nozzleComboBox?.SelectedItem?.ToString(),
                Attendant = attendantTextBox?.Text,
                FuelType = fuelTypeComboBox?.SelectedItem?.ToString(),
                Liters = litersNumericUpDown?.Value ?? 0
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

        private async Task<bool> LogTransactionAsync(Transaction transaction)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8000/backoffice/");
                var json = JsonConvert.SerializeObject(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var result = await client.PostAsync("api/transactions/", content);
                return result.IsSuccessStatusCode;
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

        private async Task<string[]> FetchPumpsAsync()
        {
            // Logic to fetch pumps from API
            return await FetchDataAsync("api/pumps/");
        }

        private async Task<string[]> FetchNozzlesAsync()
        {
            // Logic to fetch nozzles from API
            return await FetchDataAsync("api/nozzles/");
        }

        private async Task<string[]> FetchFuelTypesAsync()
        {
            // Logic to fetch fuel types from API
            return await FetchDataAsync("api/fuel-types/");
        }

        private async Task<string[]> FetchDataAsync(string endpoint)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8000/backoffice/");
                var response = await client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // Deserialize JSON into an array of strings (modify as necessary)
                    return JsonConvert.DeserializeObject<string[]>(json);
                }
                return new string[0]; // Return empty array on failure
            }
        }
    }

    public class Transaction
    {
        public string Pump { get; set; }
        public string Nozzle { get; set; }
        public string Attendant { get; set; }
        public string FuelType { get; set; }
        public decimal Liters { get; set; }
    }
}
