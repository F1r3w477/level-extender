# Changelog

## [4.0.1]

### Fixed

- Prevented the inventory duplication Harmony prefix from mutating item instances still owned by machines or other containers.
- Preserved vanilla and equipment-adjusted fishing bar values, including Cork Bobber adjustments.
- Limited fishing bar extension to four pixels per fishing level above level 10.
- Clamped the final fishing bar height to the 568-pixel fishing rectangle.
- Made daily crop-growth processing host-only in multiplayer.
- Added support for loaded `Farm` location instances, including farm-map replacements and additional modded Farm locations.
- Added a hard skill-level cap of 200 to avoid exceeding Stardew Valley's 32-bit XP representation.

### Changed

- Daily random crop growth remains enabled by default as an advertised Level Extender feature.
- Added a regression test covering attempts to set a skill to level 300.

### Validation

- 71 automated tests pass.
