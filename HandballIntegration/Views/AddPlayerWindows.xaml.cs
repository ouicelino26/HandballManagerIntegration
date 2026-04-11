using HandballIntegration.Data;
using HandballIntegration.Services;
using HandballManagerCore.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
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

                var positions = await _http.GetFromJsonAsync<List<Position>>(
                    $"{_settings.BaseUrl}api/Positions");

                PositionComboBox.ItemsSource = positions;

                var nationalities = await _http.GetFromJsonAsync<List<Nationality>>(
                    $"{_settings.BaseUrl}api/Nationalities");

                NationalityComboBox.ItemsSource = nationalities;
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
                TeamId = (int)TeamComboBox.SelectedValue,
                PositionId = (int)PositionComboBox.SelectedValue,
                NationalityId = (int)NationalityComboBox.SelectedValue,
                Number = int.TryParse(NumberTextBox.Text, out var n) ? n : 0
            };

            try
            {
                if (!await _apiService.PrepareAuthorizedClientAsync(_http))
                {
                    MessageBox.Show("Session administrateur requise pour ajouter une joueuse.", "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var resp = await _http.PostAsJsonAsync(
                    $"{_settings.BaseUrl}api/Players", dto);

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
    }
}
