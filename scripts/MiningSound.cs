using Godot;
using System.Buffers.Binary;

namespace StarfallMiner;

public partial class MiningSound : Node
{
    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (value) return;
            _laserPlayer?.Stop();
            foreach (var player in _players) player?.Stop();
        }
    }
    private readonly AudioStreamPlayer[] _players = new AudioStreamPlayer[8];
    private readonly Dictionary<string, AudioStreamWav> _sounds = new();
    private AudioStreamPlayer? _laserPlayer;
    public bool LaserLoopPlaying => _laserPlayer?.Playing == true;
    public int LaserLoopStarts { get; private set; }

    public override void _Ready()
    {
        for (int i = 0; i < _players.Length; i++)
        {
            _players[i] = new AudioStreamPlayer { VolumeDb = -15 };
            AddChild(_players[i]);
        }
        _sounds["laser"] = Tone(680, 200, 0.075f, 0.12f);
        _sounds["blast"] = Tone(110, 28, 0.24f, 0.65f);
        _sounds["arc"] = Tone(1000, 360, 0.14f, 0.27f);
        _sounds["mine"] = Tone(820, 1420, 0.055f, 0.03f);
        _sounds["upgrade"] = Tone(480, 1050, 0.25f, 0);
        _sounds["warp"] = Tone(110, 1700, 0.7f, 0.14f);
        _laserPlayer = new AudioStreamPlayer { Stream = LaserHum(), VolumeDb = -19 };
        AddChild(_laserPlayer);
    }

    public void SetLaserActive(bool active)
    {
        if (_laserPlayer == null) return;
        if (!Enabled || !active) { _laserPlayer.Stop(); return; }
        if (_laserPlayer.Playing) return;
        LaserLoopStarts++;
        _laserPlayer.Play();
    }

    private static AudioStreamWav LaserHum()
    {
        const int rate = 22050, count = 4410;
        var data = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / rate;
            // Integer periods in the loop: a warm carrier, bright electrical edge
            // and a slow harmonic shimmer, with no click at the loop boundary.
            double sample = (Math.Sin(Math.Tau * 220 * t) * 0.44 + Math.Sin(Math.Tau * 440 * t) * 0.22 +
                Math.Sin(Math.Tau * 880 * t) * 0.12 + Math.Sin(Math.Tau * 1320 * t) * 0.05) * (0.9 + Math.Cos(Math.Tau * 10 * t) * 0.1) * 0.36;
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2, 2), (short)(sample * 32767));
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate,
            Data = data, LoopMode = AudioStreamWav.LoopModeEnum.Forward, LoopBegin = 0, LoopEnd = count };
    }

    public void Play(string kind, float pitch = 1)
    {
        if (!Enabled || !_sounds.TryGetValue(kind, out var stream)) return;
        foreach (var player in _players)
        {
            if (player.Playing) continue;
            player.Stream = stream;
            player.PitchScale = pitch;
            player.Play();
            return;
        }
    }

    private static AudioStreamWav Tone(float start, float finish, float duration, float noise)
    {
        const int rate = 22050;
        int count = (int)(rate * duration);
        var data = new byte[count * 2];
        double phase = 0;
        using var rng = new RandomNumberGenerator { Seed = (ulong)start };
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / count;
            phase += Math.Tau * (start + (finish - start) * t) / rate;
            double envelope = Math.Pow(1 - t, 2.4) * Math.Min(t * 50, 1);
            double sample = (Math.Sin(phase) * (1 - noise) + rng.RandfRange(-1, 1) * noise) * envelope * 0.5;
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2, 2), (short)(sample * 32767));
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Data = data };
    }
}
