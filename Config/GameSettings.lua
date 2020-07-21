-- this section has no corresponding attributed class in the TesterApp and the ExternalLibraryWithSettings,
-- so this section will not be available through the SettingsManager,
-- however this table being here won't break lua processing
-- note that this is the syntax to specify the whole settings section at once

GameSettings = {
	Start = false,
	Difficulty = "medium"
}