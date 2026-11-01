using Godot;
using System.Globalization;

namespace StarfallMiner;

/// <summary>Simulation coordinator. Input, camera, persistence and UI are separate.</summary>
public partial class Main : Node2D
{
    public static readonly Vector2 Center = new(270, 408);
    public const string SavePath = "user://starfall_save_v1.json";
    public static readonly string[] Weapons = ["Photon lance", "Nova cannon", "Ion chain"];
    public static readonly Color[] WeaponColors = [new("79f3ce"), new("ffb574"), new("b5a2ff")];
    private static readonly double[] UpgradeBase = [45, 60, 50], UpgradeGrowth = [1.62, 1.7, 1.67];
    private static readonly float[] BaseCooldown = [0.24f, 0.86f, 0.62f];
    private static readonly string[] Suffixes = ["", "K", "M", "B", "T", "Qa", "Qi"];

    public MiningPlanet Planet { get; private set; } = null!;
    public MiningSound Sound { get; private set; } = null!;
    public Hud Hud { get; private set; } = null!;
    public WorldView View { get; private set; } = null!;
    public FlightInput Controls { get; private set; } = null!;
    public SaveStore Persistence { get; private set; } = null!;
    private Visuals _visuals = null!;
    private readonly RandomNumberGenerator _rng = new();
    private readonly (Vector2 Point, float Alpha)[] _stars = new (Vector2, float)[38];
    private readonly List<Vector2I> _hitKeys = new(6);
    private readonly List<Vector2> _beamPath = new(16);

    public Vector2 ShipPosition = Center + new Vector2(-155, -150), ShipVelocity;
    public float ShipAngle, Clock, OrbitAngle = -2.2f;
    public double Credits = 80, TotalEarned, SessionCredits, IncomeRate, IncomeAccum;
    public long Flux, Iron, TotalMined, SessionMined, SessionEnergy, LifetimeEnergy;
    public int Sector = 1, Weapon;
    public int[] Levels { get; } = [0, 0, 0];
    // Free weapon modules are earned on major planets; all three base weapons remain available.
    public int[] WeaponTiers { get; } = [0, 0, 0];
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
    public List<Shell> Projectiles { get; } = new(16);
    public List<DebrisFragment> Debris { get; } = new(362);
    private (string Title, string Subtitle, Color Color, float Duration)? _startupNotice;

    public override void _Ready()
    {
        IsTest = OS.GetCmdlineUserArgs().Contains("--test") || OS.GetCmdlineUserArgs().Contains("--qa");
        NoSave = IsTest;
        // Tests and inspection can never read, overwrite or delete the player's save.
        Persistence = new SaveStore(IsTest ? "res://.godot/tests/starfall_save_v1.json" : SavePath);
        _rng.Seed = 8086;
        for (int i = 0; i < _stars.Length; i++) _stars[i] = (new Vector2(_rng.Randf(), _rng.Randf()), _rng.RandfRange(0.1f, 0.35f));
        View = new WorldView { Game = this };
        AddChild(View);
        Planet = new MiningPlanet { Position = Center };
        View.Stage.AddChild(Planet);
        Planet.BlockMined += OnBlockMined;
        Planet.PlanetCleared += OnPlanetCleared;
        Planet.BlastTriggered += OnChainBlast;
        Sound = new MiningSound();
        AddChild(Sound);
        Planet.Generate(Sector);
        if (!NoSave) Persistence.Load(this);
        if (FirstRun)
        {
            ShipPosition = Center + Vector2.FromAngle(OrbitAngle) * (Planet.SurfaceRadius(OrbitAngle) + 53);
        }
        _visuals = new Visuals { Game = this };
        View.Stage.AddChild(_visuals);
        Controls = new FlightInput { Game = this };
        AddChild(Controls);
        var canvas = new CanvasLayer();
        AddChild(canvas);
        Hud = new Hud { Game = this };
        canvas.AddChild(Hud);
        View.PlaceWorld(true);
        View.ApplyOrientation();
        GetTree().AutoAcceptQuit = false;
        GetTree().QuitOnGoBack = false;
        if (_startupNotice is { } notice) Toast(notice.Title, notice.Subtitle, notice.Color, notice.Duration);
        else if (FirstRun) Toast("AUTOPILOT ONLINE", "Tap TAKE CONTROL to fly. Ship systems are below.", WeaponColors[0], 3);
        QueueRedraw();
    }

    public override void _Process(double delta) => Step((float)Math.Min(delta, 0.05));

    public void Step(float delta)
    {
        Clock += delta;
        TransitionLeft = Mathf.Max(0, TransitionLeft - delta);
        View.PlaceWorld(false, delta);
        UpdateEffects(delta);
        QueueRedraw();
        _visuals.QueueRedraw();
        if (Paused) return;
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
            TargetKey = Planet.Nearest(ShipPosition - Center);
            if (Planet.Blocks.ContainsKey(AimKey))
            {
                var aimed = RayTarget(ShipPosition, Center + MiningPlanet.KeyPoint(AimKey));
                if (Planet.Blocks.ContainsKey(aimed)) TargetKey = aimed;
            }
            if (Planet.Blocks.ContainsKey(TargetKey))
            {
                var target = Center + MiningPlanet.KeyPoint(TargetKey);
                ShipAngle = Mathf.LerpAngle(ShipAngle, (target - ShipPosition).Angle(), delta * 12);
                if ((AutoMode || !Hud.BlocksWorldInput && (Controls.FireHeld || Input.IsPhysicalKeyPressed(Key.Space))) && ShotTimer <= 0) Fire();
            }
            if (_rng.Randf() < delta * 35 && ShipVelocity.Length() > 3)
            {
                var behind = ShipPosition - Vector2.FromAngle(ShipAngle) * 17;
                AddParticle(behind, -Vector2.FromAngle(ShipAngle) * 65 + new Vector2(_rng.RandfRange(-10, 10), _rng.RandfRange(-10, 10)), new Color("7aeacb"), 0.25f, 2.4f);
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
    }

    private void MoveShip(float delta)
    {
        var previous = ShipPosition;
        if (AutoMode)
        {
            OrbitAngle += delta * 0.24f;
            float radius = Planet.SurfaceRadius(OrbitAngle) + 53;
            ShipPosition = ShipPosition.Lerp(Center + Vector2.FromAngle(OrbitAngle) * radius, 1 - Mathf.Exp(-delta * 2.6f));
            if (Planet.ContainsPoint(ShipPosition - Center, 8))
                ShipPosition = Center + (ShipPosition - Center).Normalized() * (radius + 14);
        }
        else
        {
            var displacement = Controls.Movement() * 190 * delta;
            var nextX = ShipPosition + new Vector2(displacement.X, 0);
            if (!Planet.ContainsPoint(nextX - Center, 9)) ShipPosition.X = nextX.X;
            var nextY = ShipPosition + new Vector2(0, displacement.Y);
            if (!Planet.ContainsPoint(nextY - Center, 9)) ShipPosition.Y = nextY.Y;
            if (TransitionLeft <= 0)
            {
                var bounds = View.ManualBounds;
                ShipPosition.X = Mathf.Clamp(ShipPosition.X, bounds.Position.X, bounds.End.X);
                ShipPosition.Y = Mathf.Clamp(ShipPosition.Y, bounds.Position.Y, bounds.End.Y);
            }
        }
        ShipVelocity = (ShipPosition - previous) / Mathf.Max(delta, 0.001f);
    }

    public Vector2I RayTarget(Vector2 from, Vector2 toward)
    {
        float distance = from.DistanceTo(toward);
        if (distance < 1) return MiningPlanet.NoTarget;
        var direction = (toward - from) / distance;
        for (int step = 0; step < (int)Mathf.Min(distance + 10, 420); step += 4)
        {
            var key = MiningPlanet.PointKey(from + direction * step - Center);
            if (Planet.Blocks.ContainsKey(key)) return key;
        }
        return MiningPlanet.NoTarget;
    }

    public double DamageMultiplier => Math.Pow(1.46, Levels[0]) *
        (1 + WeaponTiers[Weapon] * 0.12) * (OverdriveLeft > 0 ? 1.5 : 1);

    public void Fire(bool manualTap = false)
    {
        if (Paused || WarpTimer > 0 || Planet.Blocks.Count == 0 || (manualTap ? TapTimer : ShotTimer) > 0) return;
        var key = Planet.Blocks.ContainsKey(TargetKey) ? TargetKey : Planet.Nearest(ShipPosition - Center);
        if (!Planet.Blocks.ContainsKey(key)) return;
        var target = Center + MiningPlanet.KeyPoint(key);
        if (ShipPosition.DistanceTo(target) > 245) return;
        float cooldown = BaseCooldown[Weapon] / (1 + Levels[1] * 0.18f) / (OverdriveLeft > 0 ? 2.1f : 1);
        if (manualTap) TapTimer = 0.16f;
        else ShotTimer = Mathf.Max(0.055f, cooldown);
        double multiplier = DamageMultiplier * (manualTap ? 1.15 : 1);
        var muzzle = ShipPosition + Vector2.FromAngle((target - ShipPosition).Angle()) * 19;
        _hitKeys.Clear();
        if (Weapon == 0)
        {
            var direction = (target - muzzle).Normalized();
            var endpoint = target;
            for (int step = 0; step < 53; step += 4)
            {
                var candidate = MiningPlanet.PointKey(target + direction * step - Center);
                if (!Planet.Blocks.ContainsKey(candidate) || _hitKeys.Contains(candidate)) continue;
                _hitKeys.Add(candidate);
                endpoint = Center + MiningPlanet.KeyPoint(candidate);
                if (_hitKeys.Count >= 3) break;
            }
            for (int i = 0; i < _hitKeys.Count; i++) Planet.Damage(_hitKeys[i], 1.65 * multiplier * Math.Pow(0.68, i));
            AddBeam([muzzle, endpoint], WeaponColors[0], 0.11f);
            SpawnSparks(target, WeaponColors[0], 5);
            Sound.Play("laser", _rng.RandfRange(0.94f, 1.06f));
        }
        else if (Weapon == 1)
        {
            Projectiles.Add(new Shell { Position = muzzle, Target = target, Damage = 4.4 * multiplier, Speed = 390, Life = 1.5f });
            Sound.Play("blast", 1.6f);
        }
        else
        {
            _hitKeys.Add(key);
            _beamPath.Clear();
            _beamPath.Add(muzzle);
            for (int i = 0; i < 5; i++)
            {
                var hit = _hitKeys[^1];
                if (!Planet.Blocks.ContainsKey(hit)) break;
                var p = Center + MiningPlanet.KeyPoint(hit);
                var from = _beamPath[^1];
                _beamPath.Add(from.Lerp(p, 0.4f) + new Vector2(_rng.RandfRange(-9, 9), _rng.RandfRange(-9, 9)));
                _beamPath.Add(from.Lerp(p, 0.72f) + new Vector2(_rng.RandfRange(-7, 7), _rng.RandfRange(-7, 7)));
                _beamPath.Add(p);
                Planet.Damage(hit, 2.3 * multiplier * Math.Pow(0.87, i));
                SpawnSparks(p, WeaponColors[2], 3);
                var next = Planet.Nearest(p - Center, _hitKeys);
                if (next == MiningPlanet.NoTarget || MiningPlanet.KeyPoint(next).DistanceTo(p - Center) > 61) break;
                _hitKeys.Add(next);
            }
            AddBeam(_beamPath.ToArray(), WeaponColors[2], 0.18f);
            Sound.Play("arc", _rng.RandfRange(0.93f, 1.08f));
        }
        if (manualTap) AddRing(target, 6, 26, WeaponColors[Weapon], 0.22f);
    }

    public void SetWeapon(int index)
    {
        Weapon = Math.Clamp(index, 0, 2);
        ShotTimer = Mathf.Min(ShotTimer, 0.1f);
        Hud.Refresh();
        Sound.Play("mine", 0.7f + Weapon * 0.15f);
        SaveProgress();
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
        TransitionLeft = 0.55f;
        View.ApplyOrientation();
        Hud.ClosePanel();
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
            Toast("MORE ORE NEEDED", "Keep mining to afford this upgrade.", WeaponColors[1], 1.6f);
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
            Toast("ENERGY REQUIRED", "Mine blue crystals. Overdrive costs 5 energy.", WeaponColors[2], 2.5f);
            return;
        }
        Flux -= 5;
        OverdriveLeft = 8;
        OverdriveCooldown = 28;
        AddRing(ShipPosition, 10, 90, WeaponColors[2], 0.6f);
        Sound.Play("upgrade", 0.8f);
        Toast("REACTOR OVERDRIVE", "Double fire rate. 50% more damage. 8 seconds.", WeaponColors[2], 2.5f);
        Hud.Refresh();
        SaveProgress();
    }

    private void OnBlockMined(double value, long energy, Vector2 point, int kind)
    {
        double earned = value * (1 + Levels[2] * 0.3);
        TotalMined++;
        SessionMined++;
        for (int i = 0; i < 2; i++)
            Debris.Add(new DebrisFragment
            {
                Position = point + new Vector2(_rng.RandfRange(-3, 3), _rng.RandfRange(-3, 3)),
                Velocity = Vector2.FromAngle(_rng.Randf() * Mathf.Tau) * _rng.RandfRange(20, 42),
                Age = -i * 0.035f, Value = i == 0 ? earned : 0, Energy = i == 0 ? energy : 0,
                Kind = kind, Size = i == 0 ? 2.4f : 1.6f
            });
        if (kind is MiningPlanet.Gold or MiningPlanet.Crystal) Sound.Play("mine", kind == MiningPlanet.Gold ? 1.15f : 1.35f);
        if (AimKey == MiningPlanet.PointKey(point - Center)) AimKey = MiningPlanet.NoTarget;
    }

    private void Collect(DebrisFragment fragment)
    {
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
        AddRing(point, 4, 57, new Color("ffab7a"), 0.38f);
        SpawnSparks(point, new Color("ffbd83"), 4);
        Sound.Play("blast", 1.2f);
    }

    private void OnPlanetCleared()
    {
        if (WarpTimer > 0) return;
        WarpTimer = WarpDuration;
        var reward = SectorDesign.RewardFor(Sector);
        Credits += reward.Credits;
        TotalEarned += reward.Credits;
        Iron += reward.Iron;
        Flux += reward.Energy;
        SessionCredits += reward.Credits;
        SessionEnergy += reward.Energy;
        LifetimeEnergy += reward.Energy;
        if (reward.WeaponIndex >= 0) WeaponTiers[reward.WeaponIndex]++;
        Projectiles.Clear();
        Sound.Play("warp");
        AddRing(Center, 30, 260, WeaponColors[2], 1.4f);
        string module = reward.WeaponIndex >= 0 ? $"  ·  {Weapons[reward.WeaponIndex]} MK {WeaponTiers[reward.WeaponIndex] + 1}" : "";
        Toast($"{Planet.Profile.Name} COMPLETE", $"+{Compact(reward.Credits)} CR  ·  +{reward.Iron} iron  ·  +{reward.Energy} energy{module}", WeaponColors[0], 3.8f);
        Hud.Refresh();
        SaveProgress();
    }

    public void AdvanceSector()
    {
        Sector++;
        Planet.Generate(Sector);
        OrbitAngle = -2.2f;
        ShipPosition = Center + Vector2.FromAngle(OrbitAngle) * (Planet.SurfaceRadius(OrbitAngle) + 53);
        AimKey = MiningPlanet.NoTarget;
        ShotTimer = 0.4f;
        Projectiles.Clear();
        Toast($"SECTOR {Sector:00} / {Planet.Profile.Name}", Planet.Profile.Lesson, WeaponColors[2], 3.2f);
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
                AddRing(shell.Target, 4, 43, WeaponColors[1], 0.4f);
                SpawnSparks(shell.Target, WeaponColors[1], 6);
                Planet.DamageRadius(shell.Target - Center, 36, shell.Damage);
                Sound.Play("blast");
            }
            else
            {
                shell.Position += direction.Normalized() * shell.Speed * delta;
                Projectiles[i] = shell;
                AddParticle(shell.Position, -direction.Normalized() * 25, WeaponColors[1], 0.22f, 2);
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
        if (Hud != null) Hud.ShowToast(title, subtitle, color, duration);
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
        Flux = Iron = TotalMined = SessionMined = SessionEnergy = LifetimeEnergy = 0;
        Sector = 1;
        Array.Clear(Levels);
        Array.Clear(WeaponTiers);
        Weapon = 0;
        TotalEarned = SessionCredits = IncomeRate = IncomeAccum = 0;
        WarpTimer = OverdriveLeft = OverdriveCooldown = 0;
        AutoMode = true;
        Planet.Generate(1);
        Projectiles.Clear(); Particles.Clear(); Beams.Clear(); Rings.Clear(); Debris.Clear();
        OrbitAngle = -2.2f;
        ShipPosition = Center + Vector2.FromAngle(OrbitAngle) * (Planet.SurfaceRadius(OrbitAngle) + 53);
        AimKey = TargetKey = MiningPlanet.NoTarget;
        ShotTimer = 0.3f;
        TapTimer = 0;
        Paused = false;
        Hud.SetPauseVisible(false);
        ReleaseControls();
        TransitionLeft = 0.55f;
        View.ApplyOrientation();
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

    public override void _Draw()
    {
        if (View == null) return;
        DrawRect(new Rect2(Vector2.Zero, View.Size), new Color("080d16"));
        foreach (var star in _stars) DrawRect(new Rect2(star.Point * View.Size, Vector2.One), new Color(0.65f, 0.8f, 0.88f, star.Alpha));
    }
}
