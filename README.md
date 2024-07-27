# Hoi4UniversalTranslator

## Description

The **Hoi4UniversalTranslator** is a command-line interface (CLI) tool designed to bulk translate text files from the popular game Hearts of Iron IV. With it, you can easily convert files from one language to another, streamlining the localization and customization process for your game.

## Features

- **Bulk translation:** Translates multiple files at once, saving time and effort.
- **Supports multiple languages:** any Language you have on google translate

## Installation From Source

### You Need .NET SDK 8.0 To Build Then

1. **Clone the repository:**

   ```bash
   git clone https://github.com/soulwach900/Hoi4UniversalTranslator.git
   ```

   ```bash
   cd "Hoi4UniversalTranslator"
   ```

2. **Build Project:**

   ```bash
   dotnet build
   ```

3. **Running:**

   ```bash
   dotnet run
   ```

## How to Change the Translation Language?

Open **config.json** and change the **"ToLang"** line to your language

## Broken Interface

there are specific color characters, fonts etc... that may break the game interface so it is not guaranteed that it will work 100%

## Change Log
- 0.1A  : Much Faster Translation, Added In Output Colors

## Coming Soon

- [ ] Fix All paradox Codes
- [ ] Create a Gui
- [ ] Progress Bar / Improve Output ( CLI )
