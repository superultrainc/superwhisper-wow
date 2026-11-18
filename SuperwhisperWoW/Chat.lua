local _, addon = ...
BINDING_HEADER_SUPERWHISPERWOW = "Superwhisper"
BINDING_NAME_SWW_LISTEN = "Dictate chat"

local function stop(box)
    box.swwArmed = false
    box.swwLabel:Hide()
    box.swwAnimation:Stop()
    if box.swwHeaderShown then box.swwHeader:Show() end
    box.swwHeaderShown = false
end

local function decorate(box)
    local header = box.header or _G[box:GetName() .. "Header"]
    local label = CreateFrame("Frame", nil, box)
    label:SetPoint("LEFT", box, "LEFT", 5, 0)
    label:SetSize(120, 18)
    local mic = label:CreateTexture(nil, "OVERLAY")
    mic:SetSize(14, 16)
    mic:SetPoint("LEFT")
    mic:SetTexture("Interface\\AddOns\\SuperwhisperWoW\\Mic")
    local text = label:CreateFontString(nil, "OVERLAY", "ChatFontNormal")
    text:SetPoint("LEFT", mic, "RIGHT", 4, 0)
    text:SetTextColor(.75, .75, .75)
    text:SetText("Listening…")
    local group = mic:CreateAnimationGroup()
    group:SetLooping("BOUNCE")
    local fade = group:CreateAnimation("Alpha")
    fade:SetFromAlpha(1)
    fade:SetToAlpha(.45)
    fade:SetDuration(.8)
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
    box.swwHeaderShown = box.swwHeader and box.swwHeader:IsShown()
    if box.swwHeaderShown then box.swwHeader:Hide() end
    box.swwArmed = true
    box.swwLabel:Show()
    box.swwAnimation:Play()
end

local events = CreateFrame("Frame")
events:RegisterEvent("PLAYER_LOGIN")
events:SetScript("OnEvent", function()
    if not GetBindingKey("SWW_LISTEN") and GetBindingAction("CTRL-SHIFT-F12") == "" then
        SetBinding("CTRL-SHIFT-F12", "SWW_LISTEN")
    end
end)
