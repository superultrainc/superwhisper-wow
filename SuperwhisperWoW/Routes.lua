local _, addon = ...

local channels = {
    {"raid warning", "/rw"}, {"party", "/p"}, {"raid", "/raid"},
    {"guild", "/g"}, {"officer", "/o"}, {"say", "/s"},
    {"yell", "/y"}, {"reply", "/r"}, {"whisper", "/w"},
}

function addon.route(text)
    text = text:match("^%s*(.-)%s*$")
    if text == "" or text:find("^/") then return nil end
    local lower = text:lower()
    for _, route in ipairs(channels) do
        local name, command = unpack(route)
        if lower == name then return nil end
        if lower:sub(1, #name) == name then
            local rest = text:sub(#name + 1)
            local message = rest:match("^[,:;%.!?%-]?%s+(.+)$")
            if message then
                if command == "/w" then
                    local recipient, body = message:match("^(%S+)%s+(.+)$")
                    if not recipient or body:sub(1, 1) == "/" then return nil end
                    recipient = recipient:gsub("[,.:;]+$", "")
                    if recipient == "" or recipient:find("[/%c|]") then return nil end
                    message = recipient .. " " .. body
                end
                if message:sub(1, 1) == "/" then return nil end
                return command .. " " .. message
            end
            if rest:match("^[,:;%.!?%-]+$") then return nil end
        end
    end
    return text
end
