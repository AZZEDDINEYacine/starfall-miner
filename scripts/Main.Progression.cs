using Godot;

namespace StarfallMiner;

public partial class Main
{
    [Export] public CraftingCatalog Catalog { get; set; } = null!;
    [Export] public ProgressionTuning Progression { get; set; } = null!;
    public EquipmentState Equipment { get; set; } = EquipmentState.Fresh();
    public string EquipmentMessage { get; private set; } = "Mine deep deposits or buy materials at the market.";
    public int EngineeringRank
    {
        get
        {
            int rank = 1;
            for (int i = 0; i < Catalog.RankSectors.Length; i++)
                if (Equipment.HighestSector >= Catalog.RankSectors[i] || i < Catalog.RankMiningMilestones.Length && TotalMined >= Catalog.RankMiningMilestones[i]) rank = i + 1;
            return rank;
        }
    }
    public int DroneBays => Math.Min(EquipmentState.DroneCapacity, EngineeringRank);
    public int EquipmentLevelCap => Math.Min(50, EngineeringRank * 5);
    public string NextObjective
    {
        get
        {
            if (Equipment.Weapons.Count == 1 && Equipment.Weapons[0].Level == 0) return $"First upgrade: Power costs {Compact(WeaponUpgradeCost(Equipment.Weapons[0]))} CR in Fleet → Weapons.";
            if (Equipment.Weapons.Count == 1 && Equipment.Weapons[0].RateLevel == 0) return $"Add Fire Rate for {Compact(FireRateUpgradeCost(Equipment.Weapons[0]))} CR, then save for a Nova cannon.";
            if (Equipment.Weapons.Count == 1) return "Craft a Nova cannon: find Titanium in the mantle, or buy it at the market.";
            if (Equipment.Drones.Count == 0) return "Craft a Scout drone, then fit a spare weapon in Fleet.";
            bool armed = false;
            foreach (var drone in Equipment.Drones) armed |= drone.WeaponId != 0;
            if (!armed) return "Arm your drone with a reserve weapon in Fleet.";
            bool deployed = false;
            foreach (var drone in Equipment.Drones) deployed |= !drone.Docked && !drone.Recalling;
            if (!deployed) return "Deploy an armed drone from Fleet. Recall it for upgrades and refitting.";
            if (Equipment.HardpointLevel < Math.Min(2, EngineeringRank - 1)) return "Install another mothership hardpoint in Ship, then fit a reserve weapon beside your primary.";
            if (EngineeringRank == 1) return $"Mine {Math.Max(0, Catalog.RankMiningMilestones[1] - TotalMined)} more rocks or reach sector 2: unlock a second ship mount, Ion and Nova wave.";
            if (EngineeringRank == 2) return $"Mine {Math.Max(0, Catalog.RankMiningMilestones[2] - TotalMined)} more rocks or reach sector 4 for the third ship mount and advanced blueprints.";
            if (EngineeringRank == 3) return $"Mine {Math.Max(0, Catalog.RankMiningMilestones[3] - TotalMined)} more rocks or reach sector 7 for rank 4.";
            return "Upgrade your fleet and push into richer sectors. Deep deposits fund new builds.";
        }
    }

    private bool EquipmentResult(bool success, string message)
    {
        EquipmentMessage = message;
        if (success) { Hud.RefreshInventory(); Sound.Play("upgrade"); SaveProgress(); }
        else Hud.Refresh();
        return success;
    }

    public string RecipeCost(EquipmentBlueprint recipe) => $"{Compact(recipe.Credits)} CR · {recipe.Titanium} Ti · {recipe.Iridium} Ir · {recipe.Aether} Aether";

    public bool Craft(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= Catalog.Recipes.Count) return false;
        var recipe = Catalog.Recipes[recipeIndex];
        if (recipe.Type < 0 || recipe.Type > (recipe.Drone ? 2 : Weapons.Length - 1) || recipe.Rank < 1 || !double.IsFinite(recipe.Credits) || recipe.Credits < 0 ||
            recipe.Titanium < 0 || recipe.Iridium < 0 || recipe.Aether < 0)
            return EquipmentResult(false, "This recipe is unavailable.");
        if (EngineeringRank < recipe.Rank) return EquipmentResult(false, $"Requires engineering rank {recipe.Rank}.");
        if (recipe.Drone ? Equipment.Drones.Count >= DroneBays : Equipment.Weapons.Count >= EquipmentState.WeaponCapacity)
            return EquipmentResult(false, recipe.Drone ? "All drone bays are occupied. Clear more sectors to unlock bays." : "Weapon storage is full.");
        if (Credits < recipe.Credits || Equipment.Materials[0] < recipe.Titanium || Equipment.Materials[1] < recipe.Iridium || Equipment.Materials[2] < recipe.Aether)
            return EquipmentResult(false, "Not enough credits or materials. Mine deeper or visit the market.");
        Credits -= recipe.Credits;
        Equipment.Materials[0] -= recipe.Titanium; Equipment.Materials[1] -= recipe.Iridium; Equipment.Materials[2] -= recipe.Aether;
        int id = Equipment.NextId++;
        if (recipe.Drone)
        {
            Equipment.Drones.Add(new MiningDrone { Id = id, Type = recipe.Type, Position = ShipPosition, Docked = true, Recalling = true });
            return EquipmentResult(true, $"{recipe.Title} built. Fit a spare weapon in Fleet, then deploy it.");
        }
        Equipment.Weapons.Add(new CraftedWeapon { Id = id, Type = recipe.Type });
        return EquipmentResult(true, $"{recipe.Title} crafted. Equip it on the ship or a drone in Fleet.");
    }

    public bool TradeMaterial(int kind, int quantity, bool buy)
    {
        if (kind < 0 || kind > 2 || quantity is < 1 or > 5) return false;
        if (Catalog.MarketPrices[kind] <= 0) return EquipmentResult(false, "This market listing is unavailable.");
        double cost = (double)Catalog.MarketPrices[kind] * quantity;
        if (buy)
        {
            if (Credits < cost || Equipment.Materials[kind] > EquipmentState.MaterialCapacity - quantity)
                return EquipmentResult(false, "Not enough credits, or material storage is full.");
            Credits -= cost;
            Equipment.Materials[kind] += quantity;
        }
        else
        {
            if (Equipment.Materials[kind] < quantity) return EquipmentResult(false, "Not enough material to sell.");
            Equipment.Materials[kind] -= quantity;
            cost = Math.Floor(cost * 0.35);
            Credits += cost; TotalEarned += cost; // Market sales do not inflate offline mining income.
        }
        return EquipmentResult(true, $"{(buy ? "Bought" : "Sold")} {quantity} {Catalog.MaterialNames[kind]} for {Compact(cost)} CR.");
    }

    public double WeaponUpgradeCost(CraftedWeapon item) => Progression.Cost(item.Level, item.Type, Progression.EarlyPowerCosts, Progression.LaterPowerBase);
    public double FireRateUpgradeCost(CraftedWeapon item) => Progression.Cost(item.RateLevel, item.Type, Progression.EarlyRateCosts, Progression.LaterRateBase);
    public bool UpgradeFireRate(int id)
    {
        var item = Equipment.FindWeapon(id);
        if (item == null) return false;
        if (!WeaponServiceable(id)) return EquipmentResult(false, "Recall the drone and wait for docking before tuning its weapon.");
        if (item.RateLevel >= EquipmentLevelCap) return EquipmentResult(false, "Fire-rate rank limit reached. Clear more sectors.");
        double cost = FireRateUpgradeCost(item);
        if (Credits < cost) return EquipmentResult(false, "Not enough credits for this fire-rate upgrade.");
        Credits -= cost;
        item.RateLevel++;
        return EquipmentResult(true, $"{Main.Weapons[item.Type]} fire rate LV {item.RateLevel}. +{Math.Round((FireRateBonus(item.RateLevel) - 1) * 100)}% cadence from rate tuning.");
    }
    public double DroneUpgradeCost(MiningDrone drone) => Progression.Cost(drone.Level, drone.Type, Progression.EarlyDroneCosts, Progression.LaterDroneBase);
    public bool UpgradeEquipment(int id, bool drone)
    {
        var unit = drone ? Equipment.FindDrone(id) : null;
        var item = drone ? null : Equipment.FindWeapon(id);
        if (unit == null && item == null) return false;
        if (unit != null && !unit.Docked || item != null && !WeaponServiceable(item.Id))
            return EquipmentResult(false, "Recall the drone and wait for docking before servicing its chassis or weapon.");
        int level = unit?.Level ?? item!.Level;
        if (level >= EquipmentLevelCap) return EquipmentResult(false, "Rank upgrade limit reached. Clear more sectors.");
        double cost = unit != null ? DroneUpgradeCost(unit) : WeaponUpgradeCost(item!);
        if (Credits < cost) return EquipmentResult(false, "Not enough credits for this upgrade.");
        Credits -= cost;
        if (unit != null) unit.Level++; else item!.Level++;
        return EquipmentResult(true, unit != null ?
            $"{Catalog.Chassis(unit.Type).Title} upgraded to LV {unit.Level}. Mount power and firing rate improved." :
            $"{Main.Weapons[item!.Type]} Power LV {item.Level}: +{Math.Round((WeaponPower(item.Level) - 1) * 100)}% damage.");
    }

    public bool EquipShip(int weaponId)
    {
        var item = Equipment.FindWeapon(weaponId);
        if (item == null) return false;
        if (!WeaponServiceable(weaponId)) return EquipmentResult(false, "Recall this drone before removing its weapon.");
        foreach (var drone in Equipment.Drones) if (drone.WeaponId == weaponId) drone.WeaponId = 0;
        for (int i = 0; i < 2; i++) if (Equipment.ShipAuxWeaponIds[i] == weaponId) Equipment.ShipAuxWeaponIds[i] = Equipment.ShipWeaponId;
        Equipment.ShipWeaponId = weaponId;
        Weapon = item.Type;
        ShotTimer = Math.Min(ShotTimer, 0.1f);
        return EquipmentResult(true, $"{Main.Weapons[item.Type]} equipped on the ship.");
    }

    public bool EquipDrone(int droneId, int weaponId)
    {
        var drone = Equipment.FindDrone(droneId);
        if (drone == null) return false;
        if (weaponId != 0 && (Equipment.FindWeapon(weaponId) == null || Equipment.ShipMounted(weaponId)))
            return EquipmentResult(false, "Keep a weapon on the ship. Craft or select a spare for the drone.");
        if (!drone.Docked || weaponId != 0 && !WeaponServiceable(weaponId))
            return EquipmentResult(false, "Recall the drone and wait for docking before changing its weapon.");
        foreach (var other in Equipment.Drones) if (other.WeaponId == weaponId && other.Id != droneId) other.WeaponId = 0;
        drone.WeaponId = weaponId;
        drone.ShotTimer = 0.1f;
        return EquipmentResult(true, weaponId == 0 ? "Drone disarmed; its weapon returned to reserve." : "Drone armed. Deploy it from Fleet to start independent mining.");
    }

    public void CycleDroneWeapon(int droneId)
    {
        var drone = Equipment.FindDrone(droneId);
        if (drone == null) return;
        int current = -1;
        for (int i = 0; i < Equipment.Weapons.Count; i++) if (Equipment.Weapons[i].Id == drone.WeaponId) current = i;
        for (int offset = 1; offset <= Equipment.Weapons.Count; offset++)
        {
            var item = Equipment.Weapons[(current + offset) % Equipment.Weapons.Count];
            if (!Equipment.Mounted(item.Id)) { EquipDrone(droneId, item.Id); return; }
        }
        if (drone.WeaponId != 0) EquipDrone(droneId, 0);
        else EquipmentResult(false, "No reserve weapons. Craft one or move a ship weapon into reserve first.");
    }

    private int ShipWeaponLevel => Equipment.FindWeapon(Equipment.ShipWeaponId) is { } item && item.Type == Weapon ? item.Level : 0;
    private int ShipRateLevel => Equipment.FindWeapon(Equipment.ShipWeaponId) is { } item && item.Type == Weapon ? item.RateLevel : 0;
    public bool HasShipWeaponType(int type)
    {
        foreach (var item in Equipment.Weapons)
            if (item.Type == type && (Equipment.ShipMounted(item.Id) || !Equipment.Mounted(item.Id))) return true;
        return false;
    }

    public int ShipHardpoints => 1 + Equipment.HardpointLevel;
    public double HardpointCost => Equipment.HardpointLevel == 0 ? Progression.SecondHardpointCost : Progression.ThirdHardpointCost;
    public bool BuyHardpoint()
    {
        if (Equipment.HardpointLevel >= 2) return EquipmentResult(false, "All three mothership hardpoints installed.");
        if (EngineeringRank < Equipment.HardpointLevel + 2) return EquipmentResult(false, "Reach the next engineering rank to install this hardpoint.");
        if (!double.IsFinite(HardpointCost) || HardpointCost < 0) return EquipmentResult(false, "This hardpoint expansion is unavailable.");
        if (Credits < HardpointCost) return EquipmentResult(false, "Not enough credits for a hardpoint expansion.");
        Credits -= HardpointCost; Equipment.HardpointLevel++;
        return EquipmentResult(true, "Hardpoint installed. Fit a reserve weapon beside your primary in Fleet.");
    }
    public bool ToggleShipMount(int id)
    {
        if (Equipment.FindWeapon(id) == null) return false;
        if (Equipment.ShipWeaponId == id) return EquipmentResult(false, "The primary weapon stays fitted. Select another primary to replace it.");
        for (int i = 0; i < 2; i++)
            if (Equipment.ShipAuxWeaponIds[i] == id) { Equipment.ShipAuxWeaponIds[i] = 0; return EquipmentResult(true, "Auxiliary weapon returned to reserve."); }
        if (ShipHardpoints == 1) return EquipShip(id);
        if (!WeaponServiceable(id)) return EquipmentResult(false, "Recall the drone before transferring this weapon.");
        for (int i = 0; i < Equipment.HardpointLevel; i++)
        {
            if (Equipment.ShipAuxWeaponIds[i] != 0) continue;
            foreach (var drone in Equipment.Drones) if (drone.WeaponId == id) drone.WeaponId = 0;
            Equipment.ShipAuxWeaponIds[i] = id; _auxTimers[i] = 0.2f;
            return EquipmentResult(true, "Auxiliary weapon fitted. All ship mounts fire together.");
        }
        return EquipmentResult(false, "All ship mounts occupied. Unfit an auxiliary weapon first.");
    }
    private bool WeaponServiceable(int id)
    {
        foreach (var drone in Equipment.Drones) if (drone.WeaponId == id && !drone.Docked) return false;
        return true;
    }
    public bool RecallDrone(int id)
    {
        var drone = Equipment.FindDrone(id);
        if (drone == null) return false;
        drone.Recalling = true;
        return EquipmentResult(true, drone.Docked ? "Drone is docked and ready for service." : "Recall transmitted. Drone returning to the mothership.");
    }
    public bool DeployDrone(int id)
    {
        var drone = Equipment.FindDrone(id);
        if (drone == null) return false;
        if (drone.WeaponId == 0) return EquipmentResult(false, "Fit a weapon before deploying this drone.");
        drone.Recalling = drone.Docked = false; drone.Target = MiningPlanet.NoTarget; drone.TransferLeft = 0;
        return EquipmentResult(true, "Drone deployed on an independent mining route.");
    }
    private bool _serviceDockChanged;
    public void RecallFleet()
    {
        foreach (var drone in Equipment.Drones) drone.Recalling = true;
        EquipmentMessage = "Fleet recall transmitted. Service becomes available after docking.";
        Hud.RefreshInventory();
    }
    public double WeaponPower(int level) => 1 + Math.Clamp(level, 0, 50) * Progression.PowerGainPerLevel;
    public static float WeaponRate(int level) => 1 + Math.Min(level, 50) * 0.012f;
    public static float FireRateBonus(int level) => 1 + Math.Min(level, 3) * 0.10f + Math.Max(0, Math.Min(level, 50) - 3) * 0.02f;
    private readonly float[] _auxTimers = new float[2];
    private void FireAuxiliaries(float delta)
    {
        for (int i = 0; i < 2; i++)
        {
            _auxTimers[i] -= delta;
            var item = Equipment.FindWeapon(Equipment.ShipAuxWeaponIds[i]);
            if (item?.Type == ContinuousLaser)
            {
                var aim = AutoMode ? Vector2.FromAngle(ShipAngle) : (Navigation.Destination - ShipPosition).Normalized();
                UpdateShipLaser(i + 1, item, ShipPosition + new Vector2(-aim.Y, aim.X) * (i == 0 ? -10 : 10), delta);
                continue;
            }
            if (item == null || _auxTimers[i] > 0 || WarpTimer > 0 || Hud.BlocksWorldInput || (AutoMode ? !Autopilot.CanFire : !Navigation.ClearingPath)) continue;
            var key = TargetKey;
            if (!Planet.Blocks.ContainsKey(key)) continue;
            var target = Center + MiningPlanet.KeyPoint(key);
            if (ShipPosition.DistanceTo(target) > WeaponRange) continue;
            _auxTimers[i] = WeaponCooldown(item.Type, item.Level, item.RateLevel) / (AutoMode ? 1 : ManualFireRateMultiplier);
            var direction = (target - ShipPosition).Normalized();
            EmitWeapon(item.Type, ShipPosition + new Vector2(-direction.Y, direction.X) * (i == 0 ? -10 : 10), target, key, direction, ShipDamageMultiplier * WeaponPower(item.Level));
        }
    }
    // Independent drones retain their outside transfer routes. The mothership's
    // local-perception pilot never consults this planet-wide routing helper.
    private Vector2 CruiseDrone(Vector2 position, float delta)
    {
        var radial = (position - Center).Normalized();
        if (radial.LengthSquared() < 0.1f) radial = Vector2.Up;
        float clearance = Planet.SurfaceRadius(radial.Angle()) + 65;
        if (position.DistanceTo(Center) < clearance - 2)
        {
            var climb = position.MoveToward(Center + radial * clearance, AutopilotSteering.MaxSpeed * delta);
            return !Planet.ContainsPoint(climb - Center, 8) ? climb : position;
        }
        float angle = radial.Angle() + 0.23f;
        var destination = Center + Vector2.FromAngle(angle) * (Planet.SurfaceRadius(angle) + 65);
        var next = position.MoveToward(destination, AutopilotSteering.MaxSpeed * delta);
        if (!Planet.ContainsPoint(next - Center, 8)) return next;
        next = position + radial * AutopilotSteering.MaxSpeed * delta;
        return !Planet.ContainsPoint(next - Center, 8) ? next : position;
    }

    private void UpdateDrones(float delta, bool serviceOnly = false)
    {
        _serviceDockChanged = false;
        for (int i = 0; i < Equipment.Drones.Count; i++)
        {
            var drone = Equipment.Drones[i];
            if (drone.Recalling)
            {
                // Recall uses a high orbital transfer so it cannot snag on a mined tunnel.
                // Drones have no collision damage; returning craft phase through rock without mining it.
                drone.Position = drone.Position.MoveToward(ShipPosition, (210 + drone.Level * 2) * delta);
                drone.Angle = (ShipPosition - drone.Position).Angle();
                if (drone.Position.DistanceTo(ShipPosition) < 12)
                {
                    if (!drone.Docked) { _serviceDockChanged = true; EquipmentMessage = "Drone docked. Chassis and weapon service are available."; }
                    drone.Docked = true; drone.Position = ShipPosition;
                }
                continue;
            }
            if (drone.Docked) { drone.Position = ShipPosition; continue; }
            if (serviceOnly || WarpTimer > 0) continue;
            var item = Equipment.FindWeapon(drone.WeaponId);
            if (item == null) { drone.Recalling = true; continue; }
            drone.RetargetTimer -= delta;
            if (!Planet.Blocks.ContainsKey(drone.Target) || drone.RetargetTimer <= 0)
            {
                drone.RetargetTimer = 0.8f;
                // Spread the fleet around the planet instead of forming a ship escort.
                float orbitAngle = (i + 1) * Mathf.Tau / (Equipment.Drones.Count + 1) + OrbitAngle;
                var anchor = Vector2.FromAngle(orbitAngle) * (Planet.SurfaceRadius(orbitAngle) + 55);
                drone.Target = Planet.Nearest(anchor);
            }
            if (!Planet.Blocks.ContainsKey(drone.Target)) continue;
            var target = Center + MiningPlanet.KeyPoint(drone.Target);
            var chassis = Catalog.Chassis(drone.Type);
            var radial = (target - Center).Normalized();
            if (radial.LengthSquared() < 0.1f) radial = (drone.Position - Center).Normalized();
            if (radial.LengthSquared() < 0.1f) radial = Vector2.Up;
            var desired = target + radial * 58;
            var candidate = drone.Position.MoveToward(desired, (85 + drone.Level * 2) * delta);
            drone.TransferLeft = Mathf.Max(0, drone.TransferLeft - delta);
            if (drone.TransferLeft > 0) drone.Position = CruiseDrone(drone.Position, delta);
            else if (!Planet.ContainsPoint(candidate - Center, 8)) drone.Position = candidate;
            else
            {
                // Commit to clearing the lip before retrying the direct approach.
                drone.TransferLeft = 2;
                drone.Position = CruiseDrone(drone.Position, delta);
                drone.Target = Planet.Nearest(drone.Position - Center);
                target = Center + MiningPlanet.KeyPoint(drone.Target);
            }
            drone.Angle = (target - drone.Position).Angle();
            drone.ShotTimer -= delta;
            if ((item.Type != ContinuousLaser && drone.ShotTimer > 0) || drone.Position.DistanceTo(target) > Mathf.Min(WeaponRange, chassis.Range)) continue;
            // Intercept the first rock along the beam; never mine through intact terrain.
            var key = RayTarget(drone.Position, target);
            if (!Planet.Blocks.ContainsKey(key)) continue;
            target = Center + MiningPlanet.KeyPoint(key);
            if (item.Type == ContinuousLaser)
            {
                UpdateLaser(3 + Equipment.Drones.IndexOf(drone), drone.Position, target - drone.Position,
                    DamageMultiplier * WeaponPower(item.Level) * chassis.Power * (1 + drone.Level * 0.05),
                    BaseCooldown[ContinuousLaser] / WeaponCooldown(item.Type, item.Level, item.RateLevel) * chassis.FireRate * (1 + drone.Level * 0.01f), delta);
                continue;
            }
            drone.ShotTimer = WeaponCooldown(item.Type, item.Level, item.RateLevel) / (chassis.FireRate * (1 + drone.Level * 0.01f));
            EmitWeapon(item.Type, drone.Position, target, key, (target - drone.Position).Normalized(),
                DamageMultiplier * WeaponPower(item.Level) * chassis.Power * (1 + drone.Level * 0.05));
        }
        if (_serviceDockChanged) { Hud.RefreshInventory(); SaveProgress(); }
    }
}
