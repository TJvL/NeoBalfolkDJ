# NeoBalfolkDJ Documentation

Welcome to the NeoBalfolkDJ documentation. This guide will help you get started and make the most of the application for your Balfolk dance nights.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Main Interface](#main-interface)
3. [Track List](#track-list)
4. [Dance Tree](#dance-tree)
5. [Queue Management](#queue-management)
6. [Playback Controls](#playback-controls)
7. [Presentation Displays](#presentation-displays)
8. [Settings](#settings)
9. [Tips for DJs](#tips-for-djs)

---

## Getting Started

### First Launch

When you first launch NeoBalfolkDJ, you'll need to configure your music directory:

1. Click the **Settings** button (gear icon) in the toolbar
2. Click **Browse** next to "Music Directory"
3. Select the folder containing your music files
4. The application will scan and load your tracks

### Supported Audio Formats

NeoBalfolkDJ supports common audio formats including:
- MP3
- FLAC
- WAV
- OGG
- M4A

Track metadata (artist, title, dance type) is read from the file's ID3 tags or filename.

---

## Main Interface

The main window is divided into several areas:

- **Toolbar** (top) - Access settings, queue controls, and special markers
- **Track List / Dance Tree** (left) - Browse and search your music library
- **Queue** (right) - Your current playlist and upcoming tracks
- **Playback Controls** (bottom left) - Play, pause, skip, and track progress

---

## Track List

The track list shows all tracks in your music library. You can:

- **Search** - Type in the search box to filter tracks by artist, title, or dance
- **Sort** - Click column headers to sort by different fields
- **Add to Queue** - Double-click a track

### Switching Views

Click the tree/list toggle button to switch between:
- **Track List** - Flat list of all tracks with search
- **Dance Tree** - Hierarchical view organized by dance categories

---

## Dance Tree

The dance tree organizes dances into categories with configurable weights for randomization.

### Structure

- **Categories** - Groups of related dances (e.g., "Koppeldansen", "Bretons")
- **Dances** - Individual dance types (e.g., "Mazurka", "Scottish")
- **Weights** - Higher weights mean higher probability in random selection

### Editing the Tree

- **Add Category/Dance** - Select a category and use the add buttons
- **Edit Weight** - Click on an item to modify its weight
- **Delete** - Select an item and use the delete button (confirmation required)
- **Undo/Redo** - Use the undo/redo buttons to revert changes

### Import/Export

You can import and export the dance tree as JSON files for backup or sharing.

---

## Queue Management

The queue shows your planned tracks and markers.

### Adding Items

- **Tracks** - Double-click from track list or use the dice button for random
- **Stop Marker** - Pauses playback (for announcements)
- **Delay Marker** - Inserts a timed pause between tracks
- **Message Marker** - Displays a custom message on presentation screens

### Queue Controls

- **Remove** - Select an item and click the remove button
- **Clear Queue** - Remove all items (confirmation required)
- **Reorder** - Drag and drop to change order

### Auto-Queue

When enabled in settings, a random track is automatically suggested based on your dance tree weights.

---

## Playback Controls

- **Play/Pause** - Start or pause the current track
- **Restart** - Restart the current track from the beginning
- **Next/Clear** - Skip to next item or clear current track when queue is empty

The progress bar shows current position and total duration.

---

## Presentation Displays

Presentation displays show dance information on external screens for dancers to see.

### Setup

1. Go to **Settings**
2. Set **Presentation Displays** to the number of screens you want (1-6)
3. Position the windows on your external displays

### Display Content

- **Current Dance** - Name of the dance currently playing
- **Artist & Title** - Track information
- **Progress Bar** - Visual indicator of track progress
- **Next Dance** - What's coming up next

### Special Displays

- **Stop** - Shown when a stop marker is active
- **Delay** - Countdown timer during delays
- **Message** - Custom message text for announcements

---

## Settings

Access settings via the gear icon in the toolbar.

### Available Settings

| Setting | Description |
|---------|-------------|
| Music Directory | Folder containing your music files |
| Maximum Queue Items | Limit on queue length (1-100) |
| Delay Duration | Default duration for delay markers |
| Presentation Displays | Number of presentation windows (0-6) |
| Auto-queue Random Track | Automatically suggest tracks during playback |
| Allow Duplicate Tracks | Whether tracks can repeat in a session |
| Theme | Light, Dark, or Auto (follows system) |

### Dance Synonyms

Edit dance synonyms to help categorize tracks with different naming conventions. For example, "Waltz" and "Wals" can be treated as the same dance.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Z | Undo (in dance tree or synonym editor) |
| Ctrl+Shift+Z | Redo (in dance tree or synonym editor) |
| Delete | Remove selected queue item (except auto-queued tracks) |

---

## Troubleshooting

### No Sound

- Check that your system audio is working
- Verify the correct audio output device is selected
- On Linux, ensure ALSA utils or an alternative player is installed

### Tracks Not Loading

- Verify the music directory path in settings
- Check that files have supported formats
- Ensure files are not corrupted

### Presentation Display Issues

- Make sure the display count is greater than 0 in settings
- Position windows on the correct monitors
- Check display cable connections

---

*Happy Dancing! 🎵💃🕺*

