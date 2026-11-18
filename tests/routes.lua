local addon = {}
assert(loadfile("SuperwhisperWoW/Routes.lua"))("SuperwhisperWoW", addon)
for _, case in ipairs({
    {"Raid warning: Stack", "/rw Stack"}, {"Party, pull", "/p pull"},
    {"raid now", "/raid now"}, {"guild hi", "/g hi"}, {"officer hi", "/o hi"},
    {"say hi", "/s hi"}, {"yell hi", "/y hi"}, {"reply yes", "/r yes"},
    {"Whisper Thrall-Realm, got it", "/w Thrall-Realm got it"},
    {"partygoers welcome", "partygoers welcome"}, {"no prefix", "no prefix"},
    {"party /cast Hearthstone"}, {"whisper Thrall /reload"}, {"guild."}, {"whisper Thrall"}, {"/cast fireball"}, {""}
}) do
    assert(addon.route(case[1]) == case[2], case[1])
end
