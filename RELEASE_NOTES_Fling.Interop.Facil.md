Release notes (Fling.Interop.Facil)

### 0.3.1 (2025-01-31)

* Added support for batch saving (saving a batch of root entities with all child entities and doing at most one
  insert/update/delete call per table).

### 0.3.0 (2025-01-16)

See the v0.7.0 notes in [RELEASE_NOTES.md](RELEASE_NOTES.md) for more background on the changes in this version.

* **Breaking:** The functions `loadChild`, `loadOptChild`, `loadChildren`, `batchLoadChild`, `batchLoadOptChild`, and
  `batchLoadChildren` now work with a loader where `'arg` is `SqlConnection * SqlTransaction` instead of the previous
  `string` (a connection string). This change is necessary to be able to load everything in a transaction after the
  changes in Fling 0.7.0.
* **Breaking:** Renamed `withTransactionFromConnStr` to `saveWithTransactionFromConnStr` for consistency with the new
  helpers mentioned below.
* Added `loadWithTransactionFromConnStr`, `loadWithSnapshotTransactionFromConnStr`,
  `loadBatchWithTransactionFromConnStr`, and `loadBatchWithSnapshotTransactionFromConnStr` that can be used in place of
  the main library's `loadSerial` and `loadBatchSerial` (previously: `loadSerialWithTransaction` and
  `loadBatchSerialWithTransaction`). These helpers open a connection, start a transaction and run the loader using that
  connection/transaction. (The loader's `'arg` must be `SqlConnection * SqlTransaction`.)
* Added `loadOne`, `loadOneNoParam`, `loadMany`, and `loadManyNoParam` to simplify usage of the load function created
  using the helpers above with Facil scripts.

### 0.2.8 (2024-11-05)

* Added `batchSaveChildren*` functions for batching inserts/updates/deletes of to-many children
* Updated Microsoft.Data.SqlClient from 5.2.1 to 5.2.2

### 0.2.7 (2024-07-29)

* Updated Microsoft.Data.SqlClient from 5.1.4 to 5.2.1

### 0.2.6 (2024-01-19)

* Updated Microsoft.Data.SqlClient from 5.1.1 to 5.1.4

### 0.2.5 (2023-08-08)

* Updated Microsoft.Data.SqlClient from 5.1.0 to 5.1.1

### 0.2.4 (2023-01-24)

* Updated Microsoft.Data.SqlClient from 5.0.1 to 5.1.0

### 0.2.3 (2022-10-25)

* Updated for Fling 0.4.0
* Updated Microsoft.Data.SqlClient from 5.0.0 to 5.0.1

### 0.2.2 (2022-08-08)

* Updated Microsoft.Data.SqlClient from 4.1.0 to 5.0.0

### 0.2.1 (2022-01-06)

* Update for Facil 2.2.0

### 0.2.0 (2021-10-29)

* Target .NET 6

### 0.1.2 (2021-04-28)

* Added mock Facil scripts `FacilIgnore` and `FacilThrow` that can be used as insert/update/delete scripts and will,
  respectively, do nothing or throw if called.

### 0.1.1 (2021-04-27)

* Updated for new APIs in Fling 0.2.2

### 0.1.0 (2021-03-17)

* Initial release
