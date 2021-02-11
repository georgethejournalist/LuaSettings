# LuaSettings

![.NET 4.6](https://github.com/georgethejournalist/LuaSettings/workflows/.NET%204.6/badge.svg?branch=master)

A C# library using NeoLua (https://github.com/neolithos/neolua) for easy handling of configuration files in Lua using a single attribute. Useful for any application settings where you don't want to bind yourself with the rigid structure of XAMLs, inability to add comments (looking at you, JSON) or just plain old want your settings to be scriptable.

## What it is

LuaSettings is a library that allows you represent any C# classes as 'settings sections' that are then easily obtainable from Lua files.

## Features

- Loads lua files with settings; 
- Automatically creates C# instances of settings classes (any class with `SettingsAttribute` );
- Populates these instances with data from lua files.

## Intended purpose

LuaSettings can be added to any C# project where you require easily obtainable and human-readable (and editable) application settings, which are scriptable. (This sounds like a mouthful, but once you try using lua for your config files, you won't want to go back to XAML.)

## How does it work?

**Clone** the repo, open the **LuaSettingsTester.sln** and look through the example program. Then read through the example config files to give you an idea of what can be done through these lua files.

Otherwise it uses NeoLua to provide easy access to a Lua environment and handles the process of registering and loading settings sections from lua and deserializing them into C# instances that you can use right away.



## Usage

The usage is quite simple and consists of basically three steps:

1. Mark any class that you want to represent a settings section with the **SettingsAttribute**, **providing a string as a key** for the settings section, like so:



```c#
[Settings("ExampleGUISettings")]
public class ExampleGUISettings
{
	...
} 
```



2. Create a settings file for the application, fill in whatever lua magic you want. See the Config folder in the repository for examples. Here's a simple use:

```lua
ExampleGUISettings.Width = 500
ExampleGUISettings.Height = 300
ExampleGUISettings.Size = 200
```

3. Load the settings in your application through the SettingsManager, similarly to this:

```c#
// create the settings manager and point it to the folder with your config (can be absolute or relative path, like here)
var settingsManager = new SettingsManager("../../../../Config");
// whenever your application desires, load the settings (usually on start-up)
settingsManager.LoadSettings();
// anyone who wants to get a particular settings section can get it by a key and cast it
var renderSettings = settingsManager.GetSettingsSection("RenderSettings") as RenderSettings;
// and here you can use the settings as you see fit, pseudo-example below
renderPipeline.SetResolution(renderSettings.width, renderSettings.height);
```



## Known issues & limitations

- No serialization support at the moment, but it is planned to be added
