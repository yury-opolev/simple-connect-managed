namespace SimpleConnect.Client.Services;

public static class RandomNameGenerator
{
    private static readonly Random random = new();

    private static readonly string[] adjectives =
    {
        "Happy", "Brave", "Calm", "Eager", "Fancy", "Gentle", "Jolly", "Kind", "Lively", "Merry",
        "Noble", "Proud", "Quick", "Royal", "Swift", "Warm", "Witty", "Bold", "Bright", "Clever",
        "Daring", "Fair", "Grand", "Humble", "Keen", "Lucky", "Mighty", "Neat", "Polite", "Sharp",
        "Sunny", "Tender", "Vivid", "Wise", "Agile", "Blissful", "Cheerful", "Dainty", "Earnest", "Fierce",
        "Graceful", "Hearty", "Joyful", "Kindly", "Light", "Modest", "Nimble", "Peaceful", "Radiant", "Serene",
        "Tranquil", "Upbeat", "Valiant", "Zesty", "Amber", "Azure", "Coral", "Crystal", "Emerald", "Golden",
        "Ivory", "Jade", "Marble", "Opal", "Pearl", "Ruby", "Silver", "Velvet", "Crimson", "Scarlet",
        "Cosmic", "Dreamy", "Frosty", "Gleaming", "Harmonic", "Infinite", "Lunar", "Mystic", "Stellar", "Tropical",
        "Arctic", "Autumn", "Breeze", "Cedar", "Dawn", "Floral", "Glacier", "Harbor", "Island", "Meadow",
        "Ocean", "Prairie", "River", "Spring", "Sunset", "Thunder", "Valley", "Willow", "Zenith", "Misty"
    };

    private static readonly string[] nouns =
    {
        "Panda", "Falcon", "Dolphin", "Tiger", "Eagle", "Fox", "Wolf", "Bear", "Hawk", "Owl",
        "Lion", "Raven", "Swan", "Crane", "Deer", "Hare", "Lynx", "Otter", "Seal", "Wren",
        "Badger", "Finch", "Heron", "Koala", "Lark", "Moose", "Osprey", "Parrot", "Robin", "Stork",
        "Turtle", "Whale", "Bison", "Cobra", "Drake", "Egret", "Gecko", "Ibis", "Kiwi", "Lemur",
        "Newt", "Oriole", "Puma", "Quail", "Skunk", "Viper", "Yak", "Zebra", "Alpaca", "Bobcat",
        "Canary", "Dingo", "Ferret", "Gopher", "Hyena", "Iguana", "Jackal", "Marten", "Ocelot", "Pelican",
        "Star", "Moon", "Cloud", "Storm", "Flame", "Wave", "Stone", "River", "Ridge", "Peak",
        "Forest", "Garden", "Harbor", "Island", "Lake", "Meadow", "Ocean", "Prairie", "Summit", "Valley",
        "Arrow", "Banner", "Bridge", "Castle", "Crown", "Forge", "Gate", "Haven", "Knight", "Lantern",
        "Mirror", "Oracle", "Prism", "Quest", "Sage", "Tower", "Vault", "Anchor", "Beacon", "Compass"
    };

    public static string Generate()
    {
        var adjective = adjectives[random.Next(adjectives.Length)];
        var noun = nouns[random.Next(nouns.Length)];
        return $"{adjective} {noun}";
    }
}
