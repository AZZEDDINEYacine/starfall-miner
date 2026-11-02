using Godot;
using System.Text.Json.Serialization;

namespace StarfallMiner;

public sealed class CraftedWeapon
{
    public int Id, Type, Level;
    public int RateLevel;
}

public sealed class MiningDrone
{
    public int Id, Type, Level, WeaponId;
    [JsonIgnore] public Vector2 Position;
    [JsonIgnore] public float Angle, ShotTimer;
    [JsonIgnore] public Vector2I Target = MiningPlanet.NoTarget;
    public bool Recalling = true;
    [JsonIgnore] public bool Docked = true;
    [JsonIgnore] public float RetargetTimer, TransferLeft;
}

/// <summary>Owned instances and unique mount assignments; transient flight state is never serialized.</summary>
public sealed class EquipmentState
{
    public long[] Materials = [0, 0, 0];
    public List<CraftedWeapon> Weapons = new();
    public List<MiningDrone> Drones = new();
    public int ShipWeaponId = 1, NextId = 2, HighestSector = 1;
    public int[] ShipAuxWeaponIds = [0, 0];
    public int HardpointLevel;
    public const int WeaponCapacity = 24, DroneCapacity = 3;
    public const long MaterialCapacity = 1_000_000_000_000;

    public static EquipmentState Fresh()
    {
        var state = new EquipmentState();
        state.Weapons.Add(new CraftedWeapon { Id = 1, Type = 0 });
        return state;
    }

    public static EquipmentState Legacy(int sector, int weapon)
    {
        var state = Fresh();
        state.Weapons.Add(new CraftedWeapon { Id = 2, Type = 1 });
        state.Weapons.Add(new CraftedWeapon { Id = 3, Type = 2 });
        state.ShipWeaponId = Math.Clamp(weapon, 0, 2) + 1;
        state.NextId = 4;
        state.HighestSector = Math.Max(1, sector);
        return state;
    }

    public CraftedWeapon? FindWeapon(int id)
    {
        foreach (var item in Weapons) if (item.Id == id) return item;
        return null;
    }
    public MiningDrone? FindDrone(int id)
    {
        foreach (var drone in Drones) if (drone.Id == id) return drone;
        return null;
    }
    public bool Mounted(int weaponId)
    {
        if (ShipMounted(weaponId)) return true;
        foreach (var drone in Drones) if (drone.WeaponId == weaponId) return true;
        return false;
    }
    public bool ShipMounted(int id) => id > 0 && (ShipWeaponId == id || ShipAuxWeaponIds[0] == id || ShipAuxWeaponIds[1] == id);
    public int MountedShipCount => 1 + (ShipAuxWeaponIds[0] > 0 ? 1 : 0) + (ShipAuxWeaponIds[1] > 0 ? 1 : 0);
    public void AddMaterial(int kind, long count)
    {
        if (kind >= 0 && kind < 3 && count > 0)
            Materials[kind] += Math.Min(count, MaterialCapacity - Materials[kind]);
    }

    public bool Valid()
    {
        if (Materials == null || Materials.Length != 3 || Materials.Any(n => n < 0 || n > MaterialCapacity) ||
            ShipAuxWeaponIds == null || ShipAuxWeaponIds.Length != 2 || HardpointLevel is < 0 or > 2 || Weapons == null || Drones == null || Weapons.Count < 1 || Weapons.Count > WeaponCapacity ||
            Drones.Count > DroneCapacity || HighestSector < 1 || HighestSector > 1_000_000 || NextId < 2 || NextId > 1_000_000) return false;
        var ids = new HashSet<int>();
        foreach (var weapon in Weapons)
            if (weapon == null || weapon.Id <= 0 || weapon.Id >= NextId || !ids.Add(weapon.Id) || weapon.Type < 0 || weapon.Type >= Main.Weapons.Length || weapon.Level is < 0 or > 50 || weapon.RateLevel is < 0 or > 50) return false;
        var mounts = new HashSet<int>();
        if (FindWeapon(ShipWeaponId) == null || !mounts.Add(ShipWeaponId)) return false;
        for (int i = 0; i < ShipAuxWeaponIds.Length; i++)
            if (ShipAuxWeaponIds[i] != 0 && (i >= HardpointLevel || FindWeapon(ShipAuxWeaponIds[i]) == null || !mounts.Add(ShipAuxWeaponIds[i]))) return false;
        foreach (var drone in Drones)
            if (drone == null || drone.Id <= 0 || drone.Id >= NextId || !ids.Add(drone.Id) || drone.Type is < 0 or > 2 || drone.Level is < 0 or > 50 ||
                drone.WeaponId != 0 && (FindWeapon(drone.WeaponId) == null || !mounts.Add(drone.WeaponId))) return false;
        return true;
    }
}
