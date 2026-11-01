using Godot;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileAccess = Godot.FileAccess;

namespace StarfallMiner;

/// <summary>Explicit v1 wire format, shared with the original 0.1 and 0.2 game.</summary>
public sealed class SaveStore(string path)
{
    public string Path { get; } = path;
    public string? LastError { get; private set; }
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        IncludeFields = true
    };

    public bool Save(Main game)
    {
        try
        {
            var data = new SaveData
            {
                Version = 1, Credits = game.Credits, Flux = game.Flux, Sector = game.Sector,
                Weapon = game.Weapon, Levels = (int[])game.Levels.Clone(), Auto = game.AutoMode, Muted = game.Muted,
                VisualStyle = game.CurrentVisualStyle.Id,
                Equipment = game.Equipment,
                TotalEarned = game.TotalEarned, TotalMined = game.TotalMined, Rate = game.IncomeRate,
                Timestamp = Time.GetUnixTimeFromSystem(), TotalBlocks = game.Planet.TotalBlocks,
                Cleared = game.WarpTimer > 0, OverdriveCooldown = game.OverdriveCooldown,
                LifetimeEnergy = game.LifetimeEnergy, ShipX = game.ShipPosition.X, ShipY = game.ShipPosition.Y,
                Tiles = new(game.Planet.Blocks.Count), Pickups = new(game.Debris.Count)
            };
            foreach (var (key, block) in game.Planet.Blocks)
                data.Tiles.Add(new SavedTile { X = key.X, Y = key.Y, Hp = block.Hp, MaxHp = block.MaxHp, Kind = block.Kind, Shade = block.Shade, Value = block.Value,
                    Material = block.Material, MaterialAmount = block.MaterialAmount });
            foreach (var fragment in game.Debris)
                data.Pickups.Add(new SavedPickup
                {
                    X = fragment.Position.X, Y = fragment.Position.Y, Age = fragment.Age, Value = fragment.Value,
                    Energy = fragment.Energy, Kind = fragment.Kind, Size = fragment.Size, Material = fragment.Material, MaterialAmount = fragment.MaterialAmount
                });
            string json = JsonSerializer.Serialize(data, Options);
            string absolute = ProjectSettings.GlobalizePath(Path);
            DirAccess.MakeDirRecursiveAbsolute(absolute.GetBaseDir());
            using (var file = FileAccess.Open(Path + ".tmp", FileAccess.ModeFlags.Write))
            {
                if (file == null) throw new IOException($"Open failed: {FileAccess.GetOpenError()}");
                file.StoreString(json);
                file.Flush();
                if (file.GetError() != Error.Ok) throw new IOException($"Write failed: {file.GetError()}");
            }
            var error = DirAccess.RenameAbsolute(Path + ".tmp", Path);
            if (error != Error.Ok) throw new IOException($"Commit failed: {error}");
            LastError = null;
            return true;
        }
        catch (Exception error) when (error is IOException or JsonException or ArgumentException)
        {
            LastError = error.Message;
            GD.PushWarning($"Could not save progress: {LastError}");
            return false;
        }
    }

    public bool Load(Main game, double? now = null)
    {
        if (!FileAccess.FileExists(Path)) return false;
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(FileAccess.GetFileAsString(Path), Options);
            if (data == null || data.Version != 1 || data.Levels?.Length != 3 || data.Tiles == null ||
                data.Tiles.Count == 0 && !data.Cleared) throw new JsonException("Incomplete v1 save.");
            if (data.Equipment != null && !data.Equipment.Valid()) throw new JsonException("Invalid equipment or duplicate weapon mounts.");
            // Validate the entire payload before changing the live game or claiming catch-up.
            // Silently skipping a bad tile or pickup could destroy or duplicate its reward.
            var restored = new Dictionary<Vector2I, OreBlock>(data.Tiles.Count);
            foreach (var tile in data.Tiles)
            {
                if (tile == null || Math.Abs((long)tile.X) > MiningPlanet.MaxGridExtent || Math.Abs((long)tile.Y) > MiningPlanet.MaxGridExtent ||
                    !Finite(tile.Hp, tile.MaxHp, tile.Value) || tile.Hp <= 0 || tile.MaxHp <= 0 || tile.Value < 0 ||
                    !ValidMaterial(tile.Material, tile.MaterialAmount) || !restored.TryAdd(new Vector2I(tile.X, tile.Y), new OreBlock
                    {
                        Hp = tile.Hp, MaxHp = tile.MaxHp, Kind = Math.Clamp(tile.Kind, 0, 4),
                        Shade = Math.Clamp(tile.Shade, 0, 2), Value = tile.Value, Material = tile.Material, MaterialAmount = tile.MaterialAmount
                    })) throw new JsonException("Invalid or duplicate saved tile.");
            }
            var pickups = new List<DebrisFragment>(data.Pickups?.Count ?? 0);
            if (data.Pickups != null)
            foreach (var pickup in data.Pickups)
            {
                if (pickup == null || !Finite(pickup.X, pickup.Y, pickup.Age, pickup.Value, pickup.Size) || pickup.Value < 0 || pickup.Energy < 0 || !ValidMaterial(pickup.Material, pickup.MaterialAmount))
                    throw new JsonException("Invalid pending reward.");
                pickups.Add(new DebrisFragment
                {
                    Position = new Vector2(pickup.X, pickup.Y), Velocity = Vector2.Zero,
                    Age = Math.Clamp(pickup.Age, 0, 2), Value = pickup.Value, Energy = pickup.Energy,
                    Kind = Math.Clamp(pickup.Kind, 0, 4), Size = Math.Clamp(pickup.Size, 1, 4), Material = pickup.Material, MaterialAmount = pickup.MaterialAmount
                });
            }
            if (!Finite(data.Credits, data.TotalEarned, data.Rate, data.OverdriveCooldown) ||
                data.Timestamp is { } timestamp && !double.IsFinite(timestamp) ||
                data.ShipX is { } sx && !float.IsFinite(sx) || data.ShipY is { } sy && !float.IsFinite(sy))
                throw new JsonException("Invalid saved number.");
            game.ReleaseControls(); // Destinations are transient, including when reloading in place.
            game.FirstRun = false;
            game.Credits = Math.Max(0, data.Credits);
            game.Flux = Math.Max(0, data.Flux);
            game.Sector = Math.Clamp(data.Sector, 1, 1000000);
            game.Weapon = Math.Clamp(data.Weapon, 0, Main.Weapons.Length - 1);
            game.Equipment = data.Equipment ?? EquipmentState.Legacy(game.Sector, game.Weapon);
            game.Weapon = game.Equipment.FindWeapon(game.Equipment.ShipWeaponId)!.Type;
            for (int i = 0; i < 3; i++) game.Levels[i] = Math.Clamp(data.Levels[i], 0, 500);
            game.AutoMode = data.Auto;
            game.RestoreVisualStyle(data.VisualStyle);
            game.Muted = data.Muted;
            game.Sound.Enabled = !game.Muted;
            game.TotalEarned = Math.Max(0, data.TotalEarned);
            game.TotalMined = Math.Max(0, data.TotalMined);
            game.LifetimeEnergy = Math.Max(0, data.LifetimeEnergy);
            game.ShipPosition = new Vector2(data.ShipX ?? Main.Center.X - 155, data.ShipY ?? Main.Center.Y - 150);
            game.Debris.Clear();
            game.Debris.AddRange(pickups);
            game.Planet.Generate(game.Sector);
            if (data.Cleared)
            {
                game.Planet.Generate(++game.Sector);
                game.PlaceShipAtOrbit();
            }
            else
            {
                game.Planet.Blocks.Clear();
                foreach (var pair in restored) game.Planet.Blocks.Add(pair.Key, pair.Value);
                game.Planet.TotalBlocks = Math.Max(restored.Count, data.TotalBlocks ?? game.Planet.TotalBlocks);
                game.Planet.NotifyTilesChanged();
            }
            game.WarpTimer = 0;
            game.Equipment.HighestSector = Math.Max(game.Equipment.HighestSector, game.Sector);
            foreach (var drone in game.Equipment.Drones) { drone.Position = game.ShipPosition; drone.Docked = drone.Recalling; }
            game.Waves.Clear();
            game.Projectiles.Clear();
            game.AimKey = game.TargetKey = MiningPlanet.NoTarget;
            double current = now ?? Time.GetUnixTimeFromSystem();
            double away = Math.Clamp(current - (data.Timestamp ?? current), 0, 4 * 3600);
            game.OverdriveCooldown = (float)Math.Max(0, data.OverdriveCooldown - away);
            game.IncomeRate = Math.Max(0, data.Rate);
            if (away > 45 && game.AutoMode && game.IncomeRate > 0)
            {
                double offline = Math.Floor(away * game.IncomeRate * 0.25);
                game.Credits += offline;
                game.TotalEarned += offline;
                game.Toast("WELCOME BACK", $"Autopilot earned {Main.Compact(offline)} credits while away.", game.Palette.WeaponLance, 5);
            }
            LastError = null;
            // Mark catch-up claimed immediately. In tests this is always an isolated file.
            return Save(game);
        }
        catch (Exception error) when (error is JsonException or IOException or OverflowException)
        {
            LastError = error.Message;
            GD.PushWarning($"Save left untouched: {LastError}");
            return false;
        }
    }

    private static bool Finite(params double[] values) => Array.TrueForAll(values, double.IsFinite);
    private static bool ValidMaterial(int kind, long count) => kind is >= -1 and <= 2 && count >= 0 && count <= EquipmentState.MaterialCapacity && (kind == -1 ? count == 0 : count > 0);

    // Names are intentionally frozen to the GDScript v1 schema. All optional 0.2
    // fields have defaults, so original 0.1 saves remain valid without conversion.
    public sealed class SaveData
    {
        public int Version;
        public double Credits = 80;
        public long Flux;
        public int Sector = 1, Weapon;
        public int[]? Levels;
        public bool Auto = true, Muted;
        public string? VisualStyle;
        public EquipmentState? Equipment;
        public double TotalEarned, Rate;
        public long TotalMined, LifetimeEnergy;
        public double? Timestamp;
        public List<SavedTile>? Tiles;
        public int? TotalBlocks;
        public bool Cleared;
        public float OverdriveCooldown;
        public List<SavedPickup>? Pickups;
        public float? ShipX, ShipY;
    }

    public sealed class SavedTile
    {
        public int Material = -1;
        public long MaterialAmount;
        [JsonRequired] public int X { get; set; }
        [JsonRequired] public int Y { get; set; }
        [JsonRequired] public double Hp { get; set; }
        [JsonRequired] public double MaxHp { get; set; }
        [JsonRequired] public int Kind { get; set; }
        [JsonRequired] public int Shade { get; set; }
        [JsonRequired] public double Value { get; set; }
    }

    public sealed class SavedPickup
    {
        public int Material = -1;
        public long MaterialAmount;
        [JsonRequired] public float X { get; set; }
        [JsonRequired] public float Y { get; set; }
        [JsonRequired] public double Value { get; set; }
        [JsonRequired] public long Energy { get; set; }
        [JsonRequired] public int Kind { get; set; }
        public float Age { get; set; }
        public float Size { get; set; } = 2;
    }
}
