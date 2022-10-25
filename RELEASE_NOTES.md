Release notes
==============

These are the release notes for Fling (the main package). See [RELEASE_NOTES_Fling.Interop.Facil.md](RELEASE_NOTES_Fling.Interop.Facil.md) for the release notes for Fling.Interop.Facil.

### Unreleased

* **Breaking:** `runLoader` is renamed to `loadParallelWithoutTransaction`
* **Breaking:** `runBatchLoader` is renamed to `loadBatchParallelWithoutTransaction`
* Added `loadSerialWithTransaction`
* Added `loadBatchSerialWithTransaction`

### 0.3.0 (2021-10-29)

* Target .NET 6

### 0.2.2 (2021-04-27)

* Added `...WithDifferentOldNew` variants of all child save functions that allows specifying a separate `toDto` function for the old and new entity. This can be useful for example if you are persisting computed state. You can load the existing persisted state from the DB together with your entity, and when persisting, you can compare the persisted state from the old entity with the new computed state based on the new entity.

### 0.2.1 (2021-03-17)

* Added `saveChildWithoutUpdate`, `saveOptChildWithoutUpdate`, and `saveChildrenWithoutUpdate` for child entities that don’t support update (e.g. association tables, where the key is essentially the whole DTO).

### 0.2.0 (2021-03-17)

* Breaking: `saveRoot` now takes insert/update functions that return `Async<unit>`. For the old signature, use the new function `saveRootWithOutput`.

### 0.1.0 (2021-03-15)

* Initial release
