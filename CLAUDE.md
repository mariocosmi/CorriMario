# CLAUDE.md

Questo file fornisce indicazioni a Claude Code (claude.ai/code) quando lavora con il codice in questo repository.

## Panoramica del progetto

Corri Mario è un'app di fitness tracking migrata da Windows Phone 7.1 Silverlight a .NET MAUI. Traccia la distanza percorsa correndo tramite GPS e gamifica l'esperienza mostrando quanto "musetto" hai guadagnato in base alla distanza.

È un'**applicazione single-page** con tutta l'UI e la logica in MainPage. L'app è basata su .NET 8.0 e supporta Android, iOS, Windows e macOS Catalyst.

## Comandi di build ed esecuzione

### Restore e Build

```bash
# Ripristina i pacchetti NuGet
dotnet restore

# Build per piattaforme specifiche
dotnet build -f net8.0-android
dotnet build -f net8.0-ios                        # Richiede macOS
dotnet build -f net8.0-windows10.0.19041.0        # Richiede Windows
dotnet build -f net8.0-maccatalyst                # Richiede macOS
```

### Esecuzione su dispositivi/emulatori

```bash
# Android
dotnet build -f net8.0-android -t:Run

# iOS (richiede macOS)
dotnet build -f net8.0-ios -t:Run

# Windows
dotnet build -f net8.0-windows10.0.19041.0 -t:Run
```

## Architettura ad alto livello

### Pattern Single-Page Application

L'intera app gira su un singolo ContentPage (`MainPage.xaml/.cs`). Non c'è un framework di navigazione—tutta la funzionalità è autonoma:

- **App.xaml.cs**: Punto di ingresso minimo dell'applicazione che imposta MainPage come root
- **MainPage.xaml.cs**: Contiene tutta la logica di tracciamento GPS, gestione dello stato e aggiornamenti UI
- **MauiProgram.cs**: Configurazione standard dell'app MAUI

### Strategia di persistenza dati

**Usa l'API Preferences di MAUI** (store chiave-valore che persiste tra le sessioni):

- `totale`: Distanza totale percorsa in metri (double)
- `distanza`: Stringa formattata della distanza mostrata all'utente
- `percentuale`: Percentuale di riempimento visuale per il livello corrente
- `avviato`: Stato di tracking booleano (in esecuzione/in pausa)
- `lastLat`, `lastLong`: Ultima posizione GPS per il calcolo della distanza

**Importante**: Le proprietà in MainPage leggono/scrivono direttamente su Preferences—non c'è stato in memoria. Ogni getter/setter di proprietà usa `Preferences.Get()`/`Preferences.Set()`.

### Architettura del tracciamento GPS

**Pattern async polling loop** (non basato su eventi):

1. L'utente preme Start → `StartTracking()` crea un `CancellationTokenSource`
2. Genera un loop `Task.Run()` in background che interroga il GPS ogni 10 secondi
3. Ogni lettura GPS chiama `OnPositionChanged()` sul thread principale tramite `MainThread.InvokeOnMainThreadAsync()`
4. La distanza viene calcolata solo se si è mossi >25m dall'ultima posizione (filtra il jitter GPS)
5. L'utente preme Pausa → `StopTracking()` cancella il token, fermando il loop

**Decisioni tecniche chiave**:
- **Soglia di movimento di 25 metri**: Ignora le letture GPS entro 25m dall'ultima posizione per prevenire il drift
- **Intervallo di polling di 10 secondi**: `await Task.Delay(10000)` tra i controlli GPS
- **Modalità alta precisione**: Usa `GeolocationAccuracy.Best` per tracciamento preciso della distanza
- **Gestione permessi**: Richiede il permesso `LocationWhenInUse` al primo utilizzo

### Sistema visuale "Musettometro"

**Clipping progressivo delle immagini** usando XAML RectangleGeometry:

L'UI mostra immagini sovrapposte (foreground + background) che vengono ritagliate per creare un effetto di "riempimento":

1. **Progressione dei livelli** (4 livelli basati sulla distanza):
   - 0-3000m: level0.jpg sfondo, level1.jpg primo piano
   - 3000-6000m: level1.jpg sfondo, level2.jpg primo piano
   - 6000-9000m: level2.jpg sfondo, level3.jpg primo piano
   - 9000m+: Completamente riempito (level3.jpg)

2. **Rettangoli di clipping**:
   - `clipMusetto`: Mostra la porzione "riempita" (altezza = percentuale del segmento corrente di 3000m)
   - `clipMusettoSfondo`: Mostra la porzione "non riempita" (altezza rimanente)

3. **Trigger di aggiornamento**:
   - Chiamato da `OnPositionChanged()` quando la distanza aumenta
   - Chiamato anche all'evento `SizeChanged` per gestire cambi di layout/rotazioni

**Perché è importante**: Quando si modifica l'UI, ricorda che le immagini devono caricarsi prima che il clipping funzioni. Il gestore `SizeChanged` assicura che il clipping venga ricalcolato dopo il layout.

## Configurazione specifica per piattaforma

### Android (Platforms/Android/)
- **AndroidManifest.xml**: Dichiara i permessi `ACCESS_FINE_LOCATION` e `ACCESS_COARSE_LOCATION`
- **MainActivity.cs**: Activity MAUI standard con gestione orientamento/dimensioni schermo

### iOS (Platforms/iOS/)
- **Info.plist**: Include `NSLocationWhenInUseUsageDescription` con spiegazione in italiano
- **AppDelegate.cs**: Delegate iOS standard per MAUI

### Windows (Platforms/Windows/)
- **App.xaml**: Wrapper dell'applicazione WinUI per MAUI

## Note sulla migrazione (Windows Phone → MAUI)

Se devi migrare o modernizzare il codice, comprendi queste sostituzioni chiave:

| Windows Phone 7.1 | Equivalente .NET MAUI |
|-------------------|----------------------|
| `GeoCoordinateWatcher` + eventi | `Geolocation.GetLocationAsync()` polling |
| `PhoneApplicationService.State` | API `Preferences` |
| `PhoneApplicationPage` | `ContentPage` |
| `GeoCoordinate.GetDistanceTo()` | `Location.CalculateDistance()` |
| Namespace Phone | Namespace `Microsoft.Maui.*` |

**I file con estensione `.old`** sono il codice originale di Windows Phone preservato come riferimento.

## Dettagli implementativi chiave

### Perché Task.Run per il tracciamento GPS?
L'API Geolocation di MAUI è request/response (non event-driven come il vecchio GeoCoordinateWatcher). L'app usa `Task.Run()` con un loop while per creare un tracciamento continuo, gestibile tramite `CancellationToken`.

### Perché sia DataContext che BindingContext?
`this.DataContext = this` deriva dalla migrazione da Windows Phone; `this.BindingContext = this` è lo standard MAUI. Entrambi sono impostati per compatibilità durante la migrazione.

### Implementazione INotifyPropertyChanged
MainPage implementa `INotifyPropertyChanged` manualmente. Proprietà come `Distanza`, `Avviato`, `Percentuale` notificano l'UI dei cambiamenti tramite `OnPropertyChanged()`. Questo è necessario perché i valori sono memorizzati in Preferences (non in backing fields), quindi l'UI non si aggiornerebbe automaticamente altrimenti.

### Lifecycle OnDisappearing
Quando la pagina scompare (app in background), `OnDisappearing()` ferma il tracciamento GPS per risparmiare batteria. I dati persistono in Preferences, quindi il tracciamento può riprendere quando l'utente ritorna.

## Aggiungere nuove funzionalità

### Per aggiungere una nuova metrica tracciata:
1. Aggiungi l'inizializzazione della chiave Preference nel costruttore `MainPage()`
2. Crea una proprietà pubblica con `get`/`set` che usa `Preferences.Get()`/`Preferences.Set()`
3. Chiama `OnPropertyChanged()` nel setter
4. Effettua il binding all'elemento UI in MainPage.xaml

### Per modificare il comportamento GPS:
- Cambiare l'intervallo di polling: Modifica `await Task.Delay(10000)` in StartTracking()
- Cambiare la precisione: Modifica `GeolocationAccuracy.Best` in GeolocationRequest
- Cambiare la soglia di movimento: Modifica `if (dist >= 25)` in OnPositionChanged()

### Per aggiungere nuove immagini di livello:
1. Aggiungi l'immagine in `Resources/Images/`
2. Aggiorna la logica di `ImmagineAdatta()` e `ImmagineSfondo()`
3. Aggiorna il calcolo di `AggiornaMusetto()` (attualmente `totDist % 3000` per livelli da 3000m)
