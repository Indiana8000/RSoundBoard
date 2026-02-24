# RSoundBoard

A network-enabled soundboard application for Windows with web interface and virtual audio cable integration.

---

# 📱 User Guide

## What is RSoundBoard?

RSoundBoard is a Windows application that lets you play sound effects from any device on your network. Control your soundboard from your phone, tablet, or another computer while the audio plays on your Windows PC. Perfect for streaming, gaming, and Discord calls!

## ✨ Key Features

- 🌐 **Web Interface** - Control from any device on your network via browser
- 🎵 **Local Audio Playback** - Sounds play on your Windows PC, not in the browser
- 🎮 **Gaming & Streaming Ready** - Works perfectly with Discord, OBS, games, and virtual audio cables
- 📱 **Mobile Friendly** - Responsive design works great on phones and tablets
- 🎯 **Simple Management** - Easy-to-use Windows interface for organizing sounds
- 🚀 **No Installation Required** - Single executable file, just download and run

## 🔧 Installation

1. **Download** the latest `RSoundBoard.exe` from the [Releases](https://github.com/Indiana8000/RSoundBoard/releases) page
2. **Run** the executable - no installation needed
3. **Allow Firewall Access** when Windows prompts you (required for network access)
4. The application starts on port **5000** by default

## 🎮 Basic Usage

### Setting Up Your Soundboard

1. **Launch** `RSoundBoard.exe`
2. The **management window** opens automatically
3. Click **"Add Button"** to add your first sound:
   - Enter a **label** (e.g., "Applause", "Airhorn")
   - Select your **sound file** (WAV or MP3)
   - Optionally set a **group** name for organization
   - Set the **order** number (lower numbers appear first)
4. Click **"Open Web Interface"** to view your soundboard

### Playing Sounds

**From the Web Interface:**
- Open a browser and go to `http://localhost:5000`
- Click any button to play that sound
- Playing a new sound stops the current one

**From Other Devices:**
- Find your PC's IP address (shown in the management window or use `ipconfig` in CMD)
- Open `http://YOUR-PC-IP:5000` in any browser on your network
- Example: `http://192.168.1.100:5000`

## 🎧 Using with Voicemeeter & Virtual Audio

### Why Use Virtual Audio Cables?

To use RSoundBoard with games, Discord, or streaming software, you need to route the audio properly. **[Voicemeeter](https://vb-audio.com/Voicemeeter/)** is a free virtual audio mixer that makes this easy.

### Recommended Setup with Voicemeeter

#### Installation

1. **Download & Install** [Voicemeeter Banana](https://vb-audio.com/Voicemeeter/banana.htm) (free)
2. **Restart your computer** after installation
3. Launch **Voicemeeter Banana**

#### Configuration for Gaming + Discord + Soundboard

**Step 1: Set Windows Audio Output**
- Right-click the **speaker icon** in Windows system tray
- Select **"Sound Settings"** → **"Output Device"**
- Choose **"Voicemeeter Input (VB-Audio Voicemeeter VAIO)"**

**Step 2: Configure RSoundBoard Output**
- In RSoundBoard, set the output device to a virtual cable (e.g., **"Voicemeeter AUX Input (VB-Audio Voicemeeter VAIO)"**)
- Alternatively, you can keep the default output which is already routed to Voicemeeter (Step 1)

**Step 3: Configure Voicemeeter**
- **Hardware Input 1**: Your microphone
- **Hardware Out A1**: Your headphones/speakers (for you to hear)
- **Hardware Out A2**: VB-Audio Cable (for Discord/apps to receive)
- Set the routing in Voicemeeter:
  - For your microphone: Enable A1 (you hear it) and A2 (Discord hears it)
  - For RSoundBoard output: Enable A1 (you hear it) and disable A2 (Discord does NOT hear game audio + individual volume controle)

**Step 4: Configure Discord/OBS**
- In **Discord**: Settings → Voice & Video → Input Device → **"Voicemeeter Output B2"**
- In **OBS**: Add Audio Source → Select **"Voicemeeter Output B2"**

#### Result
- ✅ You hear: Game audio + Discord voices + Soundboard
- ✅ Discord/Stream hears: Your microphone + Soundboard
- ✅ Discord/Stream does NOT hear: Your game audio (unless you want them to)

### Alternative: Simple Setup (Need only VB-Audio Cable)

1. Set **RSoundBoard input** to your microphone
2. Set **RSoundBoard output** to **VB-Audio Cable**
3. Set **Discord input** to **VB-Audio Cable Output**

### Troubleshooting

**No sound playing?**
- Check if the correct output device is selected in Windows Sound Settings
- Verify the sound file path is correct in RSoundBoard
- Make sure Voicemeeter is running

**Discord can't hear sounds?**
- Check Discord input device is set to the virtual cable
- Ensure Voicemeeter's A2 output is enabled and routed correctly
- Test the virtual cable with Windows Sound Recorder

**Web interface not loading?**
- Check if Windows Firewall is blocking port 5000
- Verify you're using the correct IP address
- Try accessing from localhost first: `http://localhost:5000`

## 🔒 Security Note

RSoundBoard has **no authentication** and is designed for use on **trusted local networks only**. Anyone on your network can access and play sounds. Do not expose it to the public internet.

## 📂 File Locations

- **Configuration File**: `soundboard_data.json` (created in the same folder as the .exe)
- **Sound Files**: Stored wherever you choose; the configuration only saves the path

## 💡 Tips & Tricks

- **Organize with Groups**: Use group names like "Music", "Effects", "Memes" to keep things tidy
- **Mobile Bookmark**: Save the web interface URL as a home screen bookmark on your phone
- **Wireless Control**: Perfect for streamers who want soundboard control away from the PC

---

# 👨‍💻 Developer Documentation

## 🏗️ Project Architecture

### Tech Stack

- **Framework**: .NET 8 (net8.0-windows)
- **Language**: C# 12 with nullable reference types enabled
- **Web Server**: ASP.NET Core Minimal API (Kestrel)
- **Audio Engine**: NAudio 2.2.1
- **Desktop UI**: Windows Forms
- **Deployment**: Single-file self-contained executable (win-x64)

### Architecture Pattern

The application follows a **service-based architecture** with dependency injection:

- **Presentation Layer**: Windows Forms (management UI) + Static HTML/CSS/JS (web UI)
- **API Layer**: ASP.NET Core Minimal API endpoints
- **Business Logic**: Service classes (`SoundService`, `SettingsService`)
- **Data Access**: Repository pattern (`ButtonRepository`)
- **Data Storage**: JSON file persistence

### Project Structure

```
RSoundBoard/
├── Program.cs                  # Entry point - configures DI, starts web server & WinForms
├── Models/
│   └── SoundButton.cs          # Data model for sound buttons
├── Services/
│   ├── ButtonRepository.cs     # JSON-based persistence
│   ├── SoundService.cs         # NAudio wrapper for audio playback
│   └── SettingsService.cs      # Application settings management
├── HostUI/
│   ├── MainForm.cs             # Main management window
│   └── ButtonEditDialog.cs    # Button add/edit dialog
└── wwwroot/
    ├── index.html              # Web soundboard interface
    └── style.css               # Styling for web interface

Generated at runtime:
├── soundboard_data.json        # Button configuration
└── settings.json               # Application settings
```

## 🛠️ Development Setup

### Prerequisites

- **Windows 10/11**
- **.NET 8 SDK** or later
- **Visual Studio 2022** or **VS Code** with C# extension

### Building the Project

**Clone the repository:**
```bash
git clone https://github.com/Indiana8000/RSoundBoard.git
cd RSoundBoard
```

**Restore dependencies:**
```bash
dotnet restore
```

**Run in development mode:**
```bash
dotnet run
```

**Build for release:**
```bash
dotnet build -c Release
```

**Publish as single-file executable:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output location: `bin/Release/net8.0-windows/win-x64/publish/RSoundBoard.exe`

## 📡 API Reference

All API endpoints return JSON and follow REST conventions.

### Endpoints

#### `GET /api/buttons`
Returns all sound buttons.

**Response:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "label": "Applause",
    "filePath": "C:\\Sounds\\applause.wav",
    "group": "Effects",
    "order": 0
  }
]
```

#### `POST /api/play/{id}`
Plays the sound with the specified GUID.

**Response:** `200 OK` or `404 Not Found`

#### `POST /api/stop`
Stops the currently playing sound.

**Response:** `200 OK`

#### `POST /api/buttons`
Creates a new sound button.

**Request Body:**
```json
{
  "label": "New Sound",
  "filePath": "C:\\path\\to\\sound.mp3",
  "group": "Group Name",
  "order": 10
}
```

**Response:** `201 Created` with created button object

#### `PUT /api/buttons/{id}`
Updates an existing button.

**Request Body:** Same as POST

**Response:** `200 OK` or `404 Not Found`

#### `DELETE /api/buttons/{id}`
Deletes a button.

**Response:** `200 OK` or `404 Not Found`

## 🧩 Key Components

### Services

**`SoundService`**
- Manages NAudio `IWavePlayer` and `AudioFileReader` instances
- Ensures only one sound plays at a time
- Thread-safe using `SemaphoreSlim`
- Implements proper disposal pattern

**`ButtonRepository`**
- CRUD operations for `SoundButton` entities
- JSON file persistence
- Thread-safe read/write operations

**`SettingsService`**
- Stores and retrieves application settings (e.g., output device)
- JSON-based configuration file

### Dependency Injection

Services are registered as singletons in `Program.cs`:

```csharp
builder.Services.AddSingleton<ButtonRepository>();
builder.Services.AddSingleton<SoundService>();
builder.Services.AddSingleton<SettingsService>();
```

### Audio Playback

Uses **NAudio** library:
- `WaveOutEvent` for audio output
- `AudioFileReader` for reading MP3/WAV files
- Supports device selection via device number

### Web Server

- Listens on `http://0.0.0.0:5000` (all network interfaces)
- Serves static files from embedded resources
- Minimal API pattern for endpoints
- No HTTPS (designed for LAN use)

## 🔧 Code Style Guidelines

Follow the project's established conventions (see `.github/copilot-instructions.md`):

- **Nullable Reference Types**: Enabled - use `?` for nullable types
- **Implicit Usings**: Enabled - avoid redundant using statements
- **Var keyword**: Use `var` when type is obvious
- **String initialization**: Use `string.Empty` instead of `""`
- **Async/Await**: Use async methods consistently
- **Naming**:
  - Classes, Methods, Properties: `PascalCase`
  - Private fields: `_camelCase`
  - Parameters, locals: `camelCase`

## 🧪 Testing

Currently, the project does not include automated tests. Contributions adding unit and integration tests are welcome.

### Manual Testing Checklist

- [ ] Add/Edit/Delete buttons via desktop UI
- [ ] Play sounds from web interface
- [ ] Test from mobile device on same network
- [ ] Verify only one sound plays at a time
- [ ] Check audio device selection (if multiple devices available)
- [ ] Verify JSON persistence after app restart

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create a feature branch**: `git checkout -b feature/your-feature`
3. **Follow code style** guidelines (see above)
4. **Test your changes** thoroughly
5. **Commit** with clear messages in English
6. **Submit a pull request**

### Areas for Contribution

- Audio format support (FLAC, OGG, etc.)
- Volume control per button
- Hotkey support
- Sound preview in management UI
- Authentication/authorization for web interface
- Automated tests

## 📄 License

This project is open source. See the repository for license details.

## 🙏 Acknowledgments

- **NAudio** - Audio playback library
- **Voicemeeter** - Virtual audio mixing (recommended companion software)

---

**Repository**: https://github.com/Indiana8000/RSoundBoard
