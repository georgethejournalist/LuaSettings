local current_dir=io.popen"cd":read'*l'
print(current_dir)
dofile("GameSettings.lua")
dofile("LogSettings.lua")
dofile("RenderSettings.lua")
dofile("AdditionalConfigFolder/PlayerSettings.lua")


local a = 100
a = a + a

ExampleGUISettings.Width = 500
ExampleGUISettings.Height = 300
ExampleGUISettings.Size = 200
ExampleGUISettings.Width = a

--ExternalSettings.SomeValue = 3.14

