Release notes
==============

These are the release notes for Fling (the main package). See [RELEASE_NOTES_Fling.Interop.Facil.md](RELEASE_NOTES_Fling.Interop.Facil.md) for the release notes for Fling.Interop.Facil.

### 0.2.1 (2021-03-17)

* Added `saveChildWithoutUpdate`, `saveOptChildWithoutUpdate`, and `saveChildrenWithoutUpdate` for child entities that don’t support update (e.g. association tables, where the key is essentially the whole DTO).

### 0.2.0 (2021-03-17)

* Breaking: `saveRoot` now takes insert/update functions that return `Async<unit>`. For the old signature, use the new function `saveRootWithOutput`.

### 0.1.0 (2021-03-15)

* Initial release
