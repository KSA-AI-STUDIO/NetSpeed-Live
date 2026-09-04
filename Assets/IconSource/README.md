# Icon Source Assets — NetSpeed Live (Signal Radar)

Production source for `Assets\app.ico` (Stage 13, Concept 7 "Signal Radar").

## Files

| File | Purpose |
|---|---|
| `NetSpeedLive-Icon.svg` | Vector master. Editable source of truth for the design geometry and palette. |
| `NetSpeedLive-Icon-Master.png` | 256×256 raster master rendered from the design. |

`Assets\app.ico` (project root) is the production Windows icon package generated
from this design. It embeds 16/20/24/32/40/48 px as 32-bit BGRA DIB frames and
64/128/256 px as PNG-compressed frames.

## Design notes

- Radar arcs + centre dot: live monitoring identity, upper portion.
- Blue/cyan **down arrow first (left)** — download.
- Green **up arrow second (right)** — upload.
- Palette: `#0A84FF`/`#00C2FF` (download), `#32D74B`/`#A6FF00` (upload),
  `#0D1522`→`#18294A` navy background, `#7FD0FF`/`#EAF6FF` radar.
- Below 32 px the smallest radar arc is dropped and strokes are thickened so
  the 16×16 tray rendering stays legible (arrows + one arc + dot).

## Regenerating `app.ico`

The .ico was produced from the same geometry via a GDI+ generator script
(documented in CHANGELOG Stage 13). To change the icon, update the SVG/PNG
here and rebuild the multi-resolution package with any ICO packer that emits
32-bit DIB frames for ≤48 px and PNG frames for larger sizes.
