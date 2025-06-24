#  MP2 BOT


# DreamPoeBot 2 Custom Bot with Plugins

**A lightweight custom implementation built on top of the DreamPoeBot 2 framework, featuring modular plugins for Path of Exile automation.**

---

## 📘 What is DreamPoeBot 2?

DreamPoeBot 2 (DPB2) is an advanced automation framework for *Path of Exile* and *Path of Exile 2*, written in C# using .NET 8.0. 
It provides a full-featured API that automates game mechanics such as mapping, flask usage, login, and navigation.

---

## ⚙️ Getting Started

### Prerequisites

You must have the following reference files from your DPB2 installation directory:

- `log4net.dll`
- `DreamPoeBot.exe`
- `SharpDx.dll`
- `MahApps.Metro.dll`
- `Newtonsoft.Json.dll`

Place these reference files alongside your plugin projects or reference them directly in Visual Studio.

---

## 🧩 Available Plugins

### 1. **Mp2 (Core)**
Responsible for core automation systems including:
- Mapping
- Simulacrum
- Auto potion usage (AutoPot)
- Navigation and task handling
- And More

### 2. **Mp2AutoLogin**
Handles automatic login functionality in case of disconnects.

### 3. **Mp2Mover**
Manages character movement and pathfinding logic.

### 4. **Mp2Routine**
Customizes skill and routine execution per build. Allows fine-tuned skill rotations and logic.

---

## 🛠️ Installation & Setup

1. Compile each plugin to generate `.dll` files (e.g., `Mp2.dll`, `Mp2AutoLogin.dll`, etc.).
2. Navigate to your DPB2 client folder and create the following plugin directory structure:

```
DPB2/
└── Plugins/
    ├── Mp2/
    │   └── Mp2.dll
    ├── Mp2AutoLogin/
    │   └── Mp2AutoLogin.dll
    ├── Mp2Mover/
    │   └── Mp2Mover.dll
    └── Mp2Routine/
        └── Mp2Routine.dll
```

Each plugin must be placed inside its corresponding folder under `Plugins`.

---

## 🚀 Running the Bot

1. Launch `DreamPoeBot.exe` from your DPB2 folder.
2. The bot will automatically detect and load the plugins located in the `Plugins` folder.
3. Configure settings via the DreamPoeBot UI or plugin-specific configuration files.
4. Start your desired routine, such as mapping or flask automation.

---

## 🛡️ Notes & Safety

- You are responsible for ensuring your plugin references are correctly set up.
- This project assumes familiarity with DPB2 and its configuration structure.

---

## 👥 Contributing

Pull requests are welcome! Feel free to contribute with bug fixes, new features, or documentation improvements.

---

## 📄 License

This project is open-source and distributed under the [MIT License](LICENSE).
