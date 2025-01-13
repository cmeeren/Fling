Release notes
==============

These are the release notes for Fling (the main package).
See [RELEASE_NOTES_Fling.Interop.Facil.md](RELEASE_NOTES_Fling.Interop.Facil.md) for the release notes for
Fling.Interop.Facil.

### Unreleased

* **Breaking:** Now supports loading the root in the same transaction as the children. To achieve this,
  `loadSerialWithTransaction` and `loadBatchSerialWithTransaction` now accept `Async`-wrapped root DTOs, and load these
  in the same transaction as the children. The previous functions can be accessed under the new names
  `loadChildrenSerialWithTransaction` and `loadChildrenBatchSerialWithTransaction`.
* **Possibly breaking:** `loadSerialWithTransaction` and `loadBatchSerialWithTransaction` now use
  `TransactionScopeOption.Required` instead of
  `TransactionScopeOption.RequiresNew` to avoid locking issues. This is also likely to be more correct in the contexts
  in which Fling is used.
* Added `loadSerialWithSnapshotTransaction` and `loadBatchSerialWithSnapshotTransaction` which are identical to their
  non-`Snapshot` counterparts but use `TransactionIsolationLevel.Snapshot`.

### 0.5.1 (2024-11-07)

* Fixed bug introduced in 0.5.0 where `...WithoutUpdate` functions would throw even if there was nothing to update

### 0.5.0 (2024-11-06)

* **Breaking** (hopefully not in practice): For to-many child entities, all deletes are performed first, then all
  updates, then all inserts. The previous behavior was deletes first, then each new child (in the order returned by your
  code) was either inserted or updated.
* Added `batchSaveChildren*` functions for batching inserts/updates/deletes of to-many children

### 0.4.0 (2022-10-25)

* **Breaking:** `runLoader` is renamed to `loadParallelWithoutTransaction`
* **Breaking:** `runBatchLoader` is renamed to `loadBatchParallelWithoutTransaction`
* Added `loadSerialWithTransaction`
* Added `loadBatchSerialWithTransaction`

### 0.3.0 (2021-10-29)

* Target .NET 6

### 0.2.2 (2021-04-27)

* Added `...WithDifferentOldNew` variants of all child save functions that allows specifying a separate `toDto` function
  for the old and new entity. This can be useful for example if you are persisting computed state. You can load the
  existing persisted state from the DB together with your entity, and when persisting, you can compare the persisted
  state from the old entity with the new computed state based on the new entity.

### 0.2.1 (2021-03-17)

* Added `saveChildWithoutUpdate`, `saveOptChildWithoutUpdate`, and `saveChildrenWithoutUpdate` for child entities that
  don’t support update (e.g. association tables, where the key is essentially the whole DTO).

### 0.2.0 (2021-03-17)

* Breaking: `saveRoot` now takes insert/update functions that return `Async<unit>`. For the old signature, use the new
  function `saveRootWithOutput`.

### 0.1.0 (2021-03-15)

* Initial release
