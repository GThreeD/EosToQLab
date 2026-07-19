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
