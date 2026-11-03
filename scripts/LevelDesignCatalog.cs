namespace StarfallMiner;

/// <summary>One hundred authored planet concepts grouped into ten campaign universes.</summary>
public sealed record PlanetLevelDesign(string Name, int Difficulty, int RewardTier, string VisualBrief);
public sealed record UniverseLevelDesign(string Name, string Motif, PlanetLevelDesign[] Levels);

public static class LevelDesignCatalog
{
    public const int LevelsPerUniverse = 10;
    public const int UniverseCount = 10;
    public const int CampaignLength = LevelsPerUniverse * UniverseCount;
    public static string AtlasPathForUniverse(int universeIndex) =>
        $"res://assets/level_catalog/universe_{Math.Clamp(universeIndex, 0, UniverseCount - 1) + 1:00}.png";

    public static string AtlasPathForLevel(int level) => AtlasPathForUniverse((Math.Clamp(level, 1, CampaignLength) - 1) / LevelsPerUniverse);
    public static readonly UniverseLevelDesign[] Universes =
    [
        new("The Firstlight Reach","Coastal forests, cloud meadows, and luminous valleys",
        [
            new("Mosslight Cradle",1,1,"rainforest shelf, titan tree, lagoon; amber creek"),
            new("Cloudstep Vale",1,1,"cloud meadows, stone bridge; climb the lower ridge"),
            new("Whispering Shoal",1,1,"coral islands, deep basin; hop between shelves"),
            new("Lumen Orchard",2,2,"glowing fruit, crescent mountain; follow root vein"),
            new("Rainbell Canopy",2,2,"bell leaves, waterfall ravine; enter north cleft"),
            new("Amberroot Basin",2,2,"fossil roots, mineral spring; trace central root"),
            new("Skydrift Meadow",3,3,"floating grass islands; clear three shelves"),
            new("Dawnpetal World",3,3,"giant flower valley, sunrise crater; enter petal cleft"),
            new("Thornwake Grove",3,3,"old trunks, fern canyon; mine exposed canyon"),
            new("Everglen Heart",4,4,"forest waterfall, ancient arch; reach arch by west falls")
        ]),
        new("The Cinderbound March","Basalt ridges, lava channels, and volcanic ruins",
        [
            new("Ashen Ladder",1,1,"volcanic steps, smoking ridge; climb west seam"),
            new("Brassflow Crater",1,1,"brass caldera, lava streams; cross south lip"),
            new("Sootglass Divide",1,1,"obsidian split, twin cones; mine central divide"),
            new("Furnace Bloom",2,2,"lava flower, scorched petals; break petal bases"),
            new("Pyreback Range",2,2,"mountain chain, magma lake; use ridge saddle"),
            new("Emberfall Shelf",2,2,"lava waterfall, escarpment; descend from ledge"),
            new("Crown of Cinders",3,3,"volcanic spires; breach two weak notches"),
            new("Molten Reliquary",3,3,"ruined smelters, magma vault; clear towers"),
            new("Blackflare Basin",3,3,"dark bowl, flame faults; follow one fault"),
            new("Vulcan's Last Bell",4,4,"bell caldera, molten throat; enter west wall")
        ]),
        new("The Rimeglass Drift","Glacier gorges, auroras, and frozen observatories",
        [
            new("Frostwake Shelf",1,1,"glacier tongue, wind-carved snow; follow blue seam"),
            new("Pale Meridian",1,1,"diagonal ice canyon, serrated caps; use canyon"),
            new("Aurora Hollows",1,1,"twin hollows, aurora; connect thin ice bridge"),
            new("Shiverpine Crown",2,2,"frozen pines, crown; mine crevasse edge"),
            new("Mirrorfjord",2,2,"reflective fjord, ice harbor; follow fjord floor"),
            new("Blueglass Cathedral",2,2,"ice arches, frozen lake; break side supports"),
            new("Snowblind Steppe",3,3,"blizzard plain, sinkholes; use holes as islands"),
            new("Glacierwake Falls",3,3,"frozen waterfall, crevasse; mine ledges downward"),
            new("Nightfrost Observatory",3,3,"stranded observatory, glacier dome; open ice"),
            new("The Quiet Comet",4,4,"ice comet, crystal tail; clear tail before cap")
        ]),
        new("The Pelagic Choir","Ocean currents, coral shelves, and abyssal landmarks",
        [
            new("Pelagia Crown",2,2,"spiral current, islands, waterfall; reef shelf route"),
            new("Tideglass Atoll",2,2,"turquoise atoll, dark lagoon; rim then east inlet"),
            new("Whalefall Blue",2,2,"whalebone reef, currents; enter rib arch"),
            new("Coral Lanterns",3,3,"glowing coral towers; clear tower bases"),
            new("Stormsalt Reach",3,3,"storm eye, whitecaps; cross calm eye"),
            new("Pearlcurrent Maze",3,3,"braided currents, pearl caves; follow one channel"),
            new("Kelpstar Deep",4,4,"kelp forest, star trench; open trench arms"),
            new("Blueglass Archipelago",4,4,"five sea stacks; use central passage"),
            new("Abyssal Bell",4,4,"luminous bell, dark basin; break upper shell"),
            new("The Last Tide",5,5,"giant wave, reef citadel; breach after crest")
        ]),
        new("The Saffron Antiquity","Sand seas, lost observatories, and buried cities",
        [
            new("Saffron Reliquary",2,2,"desert observatory, oasis; follow river bend"),
            new("Dunewake Crown",2,2,"crescent dunes, basin; mine lee side"),
            new("Glasswind Tomb",2,2,"glass-sand tomb; open roof then descend"),
            new("Sunken Caravan",3,3,"trail, sandstone arches; trace between mesas"),
            new("Obelisk of Hours",3,3,"leaning clock obelisk; follow shadow axis"),
            new("Mirage Orchard",3,3,"hidden oasis, sandstone walls; tunnel narrow neck"),
            new("Copperglass Expanse",4,4,"copper sands, glass ridges; follow dark seam"),
            new("The Buried Choir",4,4,"seven buried towers; connect exposed foundations"),
            new("Redmoon Ziggurat",4,4,"stepped ziggurat; follow thin stairs"),
            new("Sundial at World's End",5,5,"sundial plaza, sun engine; break spokes")
        ]),
        new("The Luminous Menagerie","Bioluminescent jungles and alien wildlife",
        [
            new("Mushroom Moonlet",2,2,"giant mushrooms, luminous pond; traverse shelves"),
            new("Lanternwing Vale",2,2,"glowing moths, fern valley; open valley floor"),
            new("Sporefall Basin",2,2,"mushroom falls, glowing spores; mine root cleft"),
            new("Glassfrog Marsh",3,3,"clear pools, reeds, nest; enter soft bank"),
            new("Giant's Garden",3,3,"alien flowers, jungle shelf; follow stem corridor"),
            new("Mothlight Tangle",3,3,"canopy maze, sleeping beast; break upper route"),
            new("Velvetfang Orchard",4,4,"carnivorous blooms, orchard; use side lane"),
            new("The Singing Hive",4,4,"organic hive, glowing reeds; clear outer cells"),
            new("Mooncalf Sanctuary",4,4,"gentle mooncalf, enchanted trees; streambed route"),
            new("Elderbloom Wilds",5,5,"ancient flower forest; cut trunk scar")
        ]),
        new("The Clockwork Arc","Factories, railways, machine reefs, and automatons",
        [
            new("Copperwake Foundry",3,3,"coastal foundry, tidal wheel; conveyor trench"),
            new("Gearfall Station",3,3,"orbital station in rock; open docking bay"),
            new("Neon Switchyard",3,3,"rail yard, neon city; follow track bed"),
            new("Chrome Orchard",4,4,"mechanical trees, reservoir; clear roots"),
            new("The Brass Leviathan",4,4,"machine coil, refinery; mine exposed spine"),
            new("Violet Assembly",4,4,"robotic arms, casting pit; enter loading gates"),
            new("Hollow Megacity",5,5,"city canyon, transit core; descend avenue"),
            new("Clockwork Reef",5,5,"turbines, sea cliff; follow low pipe"),
            new("Prism Engine World",5,5,"crystal reactor, cooling towers; open two lanes"),
            new("Last Automaton",5,5,"robot guardian, furnace; enter shoulder hatch")
        ]),
        new("The Gloam Archive","Haunted keeps, crypts, and spectral archives",
        [
            new("Wicklight Hollow",3,3,"candle village, bare trees; lantern path"),
            new("Hushwood Manor",3,3,"crooked manor, whispering grove; open garden gate"),
            new("Bellgrave Crossing",3,3,"broken bell tower, grave hills; bridge route"),
            new("The Paper Crypt",4,4,"buried archive, page catacombs; loose-page entrance"),
            new("Ravenclock Keep",4,4,"raven clock keep, mist ridge; cracked clock face"),
            new("Mourning Orchard",4,4,"pale trees, ruined chapel; clear orchard rows"),
            new("Lanternless City",5,5,"abandoned roofs, glowing subway; track below"),
            new("The Unwritten Wing",5,5,"impossible library, central stair; mine landings"),
            new("Widow's Eclipse",5,5,"castle, shadow chasm; cross narrow bridge"),
            new("Nocturne Reliquary",5,5,"spectral cathedral, silent obelisks; underside route")
        ]),
        new("The Mythic Orrery","Floating islands, mythic beasts, and star relics",
        [
            new("Aetherstep Isles",3,3,"floating meadows, cloud whirlpool; clear small isles"),
            new("Prismroot Garden",3,3,"crystal roots, luminous spring; trace root channel"),
            new("Moonwell Spire",3,3,"leaning tower, twin-moon lake; open tower base"),
            new("Dragon's Teacup",4,4,"sleeping dragon, mountain bowl; avoid spine"),
            new("Starweaver Loom",4,4,"constellation threads, peaks; follow bright thread"),
            new("Cloudcastle Drift",4,4,"hanging castle, waterfalls; clear floating bridge"),
            new("The Singing Monolith",5,5,"rune stone, curved terraces; break terraces"),
            new("Phoenix Ember Nest",5,5,"phoenix nest, warm crater; enter feather gap"),
            new("Astral Menhir Field",5,5,"standing stones, star map; connect three bases"),
            new("Myth of the First Star",5,5,"star-beast, celestial seed; constellation route")
        ]),
        new("The Uncanny Frontier","Playful oddities with readable mining landmarks",
        [
            new("Potato Prime",4,4,"potato world, farmhouse, furrows; follow furrow"),
            new("Banana Split Reach",4,4,"peeled banana ridge, chocolate lagoon; peel route"),
            new("The Burger Moon",4,4,"burger mesa, cheese fall; descend lettuce gap"),
            new("Cluckstar Roost",5,5,"giant chicken, mineral eggs; feather route"),
            new("Upside-Down Aquarium",5,5,"fish islands, coral ceiling; follow school channel"),
            new("The Great Sock",5,5,"knitted sock, heel cavern; open heel patch"),
            new("Noodle Nebula Rock",5,5,"noodle canyon, warm spring; trace canyon"),
            new("Mildly Annoyed Titan",5,5,"grumpy face, twin caves; cheek to nose seam"),
            new("The Unreasonably Large Teacup",5,5,"cup basin, tea sea; mine handle joint"),
            new("Goose with a Planet",5,5,"cosmic goose, beak cave; wing gap then beak")
        ])
    ];

    public static (int Level, int UniverseIndex, UniverseLevelDesign Universe, PlanetLevelDesign Planet) ForLevel(int level)
    {
        if (level < 1 || level > CampaignLength) throw new ArgumentOutOfRangeException(nameof(level));
        int u=(level-1)/LevelsPerUniverse, p=(level-1)%LevelsPerUniverse;
        return (level,u,Universes[u],Universes[u].Levels[p]);
    }

    public static bool IsWellFormed()
    {
        if(Universes.Length!=UniverseCount)return false;
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var universe in Universes)
        {
            if(universe.Levels.Length!=LevelsPerUniverse)return false;
            foreach(var planet in universe.Levels)if(!names.Add(planet.Name))return false;
        }
        return names.Count==CampaignLength;
    }
}
