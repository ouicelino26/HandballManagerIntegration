using HandballIntegration.Data;
using HandballIntegration.Services;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static IntegrationViewModel;

namespace HandballIntegration.Views
{
    public partial class AddPlayerWindows : Window
    {
        private readonly HttpClient _http;
        private readonly ApiService _apiService;
        private readonly ApiSettings _settings;

        public Player CreatedPlayer { get; private set; }

        public AddPlayerWindows(string firstName,
                                string lastName,
                                List<TeamLight> teams)
        {
            InitializeComponent();

            _http = App.Services.GetRequiredService<HttpClient>();
            _apiService = App.Services.GetRequiredService<ApiService>();
            _settings = App.Services.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value;

            FirstNameTextBox.Text = firstName;
            LastNameTextBox.Text = lastName;

            TeamComboBox.ItemsSource = teams;
            TeamComboBox.SelectedIndex = 0;

            Loaded += AddPlayerWindows_Loaded;
        }

        private async void AddPlayerWindows_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await _apiService.PrepareAuthorizedClientAsync(_http))
                {
                    MessageBox.Show("Session administrateur requise pour ajouter une joueuse.", "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DialogResult = false;
                    Close();
                    return;
                }

                PositionComboBox.ItemsSource = await LoadPositionsAsync();
                NationalityComboBox.ItemsSource = await LoadNationalitiesAsync();

                if (PositionComboBox.Items.Count > 0)
                {
                    PositionComboBox.SelectedIndex = 0;
                }

                if (NationalityComboBox.Items.Count > 0)
                {
                    NationalityComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement listes : " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NumberOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private async void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            var dto = new PlayerCreateDto
            {
                Name = FirstNameTextBox.Text.Trim(),
                Surname = LastNameTextBox.Text.Trim(),
                Birthday = BirthDatePicker.SelectedDate,
                TeamId = GetSelectedId(TeamComboBox),
                PositionId = GetSelectedId(PositionComboBox),
                NationalityId = GetSelectedId(NationalityComboBox),
                Number = int.TryParse(NumberTextBox.Text, out var n) ? n : 0,
                IsActive = IsActiveCheckBox.IsChecked != false
            };

            try
            {
                if (!await _apiService.PrepareAuthorizedClientAsync(_http))
                {
                    MessageBox.Show("Session administrateur requise pour ajouter une joueuse.", "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var resp = await _http.PostAsJsonAsync(
                    $"{_settings.ApiBaseUrl}api/Players", dto);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show("Erreur creation joueuse :\n" + body);
                    return;
                }

                CreatedPlayer = await resp.Content.ReadFromJsonAsync<Player>();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur API : " + ex.Message);
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Nom et prenom obligatoires.");
                return false;
            }

            if (TeamComboBox.SelectedItem == null ||
                PositionComboBox.SelectedItem == null ||
                NationalityComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez selectionner equipe, poste et nationalite.");
                return false;
            }

            return true;
        }

        private async Task<List<LookupItemDto>> LoadPositionsAsync()
        {
            var lookupPositions = await _http.GetFromJsonAsync<List<LookupItemDto>>(
                $"{_settings.ApiBaseUrl}api/Lookups/positions");

            if (lookupPositions?.Any() == true)
            {
                return lookupPositions;
            }

            return await _http.GetFromJsonAsync<List<LookupItemDto>>(
                       $"{_settings.ApiBaseUrl}api/Positions")
                   ?? new List<LookupItemDto>();
        }

        private async Task<List<LookupItemDto>> LoadNationalitiesAsync()
        {
            var lookupNationalities = await _http.GetFromJsonAsync<List<LookupItemDto>>(
                $"{_settings.ApiBaseUrl}api/Lookups/nationalities");

            if (lookupNationalities?.Any() == true)
            {
                return lookupNationalities;
            }

            var legacyNationalities = await _http.GetFromJsonAsync<List<Nationality>>(
                $"{_settings.ApiBaseUrl}api/Nationalities");

            return legacyNationalities?
                .Select(item => new LookupItemDto
                {
                    Id = item.Id,
                    Name = string.IsNullOrWhiteSpace(item.NationalityF) ? item.Country : item.NationalityF
                })
                .ToList()
                ?? new List<LookupItemDto>();
        }

        private static int GetSelectedId(ComboBox comboBox)
        {
            return comboBox.SelectedValue switch
            {
                int value => value,
                long value => (int)value,
                short value => value,
                byte value => value,
                _ => 0
            };
        }
    }
}
