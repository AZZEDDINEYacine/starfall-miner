using Godot;
using System.Globalization;

namespace StarfallMiner;

/// <summary>Simulation coordinator. Input, camera, persistence and UI are separate.</summary>
public partial class Main : Node2D
{
    public static readonly Vector2 Center = new(270, 408);
    public const string SavePath = "user://starfall_save_v1.json";
    public static readonly string[] Weapons = ["Photon lance", "Nova cannon", "Ion chain", "Nova wave", "Bore laser"];
    public const float WeaponRange = 245;
    public const float ManualFireRateMultiplier = 1.25f;
    private static readonly string[] SectorNames = ["THE CINDER BELT", "AZURE DRIFT", "VIOLET REACH", "VERDANT ECHO"];
    private static readonly double[] UpgradeBase = [45, 60, 50], UpgradeGrowth = [1.62, 1.7, 1.67];
    private static readonly float[] BaseCooldown = [0.5f, 1.65f, 1.4f, 4.8f, 1.15f];
    private static readonly string[] Suffixes = ["", "K", "M", "B", "T", "Qa", "Qi"];

    [Export] public VisualPalette Palette { get; set; } = null!;
    [Export] public ManualFlightSettings FlightSettings { get; set; } = null!;
    [Export] public Godot.Collections.Array<PlanetVisualStyle> VisualStyles { get; set; } = [];
    public int VisualStyleIndex { get; private set; }
    public PlanetVisualStyle CurrentVisualStyle => VisualStyles[VisualStyleIndex];
    public MiningPlanet Planet { get; private set; } = null!;
    public MiningSound Sound { get; private set; } = null!;
    public Hud Hud { get; private set; } = null!;
    public ModeStatusOverlay ModeStatus { get; private set; } = null!;
    public WorldView View { get; private set; } = null!;
    public FlightInput Controls { get; private set; } = null!;
    public ManualNavigation Navigation { get; } = new();
    public SaveStore Persistence { get; private set; } = null!;
    private Visuals _visuals = null!;
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<Vector2I> _hitKeys = new(6);
    private readonly List<Vector2> _beamPath = new(16);

    public Vector2 ShipPosition = Center + new Vector2(-155, -150), ShipVelocity;
    public float ShipAngle, Clock, OrbitAngle = -2.2f;
    public double Credits = 80, TotalEarned, SessionCredits, IncomeRate, IncomeAccum;
    public long Flux, TotalMined, SessionMined, SessionEnergy, LifetimeEnergy;
    public int Sector = 1, Weapon;
    public int[] Levels { get; } = [0, 0, 0];
    public bool AutoMode = true, Paused, Muted, FirstRun = true;
    public bool IsTest { get; private set; }
    public bool NoSave;
    public Vector2I TargetKey = MiningPlanet.NoTarget, AimKey = MiningPlanet.NoTarget;
    public float ShotTimer = 0.3f, TapTimer, OverdriveLeft, OverdriveCooldown, WarpTimer, TransitionLeft;
    public const float WarpDuration = 2.8f;
    private float _incomeWindow, _saveTimer, _hudTimer;
    public List<Particle> Particles { get; } = new(288);
    public List<Beam> Beams { get; } = new(16);
    public List<Ring> Rings { get; } = new(32);
    public List<NovaWave> Waves { get; } = new(16);
    public List<Shell> Projectiles { get; } = new(16);
    public List<DebrisFragment> Debris { get; } = new(362);
    private (string Title, string Subtitle, Color Color, float Duration)? _startupNotice;

    // Persistent components are scene nodes. Resolve their paths before child _Ready() calls.
    public override void _EnterTree()
    {
        View = GetNode<WorldView>("WorldView");
        Planet = GetNode<MiningPlanet>("WorldView/PersistentWorld/Planet");
        _visuals = GetNode<Visuals>("WorldView/PersistentWorld/Visuals");
        Sound = GetNode<MiningSound>("MiningSound");
        Controls = GetNode<FlightInput>("FlightInput");
        Hud = GetNode<Hud>("UI/Hud");
        ModeStatus = GetNode<ModeStatusOverlay>("UI/ModeStatus");
        View.Game = this;
        _visuals.Game = this;
        Controls.Game = this;
        Hud.Game = this;
    }

    public override void _Ready()
    {
        IsTest = OS.GetCmdlineUserArgs().Contains("--test") || OS.GetCmdlineUserArgs().Contains("--qa");
        NoSave = IsTest;
        // Tests and inspection can never read, overwrite or delete the player's save.
        Persistence = new SaveStore(IsTest ? "res://.godot/tests/starfall_save_v1.json" : SavePath);
        _rng.Seed = 8086;
        Planet.BlockMined += OnBlockMined;
        Planet.PlanetCleared += OnPlanetCleared;
        Planet.BlastTriggered += OnChainBlast;
        Planet.Generate(Sector);
        RestoreVisualStyle(null);
        PlaceShipAtOrbit();
        if (!NoSave) Persistence.Load(this);
        View.PlaceWorld(true);
        Hud.Rebuild();
        ModeStatus.Play(AutoMode);
        View.ApplyOrientation();
        GetTree().AutoAcceptQuit = false;
        GetTree().QuitOnGoBack = false;
        if (_startupNotice is { } notice) Toast(notice.Title, notice.Subtitle, notice.Color, notice.Duration);
        else if (FirstRun) Toast("AUTOPILOT ONLINE", "Mine credits. Craft weapons and drones in Ship Systems.", Palette.WeaponLance, 3);
    }

    public override void _Process(double delta) => Step((float)Math.Min(delta, 0.05));

    public void Step(float delta)
    {
        Clock += delta;
        TransitionLeft = Mathf.Max(0, TransitionLeft - delta);
        UpdateEffects(delta);
        QueueRedraw();
        _visuals.QueueRedraw();
        if (Paused) { UpdateDrones(delta, true); Hud.RefreshService(); View.PlaceWorld(false, delta); return; }
        OverdriveLeft = Mathf.Max(0, OverdriveLeft - delta);
        OverdriveCooldown = Mathf.Max(0, OverdriveCooldown - delta);
        TapTimer = Mathf.Max(0, TapTimer - delta);
        ShotTimer -= delta;
        if (WarpTimer > 0)
        {
            WarpTimer -= delta;
            ShipPosition = ShipPosition.Lerp(Center + new Vector2(0, -350), delta * 1.7f);
            ShipAngle = Mathf.LerpAngle(ShipAngle, -Mathf.Pi * 0.5f, delta * 5);
            if (WarpTimer <= 0) AdvanceSector();
        }
        else
        {
            MoveShip(delta);
            TargetKey = AutoMode ? Planet.Nearest(ShipPosition - Center) : Navigation.Obstacle;
            if (AutoMode && Planet.Blocks.ContainsKey(AimKey))
            {
                var aimed = RayTarget(ShipPosition, Center + MiningPlanet.KeyPoint(AimKey));
                if (Planet.Blocks.ContainsKey(aimed)) TargetKey = aimed;
            }
            if (AutoMode && Planet.Blocks.ContainsKey(TargetKey))
            {
                var target = Center + MiningPlanet.KeyPoint(TargetKey);
                ShipAngle = Mathf.LerpAngle(ShipAngle, (target - ShipPosition).Angle(), delta * 12);
            }
            else if (!AutoMode && Navigation.Active) ShipAngle = Mathf.LerpAngle(ShipAngle, Navigation.AimDirection.Angle(), delta * 18);
            if ((AutoMode || !Hud.BlocksWorldInput && Navigation.ClearingPath) && ShotTimer <= 0) Fire();
            if (WarpTimer <= 0) { FireAuxiliaries(delta); UpdateDrones(delta); }
            if (_rng.Randf() < delta * 35 && ShipVelocity.Length() > 3)
            {
                var behind = ShipPosition - Vector2.FromAngle(ShipAngle) * 17;
                AddParticle(behind, -Vector2.FromAngle(ShipAngle) * 65 + new Vector2(_rng.RandfRange(-10, 10), _rng.RandfRange(-10, 10)), Palette.EngineExhaust, 0.25f, 2.4f);
            }
        }
        _incomeWindow += delta;
        if (_incomeWindow >= 2)
        {
            IncomeRate += (IncomeAccum / _incomeWindow - IncomeRate) * 0.65;
            IncomeAccum = _incomeWindow = 0;
        }
        _saveTimer += delta;
        if (_saveTimer >= 8) { _saveTimer = 0; SaveProgress(); }
        _hudTimer += delta;
        if (_hudTimer >= 0.12f) { _hudTimer = 0; Hud.Refresh(); }
        View.PlaceWorld(false, delta);
    }

    private void MoveShip(float delta)
    {
        var previous = ShipPosition;
        if (AutoMode)
        {
            ShipPosition = AutopilotSteering.Advance(ShipPosition, Center, Planet, delta);
            OrbitAngle = (ShipPosition - Center).Angle();
        }
        else
        {
            Controls.UpdateSteering();
            if (!Hud.BlocksWorldInput) ShipPosition = Navigation.Advance(ShipPosition, Planet, FlightSettings, delta);
        }
        ShipVelocity = (ShipPosition - previous) / Mathf.Max(delta, 0.001f);
    }

    public void PlaceShipAtOrbit(float angle = -2.2f)
    {
        OrbitAngle = angle;
        ShipPosition = Center + Vector2.FromAngle(angle) * (Planet.SurfaceRadius(angle) + 53);
        ShipAngle = (Center - ShipPosition).Angle();
        ShipVelocity = Vector2.Zero;
        Navigation.Stop();
        foreach (var drone in Equipment.Drones) { drone.Position = ShipPosition + (ShipPosition - Center).Normalized() * 20; drone.Target = MiningPlanet.NoTarget; }
    }

    public Vector2I RayTarget(Vector2 from, Vector2 toward)
    {
        float distance = from.DistanceTo(toward);
        if (distance < 1) return MiningPlanet.NoTarget;
        var direction = (toward - from) / distance;
        for (int step = 0; step <= (int)Mathf.Min(distance, WeaponRange); step += 4)
        {
            var key = MiningPlanet.PointKey(from + direction * step - Center);
            if (Planet.Blocks.ContainsKey(key)) return key;
        }
        return MiningPlanet.NoTarget;
    }

    public double DamageMultiplier => (1 + 0.12 * Math.Sqrt(Levels[0])) * (OverdriveLeft > 0 ? 1.5 : 1);
    public float WeaponCooldown(int type, int level) => BaseCooldown[type] / (WeaponRate(level) * (1 + 0.04f * Mathf.Sqrt(Levels[1]))) / (OverdriveLeft > 0 ? 1.65f : 1);
    public float FiringCooldown => WeaponCooldown(Weapon, ShipWeaponLevel) / (AutoMode ? 1 : ManualFireRateMultiplier);

    public void Fire(bool manualTap = false)
    {
        if (Paused || WarpTimer > 0 || Planet.Blocks.Count == 0 || (manualTap ? TapTimer : ShotTimer) > 0) return;
        var key = AutoMode ? (Planet.Blocks.ContainsKey(TargetKey) ? TargetKey : Planet.Nearest(ShipPosition - Center)) :
            RayTarget(ShipPosition, ShipPosition + Navigation.AimDirection * WeaponRange);
        bool hit = Planet.Blocks.ContainsKey(key);
        if (!hit || !AutoMode && !Navigation.ClearingPath) return;
        var target = Center + MiningPlanet.KeyPoint(key);
        if (AutoMode && ShipPosition.DistanceTo(target) > WeaponRange) return;
        var shotDirection = AutoMode ? (target - ShipPosition).Normalized() : Navigation.AimDirection;
        if (!AutoMode && hit) target = ShipPosition + shotDirection * Mathf.Clamp((target - ShipPosition).Dot(shotDirection), 19, WeaponRange);
        if (manualTap) TapTimer = 0.16f;
        else ShotTimer = FiringCooldown;
        EmitWeapon(Weapon, ShipPosition, target, key, shotDirection, DamageMultiplier * WeaponPower(ShipWeaponLevel) * (manualTap ? 1.15 : 1));
        if (manualTap) AddRing(target, 6, 26, Palette.Weapon(Weapon), 0.22f);
    }

    private void EmitWeapon(int weapon, Vector2 origin, Vector2 target, Vector2I key, Vector2 shotDirection, double multiplier)
    {
        bool hit = Planet.Blocks.ContainsKey(key);
        var muzzle = origin + shotDirection * 19;
        _hitKeys.Clear();
        if (weapon == 0)
        {
            var direction = shotDirection;
            var endpoint = target;
            for (int step = 0; hit && step < 53; step += 4)
            {
                var candidate = MiningPlanet.PointKey(target + direction * step - Center);
                if (!Planet.Blocks.ContainsKey(candidate) || _hitKeys.Contains(candidate)) continue;
                _hitKeys.Add(candidate);
                endpoint = Center + MiningPlanet.KeyPoint(candidate);
                if (_hitKeys.Count >= 1) break;
            }
            for (int i = 0; i < _hitKeys.Count; i++) Planet.Damage(_hitKeys[i], 1.1 * multiplier * Math.Pow(0.68, i));
            AddBeam([muzzle, endpoint], Palette.Weapon(0), 0.11f);
            if (hit) SpawnSparks(target, Palette.Weapon(0), 5);
            Sound.Play("laser", _rng.RandfRange(0.94f, 1.06f));
        }
        else if (weapon == 1)
        {
            Projectiles.Add(new Shell { Position = muzzle, Target = target, Damage = 3.2 * multiplier, Speed = 260, Life = 2.5f });
            Sound.Play("blast", 1.6f);
        }
        else if (weapon == 2)
        {
            if (hit) _hitKeys.Add(key);
            _beamPath.Clear();
            _beamPath.Add(muzzle);
            if (!hit) _beamPath.Add(target);
            for (int i = 0; hit && i < 3; i++)
            {
                var hitKey = _hitKeys[^1];
                if (!Planet.Blocks.ContainsKey(hitKey)) break;
                var p = Center + MiningPlanet.KeyPoint(hitKey);
                var from = _beamPath[^1];
                _beamPath.Add(from.Lerp(p, 0.4f) + new Vector2(_rng.RandfRange(-9, 9), _rng.RandfRange(-9, 9)));
                _beamPath.Add(from.Lerp(p, 0.72f) + new Vector2(_rng.RandfRange(-7, 7), _rng.RandfRange(-7, 7)));
                _beamPath.Add(p);
                Planet.Damage(hitKey, 1.6 * multiplier * Math.Pow(0.87, i));
                SpawnSparks(p, Palette.Weapon(2), 3);
                var next = Planet.Nearest(p - Center, _hitKeys);
                if (next == MiningPlanet.NoTarget || MiningPlanet.KeyPoint(next).DistanceTo(p - Center) > 61) break;
                _hitKeys.Add(next);
            }
            AddBeam(_beamPath.ToArray(), Palette.Weapon(2), 0.18f);
            Sound.Play("arc", _rng.RandfRange(0.93f, 1.08f));
        }
        else if (weapon == 3)
        {
            // A real expanding damage front, not a delayed instantaneous area hit.
            if (Waves.Count < 16) Waves.Add(new NovaWave { Position = target, Damage = 5.8 * multiplier, Radius = 104, Duration = 1.6f });
            Sound.Play("blast", 0.65f);
        }
        else if (weapon == 4)
        {
            for (int step = 0; step <= 32; step += 4)
            {
                var candidate = MiningPlanet.PointKey(target + shotDirection * step - Center);
                if (!Planet.Blocks.ContainsKey(candidate) || _hitKeys.Contains(candidate)) continue;
                _hitKeys.Add(candidate);
                if (_hitKeys.Count == 2) break;
            }
            foreach (var candidate in _hitKeys)
            {
                double armorCut = Planet.Blocks.TryGetValue(candidate, out var ore) && ore.Material >= 0 ? 2.5 : 1;
                Planet.Damage(candidate, 2.6 * multiplier * armorCut);
            }
            AddBeam([muzzle, target + shotDirection * 24], Palette.Weapon(4), 0.24f);
            Sound.Play("laser", 0.7f);
        }
    }

    public void SetWeapon(int index)
    {
        index = Math.Clamp(index, 0, Weapons.Length - 1);
        CraftedWeapon? owned = null;
        foreach (var item in Equipment.Weapons)
            if (item.Type == index && (!Equipment.Mounted(item.Id) || Equipment.ShipMounted(item.Id))) { owned = item; break; }
        if (owned == null)
        {
            EquipmentMessage = "Craft this weapon in Fabricator, or free its drone mount in Fleet.";
            Hud.OpenWeaponRecipes();
            return;
        }
        for (int i = 0; i < 2; i++)
            if (Equipment.ShipAuxWeaponIds[i] == owned.Id) Equipment.ShipAuxWeaponIds[i] = Equipment.ShipWeaponId;
        Equipment.ShipWeaponId = owned.Id;
        Weapon = index;
        ShotTimer = Mathf.Min(ShotTimer, 0.1f);
        Hud.Refresh();
        Sound.Play("mine", 0.7f + Weapon * 0.15f);
        SaveProgress();
    }

    public void SetVisualStyle(int index, bool save = true)
    {
        VisualStyleIndex = Math.Clamp(index, 0, VisualStyles.Count - 1);
        Planet.GetNode<PlanetRenderer>("Renderer").Style = CurrentVisualStyle;
        Planet.NotifyTilesChanged();
        var stars = GetNode<Starfield>("Starfield");
        stars.Style = CurrentVisualStyle;
        stars.QueueRedraw();
        _visuals.QueueRedraw();
        if (save) SaveProgress();
    }

    public void RestoreVisualStyle(string? id)
    {
        int index = 0;
        for (int i = 0; i < VisualStyles.Count; i++)
            if (VisualStyles[i].Id == id) { index = i; break; }
        SetVisualStyle(index, false);
    }

    public void ReleaseControls()
    {
        Controls?.Release();
        Hud?.CancelTouches();
    }

    public void SetAuto(bool value)
    {
        if (value == AutoMode) return;
        AutoMode = value;
        OrbitAngle = (ShipPosition - Center).Angle();
        AimKey = MiningPlanet.NoTarget;
        ReleaseControls();
        View.BeginModeChange();
        Hud.ClosePanel();
        Hud.BeginModeTransition(value);
        ModeStatus.Play(value);
        Hud.RequestLayout();
        Sound.Play("upgrade", value ? 0.85f : 0.65f);
        SaveProgress();
    }

    public double UpgradeCost(int index) => Math.Floor(UpgradeBase[index] * Math.Pow(UpgradeGrowth[index], Levels[index]));

    public bool BuyUpgrade(int index)
    {
        double cost = UpgradeCost(index);
        if (Credits < cost)
        {
            Toast("MORE ORE NEEDED", "Keep mining to afford this upgrade.", Palette.Weapon(1), 1.6f);
            return false;
        }
        Credits -= cost;
        Levels[index]++;
        Sound.Play("upgrade");
        Hud.Refresh();
        SaveProgress();
        return true;
    }

    public void ActivateOverdrive()
    {
        if (Paused || OverdriveCooldown > 0 || WarpTimer > 0) return;
        if (Flux < 5)
        {
            Toast("ENERGY REQUIRED", "Mine blue crystals. Overdrive costs 5 energy.", Palette.Weapon(2), 2.5f);
            return;
        }
        Flux -= 5;
        OverdriveLeft = 8;
        OverdriveCooldown = 28;
        AddRing(ShipPosition, 10, 90, Palette.Weapon(2), 0.6f);
        Sound.Play("upgrade", 0.8f);
        Toast("REACTOR OVERDRIVE", "Double fire rate. 50% more damage. 8 seconds.", Palette.Weapon(2), 2.5f);
        Hud.Refresh();
        SaveProgress();
    }

    private void OnBlockMined(OreBlock ore, Vector2 point)
    {
        int kind = ore.Kind;
        long energy = kind == MiningPlanet.Crystal ? 1 : kind == MiningPlanet.Core ? 5 : 0;
        double earned = ore.Value * (1 + Math.Min(Levels[2], 50) * 0.04);
        TotalMined++;
        SessionMined++;
        for (int i = 0; i < 2; i++)
            Debris.Add(new DebrisFragment
            {
                Position = point + new Vector2(_rng.RandfRange(-3, 3), _rng.RandfRange(-3, 3)),
                Velocity = Vector2.FromAngle(_rng.Randf() * Mathf.Tau) * _rng.RandfRange(20, 42),
                Age = -i * 0.035f, Value = i == 0 ? earned : 0, Energy = i == 0 ? energy : 0,
                Kind = kind, Size = i == 0 ? 2.4f : 1.6f,
                Material = i == 0 ? ore.Material : -1, MaterialAmount = i == 0 ? ore.MaterialAmount : 0
            });
        if (kind is MiningPlanet.Gold or MiningPlanet.Crystal) Sound.Play("mine", kind == MiningPlanet.Gold ? 1.15f : 1.35f);
        if (AimKey == MiningPlanet.PointKey(point - Center)) AimKey = MiningPlanet.NoTarget;
    }

    private void Collect(DebrisFragment fragment)
    {
        Equipment.AddMaterial(fragment.Material, fragment.MaterialAmount);
        Credits += fragment.Value;
        Flux += fragment.Energy;
        TotalEarned += fragment.Value;
        IncomeAccum += fragment.Value;
        SessionCredits += fragment.Value;
        SessionEnergy += fragment.Energy;
        LifetimeEnergy += fragment.Energy;
    }

    public void UpdateDebris(float delta)
    {
        for (int i = Debris.Count - 1; i >= 0; i--)
        {
            var fragment = Debris[i];
            fragment.Age += delta;
            var toward = ShipPosition - fragment.Position;
            if (toward.Length() < 12 || fragment.Age > 2.15f)
            {
                Collect(fragment);
                Debris.RemoveAt(i);
                continue;
            }
            if (fragment.Age < 0.16f) fragment.Position += fragment.Velocity * delta;
            else
            {
                float speed = Mathf.Min(650, 95 + (fragment.Age - 0.16f) * 780);
                fragment.Velocity = fragment.Velocity.Lerp(toward.Normalized() * speed, 1 - Mathf.Exp(-delta * 14));
                var step = fragment.Velocity * delta;
                if (step.Length() >= toward.Length()) { Collect(fragment); Debris.RemoveAt(i); }
                else fragment.Position += step;
            }
        }
        while (Debris.Count > 360) { Collect(Debris[0]); Debris.RemoveAt(0); }
    }

    private void OnChainBlast(Vector2 point)
    {
        AddRing(point, 4, 57, Palette.ChainBlast, 0.38f);
        SpawnSparks(point, Palette.ChainSparks, 4);
        Sound.Play("blast", 1.2f);
    }

    private void OnPlanetCleared()
    {
        if (WarpTimer > 0) return;
        ReleaseControls();
        WarpTimer = WarpDuration;
        double bonus = 150 + 75 * Math.Min(Sector - 1, 500);
        Credits += bonus;
        TotalEarned += bonus;
        Flux += 5;
        SessionCredits += bonus;
        SessionEnergy += 5;
        LifetimeEnergy += 5;
        Projectiles.Clear(); Waves.Clear();
        Sound.Play("warp");
        AddRing(Center, 30, 260, Palette.Weapon(2), 1.4f);
        Toast($"SECTOR {Sector:00} COMPLETE", $"+{Compact(bonus)} credits  ·  +5 energy  ·  preparing warp", Palette.Weapon(0), 2.7f);
        Hud.Refresh();
        SaveProgress();
    }

    public void AdvanceSector()
    {
        Sector++;
        Equipment.HighestSector = Math.Max(Equipment.HighestSector, Sector);
        Planet.Generate(Sector);
        PlaceShipAtOrbit();
        AimKey = MiningPlanet.NoTarget;
        ShotTimer = 0.4f;
        Projectiles.Clear(); Waves.Clear();
        Toast($"ENTERING SECTOR {Sector:00}", SectorNames[(Sector - 1) % 4] + "  /  tougher ore, richer rewards", Palette.Weapon(2), 2.8f);
        Hud.Refresh();
        SaveProgress();
    }

    public void UpdateEffects(float delta)
    {
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var p = Particles[i];
            p.Life -= delta;
            if (p.Life <= 0) Particles.RemoveAt(i);
            else { p.Position += p.Velocity * delta; p.Velocity *= 1 - delta * 2; Particles[i] = p; }
        }
        for (int i = Beams.Count - 1; i >= 0; i--)
        {
            var beam = Beams[i];
            beam.Life -= delta;
            if (beam.Life <= 0) Beams.RemoveAt(i); else Beams[i] = beam;
        }
        for (int i = Rings.Count - 1; i >= 0; i--)
        {
            var ring = Rings[i];
            ring.Life -= delta;
            if (ring.Life <= 0) Rings.RemoveAt(i); else Rings[i] = ring;
        }
        if (Paused) return;
        UpdateDebris(delta);
        for (int i = Waves.Count - 1; i >= 0; i--)
        {
            if (i >= Waves.Count) continue;
            var wave = Waves[i];
            float before = wave.Radius * Mathf.Clamp(wave.Age / wave.Duration, 0, 1);
            wave.Age += delta;
            float after = wave.Radius * Mathf.Clamp(wave.Age / wave.Duration, 0, 1);
            Planet.DamageAnnulus(wave.Position - Center, before, after, wave.Radius, wave.Damage);
            if (i < Waves.Count && wave.Age >= wave.Duration) Waves.RemoveAt(i);
        }
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            // An impact can clear the sector, which removes all remaining shells.
            if (i >= Projectiles.Count) continue;
            var shell = Projectiles[i];
            shell.Life -= delta;
            var direction = shell.Target - shell.Position;
            if (direction.Length() <= shell.Speed * delta || shell.Life <= 0)
            {
                Projectiles.RemoveAt(i);
                AddRing(shell.Target, 4, 43, Palette.Weapon(1), 0.4f);
                SpawnSparks(shell.Target, Palette.Weapon(1), 6);
                Planet.DamageRadius(shell.Target - Center, 26, shell.Damage);
                Sound.Play("blast");
            }
            else
            {
                shell.Position += direction.Normalized() * shell.Speed * delta;
                Projectiles[i] = shell;
                AddParticle(shell.Position, -direction.Normalized() * 25, Palette.Weapon(1), 0.22f, 2);
            }
        }
    }

    private void SpawnSparks(Vector2 point, Color color, int count)
    {
        for (int i = 0; i < count; i++)
            AddParticle(point + new Vector2(_rng.RandfRange(-5, 5), _rng.RandfRange(-5, 5)),
                Vector2.FromAngle(_rng.Randf() * Mathf.Tau) * _rng.RandfRange(18, 90), color, _rng.RandfRange(0.22f, 0.55f), _rng.RandfRange(1.3f, 3.8f));
    }

    private void AddParticle(Vector2 point, Vector2 velocity, Color color, float life, float size)
    {
        if (Particles.Count >= 280) Particles.RemoveAt(0);
        Particles.Add(new Particle { Position = point, Velocity = velocity, Color = color, Life = life, MaxLife = life, Size = size });
    }
    private void AddBeam(Vector2[] points, Color color, float life) => Beams.Add(new Beam { Points = points, Color = color, Life = life, MaxLife = life });
    public void AddRing(Vector2 point, float start, float finish, Color color, float life) =>
        Rings.Add(new Ring { Position = point, Start = start, Finish = finish, Color = color, Life = life, MaxLife = life });

    public void Toast(string title, string subtitle, Color color, float duration)
    {
        if (Hud?.CanShowToast == true) Hud.ShowToast(title, subtitle, color, duration);
        else _startupNotice = (title, subtitle, color, duration);
    }

    public void TogglePause()
    {
        Paused = !Paused;
        ReleaseControls();
        Hud.SetPauseVisible(Paused);
        if (Paused) SaveProgress();
    }

    public void ToggleSound()
    {
        Muted = !Muted;
        Sound.Enabled = !Muted;
        Hud.Refresh();
        SaveProgress();
    }

    public void ResetProgress()
    {
        Credits = 80;
        Equipment = EquipmentState.Fresh();
        EquipmentMessage = "Mine the mantle for Titanium, or buy it at the market.";
        Flux = TotalMined = SessionMined = SessionEnergy = LifetimeEnergy = 0;
        Sector = 1;
        Array.Clear(Levels);
        Weapon = 0;
        TotalEarned = SessionCredits = IncomeRate = IncomeAccum = 0;
        WarpTimer = OverdriveLeft = OverdriveCooldown = 0;
        AutoMode = true;
        Planet.Generate(1);
        Projectiles.Clear(); Waves.Clear(); Particles.Clear(); Beams.Clear(); Rings.Clear(); Debris.Clear();
        PlaceShipAtOrbit();
        AimKey = TargetKey = MiningPlanet.NoTarget;
        ShotTimer = 0.3f;
        TapTimer = 0;
        Paused = false;
        Hud.SetPauseVisible(false);
        ReleaseControls();
        View.BeginModeChange();
        Hud.RequestLayout();
        SaveProgress();
    }

    public void SaveProgress() { if (!NoSave) Persistence.Save(this); }

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationWMGoBackRequest: if (Hud != null) TogglePause(); break;
            case NotificationWMCloseRequest: if (Planet != null) SaveProgress(); GetTree().Quit(); break;
            case NotificationApplicationPaused:
            case NotificationWMWindowFocusOut:
            case NotificationApplicationFocusOut:
                if (Planet != null) { ReleaseControls(); SaveProgress(); }
                break;
        }
    }

    public static string Compact(double value)
    {
        double magnitude = Math.Abs(value);
        int tier = 0;
        while (magnitude >= 1000 && tier < Suffixes.Length - 1) { magnitude /= 1000; value /= 1000; tier++; }
        return (tier == 0 ? Math.Truncate(value).ToString("0", CultureInfo.InvariantCulture) : value.ToString(magnitude >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture)) + Suffixes[tier];
    }
}
