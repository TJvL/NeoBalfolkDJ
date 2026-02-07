# NeoBalfolkDJ

**NeoBalfolkDJ** is a free and open-source application designed specifically for Balfolk dance nights. It helps DJs manage their music library, display dance information to dancers, and keep the evening running smoothly.

The application is mainly build with a small dance night in mind, but use it however you use fit.

## Core Features

- **Presentation Displays** - Connect to big screens to show dancers the current dance and upcoming dance, so everyone knows what's coming next on the dance floor.

- **Weighted Randomization** - Intelligent random track selection based on customizable dance categories and weights, ensuring variety while respecting your preferences.

- **Dance Categorization** - Organize your music library into a hierarchical dance tree with categories (e.g., Koppeldansen, Bretons, Mixers) and individual dances, each with configurable weights.

- **Dance Synonyms** - Define synonyms for dance names to automatically categorize tracks with different naming conventions.

- **Queue Management** - Build and manage your playlist with drag-and-drop support, manual ordering, and automatic track suggestions.

- **Stop Markers** - Insert stop markers in your queue to pause playback at specific points, perfect for announcements or planned breaks.

- **Delay Markers** - Add timed delays between tracks for setup time, partner finding, or short breaks.

- **Message Markers** - Display custom messages on presentation screens for announcements, break notices, or any information you want to share with dancers.

- **Session History** - Track what you've played during the session to avoid repetition and export your playlist for future reference.

📖 **[Read the full documentation](documentation/help.md)** for detailed instructions on how to use the application.

🛠️ **[Architecture Documentation](documentation/architecture.md)** for developers - covers MVVM, dependency injection, event aggregator, command bus, and more.

---

## Technology

NeoBalfolkDJ is built with:

- **.NET 10** and **C#** for robust, modern application development
- **Avalonia UI** for cross-platform desktop support
- **NetCoreAudio** [Visit their repository for information](https://github.com/mobiletechtracker/NetCoreAudio)

This allows the application to run natively on:

- 🐧 **Linux**
- 🪟 **Windows 11**
- 🍎 **macOS**

### Releases

Pre-built binaries for all supported platforms are available through [GitHub Releases](https://github.com/your-username/NeoBalfolkDJ/releases). Download the appropriate version for your operating system and start using NeoBalfolkDJ right away.

---

## Audio Playback Requirements

NeoBalfolkDJ uses **NetCoreAudio** for audio playback. Depending on your operating system, you may need to install additional libraries for audio to work correctly.

### Linux

NetCoreAudio requires one of the following audio players to be installed:

- **ALSA Utils** (`aplay`) - Usually pre-installed on most distributions
  ```bash
  # Debian/Ubuntu
  sudo apt install alsa-utils
  
  # Fedora
  sudo dnf install alsa-utils
  
  # Arch
  sudo pacman -S alsa-utils
  ```

- Alternatively, **FFplay** (part of FFmpeg) or **mpv** can be used

### Windows

No additional dependencies required. NetCoreAudio uses the built-in Windows audio APIs.

### macOS

NetCoreAudio uses the built-in **afplay** command, which is included with macOS. No additional installation required.

---

## License

NeoBalfolkDJ is open-source software. Feel free to use, modify, and distribute it according to the license terms.

---

*Made with ❤️ for the Balfolk community*

