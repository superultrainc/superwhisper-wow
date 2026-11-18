local path = ...
local chord = "ALT + Alt_R"

local function edge(name)
    return function()
        local file = io.open(path, "a")
        if file then
            file:write(name, "\n")
            file:close()
        end
    end
end

hl.unbind(chord)
o.bind_hold(chord, "Superwhisper WoW press", edge("press"),
    "Superwhisper WoW release", edge("release"), {ignore_mods = true})
