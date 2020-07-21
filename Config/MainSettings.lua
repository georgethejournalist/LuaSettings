-- you can use some default lua functions like 'print' and of course 'dofile' to process additional files
-- here's an example of getting a working directory for the NeoLua context
local current_dir=io.popen"cd":read'*l'
print("Processing settings with current dir set to:", current_dir)

-- you can structure your settings files in any way you want, here are examples including files at the same folder level and other in a different folder
-- because we're in lua, your config file is programmable - you could for example set the path to a config file dynamically, based on some additional parameters
dofile("GameSettings.lua")
dofile("LogSettings.lua")
dofile("RenderSettings.lua")
dofile("AdditionalConfigFolder/PlayerSettings.lua")

-- usual lua rules apply - you can define local variables, use them in functions etc
local a = 100
a = a + a

-- here are example of setting single properties for a settings section that will be available through the SettingsManager (it has a corresponding attributed class)
ExampleGUISettings.Width = 500
ExampleGUISettings.Height = 300
ExampleGUISettings.Size = 200
-- lua is programmable and does not care that you have set the value several times already - the last one wins :)
ExampleGUISettings.Width = a

-- commenting values out works of course
--ExternalSettings.SomeValue = 3.14

-- cascading settings is possible (and desirable!)
-- because we call dofile on PlayerSettings above, setting ExternalSettings.SomeValue here will overwrite the value
-- this gives us additional control - we might set default values in separate files and only overwrite some specific properties in here
ExternalSettings.SomeValue = 2.5
