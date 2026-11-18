-- Minimal WoW widget doubles retain the values applied by the real listening binding.
local function widget()
    local calls = {}
    return setmetatable({calls=calls}, {
        __index = function(_, key)
            if key:match("^sww") then return nil end
            return function(_, ...) calls[key] = {...} end
        end,
    })
end

local box, header, label, mic, text, group = widget(), widget(), widget(), widget(), widget(), widget()
local hooks, fades = {}, {}
box.header = header
function header:IsShown() return true end
function box:GetText() return "" end
function box:GetFont() return "chat font", 14 end
function box:GetTextColor() return .4, .6, .8, 1 end
function box:HasFocus() return true end
function box:HookScript(name, callback) hooks[name] = callback end
function label:CreateTexture() return mic end
function label:CreateFontString() return text end
function mic:CreateAnimationGroup() return group end
function group:CreateAnimation(kind)
    assert(kind == "Alpha")
    local fade = widget()
    table.insert(fades, fade)
    return fade
end
function CreateFrame(_, _, parent) return parent == box and label or widget() end
ChatFrameUtil = {ChooseBoxForSend=function() return box end, OpenChat=function() end}

assert(loadfile("SuperwhisperWoW/Chat.lua"))("SuperwhisperWoW", {})
SuperwhisperWoW_Listen()
assert(header.calls.Hide and label.calls.Show and group.calls.Play)
assert(box.calls.SetTextColor[4] == 0)
assert(text.calls.SetText[1] == "Listening…")
for _, component in ipairs(text.calls.SetTextColor) do assert(component == 154 / 255) end
assert(group.calls.SetLooping[1] == "REPEAT" and #fades == 2)
for index, fade in ipairs(fades) do
    assert(fade.calls.SetOrder[1] == index)
    assert(fade.calls.SetDuration[1] == .6)
    assert(fade.calls.SetSmoothing[1] == "OUT")
end
assert(fades[1].calls.SetFromAlpha[1] == .55 and fades[1].calls.SetToAlpha[1] == 1)
assert(fades[2].calls.SetFromAlpha[1] == 1 and fades[2].calls.SetToAlpha[1] == .55)
hooks.OnEditFocusLost(box)
assert(header.calls.Show and label.calls.Hide and group.calls.Stop)
assert(table.concat(box.calls.SetTextColor, ",") == "0.4,0.6,0.8,1")
