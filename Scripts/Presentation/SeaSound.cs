using Godot;
using IslandGlow.Core;
using System;

namespace IslandGlow.Presentation;

/// <summary>Small original procedural ambience and cues; no network or external media dependency.</summary>
public partial class SeaSound : Node
{
    public bool Enabled { get; private set; } = true;
    private AudioStreamPlayer _water = null!, _cue = null!;
    private readonly Random _random = new(741);
    private ulong _lastCue;

    public override void _Ready()
    {
        var settings = new ConfigFile();
        if (settings.Load("user://preferences.cfg") == Error.Ok) Enabled = settings.GetValue("audio", "enabled", true).AsBool();
        _water = new AudioStreamPlayer { Stream = Water(), VolumeDb = -20 };
        _cue = new AudioStreamPlayer { VolumeDb = -15, MaxPolyphony = 4 };
        AddChild(_water); AddChild(_cue); _water.Play();
        Apply();
    }

    public void Toggle()
    {
        Enabled = !Enabled; Apply();
        var settings = new ConfigFile(); settings.SetValue("audio", "enabled", Enabled); settings.Save("user://preferences.cfg");
    }

    private void Apply() { _water.StreamPaused = !Enabled; if (!Enabled) _cue.Stop(); }

    public void Cue(bool heavy = false)
    {
        if (!Enabled || Time.GetTicksMsec() - _lastCue < 90) return;
        _lastCue = Time.GetTicksMsec();
        int samples = heavy ? 6600 : 1800; var data = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            double t = i / 22050.0, decay = Math.Exp(-t * (heavy ? 14 : 65));
            double value = heavy ? ((_random.NextDouble() * 2 - 1) * 0.35 + Math.Sin(t * 2 * Math.PI * 68) * 0.65) * decay : Math.Sin(t * 2 * Math.PI * 410) * decay * 0.3;
            Sample(data, i, value);
        }
        _cue.Stream = new AudioStreamWav { Data = data, Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = 22050 };
        _cue.Play();
    }

    private AudioStreamWav Water()
    {
        const int count = 22050 * 24; var data = new byte[count * 2]; double slow = 0, fast = 0;
        for (int i = 0; i < count; i++)
        {
            double noise = _random.NextDouble() * 2 - 1;
            slow += (noise - slow) * 0.016; fast += (noise - fast) * 0.13;
            double t = i / 22050.0;
            double swell = 0.30 + Math.Pow(0.5 + 0.5 * Math.Sin(t * Math.PI / 3), 2) * 0.55;
            double seam = Math.Min(1, Math.Min(i, count - i - 1) / 2205.0);
            Sample(data, i, (slow * 3 + fast * 0.2) * swell * seam);
        }
        return new AudioStreamWav { Data = data, Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = 22050,
            LoopMode = AudioStreamWav.LoopModeEnum.Forward, LoopBegin = 0, LoopEnd = count };
    }

    private static void Sample(byte[] bytes, int index, double value)
    {
        short sample = (short)(Math.Clamp(value, -1, 1) * 26000);
        bytes[index * 2] = (byte)(sample & 255); bytes[index * 2 + 1] = (byte)(sample >> 8);
    }
}
