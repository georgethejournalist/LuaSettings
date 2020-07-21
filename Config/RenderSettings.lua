-- another example settings file - this one illustrates two concepts:
-- 1) it shows how you can use enums in the settings to make them more user friendly
-- 2) it shows that you can assign the whole table at once, overwriting any values that were there before

-- local enum definition, it does have a corresponding enum type in C#
local RenderMode = 
{
	None = 0,
	DirectX = 1,
	Vulkan = 2
}

-- by setting the whole table in this manner, any values that were there before will be overwritten!
-- (this means values coming either from different lua config files included through 'dofile("filename")' calls 
-- or default values from the corresponding C# class with the SettingsAttribute will be lost)
-- but if you're setting a whole section from one lua file, this is a much more comfortable way of doing it than setting each property one by one (e.g. RenderSettings.Width = 1920)

RenderSettings = 
{
	Width = 1920,
	Height = 1080,
	RenderMode = RenderMode.Vulkan
}


