# Corri Mario - .NET MAUI

Questa è la versione migrata da Windows Phone a .NET MAUI dell'app "Corri Mario".

## Descrizione

"Corri Mario" è un'app fitness che traccia la distanza percorsa correndo usando il GPS e visualizza in modo divertente quanto "musetto" (salume) hai guadagnato in base ai metri percorsi.

## Funzionalità

- **Tracciamento GPS**: Usa la posizione del dispositivo per tracciare la distanza percorsa
- **Musettometro visuale**: Visualizzazione progressiva con 4 livelli di immagini
  - Livello 0-1: 0-3000m
  - Livello 1-2: 3000-6000m
  - Livello 2-3: 6000-9000m
  - Livello 3: 9000m+
- **Pausa e riprendi**: Possibilità di mettere in pausa e riprendere il tracciamento
- **Persistenza dati**: I dati vengono salvati automaticamente

## Piattaforme supportate

- Android (21.0+)
- iOS (11.0+)
- Windows (10.0.17763.0+)
- macOS Catalyst (13.1+)

## Requisiti

- .NET 8.0 SDK o superiore
- Per Android: Android SDK
- Per iOS: Xcode e macOS
- Per Windows: Visual Studio 2022 con workload .NET MAUI

## Come compilare

### Da riga di comando

```bash
# Ripristina i pacchetti NuGet
dotnet restore

# Compila per Android
dotnet build -f net8.0-android

# Compila per iOS (richiede macOS)
dotnet build -f net8.0-ios

# Compila per Windows
dotnet build -f net8.0-windows10.0.19041.0
```

### Da Visual Studio 2022

1. Apri `CorriMario.sln`
2. Seleziona la piattaforma target (Android, iOS, Windows)
3. Premi F5 per compilare ed eseguire

### Da Visual Studio Code

1. Installa l'estensione ".NET MAUI"
2. Apri la cartella del progetto
3. Usa il comando "MAUI: Pick Android/iOS Device" per selezionare il target
4. Premi F5 per eseguire

## Come eseguire

### Android

```bash
dotnet build -f net8.0-android -t:Run
```

### iOS (richiede macOS e dispositivo o simulatore)

```bash
dotnet build -f net8.0-ios -t:Run
```

### Windows

```bash
dotnet build -f net8.0-windows10.0.19041.0 -t:Run
```

## Permessi richiesti

### Android
- `ACCESS_FINE_LOCATION`: Accesso alla posizione precisa per il GPS
- `ACCESS_COARSE_LOCATION`: Accesso alla posizione approssimativa
- `INTERNET`: Connessione internet (per i servizi di localizzazione)

### iOS
- `NSLocationWhenInUseUsageDescription`: Accesso alla posizione durante l'uso dell'app

## Modifiche dalla versione Windows Phone

- Migrazione da Windows Phone 7.1 Silverlight a .NET MAUI
- Sostituzione di `GeoCoordinateWatcher` con l'API `Geolocation` di MAUI
- Sostituzione di `PhoneApplicationService.State` con `Preferences` di MAUI
- Aggiornamento UI da PhoneApplicationPage a ContentPage
- Supporto multi-piattaforma (Android, iOS, Windows, macOS)

## Struttura del progetto

```
CorriMario/
├── App.xaml / App.xaml.cs          # Applicazione principale
├── MainPage.xaml / MainPage.xaml.cs # Pagina principale con logica GPS
├── MauiProgram.cs                   # Configurazione dell'app
├── Resources/
│   ├── Images/                      # Immagini dei livelli
│   │   ├── level0.jpg
│   │   ├── level1.jpg
│   │   ├── level2.jpg
│   │   └── level3.jpg
│   ├── AppIcon/                     # Icona dell'app
│   ├── Splash/                      # Splash screen
│   └── Styles/                      # Stili XAML
└── Platforms/
    ├── Android/                     # Codice specifico Android
    ├── iOS/                         # Codice specifico iOS
    └── Windows/                     # Codice specifico Windows
```

## Note tecniche

- **Soglia di movimento GPS**: 25 metri (per evitare jitter GPS)
- **Intervallo di aggiornamento GPS**: 10 secondi
- **Precisione GPS**: Alta (GeolocationAccuracy.Best)
- **Calcolo distanza**: Ogni 3000m si passa al livello successivo

## Autore

Mario Cosmi

## Licenza

Copyright © 2012-2025 Mario Cosmi
