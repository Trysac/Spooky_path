"""Sound effect recipes for Spooky Path. Each function returns a rendered Track."""

from synth import Track, note_freq, saw, square, triangle


def pulse(duty):
    return lambda p: square(p, duty)


def click():
    t = Track(0.08)
    t.add_tone(0, 0.025, 1100, pulse(0.5), 0.18, freq_end=800, release=0.02)
    return t


def denied():
    """A short low buzz for an action that is not allowed."""
    t = Track(0.2)
    t.add_tone(0, 0.12, 140, pulse(0.5), 0.15, release=0.04)
    return t


def jump():
    t = Track(0.2)
    t.add_tone(0, 0.1, 260, triangle, 0.3, freq_end=520, release=0.05)
    t.add_noise(0, 0.06, 0.08, smoothing=0.5, decay_power=2, seed=11)
    return t


def land():
    t = Track(0.15)
    t.add_noise(0, 0.08, 0.3, smoothing=0.8, decay_power=2, seed=12)
    t.add_tone(0, 0.05, 120, triangle, 0.3, freq_end=60, release=0.03)
    return t


def swing():
    """The explorer's staff cutting the air."""
    t = Track(0.25)
    t.add_noise(0, 0.18, 0.4, smoothing=0.6, decay_power=0.7, seed=13)
    t.add_tone(0, 0.12, 380, triangle, 0.1, freq_end=180, release=0.04)
    return t


def enemy_swing():
    """A rusty blade swung by a skeleton."""
    t = Track(0.3)
    t.add_noise(0, 0.2, 0.35, smoothing=0.45, decay_power=0.9, seed=14)
    t.add_tone(0.02, 0.15, 900, pulse(0.125), 0.05, freq_end=500, release=0.05)
    return t


def hit():
    """A blow lands on dry bone."""
    t = Track(0.18)
    t.add_noise(0, 0.05, 0.45, smoothing=0.2, decay_power=2.4, seed=15)
    t.add_tone(0, 0.06, 700, square, 0.12, freq_end=300, release=0.03)
    t.add_tone(0, 0.08, 180, triangle, 0.3, freq_end=90, release=0.03)
    return t


def bones():
    """A skeleton falls apart: a clatter of bones."""
    t = Track(0.8)
    for i, at in enumerate([0.0, 0.07, 0.12, 0.2, 0.27, 0.36, 0.48]):
        t.add_noise(at, 0.04, 0.35 - i * 0.03, smoothing=0.15, decay_power=2.5, seed=16 + i)
        t.add_tone(at, 0.03, 900 - i * 70, triangle, 0.18, freq_end=500 - i * 30, release=0.02)
    t.add_tone(0, 0.4, 160, saw, 0.12, freq_end=60, release=0.15, vibrato=0.03)
    return t


def player_hurt():
    t = Track(0.4)
    t.add_tone(0, 0.22, 420, pulse(0.5), 0.2, freq_end=120, release=0.06)
    t.add_noise(0, 0.12, 0.3, smoothing=0.3, decay_power=1.5, seed=24)
    return t


def block():
    """A blow glances off the Bone Ward."""
    t = Track(0.3)
    t.add_tone(0, 0.05, note_freq("E6"), triangle, 0.25, attack=0.002, decay=0.05, sustain=0.4, release=0.2)
    t.add_tone(0, 0.05, note_freq("B6"), triangle, 0.12, attack=0.002, decay=0.05, sustain=0.3, release=0.2)
    return t


def firebolt():
    t = Track(0.5)
    t.add_noise(0, 0.35, 0.4, smoothing=0.7, decay_power=0.8, seed=25)
    t.add_tone(0, 0.25, 220, saw, 0.12, freq_end=660, release=0.1)
    return t


def frost():
    """Frost Nova: a crystalline shimmer over a cold rush."""
    t = Track(1.0)
    t.add_noise(0, 0.7, 0.3, smoothing=0.1, decay_power=1.2, seed=26)
    for i, name in enumerate(["B6", "F#6", "D7", "A6", "E7"]):
        t.add_tone(i * 0.05, 0.08, note_freq(name), triangle, 0.12, attack=0.002, release=0.35)
    return t


def heal():
    """Mend: a warm rising chord."""
    t = Track(1.0)
    for i, name in enumerate(["C5", "E5", "G5", "C6"]):
        t.add_tone(i * 0.08, 0.3, note_freq(name), triangle, 0.25, release=0.3, vibrato=0.004)
    return t


def shield():
    """Bone Ward: a hollow chime that rings out."""
    t = Track(1.2)
    for freq, volume in [(note_freq("A4"), 0.3), (note_freq("E5") * 1.003, 0.18), (note_freq("A5") * 0.997, 0.1)]:
        t.add_tone(0, 0.08, freq, triangle, volume, attack=0.003, decay=0.1, sustain=0.5, release=0.9)
    return t


def curse():
    """A skeleton mage throws a cursed bolt."""
    t = Track(0.45)
    t.add_tone(0, 0.3, 520, pulse(0.125), 0.14, freq_end=160, release=0.08, vibrato=0.05)
    t.add_noise(0, 0.2, 0.15, smoothing=0.7, decay_power=1.2, seed=27)
    return t


def coin():
    t = Track(0.25)
    t.add_tone(0, 0.05, note_freq("B5"), pulse(0.25), 0.14, release=0.03)
    t.add_tone(0.05, 0.12, note_freq("E6"), pulse(0.25), 0.14, release=0.08)
    return t


def candy():
    """A candy corn: a sweet little three-note skip."""
    t = Track(0.35)
    for i, name in enumerate(["G5", "B5", "D6"]):
        t.add_tone(i * 0.05, 0.06, note_freq(name), triangle, 0.25, release=0.08)
    return t


def key():
    t = Track(0.6)
    for i, name in enumerate(["E5", "G#5", "B5", "E6"]):
        t.add_tone(i * 0.07, 0.1, note_freq(name), pulse(0.25), 0.14, release=0.12)
    t.add_noise(0, 0.04, 0.1, smoothing=0.1, decay_power=2, seed=28)
    return t


def unlock():
    """A key turns and a heavy door swings."""
    t = Track(1.2)
    t.add_noise(0, 0.05, 0.4, smoothing=0.2, decay_power=2, seed=29)
    t.add_noise(0.12, 0.04, 0.35, smoothing=0.2, decay_power=2, seed=30)
    t.add_tone(0.3, 0.8, 90, saw, 0.12, freq_end=70, attack=0.1, release=0.2, vibrato=0.04)
    t.add_noise(0.3, 0.8, 0.12, smoothing=0.9, decay_power=0.6, seed=31)
    return t


def lever():
    t = Track(0.5)
    t.add_noise(0, 0.15, 0.25, smoothing=0.85, decay_power=0.8, seed=32)
    t.add_noise(0.18, 0.04, 0.4, smoothing=0.2, decay_power=2, seed=33)
    t.add_tone(0.18, 0.06, 240, triangle, 0.3, freq_end=120, release=0.04)
    return t


def plate():
    """A pressure plate sinks with a stony click."""
    t = Track(0.4)
    t.add_tone(0, 0.12, 160, triangle, 0.4, freq_end=90, release=0.06)
    t.add_noise(0, 0.06, 0.3, smoothing=0.5, decay_power=2, seed=34)
    t.add_tone(0.1, 0.05, 1200, triangle, 0.08, release=0.1)
    return t


def gate():
    """A gate grinds open."""
    t = Track(1.6)
    t.add_noise(0, 1.3, 0.3, smoothing=0.93, decay_power=0.5, seed=35)
    t.add_tone(0, 1.2, 70, saw, 0.14, freq_end=95, attack=0.2, release=0.3, vibrato=0.06)
    t.add_noise(1.25, 0.08, 0.45, smoothing=0.6, decay_power=2, seed=36)
    return t


def treasure():
    t = Track(1.0)
    for i, name in enumerate(["C5", "E5", "G5", "C6", "E6", "G6"]):
        t.add_tone(i * 0.06, 0.12 if i < 5 else 0.5, note_freq(name), pulse(0.25), 0.13, release=0.15)
    return t


def potion():
    """A gulp and a bubbling sparkle."""
    t = Track(0.6)
    for i in range(4):
        t.add_tone(i * 0.07, 0.05, 300 + i * 120, triangle, 0.25, freq_end=500 + i * 160, release=0.03)
    t.add_tone(0.3, 0.2, note_freq("A5"), triangle, 0.15, release=0.2)
    return t


def purify():
    """A dark brazier is purified: a rising, cleansing chord."""
    t = Track(1.6)
    t.add_noise(0, 0.5, 0.25, smoothing=0.4, decay_power=1, seed=37)
    for i, name in enumerate(["D4", "A4", "D5", "F#5", "A5"]):
        t.add_tone(i * 0.08, 0.6, note_freq(name), triangle, 0.18, release=0.6, vibrato=0.004)
    return t


def ward_break():
    """The artefact's ward shatters."""
    t = Track(1.8)
    t.add_noise(0, 0.9, 0.5, smoothing=0.3, decay_power=1.2, seed=38)
    for i, name in enumerate(["E7", "C7", "G6", "D#6", "B5"]):
        t.add_tone(i * 0.06, 0.1, note_freq(name), triangle, 0.15, attack=0.002, release=0.5)
    t.add_tone(0, 1.0, 55, saw, 0.2, freq_end=40, release=0.4)
    return t


def artefact_hit():
    t = Track(0.4)
    t.add_tone(0, 0.08, note_freq("C#6"), triangle, 0.25, attack=0.002, release=0.25)
    t.add_tone(0, 0.08, note_freq("G6"), triangle, 0.12, attack=0.002, release=0.25)
    t.add_noise(0, 0.06, 0.3, smoothing=0.2, decay_power=2, seed=39)
    return t


def shatter():
    """The artefact explodes."""
    t = Track(2.6)
    t.add_noise(0, 1.6, 0.8, smoothing=0.85, decay_power=1.1, seed=40)
    t.add_tone(0, 1.2, 80, triangle, 0.7, freq_end=30, release=0.5)
    for i in range(10):
        t.add_tone(0.05 + i * 0.07, 0.05, 2400 - i * 150, triangle, 0.12, attack=0.002, release=0.3)
    return t


def teleport():
    t = Track(0.8)
    t.add_tone(0, 0.5, 200, pulse(0.25), 0.12, freq_end=1600, release=0.2, vibrato=0.03)
    t.add_tone(0.05, 0.5, 300, triangle, 0.15, freq_end=2000, release=0.2)
    return t


def burn():
    """Falling into lava: a sizzling roar."""
    t = Track(1.4)
    t.add_noise(0, 1.2, 0.6, smoothing=0.5, decay_power=0.9, seed=41)
    t.add_tone(0, 0.6, 240, saw, 0.12, freq_end=60, release=0.3)
    return t


def summon():
    """The artefact raises a skeleton: a low, rising moan."""
    t = Track(1.4)
    t.add_tone(0, 1.0, 70, saw, 0.2, freq_end=140, attack=0.3, release=0.3, vibrato=0.05)
    t.add_tone(0, 1.0, 74, saw, 0.15, freq_end=150, attack=0.3, release=0.3, vibrato=0.05)
    t.add_noise(0, 1.0, 0.15, smoothing=0.95, decay_power=0.5, seed=42)
    return t


def level_complete():
    t = Track(1.6)
    for name, at, length in [("D5", 0.0, 0.12), ("F#5", 0.13, 0.12), ("A5", 0.26, 0.12), ("D6", 0.39, 0.7)]:
        t.add_tone(at, length, note_freq(name), pulse(0.25), 0.2, release=0.2)
    t.add_tone(0.39, 0.7, note_freq("D3"), triangle, 0.4, release=0.3)
    return t


def victory():
    t = Track(3.2)
    melody = [("C5", 0.0, 0.15), ("E5", 0.16, 0.15), ("G5", 0.32, 0.15), ("C6", 0.48, 0.45),
              ("A5", 0.96, 0.15), ("B5", 1.12, 0.15), ("C6", 1.28, 0.15), ("E6", 1.44, 1.2)]
    for name, at, length in melody:
        t.add_tone(at, length, note_freq(name), pulse(0.25), 0.18, release=0.25, vibrato=0.004)
    for name, at in [("C3", 0.0), ("F3", 0.96), ("G3", 1.12), ("C3", 1.44)]:
        t.add_tone(at, 0.4 if at < 1.4 else 1.2, note_freq(name), triangle, 0.4, release=0.3)
    return t


def game_over():
    t = Track(2.4)
    for name, at, length in [("E4", 0.0, 0.3), ("D#4", 0.35, 0.3), ("D4", 0.7, 0.3), ("C#4", 1.05, 0.9)]:
        t.add_tone(at, length, note_freq(name), triangle, 0.35, release=0.25, vibrato=0.008)
    t.add_tone(1.05, 0.9, note_freq("A1"), triangle, 0.45, release=0.4)
    return t


EFFECTS = {f"{name}.ogg": recipe for name, recipe in {
    "click": click, "denied": denied, "jump": jump, "land": land, "swing": swing,
    "enemy_swing": enemy_swing, "hit": hit, "bones": bones, "player_hurt": player_hurt,
    "block": block, "firebolt": firebolt, "frost": frost, "heal": heal, "shield": shield,
    "curse": curse, "coin": coin, "candy": candy, "key": key, "unlock": unlock,
    "lever": lever, "plate": plate, "gate": gate, "treasure": treasure, "potion": potion,
    "purify": purify, "ward_break": ward_break, "artefact_hit": artefact_hit,
    "shatter": shatter, "teleport": teleport, "burn": burn, "summon": summon,
    "level_complete": level_complete, "victory": victory, "game_over": game_over,
}.items()}
