local _, addon = ...
BINDING_HEADER_SUPERWHISPERWOW = "Superwhisper"
BINDING_NAME_SWW_LISTEN = "Dictate chat"

local function stop(box)
    if not box.swwArmed then return end
    box.swwArmed = false
    box:SetTextColor(unpack(box.swwColor))
    box.swwLabel:Hide()
    box.swwAnimation:Stop()
    if box.swwHeaderShown then box.swwHeader:Show() end
    box.swwHeaderShown = false
end

local function decorate(box)
    local header = box.header or _G[box:GetName() .. "Header"]
    local label = CreateFrame("Frame", nil, box)
    label:SetPoint("LEFT", box, "LEFT", 8, 0)
    label:SetSize(120, 18)
    local mic = label:CreateTexture(nil, "OVERLAY")
    local _, fontSize = box:GetFont()
    mic:SetSize(.85 * fontSize, 1.1 * fontSize)
    mic:SetPoint("LEFT")
    mic:SetTexture("Interface\\AddOns\\SuperwhisperWoW\\Mic")
    local text = label:CreateFontString(nil, "OVERLAY", "ChatFontNormal")
    text:SetPoint("LEFT", mic, "RIGHT", 6, 0)
    local gray = 154 / 255
    text:SetTextColor(gray, gray, gray)
    text:SetText("Listening…")
    local group = mic:CreateAnimationGroup()
    group:SetLooping("REPEAT")
    for order = 1, 2 do
        local fade = group:CreateAnimation("Alpha")
        fade:SetOrder(order)
        fade:SetFromAlpha(order == 1 and .55 or 1)
        fade:SetToAlpha(order == 1 and 1 or .55)
        fade:SetDuration(.6)
        fade:SetSmoothing("OUT")
    end
    box.swwLabel, box.swwAnimation, box.swwHeader = label, group, header
    label:Hide()
    box:HookScript("OnHide", stop)
    box:HookScript("OnEditFocusLost", stop)
    box:HookScript("OnTextChanged", function(self, userInput)
        if not self.swwArmed or not userInput or self:GetText() == "" then return end
        stop(self)
        local value = addon.route(self:GetText())
        self:SetText(value or "")
        if value and value:sub(1, 1) == "/" then self:ParseText(0) end
        self:SetCursorPosition(#self:GetText())
    end)
end

function SuperwhisperWoW_Listen()
    local box = ChatFrameUtil.ChooseBoxForSend()
    if not box or box:GetText() ~= "" then return end
    ChatFrameUtil.OpenChat("", box.chatFrame or DEFAULT_CHAT_FRAME)
    if not box:HasFocus() then return end
    if not box.swwLabel then decorate(box) end
    if box.swwArmed then return end
    box.swwColor = {box:GetTextColor()}
    box:SetTextColor(1, 1, 1, 0)
    box.swwHeaderShown = box.swwHeader and box.swwHeader:IsShown()
    if box.swwHeaderShown then box.swwHeader:Hide() end
    box.swwArmed = true
    box.swwLabel:Show()
    box.swwAnimation:Play()
end

local events = CreateFrame("Frame")
events:RegisterEvent("PLAYER_ENTERING_WORLD")
events:RegisterEvent("PLAYER_REGEN_ENABLED")
events:SetScript("OnEvent", function(self)
    if InCombatLockdown() then return end
    ClearOverrideBindings(self)
    local function bindIfFree(chord)
        local action = GetBindingAction(chord)
        if not action or action == "" then
            SetOverrideBinding(self, false, chord, "SWW_LISTEN")
        end
    end
    bindIfFree("CTRL-SHIFT-F12")
    if IsWindowsClient and IsWindowsClient() then
        bindIfFree("ALT-CTRL-SHIFT-F12")
    end
end)
