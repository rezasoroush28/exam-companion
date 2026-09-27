# Gramophone artwork integration

The user-approved concept is implemented as a hybrid image/SVG object in `Components/Game/Gramophone.razor`. The static artwork is `wwwroot/art/gramophone/papercraft-body-v2.png` (1254 × 1254, approximately 1.5 MB). It was prepared with the built-in image-generation tool, using the approved concept as the edit reference.

The art keeps the soft folded flower horn, curved teal neck, rounded layered burgundy base, recessed vinyl, and stationary arm mount. The five topic grooves, center label, localized scratches, repair sparkle, playhead, moving tone arm, and stylus are live SVG. This preserves five topics even though the original concept illustration contained four colored rings.

The generated transparency request produced an RGB checkerboard, so a second image edit replaced the background with navy. The runtime SVG clips the body silhouette, including the open area below the horn, to blend with the actual studio background. The PNG is intentionally RGB; do not treat it as an alpha cutout.

## Image preparation prompts

Built-in edit 1: Preserve the approved object's exact square framing, position, elevated angle, soft horn silhouette, paper textures, rounded base, recessed rim, feet, and lighting. Remove background; remove colored record grooves, label and spindle, leaving blank dark vinyl; remove the movable tone arm, cartridge, needle and their shadows while retaining the stationary pivot. Do not redesign or move the object.

Built-in edit 2 (selected asset): Correct the unwanted checkerboard background to uniform dark navy #121426, including the opening below the horn. Preserve the gramophone's geometry, colors, materials and square framing. Keep the record blank and the stationary pivot; do not add grooves, label, spindle, tone arm or needle.

## Mechanics and projection

The record uses `matrix(1.65 .17 -.18 .81 613 765)`. Grooves retain their existing radii `175 - 25 × topic order` and contact angle 0.55 radians. The moving needle derives its position from that actual SVG matrix, with its local tip offset included. The curved arm starts at the stationary mount at (1002, 655).

Topic/album playback now interrupts unfinished or scratched topic chords from 1.25s to 2.35s of each 2.7-second phrase. The 1.1-second filtered noise overlays a fluttering chord dropout while the chord itself sustains, then the clean chord returns briefly and fades out in 250ms. Record rotation starts at an adjusted phase so the existing scratch geometry reaches the needle at the middle interruption. Correct-answer note feedback and clean completed/repaired topics stay clean. The browser journey verifies progression, all five grooves, scratched playback, repair, keyboard input, reduced motion, and projected needle-to-groove contact. The separate audio check renders real Web Audio offline and verifies the clean/damaged/recovered intervals. Neither game rules nor database schema changed.
