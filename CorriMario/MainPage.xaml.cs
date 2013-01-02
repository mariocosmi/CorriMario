using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Device.Location;

namespace CorriMario {
	public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged {
		// Constructor
		public MainPage() {
			InitializeComponent();
			PhoneApplicationService.Current.State["percentuale"] = "0";
			PhoneApplicationService.Current.State["totale"] = "0";
			PhoneApplicationService.Current.State["distanza"] = "";
			PhoneApplicationService.Current.State["avviato"] = "N";
			PhoneApplicationService.Current.State["lastLat"] = "0";
			PhoneApplicationService.Current.State["lastLong"] = "0";
			this.DataContext = this;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (null != handler) {
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		GeoCoordinateWatcher _watcher;

		public string Distanza {
			get {
				if (!this.Avviato)
					return "Guadagnati il musetto!";
				return PhoneApplicationService.Current.State["distanza"].ToString();
			}
			set {
				if (value != this.Distanza) {
					PhoneApplicationService.Current.State["distanza"] = value;
					NotifyPropertyChanged("Distanza");
				}
			}
		}

		public bool Avviato {
			get {
				return PhoneApplicationService.Current.State["avviato"].ToString() == "S";
			}
			set {
				if (value != this.Avviato) {
					PhoneApplicationService.Current.State["avviato"] = value ? "S" : "N";
					if (!value)
						PhoneApplicationService.Current.State["distanza"] = "";
					NotifyPropertyChanged("Avviato");
				}
			}
		}

		public double Percentuale {
			get {
				var ret = 0.0;
				double.TryParse(PhoneApplicationService.Current.State["percentuale"].ToString(), out ret);
				return ret;
			}
			set {
				if (value != this.Percentuale) {
					PhoneApplicationService.Current.State["percentuale"] = value.ToString();
					NotifyPropertyChanged("Percentuale");
				}
			}
		}

		private void Button_Tap_1(object sender, GestureEventArgs e) {
			if (_watcher == null) {
				_watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
				_watcher.MovementThreshold = 20;
				_watcher.StatusChanged += OnStatusChanged;
				_watcher.PositionChanged += OnPositionChanged;
			}
			try {
				if (this.Avviato) {
					_watcher.Stop();
					this.btnStartStop.Text = "Riparti";
				} else {
					_watcher.Start();
					this.btnStartStop.Text = "Fai una pausa";
				}
				this.Avviato = !this.Avviato;
			} catch (Exception) {
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
			}
		}

		void OnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e) {
			if (e.Position.Location.IsUnknown)
				return;
			try {
				var lastLong = 0.0;
				var lastLat = 0.0;
				Double.TryParse(PhoneApplicationService.Current.State["lastLong"].ToString(), out lastLong);
				Double.TryParse(PhoneApplicationService.Current.State["lastLat"].ToString(), out lastLat);
				if (lastLat != 0.0 && lastLong != 0.0) {
					var last = new GeoCoordinate(lastLat, lastLong);
					var dist = last.GetDistanceTo(e.Position.Location);
					var totDist = 0.0;
					Double.TryParse(PhoneApplicationService.Current.State["totale"].ToString(), out totDist);
					totDist += dist;
					PhoneApplicationService.Current.State["totale"] = totDist.ToString();
					this.Distanza = totDist.ToString("Hai corso per 0 m.");
					AggiornaMusetto(totDist);
				}
				PhoneApplicationService.Current.State["lastLong"] = e.Position.Location.Longitude;
				PhoneApplicationService.Current.State["lastLat"] = e.Position.Location.Latitude;
			} catch (Exception) {
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
			}
		}

		void AggiornaMusetto(double totDist) {
			// dovrebbe essere 5 km per etto di musetto ma io sono buono ...
			string immagine;
			if (totDist < 3000) {
				immagine = "level1.jpg";
			} else if (totDist < 6000) {
				immagine = "level2.jpg";
			} else {
				immagine = "level3.jpg";
			}
			immagine = totDist < 3000 ? "level1.jpg" : totDist < 6000 ? "level2.jpg" : "level3.jpg";
			BitmapImage bmp = new BitmapImage(new Uri(immagine, UriKind.Relative));
			this.imgMusetto.Source = bmp;
			this.Percentuale = totDist < 9000 ? this.imgMusetto.ActualHeight * (totDist % 3000) / 3000 : this.imgMusetto.ActualHeight;
			this.clipMusetto.Rect = new Rect(0, 0, this.imgMusetto.ActualWidth, this.Percentuale);
		}

		void OnStatusChanged(object sender, GeoPositionStatusChangedEventArgs e) {
		}
	}
}
