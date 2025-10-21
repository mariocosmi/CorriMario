using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CorriMario;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	private CancellationTokenSource? _cancelTokenSource;
	private bool _isTracking;

	public MainPage()
	{
		InitializeComponent();

		// Initialize preferences if not set
		if (!Preferences.ContainsKey("percentuale"))
			Preferences.Set("percentuale", 0.0);
		if (!Preferences.ContainsKey("totale"))
			Preferences.Set("totale", 0.0);
		if (!Preferences.ContainsKey("distanza"))
			Preferences.Set("distanza", "");
		if (!Preferences.ContainsKey("avviato"))
			Preferences.Set("avviato", false);
		if (!Preferences.ContainsKey("lastLat"))
			Preferences.Set("lastLat", 0.0);
		if (!Preferences.ContainsKey("lastLong"))
			Preferences.Set("lastLong", 0.0);

		this.DataContext = this;
		this.BindingContext = this;

		// Wait for layout to complete before updating
		this.SizeChanged += (s, e) =>
		{
			if (ContentPanel.Width > 0 && ContentPanel.Height > 0)
			{
				AggiornaMusetto(Preferences.Get("totale", 0.0));
			}
		};
	}

	public new event PropertyChangedEventHandler? PropertyChanged;

	protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		base.OnPropertyChanged(propertyName);
	}

	public string Distanza
	{
		get
		{
			if (!Avviato)
				return "Guadagnati il musetto!";
			return Preferences.Get("distanza", "");
		}
		set
		{
			if (value != Distanza)
			{
				Preferences.Set("distanza", value);
				OnPropertyChanged();
			}
		}
	}

	public bool Avviato
	{
		get => Preferences.Get("avviato", false);
		set
		{
			if (value != Avviato)
			{
				Preferences.Set("avviato", value);
				if (!value)
					Preferences.Set("distanza", "");
				OnPropertyChanged();
				OnPropertyChanged(nameof(Distanza));
			}
		}
	}

	public double Percentuale
	{
		get => Preferences.Get("percentuale", 0.0);
		set
		{
			if (value != Percentuale)
			{
				Preferences.Set("percentuale", value);
				OnPropertyChanged();
			}
		}
	}

	private async void Button_Clicked(object? sender, EventArgs e)
	{
		try
		{
			if (_isTracking)
			{
				// Stop tracking
				await StopTracking();
				btnStartStop.Text = "Riparti";
			}
			else
			{
				// Start tracking
				await StartTracking();
				btnStartStop.Text = "Fai una pausa";
			}
			Avviato = _isTracking;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Errore", $"Impossibile avviare il GPS: {ex.Message}", "OK");
		}
	}

	private async Task StartTracking()
	{
		// Request location permission
		var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
		if (status != PermissionStatus.Granted)
		{
			status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
		}

		if (status != PermissionStatus.Granted)
		{
			await DisplayAlert("Permesso negato", "È necessario il permesso di localizzazione per tracciare la corsa.", "OK");
			return;
		}

		_cancelTokenSource = new CancellationTokenSource();
		_isTracking = true;

		// Start location tracking loop
		_ = Task.Run(async () =>
		{
			while (!_cancelTokenSource.Token.IsCancellationRequested)
			{
				try
				{
					var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
					var location = await Geolocation.GetLocationAsync(request, _cancelTokenSource.Token);

					if (location != null)
					{
						await MainThread.InvokeOnMainThreadAsync(() => OnPositionChanged(location));
					}
				}
				catch (Exception ex)
				{
					// Location error - log it
					System.Diagnostics.Debug.WriteLine($"Errore GPS: {ex.Message}");
				}

				// Wait before next update (check every 10 seconds or when moved 25+ meters)
				await Task.Delay(10000, _cancelTokenSource.Token);
			}
		}, _cancelTokenSource.Token);
	}

	private async Task StopTracking()
	{
		if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
		{
			_cancelTokenSource.Cancel();
			_cancelTokenSource.Dispose();
			_cancelTokenSource = null;
		}
		_isTracking = false;
		await Task.CompletedTask;
	}

	private void OnPositionChanged(Location location)
	{
		try
		{
			var lastLong = Preferences.Get("lastLong", 0.0);
			var lastLat = Preferences.Get("lastLat", 0.0);

			if (lastLat != 0.0 && lastLong != 0.0)
			{
				// Calculate distance from last position
				var lastLocation = new Location(lastLat, lastLong);
				var dist = Location.CalculateDistance(lastLocation, location, DistanceUnits.Meters);

				// Only count if moved more than 25 meters (to avoid GPS jitter)
				if (dist >= 25)
				{
					var totDist = Preferences.Get("totale", 0.0);
					totDist += dist;
					Preferences.Set("totale", totDist);
					Distanza = $"Hai corso per {totDist:F0} m.";
					AggiornaMusetto(totDist);

					// Update last position
					Preferences.Set("lastLong", location.Longitude);
					Preferences.Set("lastLat", location.Latitude);
				}
			}
			else
			{
				// First position - just save it
				Preferences.Set("lastLong", location.Longitude);
				Preferences.Set("lastLat", location.Latitude);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Errore nell'aggiornamento posizione: {ex.Message}");
		}
	}

	private string ImmagineAdatta(double totDist)
	{
		return totDist < 3000 ? "level1.jpg" : totDist < 6000 ? "level2.jpg" : "level3.jpg";
	}

	private string ImmagineSfondo(double totDist)
	{
		return totDist < 3000 ? "level0.jpg" : totDist < 6000 ? "level1.jpg" : "level2.jpg";
	}

	private void AggiornaMusetto(double totDist)
	{
		try
		{
			// Set image sources
			imgMusetto.Source = ImageSource.FromFile(ImmagineAdatta(totDist));
			imgMusettoSfondo.Source = ImageSource.FromFile(ImmagineSfondo(totDist));

			// Wait for images to be sized
			if (ContentPanel.Width <= 0 || ContentPanel.Height <= 0)
				return;

			// Calculate fill percentage
			var height = ContentPanel.Height;
			Percentuale = totDist < 9000 ? height * ((totDist % 3000) / 3000) : height;

			// Update clipping rectangles
			clipMusetto.Rect = new Rect(0, 0, ContentPanel.Width, Percentuale);
			clipMusettoSfondo.Rect = new Rect(0, Percentuale, ContentPanel.Width, height - Percentuale);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Errore nell'aggiornamento musetto: {ex.Message}");
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		// Stop tracking when page disappears
		if (_isTracking)
		{
			_ = StopTracking();
		}
	}
}
