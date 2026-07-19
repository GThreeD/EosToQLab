# ESF3D compatibility fixtures

The unit tests never require a running ETC Eos installation.

- `known-3.3.5_Build_69` is a deterministic synthetic archive that covers every currently supported cue-header and
  continuation encoding.
- Real exports from a new Eos release should be added as a new versioned directory together with an `expected.json`
  contract.
- Keep real fixtures minimal: one cue for label, notes and scene text, plus one cue for every Follow/Hang
  representation. Do not include production show data.
- A fixture is immutable after it has been committed. Parser changes add a new fixture version rather than silently
  replacing an old one.

This creates a compatibility corpus: CI checks old encodings forever, while a newly exported fixture is the only manual
step when ETC changes the proprietary format.

## EOS 3.3.5 Build 69 cue-901 regression corpus

The base fixture contains seven real cues plus unrelated effect/reference records that reuse the numbers 901, 902, and

903. Two additional exports add a real cue 901, once without a label and once with label `test`.

The parser must identify the complete cue-record trailer instead of accepting the number marker alone. It must also stop
the final cue at its actual record boundary, so later global effect text is never attached as cue scene text.
