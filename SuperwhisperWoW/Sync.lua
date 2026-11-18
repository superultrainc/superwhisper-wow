local _, addon = ...
local recent = {}
local events = CreateFrame("Frame")
events:RegisterEvent("CHAT_MSG_WHISPER")
events:RegisterEvent("CHAT_MSG_WHISPER_INFORM")
events:RegisterEvent("PLAYER_LOGIN")
events:SetScript("OnEvent", function(_, event, _, name)
    if event == "PLAYER_LOGIN" then
        if C_GuildInfo and C_GuildInfo.GuildRoster then C_GuildInfo.GuildRoster()
        elseif GuildRoster then GuildRoster() end
        if C_FriendList and C_FriendList.ShowFriends then C_FriendList.ShowFriends() end
    elseif name then recent[name] = true end
end)

local function report(message)
    DEFAULT_CHAT_FRAME:AddMessage("Superwhisper: " .. message, 1, 1, 0)
end

local function sync()
    local names, counts = {}, {guild=0, friends=0, group=0, whispers=0}
    local function add(source, name)
        if type(name) ~= "string" or name == "" or names[name] then return end
        names[name] = true
        counts[source] = counts[source] + 1
    end
    for i = 1, GetNumGuildMembers() do add("guild", GetGuildRosterInfo(i)) end
    for i = 1, C_FriendList.GetNumFriends() do
        local friend = C_FriendList.GetFriendInfoByIndex(i)
        if friend then add("friends", friend.name) end
    end
    if BNGetNumFriends and C_BattleNet and C_BattleNet.GetFriendGameAccountInfo
            and C_BattleNet.GetFriendNumGameAccounts then
        for i = 1, BNGetNumFriends() do
            for j = 1, C_BattleNet.GetFriendNumGameAccounts(i) do
                local info = C_BattleNet.GetFriendGameAccountInfo(i, j)
                if info and info.clientProgram == "WoW" then add("friends", info.characterName) end
            end
        end
    end
    local raid = IsInRaid()
    for i = 1, raid and GetNumGroupMembers() or GetNumSubgroupMembers() do
        local name, realm = UnitName((raid and "raid" or "party") .. i)
        if name and realm and realm ~= "" then name = name .. "-" .. realm end
        add("group", name)
    end
    for name in pairs(recent) do add("whispers", name) end
    local list = {}
    for name in pairs(names) do table.insert(list, name) end
    table.sort(list)
    SuperwhisperWoWDB = {names=list}
    report(("found %d names (guild %d, friends %d, group %d, whispers %d)."):format(
        #list, counts.guild, counts.friends, counts.group, counts.whispers))
    report("reloading UI to save them…")
    ReloadUI()
end

SLASH_SUPERWHISPERWOW1 = "/sww"
SlashCmdList.SUPERWHISPERWOW = function(arg)
    if arg:lower():match("^%s*sync%s*$") then sync() return end
    report("hold your Superwhisper key to dictate.")
    report('say "party", "raid", "guild", "whisper Name"… first to pick a channel.')
    report("/sww sync – learn names from guild, friends and group.")
end
