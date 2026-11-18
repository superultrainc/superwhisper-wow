-- Exercise the event handler with the order used when reconnecting to a character.
local events, overrides, action, combat = {}, {}, "", false
local frame = {
    RegisterEvent = function(_, event) events[event] = true end,
    SetScript = function(self, _, callback) self.callback = callback end,
}
function CreateFrame() return frame end
function InCombatLockdown() return combat end
function GetBindingAction() return action end
function SetOverrideBinding(owner, _, key, binding)
    assert(owner == frame and not combat)
    overrides[key] = binding
end
function ClearOverrideBindings(owner)
    assert(owner == frame and not combat)
    overrides = {}
end
assert(loadfile("SuperwhisperWoW/Chat.lua"))("SuperwhisperWoW", {})
assert(events.PLAYER_ENTERING_WORLD and events.PLAYER_REGEN_ENABLED)
local chord = "CTRL-SHIFT-F12"
combat = true
frame:callback("PLAYER_ENTERING_WORLD")
assert(not overrides[chord])
combat = false
frame:callback("PLAYER_REGEN_ENABLED")
assert(overrides[chord] == "SWW_LISTEN")
action = "TOGGLEBACKPACK"
frame:callback("PLAYER_ENTERING_WORLD")
assert(not overrides[chord])
