"""
Sun Switch — Audio Asset Synthesizer
Generates 16-bit 44.1 kHz PCM WAV audio files for SFX and Music.
Standard library only (wave, struct, math, random).
"""

import math
import os
import random
import struct
import wave

SAMPLE_RATE = 44100

def write_wav(path, samples, sample_rate=SAMPLE_RATE, channels=1):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, 'w') as wf:
        wf.setnchannels(channels)
        wf.setsampwidth(2)  # 16-bit
        wf.setframerate(sample_rate)
        
        # Clip and pack
        packed = bytearray()
        for s in samples:
            val = max(-1.0, min(1.0, s))
            int_val = int(val * 32767.0)
            packed.extend(struct.pack('<h', int_val))
        wf.writeframes(packed)
    print(f"Generated: {path} ({len(samples)/sample_rate:.2f}s)")

def make_chime(freq, t, decay=4.0):
    # Rich bell/chime with harmonic and inharmonic partials
    partials = [
        (1.0, 1.0, decay),
        (2.0, 0.5, decay * 1.3),
        (2.76, 0.35, decay * 1.5),
        (3.0, 0.25, decay * 1.8),
        (4.15, 0.15, decay * 2.2),
        (5.43, 0.10, decay * 2.5),
    ]
    val = 0.0
    for mult, amp, dec in partials:
        val += amp * math.sin(2.0 * math.pi * freq * mult * t) * math.exp(-dec * t)
    return val

def gen_switch_day(path):
    # High celestial chime: C6 (1046.5 Hz) -> E6 (1318.5 Hz) -> G6 (1568 Hz) -> C7 (2093 Hz)
    dur = 0.65
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    chimes = [
        (0.00, 1046.50, 0.6),
        (0.04, 1318.51, 0.65),
        (0.08, 1567.98, 0.7),
        (0.12, 2093.00, 0.8),
    ]
    
    for offset_sec, freq, amp in chimes:
        start_idx = int(offset_sec * SAMPLE_RATE)
        for i in range(start_idx, n_samples):
            t = (i - start_idx) / SAMPLE_RATE
            env = math.exp(-4.5 * t)
            samples[i] += amp * make_chime(freq, t, decay=4.2)
            
    # Normalize
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_switch_night(path):
    # Low celestial hum: Tibetan singing bowl / deep twilight drone (A2 110Hz, E3 165Hz, A3 220Hz)
    dur = 0.85
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Attack envelope
        attack = min(1.0, t / 0.08)
        env = attack * math.exp(-2.5 * t)
        
        # Slow 3.5Hz tremolo/beating
        tremolo = 1.0 + 0.15 * math.sin(2.0 * math.pi * 3.5 * t)
        
        v = (
            0.60 * math.sin(2.0 * math.pi * 110.0 * t) +
            0.35 * math.sin(2.0 * math.pi * 164.81 * t) +
            0.25 * math.sin(2.0 * math.pi * 220.0 * t) +
            0.12 * math.sin(2.0 * math.pi * 330.0 * t) +
            0.08 * math.sin(2.0 * math.pi * 440.0 * t)
        ) * env * tremolo
        samples[i] = v
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_switch_blocked(path):
    # Buzz SFX: dissonant wooden buzz / warning buzz
    dur = 0.22
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        env = math.exp(-12.0 * t)
        # Low frequency buzz with odd harmonics (square/saw)
        f = 115.0 - 25.0 * t
        s1 = math.sin(2.0 * math.pi * f * t)
        s2 = 0.5 * math.sin(2.0 * math.pi * 2.0 * f * t)
        s3 = 0.35 * math.sin(2.0 * math.pi * 3.0 * f * t)
        # Detuned dissonant tone
        dissonance = 0.4 * math.sin(2.0 * math.pi * 123.0 * t)
        samples[i] = (s1 + s2 + s3 + dissonance) * env
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_jump(path):
    # Light liftoff leap: airy upward whoosh + subtle cloth flutter
    dur = 0.22
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    phase = 0.0
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        env = math.sin(math.pi * min(1.0, t / dur)) ** 1.5
        # Pitch rises from 200 Hz to 480 Hz
        freq = 200.0 + 280.0 * (t / dur)
        phase += 2.0 * math.pi * freq / SAMPLE_RATE
        
        # Soft tone + soft noise
        noise = (random.random() * 2.0 - 1.0) * 0.3
        samples[i] = (math.sin(phase) * 0.7 + noise) * env
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.75 for s in samples]
    write_wav(path, samples)

def gen_jump_pad(path):
    # Springy magical launch bounce: fast rising swoop (160 to 750 Hz) + resonance
    dur = 0.38
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    phase = 0.0
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        progress = t / dur
        env = math.exp(-4.5 * progress)
        freq = 160.0 + 590.0 * (progress ** 0.6)
        phase += 2.0 * math.pi * freq / SAMPLE_RATE
        
        # Resonant pop harmonic
        pop = math.sin(2.0 * math.pi * 80.0 * t) * math.exp(-25.0 * t) * 0.5
        swoop = math.sin(phase) * 0.75 + 0.25 * math.sin(2.0 * phase)
        samples[i] = (swoop * env) + pop
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_death(path):
    # Void dissolve / fall drop: descending eerie resonance into sub drop
    dur = 0.65
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    phase1, phase2 = 0.0, 0.0
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        progress = t / dur
        env = (1.0 - progress) ** 1.2
        
        f1 = 440.0 * math.exp(-3.5 * progress)
        f2 = 220.0 * math.exp(-3.5 * progress)
        phase1 += 2.0 * math.pi * f1 / SAMPLE_RATE
        phase2 += 2.0 * math.pi * f2 / SAMPLE_RATE
        
        noise = (random.random() * 2.0 - 1.0) * 0.25 * env
        tone = (math.sin(phase1) * 0.5 + math.sin(phase2) * 0.5) * env
        # Sub thump at start
        sub = math.sin(2.0 * math.pi * 65.0 * t) * math.exp(-10.0 * t) * 0.4
        
        samples[i] = tone + noise + sub
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_checkpoint(path):
    # Uplifting lantern bell: 2 ascending bell chimes (G5 784Hz -> C6 1046.5Hz)
    dur = 0.85
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    notes = [
        (0.00, 783.99, 0.65),
        (0.14, 1046.50, 0.85),
    ]
    for start_t, freq, amp in notes:
        start_idx = int(start_t * SAMPLE_RATE)
        for i in range(start_idx, n_samples):
            t = (i - start_idx) / SAMPLE_RATE
            samples[i] += amp * make_chime(freq, t, decay=3.8)
            
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_shard(path):
    # Sparkling crystal ping: fast 3-note sparkle arpeggio (E6 1318Hz -> A6 1760Hz -> E7 2637Hz)
    dur = 0.50
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    notes = [
        (0.00, 1318.51, 0.55),
        (0.05, 1760.00, 0.65),
        (0.10, 2637.02, 0.85),
    ]
    for start_t, freq, amp in notes:
        start_idx = int(start_t * SAMPLE_RATE)
        for i in range(start_idx, n_samples):
            t = (i - start_idx) / SAMPLE_RATE
            samples[i] += amp * make_chime(freq, t, decay=6.0)
            
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_ui_click(path):
    # Parchment / wood stamp click: ultra tactile, short (0.06s)
    dur = 0.06
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        env = math.exp(-80.0 * t)
        snap = math.sin(2.0 * math.pi * 1800.0 * t) * math.exp(-120.0 * t) * 0.6
        body = math.sin(2.0 * math.pi * 380.0 * t) * env * 0.7
        samples[i] = snap + body
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.80 for s in samples]
    write_wav(path, samples)

def gen_game_over(path):
    # Somber descending 3-note minor bell chime: E4 -> C4 -> A3
    dur = 1.8
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    notes = [
        (0.00, 329.63, 0.7),
        (0.35, 261.63, 0.75),
        (0.70, 220.00, 0.85),
    ]
    for start_t, freq, amp in notes:
        start_idx = int(start_t * SAMPLE_RATE)
        for i in range(start_idx, n_samples):
            t = (i - start_idx) / SAMPLE_RATE
            samples[i] += amp * make_chime(freq, t, decay=2.4)
            
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

def gen_win(path):
    # Grand celestial victory fanfare: C4 -> G4 -> C5 -> E5 -> G5 -> C6 radiant chord
    dur = 2.8
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    arpeggio = [
        (0.00, 261.63, 0.5),   # C4
        (0.12, 392.00, 0.55),  # G4
        (0.24, 523.25, 0.65),  # C5
        (0.36, 659.25, 0.75),  # E5
        (0.48, 783.99, 0.8),   # G5
        (0.60, 1046.50, 0.9),  # C6
    ]
    for start_t, freq, amp in arpeggio:
        start_idx = int(start_t * SAMPLE_RATE)
        for i in range(start_idx, n_samples):
            t = (i - start_idx) / SAMPLE_RATE
            samples[i] += amp * make_chime(freq, t, decay=1.8)
            
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.85 for s in samples]
    write_wav(path, samples)

# ----------------- MUSIC SYNTHESIS -----------------
# Seamless loops: Tempo 108 BPM (1 beat = 60/108 = 0.55555...s)
# 4 bars of 4/4 = 16 beats = 8.8888...s (or 8 bars = 17.7777...s)
# Exact sample match for seamless cross-fading!

BEAT_SEC = 60.0 / 108.0
LOOP_BEATS = 16
LOOP_DUR = BEAT_SEC * LOOP_BEATS
LOOP_SAMPLES = int(LOOP_DUR * SAMPLE_RATE)

def pluck_string(freq, t, decay=3.2):
    return (
        math.sin(2.0 * math.pi * freq * t) * 0.65 +
        math.sin(4.0 * math.pi * freq * t) * 0.25 +
        math.sin(6.0 * math.pi * freq * t) * 0.10
    ) * math.exp(-decay * t)

def gen_music_day(path):
    # Warm, buoyant acoustic/celestial daytime loop
    samples = [0.0] * LOOP_SAMPLES
    
    # Chord progression: C - G - Am - F (4 beats each)
    # Melodic notes in C Major
    # Beat positions (0 to 15)
    arp_notes = [
        # Bar 1 (C Major: C4, E4, G4, C5)
        (0.0, 261.63), (0.5, 329.63), (1.0, 392.00), (1.5, 523.25),
        (2.0, 392.00), (2.5, 329.63), (3.0, 523.25), (3.5, 392.00),
        # Bar 2 (G Major: G3, B3, D4, G4)
        (4.0, 196.00), (4.5, 246.94), (5.0, 293.66), (5.5, 392.00),
        (6.0, 293.66), (6.5, 246.94), (7.0, 392.00), (7.5, 329.63),
        # Bar 3 (Am: A3, C4, E4, A4)
        (8.0, 220.00), (8.5, 261.63), (9.0, 329.63), (9.5, 440.00),
        (10.0, 329.63), (10.5, 261.63), (11.0, 440.00), (11.5, 392.00),
        # Bar 4 (F Major: F3, A3, C4, F4)
        (12.0, 174.61), (12.5, 220.00), (13.0, 261.63), (13.5, 349.23),
        (14.0, 261.63), (14.5, 329.63), (15.0, 392.00), (15.5, 293.66),
    ]
    
    for beat, freq in arp_notes:
        start_t = beat * BEAT_SEC
        start_i = int(start_t * SAMPLE_RATE)
        dur_note = BEAT_SEC * 1.5
        n_note = int(dur_note * SAMPLE_RATE)
        for i in range(n_note):
            idx = (start_i + i) % LOOP_SAMPLES
            t = i / SAMPLE_RATE
            samples[idx] += 0.35 * pluck_string(freq, t, decay=3.0)
            
    # Warm bass notes
    bass = [
        (0.0, 130.81),   # C3
        (4.0, 98.00),    # G2
        (8.0, 110.00),   # A2
        (12.0, 87.31),   # F2
    ]
    for beat, freq in bass:
        start_t = beat * BEAT_SEC
        start_i = int(start_t * SAMPLE_RATE)
        dur_note = BEAT_SEC * 3.8
        n_note = int(dur_note * SAMPLE_RATE)
        for i in range(n_note):
            idx = (start_i + i) % LOOP_SAMPLES
            t = i / SAMPLE_RATE
            env = math.exp(-1.5 * t)
            samples[idx] += 0.30 * math.sin(2.0 * math.pi * freq * t) * env
            
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.75 for s in samples]
    write_wav(path, samples)

def gen_music_night(path):
    # Twilight counterpart: synchronized counterpoint (same chord base, ethereal ambient pads and kalimba)
    samples = [0.0] * LOOP_SAMPLES
    
    # Atmospheric pad chords (C - G - Am - F)
    chords = [
        (0.0, [261.63, 329.63, 392.00]),   # C
        (4.0, [196.00, 246.94, 293.66]),   # G
        (8.0, [220.00, 261.63, 329.63]),   # Am
        (12.0, [174.61, 220.00, 261.63]),  # F
    ]
    for beat, chord_freqs in chords:
        start_t = beat * BEAT_SEC
        start_i = int(start_t * SAMPLE_RATE)
        dur_chord = BEAT_SEC * 4.0
        n_chord = int(dur_chord * SAMPLE_RATE)
        for i in range(n_chord):
            idx = (start_i + i) % LOOP_SAMPLES
            t = i / SAMPLE_RATE
            # Slow swelling envelope
            env = math.sin(math.pi * min(1.0, t / dur_chord))
            chord_sum = sum(math.sin(2.0 * math.pi * f * t) for f in chord_freqs)
            samples[idx] += 0.22 * chord_sum * env
            
    # Night star bell / kalimba pings (interleaved counterpoint)
    star_notes = [
        (0.5, 1046.50), (1.5, 1318.51), (2.5, 1567.98),
        (4.5, 987.77),  (5.5, 1174.66), (6.5, 1318.51),
        (8.5, 880.00),  (9.5, 1046.50), (10.5, 1318.51),
        (12.5, 698.46), (13.5, 880.00), (14.5, 1046.50),
    ]
    for beat, freq in star_notes:
        start_t = beat * BEAT_SEC
        start_i = int(start_t * SAMPLE_RATE)
        dur_note = BEAT_SEC * 2.0
        n_note = int(dur_note * SAMPLE_RATE)
        for i in range(n_note):
            idx = (start_i + i) % LOOP_SAMPLES
            t = i / SAMPLE_RATE
            samples[idx] += 0.25 * make_chime(freq, t, decay=4.5)
            
    # Deep sub drone (A1 / C2 65.4 Hz)
    for i in range(LOOP_SAMPLES):
        t = i / SAMPLE_RATE
        drone = (
            0.20 * math.sin(2.0 * math.pi * 65.4 * t) +
            0.15 * math.sin(2.0 * math.pi * 130.8 * t)
        )
        samples[i] += drone
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.75 for s in samples]
    write_wav(path, samples)

def gen_music_menu(path):
    # Gentle storybook music box / celesta intro loop
    # Tempo 90 BPM (1 beat = 0.6666...s)
    menu_beat = 60.0 / 90.0
    menu_beats = 16
    menu_dur = menu_beat * menu_beats
    menu_samples = int(menu_dur * SAMPLE_RATE)
    samples = [0.0] * menu_samples
    
    # Storybook lullaby melody on celesta
    melody = [
        (0.0, 523.25),  # C5
        (1.0, 659.25),  # E5
        (2.0, 783.99),  # G5
        (3.0, 659.25),  # E5
        (4.0, 587.33),  # D5
        (5.0, 659.25),  # E5
        (6.0, 783.99),  # G5
        (7.0, 987.77),  # B5
        (8.0, 880.00),  # A5
        (9.0, 783.99),  # G5
        (10.0, 659.25), # E5
        (11.0, 523.25), # C5
        (12.0, 587.33), # D5
        (13.0, 783.99), # G5
        (14.0, 523.25), # C5
    ]
    for beat, freq in melody:
        start_t = beat * menu_beat
        start_i = int(start_t * SAMPLE_RATE)
        dur_note = menu_beat * 2.5
        n_note = int(dur_note * SAMPLE_RATE)
        for i in range(n_note):
            idx = (start_i + i) % menu_samples
            t = i / SAMPLE_RATE
            samples[idx] += 0.40 * make_chime(freq, t, decay=3.5)
            
    # Soft warm background hum
    for i in range(menu_samples):
        t = i / SAMPLE_RATE
        warmth = (
            0.15 * math.sin(2.0 * math.pi * 130.81 * t) +
            0.10 * math.sin(2.0 * math.pi * 196.00 * t)
        ) * (1.0 + 0.1 * math.sin(2.0 * math.pi * 0.5 * t))
        samples[i] += warmth
        
    max_val = max(abs(s) for s in samples) or 1.0
    samples = [s / max_val * 0.70 for s in samples]
    write_wav(path, samples)

def gen_meta_files():
    import hashlib
    base_dir = os.path.dirname(os.path.abspath(__file__))
    project_dir = os.path.dirname(base_dir)
    audio_root = os.path.join(project_dir, "Assets", "Art", "Audio")
    
    template_folder = "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    template_audio = "fileFormatVersion: 2\nguid: {guid}\nAudioImporter:\n  externalObjects: {}\n  serializedVersion: 6\n  defaultSettings:\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 0\n    quality: 1\n    conversionMode: 0\n  platformSettingOverrides: {}\n  forceToMono: 0\n  normalize: 1\n  preloadAudioData: 1\n  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    
    root_meta = audio_root + ".meta"
    if not os.path.exists(root_meta):
        g = hashlib.md5(b"Assets/Art/Audio").hexdigest()
        with open(root_meta, "w", encoding="utf-8") as f:
            f.write(template_folder.replace("{guid}", g))
            
    for root, dirs, files in os.walk(audio_root):
        for d in dirs:
            p = os.path.join(root, d)
            meta = p + ".meta"
            if not os.path.exists(meta):
                rel = os.path.relpath(p, project_dir).replace("\\", "/")
                g = hashlib.md5(rel.encode()).hexdigest()
                with open(meta, "w", encoding="utf-8") as f:
                    f.write(template_folder.replace("{guid}", g))
        for file in files:
            if file.endswith(".wav"):
                p = os.path.join(root, file)
                meta = p + ".meta"
                if not os.path.exists(meta):
                    rel = os.path.relpath(p, project_dir).replace("\\", "/")
                    g = hashlib.md5(rel.encode()).hexdigest()
                    with open(meta, "w", encoding="utf-8") as f:
                        f.write(template_audio.replace("{guid}", g))
    print("Meta files generated successfully.")

def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    project_dir = os.path.dirname(base_dir)
    sfx_dir = os.path.join(project_dir, "Assets", "Art", "Audio", "SFX")
    music_dir = os.path.join(project_dir, "Assets", "Art", "Audio", "Music")
    
    print("Generating SFX...")
    gen_switch_day(os.path.join(sfx_dir, "SwitchDay.wav"))
    gen_switch_night(os.path.join(sfx_dir, "SwitchNight.wav"))
    gen_switch_blocked(os.path.join(sfx_dir, "SwitchBlocked.wav"))
    gen_jump(os.path.join(sfx_dir, "Jump.wav"))
    gen_jump_pad(os.path.join(sfx_dir, "JumpPad.wav"))
    gen_death(os.path.join(sfx_dir, "Death.wav"))
    gen_checkpoint(os.path.join(sfx_dir, "Checkpoint.wav"))
    gen_shard(os.path.join(sfx_dir, "Shard.wav"))
    gen_ui_click(os.path.join(sfx_dir, "UIClick.wav"))
    gen_game_over(os.path.join(sfx_dir, "GameOver.wav"))
    gen_win(os.path.join(sfx_dir, "Win.wav"))
    
    print("Generating Music...")
    gen_music_day(os.path.join(music_dir, "Music_Day.wav"))
    gen_music_night(os.path.join(music_dir, "Music_Night.wav"))
    gen_music_menu(os.path.join(music_dir, "Music_Menu.wav"))
    
    print("Generating Meta files...")
    gen_meta_files()
    print("All audio files and meta files generated successfully!")

if __name__ == "__main__":
    main()
