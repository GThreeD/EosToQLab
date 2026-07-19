#!/usr/bin/env python3
"""Loss-tolerant parser for ETC Eos-style ``showdat.dat`` files.

This version keeps two label types separate:

* ``cue_label``: a UTF-16 field that starts immediately after the encoded cue
  number.
* ``scene_label``: a nested UTF-16 field inside the current cue record. In the
  supplied file, this field is introduced by a container byte ``0x02`` directly
  before the text tag ``0x03``.

Recovered primitive encodings used here:

* text: ``0x03 + uint16_le character_count + UTF-16LE``
* unsigned integer: ``0x08 + uint8``, ``0x09 + uint16_le``,
  ``0x0A + uint24_le``
* cue numbers: fixed point with scale 10,000

The complete proprietary object schema is not claimed to be known. Unknown
text inside a cue record is therefore exported separately as ``other_texts``.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import unicodedata
from dataclasses import asdict, dataclass
from decimal import Decimal
from pathlib import Path

TEXT_TAG = 0x03
UINT_WIDTHS = {0x08: 1, 0x09: 2, 0x0A: 3}
CUE_MARKER = b"\x02\x01\x00\x01\x01"
CUE_RECORD_TRAILER = b"\x00\x02\x02\x02\x01\x00\x00\x02"
CUE_SCALE = 10_000

GUID_RE = re.compile(r"^\$?[0-9A-Fa-f]{8}(?:-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12}$")


@dataclass(frozen=True)
class TextField:
    offset: int
    offset_hex: str
    char_count: int
    byte_length: int
    text: str
    confidence: str

    @property
    def end(self) -> int:
        return self.offset + self.byte_length


@dataclass(frozen=True)
class NumberCandidate:
    offset: int
    raw_value: int
    end: int
    record_end: int


@dataclass(frozen=True)
class Cue:
    cue_number: str
    cue_number_raw: int
    number_offset: int
    number_offset_hex: str
    record_start: int
    record_start_hex: str
    record_end: int
    record_end_hex: str
    cue_label: str | None
    cue_label_offset: int | None
    cue_label_offset_hex: str | None
    scene_label: str | None
    scene_label_offset: int | None
    scene_label_offset_hex: str | None
    other_texts: list[str]
    other_text_offsets: list[int]


def printable_ratio(text: str) -> float:
    if not text:
        return 0.0
    good = 0
    for ch in text:
        category = unicodedata.category(ch)
        if ch in "\t\r\n" or category[0] in {"L", "N", "P", "S", "Z"}:
            good += 1
    return good / len(text)


def confidence_for_text(text: str) -> str:
    ascii_ratio = sum(ch in "\t\r\n" or 0x20 <= ord(ch) <= 0x7E for ch in text) / max(len(text), 1)
    ratio = printable_ratio(text)
    if ratio >= 0.98 and ascii_ratio >= 0.90:
        return "high"
    if ratio >= 0.95:
        return "medium"
    return "low"


def extract_text_fields(data: bytes, max_chars: int = 65535) -> list[TextField]:
    candidates: list[TextField] = []
    for offset in range(len(data) - 3):
        if data[offset] != TEXT_TAG:
            continue
        char_count = int.from_bytes(data[offset + 1 : offset + 3], "little")
        if not 1 <= char_count <= max_chars:
            continue
        end = offset + 3 + char_count * 2
        if end > len(data):
            continue
        try:
            text = data[offset + 3 : end].decode("utf-16le", errors="strict")
        except UnicodeDecodeError:
            continue
        if printable_ratio(text) < 0.88:
            continue
        candidates.append(
            TextField(
                offset=offset,
                offset_hex=f"0x{offset:08X}",
                char_count=char_count,
                byte_length=end - offset,
                text=text,
                confidence=confidence_for_text(text),
            )
        )

    # Remove overlapping false positives, preferring longer and cleaner fields.
    ranked = sorted(
        candidates,
        key=lambda field: (field.confidence == "high", field.byte_length),
        reverse=True,
    )
    accepted: list[TextField] = []
    intervals: list[tuple[int, int]] = []
    for field in ranked:
        if any(field.offset < end and field.end > start for start, end in intervals):
            continue
        accepted.append(field)
        intervals.append((field.offset, field.end))
    return sorted(accepted, key=lambda field: field.offset)


def decode_tagged_unsigned(data: bytes, offset: int) -> tuple[int, int] | None:
    if offset >= len(data):
        return None
    width = UINT_WIDTHS.get(data[offset])
    if width is None or offset + 1 + width > len(data):
        return None
    start = offset + 1
    end = start + width
    return int.from_bytes(data[start:end], "little"), end


def scene_value_end(data: bytes, offset: int, boundary: int) -> int | None:
    if offset >= boundary:
        return None
    if data[offset] == 0x00:
        return offset + 1
    if data[offset] != TEXT_TAG or offset + 3 > boundary:
        return None
    char_count = int.from_bytes(data[offset + 1 : offset + 3], "little")
    end = offset + 3 + char_count * 2
    return end if end <= boundary else None


def find_cue_record_end(data: bytes, start: int, boundary: int) -> int | None:
    position = start
    while position < boundary:
        trailer_offset = data.find(CUE_RECORD_TRAILER, position, boundary)
        if trailer_offset < 0:
            return None
        scene_end = scene_value_end(data, trailer_offset + len(CUE_RECORD_TRAILER), boundary)
        if scene_end is not None and scene_end + 3 == boundary and data[scene_end:boundary] == b"\x04\x04\x04":
            return boundary
        position = trailer_offset + 1
    return None


def cue_number_candidates(data: bytes) -> list[NumberCandidate]:
    result: list[NumberCandidate] = []
    position = 0
    while True:
        marker_offset = data.find(CUE_MARKER, position)
        if marker_offset < 0:
            break
        number_offset = marker_offset + len(CUE_MARKER)
        decoded = decode_tagged_unsigned(data, number_offset)
        if decoded is not None:
            value, end = decoded
            if CUE_SCALE <= value <= 99_999_999:
                next_marker = data.find(CUE_MARKER, marker_offset + 1)
                boundary = next_marker if next_marker >= 0 else len(data)
                record_end = find_cue_record_end(data, end, boundary)
                if record_end is not None:
                    result.append(NumberCandidate(number_offset, value, end, record_end))
        position = marker_offset + 1
    return result


def split_monotonic_runs(candidates: list[NumberCandidate]) -> list[list[NumberCandidate]]:
    if not candidates:
        return []
    runs: list[list[NumberCandidate]] = []
    current = [candidates[0]]
    for candidate in candidates[1:]:
        previous = current[-1]
        offset_gap = candidate.offset - previous.offset
        if candidate.raw_value > previous.raw_value and 0 < offset_gap <= 131_072:
            current.append(candidate)
        else:
            runs.append(current)
            current = [candidate]
    runs.append(current)
    return runs


def select_main_cue_run(candidates: list[NumberCandidate]) -> list[NumberCandidate]:
    plausible = [
        run
        for run in split_monotonic_runs(candidates)
        if all(item.raw_value % 10 == 0 for item in run)
    ]
    if not plausible:
        return []
    return max(plausible, key=lambda run: (len(run), run[-1].offset - run[0].offset))


def format_cue_number(raw_value: int) -> str:
    value = Decimal(raw_value) / Decimal(CUE_SCALE)
    return format(value.normalize(), "f")


def useful_record_text(field: TextField) -> bool:
    text = field.text.strip()
    if not text or len(text) > 120:
        return False
    if GUID_RE.fullmatch(text):
        return False
    if len(text) == 1 and not text.isalnum():
        return False
    return True


def extract_cues(data: bytes, fields: list[TextField]) -> list[Cue]:
    all_candidates = cue_number_candidates(data)
    run = select_main_cue_run(all_candidates)
    if not run:
        return []

    result: list[Cue] = []

    for index, number in enumerate(run):
        record_start = number.offset - len(CUE_MARKER)
        record_end = number.record_end

        record_fields = [
            field
            for field in fields
            if record_start <= field.offset < record_end and useful_record_text(field)
        ]

        # Cue label: structurally adjacent to the cue number.
        cue_label_field = next((field for field in record_fields if field.offset == number.end), None)

        # Scene label: nested string field introduced by a container byte 0x02.
        scene_fields = [
            field
            for field in record_fields
            if field.offset > number.end
            and field.offset > 0
            and data[field.offset - 1] == 0x02
            and field is not cue_label_field
        ]
        scene_label_field = scene_fields[0] if scene_fields else None

        excluded_offsets = {
            field.offset
            for field in (cue_label_field, scene_label_field)
            if field is not None
        }
        other_fields = [field for field in record_fields if field.offset not in excluded_offsets]

        result.append(
            Cue(
                cue_number=format_cue_number(number.raw_value),
                cue_number_raw=number.raw_value,
                number_offset=number.offset,
                number_offset_hex=f"0x{number.offset:08X}",
                record_start=record_start,
                record_start_hex=f"0x{record_start:08X}",
                record_end=record_end,
                record_end_hex=f"0x{record_end:08X}",
                cue_label=cue_label_field.text.strip() if cue_label_field else None,
                cue_label_offset=cue_label_field.offset if cue_label_field else None,
                cue_label_offset_hex=cue_label_field.offset_hex if cue_label_field else None,
                scene_label=scene_label_field.text.strip() if scene_label_field else None,
                scene_label_offset=scene_label_field.offset if scene_label_field else None,
                scene_label_offset_hex=scene_label_field.offset_hex if scene_label_field else None,
                other_texts=[field.text.strip() for field in other_fields],
                other_text_offsets=[field.offset for field in other_fields],
            )
        )
    return result


def write_csv(path: Path, cues: list[Cue]) -> None:
    fieldnames = [
        "cue_number",
        "cue_number_raw",
        "number_offset",
        "number_offset_hex",
        "record_start",
        "record_start_hex",
        "record_end",
        "record_end_hex",
        "cue_label",
        "cue_label_offset",
        "cue_label_offset_hex",
        "scene_label",
        "scene_label_offset",
        "scene_label_offset_hex",
        "other_texts",
        "other_text_offsets",
    ]
    with path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        for cue in cues:
            row = asdict(cue)
            row["other_texts"] = json.dumps(row["other_texts"], ensure_ascii=False)
            row["other_text_offsets"] = json.dumps(row["other_text_offsets"])
            writer.writerow(row)


def write_text(path: Path, cues: list[Cue]) -> None:
    lines = ["Cue\tCue label\tScene label\tOther text\tNumber offset"]
    for cue in cues:
        lines.append(
            "\t".join(
                [
                    cue.cue_number,
                    cue.cue_label or "",
                    cue.scene_label or "",
                    " | ".join(cue.other_texts),
                    cue.number_offset_hex,
                ]
            )
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    parser.add_argument("--out-dir", type=Path, default=Path("showdat_output_v3"))
    args = parser.parse_args()

    data = args.input.read_bytes()
    fields = extract_text_fields(data)
    cues = extract_cues(data, fields)
    args.out_dir.mkdir(parents=True, exist_ok=True)

    (args.out_dir / "cues.json").write_text(
        json.dumps([asdict(cue) for cue in cues], ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    write_csv(args.out_dir / "cues.csv", cues)
    write_text(args.out_dir / "cues.txt", cues)

    summary = {
        "source_file": args.input.name,
        "size_bytes": len(data),
        "sha256": hashlib.sha256(data).hexdigest(),
        "cue_count": len(cues),
        "cue_label_count": sum(cue.cue_label is not None for cue in cues),
        "scene_label_count": sum(cue.scene_label is not None for cue in cues),
        "cue_number_scale": CUE_SCALE,
        "integer_widths": {"0x08": 1, "0x09": 2, "0x0A": 3},
        "validation": {
            "cue_100": next((asdict(cue) for cue in cues if cue.cue_number == "100"), None),
            "cue_101": next((asdict(cue) for cue in cues if cue.cue_number == "101"), None),
        },
    }
    (args.out_dir / "summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
