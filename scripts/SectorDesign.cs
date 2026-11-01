using Godot;

namespace StarfallMiner;

public enum PlanetLayout { Garden, Craters, Fissures, Vault, Canyon, Islands, Bands, Rings, Diagonal, IceVault, Obsidian, Aurelia, Foundry }

/// <summary>Hand-authored campaign identity; block placement remains seeded and endless.</summary>
public sealed record SectorProfile(
    string Name, string Region, string Lesson, PlanetLayout Layout,
    float Radius, int Difficulty, Color StoneColor, bool Rare = false, int WeaponReward = -1);

public readonly record struct SectorReward(double Credits, long Iron, long Energy, int WeaponIndex);

public static class SectorDesign
{
    private static readonly SectorProfile[] Campaign =
    [
        new("VERDANT ECHO", "EARTH", "Open a gold seam and learn the mining loop.", PlanetLayout.Garden, 16.1f, 1, new Color("486b61")),
        new("PALE SCAR", "MOON", "Crater walls reveal new firing angles.", PlanetLayout.Craters, 16.4f, 1, new Color("6a7182")),
        new("CINDERWAKE", "MERCURY", "Pierce three softer seams through a hard shell.", PlanetLayout.Fissures, 16.8f, 2, new Color("8d6550")),
        new("GILDED VEIL", "VENUS", "Release separated volatile pockets.", PlanetLayout.Vault, 17.1f, 2, new Color("8b7953")),
        new("RUSTFALL RIFT", "MARS", "Follow an ore-rich canyon to the inner vault.", PlanetLayout.Canyon, 17.5f, 3, new Color("96594f"), WeaponReward: 1),
        new("SHARD CHOIR", "ASTEROID BELT", "Clear five separated mining islands.", PlanetLayout.Islands, 18.0f, 3, new Color("756c61")),
        new("TEMPEST CROWN", "JUPITER", "Cut through alternating hard and rich bands.", PlanetLayout.Bands, 18.4f, 4, new Color("957b6b")),
        new("HALO REQUIEM", "SATURN", "Mine the ring and the protected world inside it.", PlanetLayout.Rings, 18.8f, 4, new Color("9c8768")),
        new("AXIAL GHOST", "URANUS", "Line up shots with diagonal energy veins.", PlanetLayout.Diagonal, 19.0f, 4, new Color("5a8b91"), WeaponReward: 2),
        new("ABYSSAL BLUE", "NEPTUNE", "Choose one of four routes through a dense shell.", PlanetLayout.IceVault, 19.2f, 5, new Color("506a9c")),
        new("NOX SERAPH", "RARE WORLD", "Broken obsidian arms hide volatile clusters.", PlanetLayout.Obsidian, 19.4f, 5, new Color("665a84"), Rare: true),
        new("AURELIA-IX", "RARE WORLD", "A golden outer lattice protects a second shell.", PlanetLayout.Aurelia, 19.7f, 5, new Color("a18a63"), Rare: true),
        new("HELIOS FOUNDRY", "SOLAR FORGE", "Use piercing, blast and chain damage together.", PlanetLayout.Foundry, 20.0f, 5, new Color("a06d50"), Rare: true, WeaponReward: 0)
    ];

    public static int CampaignLength => Campaign.Length;

    public static SectorProfile ForSector(int sector)
    {
        if (sector < 1) sector = 1;
        if (sector <= Campaign.Length) return Campaign[sector - 1];
        // Endless expeditions reuse the rare layouts with a new seed and a readable name.
        bool aurelia = sector % 2 == 0;
        return new SectorProfile(
            (aurelia ? "AURELIA" : "NOX") + " / EXPEDITION " + (sector - Campaign.Length),
            "DEEP SPACE", "The ore grows tougher; the reward keeps pace.",
            aurelia ? PlanetLayout.Aurelia : PlanetLayout.Obsidian,
            20.0f, 5, aurelia ? new Color("a18a63") : new Color("665a84"), true);
    }

    public static SectorReward RewardFor(int sector)
    {
        var profile = ForSector(sector);
        double growth = Math.Pow(1.27, Math.Min(sector - 1, 500));
        double rarity = profile.Rare ? (profile.Layout == PlanetLayout.Foundry ? 1.55 : 1.35) : 1;
        double credits = Math.Floor(150 * growth * (1 + (profile.Difficulty - 1) * 0.25) * rarity);
        long iron = profile.Difficulty * (profile.Difficulty + 1) / 2 + sector / 3 + (profile.Rare ? 10 : 0);
        long energy = 5 + profile.Difficulty * 2;
        return new SectorReward(credits, iron, energy, profile.WeaponReward);
    }
}
