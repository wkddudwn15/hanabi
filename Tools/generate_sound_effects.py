import math
import os
import struct
import wave


SAMPLE_RATE = 44100
OUTPUT_DIR = os.path.join("Assets", "Audio")


def envelope(t, duration, attack=0.015, release=0.035):
    fade_in = min(1.0, t / attack) if attack > 0 else 1.0
    fade_out = min(1.0, (duration - t) / release) if release > 0 else 1.0
    return max(0.0, min(fade_in, fade_out))


def write_wav(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        frames = bytearray()
        for sample in samples:
            clamped = max(-0.95, min(0.95, sample))
            frames.extend(struct.pack("<h", int(clamped * 32767)))
        wav.writeframes(frames)


def sine(freq, t):
    return math.sin(2.0 * math.pi * freq * t)


def make_launch():
    duration = 0.68
    samples = []
    phase = 0.0
    for i in range(int(SAMPLE_RATE * duration)):
        t = i / SAMPLE_RATE
        progress = t / duration
        freq = 280.0 + 980.0 * (progress ** 1.25)
        phase += 2.0 * math.pi * freq / SAMPLE_RATE
        breath = 0.65 * math.sin(phase) + 0.22 * sine(freq * 1.5, t)
        shimmer = 0.10 * sine(2400.0 + 900.0 * progress, t)
        amp = 0.26 * envelope(t, duration, 0.025, 0.080) * (1.0 - 0.18 * progress)
        samples.append((breath + shimmer) * amp)
    return samples


def tone_sequence(notes, duration, amp=0.34):
    samples = []
    note_duration = duration / len(notes)
    for i in range(int(SAMPLE_RATE * duration)):
        t = i / SAMPLE_RATE
        note_index = min(len(notes) - 1, int(t / note_duration))
        local_t = t - note_duration * note_index
        freq = notes[note_index]
        harmonic = 0.72 * sine(freq, local_t) + 0.18 * sine(freq * 2.0, local_t)
        note_env = envelope(local_t, note_duration, 0.010, 0.035)
        samples.append(harmonic * amp * note_env * envelope(t, duration, 0.006, 0.020))
    return samples


def make_perfect():
    return tone_sequence([880.0, 1174.66, 1567.98], 0.34, 0.34)


def make_good():
    return tone_sequence([659.25, 987.77], 0.26, 0.28)


def make_miss():
    duration = 0.34
    samples = []
    phase = 0.0
    for i in range(int(SAMPLE_RATE * duration)):
        t = i / SAMPLE_RATE
        progress = t / duration
        freq = 240.0 - 70.0 * progress
        phase += 2.0 * math.pi * freq / SAMPLE_RATE
        body = 0.76 * math.sin(phase) + 0.16 * sine(freq * 0.5, t)
        samples.append(body * 0.30 * envelope(t, duration, 0.010, 0.070))
    return samples


def make_result():
    duration = 0.78
    samples = []
    notes = [523.25, 659.25, 783.99, 1046.50]
    note_duration = duration / len(notes)
    for i in range(int(SAMPLE_RATE * duration)):
        t = i / SAMPLE_RATE
        note_index = min(len(notes) - 1, int(t / note_duration))
        local_t = t - note_duration * note_index
        freq = notes[note_index]
        chime = 0.62 * sine(freq, local_t) + 0.20 * sine(freq * 2.0, local_t) + 0.10 * sine(freq * 3.0, local_t)
        tail = 0.55 + 0.45 * (1.0 - t / duration)
        samples.append(chime * 0.30 * envelope(local_t, note_duration, 0.012, 0.060) * envelope(t, duration, 0.008, 0.070) * tail)
    return samples


def main():
    sounds = {
        "launch.wav": make_launch(),
        "perfect.wav": make_perfect(),
        "good.wav": make_good(),
        "miss.wav": make_miss(),
        "result.wav": make_result(),
    }
    for filename, samples in sounds.items():
        write_wav(os.path.join(OUTPUT_DIR, filename), samples)


if __name__ == "__main__":
    main()
