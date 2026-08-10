# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 0.1.7-preview.3

### Core

- Added custom allocator support to `ArrayUnsafe<T>`
- Changed custom allocation handling to use allocator strategies for allocation and disposal
- Fixed custom allocator resizing to preserve existing data
- Fixed zero-length `ArrayUnsafe<T>` creation and disposal

### Tests

- Added EditorMode coverage for Entities world-update allocators across buffers and native and unsafe collections
- Added `ArrayUnsafe<T>` coverage for custom allocator handles, zero-length arrays, and invalid allocator strategies
- Changed Core EditorMode test namespaces to follow the test directory layout

### SourceGen

- Rebuilt all source generators for `0.1.7-preview.3`

### Samples

- Updated data sample paths for `0.1.7-preview.3`

### Versioning

- `EncosyTower.Formatters` to `0.1.7-preview.3`
- `EncosyTower.SourceGen.*` to `0.1.7-preview.3`
- Package and sample references to `0.1.7-preview.3`

## 0.1.7-preview.2

### General

- Updated coding conventions for centralized validation symbols and collection exception helpers

### Core

- Added centralized validation symbols for global, collections, PubSub, processing, and stats runtime checks
- Added `EncosyTower.Collections.ThrowHelper` for collection and buffer validation
- Replaced file-local validation guard attributes with centralized global and namespace-specific symbols
- Moved collection and buffer exception helpers from `EncosyTower.Debugging` to `EncosyTower.Collections`
- Updated managed collection cleanup checks to use Encosy unmanaged type detection
- Limited `EncosyTower.Debugging.ThrowHelper` to generic exception factories
- Removed redundant file-local validation define blocks from conditional guard call sites
- Removed redundant unmanaged type validation from `BufferNative<T>`

### SourceGen

- Updated database, stats, persistence, poly-enum, and union ID output to use centralized validation symbols
- Rebuilt all source generators for `0.1.7-preview.2`
- Fixed generated persistence code to reference collection exception helpers from `EncosyTower.Collections`
- Removed generated file-local validation define blocks

### Entities.Stats

- Updated runtime and generated validation to use global and stats-specific symbols

### Mvvm

- Updated view binding validation to use centralized runtime symbols

### Tests

- Updated validation tests for centralized symbols and collection exception helpers

### Samples

- Updated the data preset and persistence sample for `0.1.7-preview.2`

### Versioning

- `EncosyTower.Formatters` to `0.1.7-preview.2`
- `EncosyTower.SourceGen.*` to `0.1.7-preview.2`
- Package and sample references to `0.1.7-preview.2`

## 0.1.7-preview.1

### General

- Added `CODING-CONVENTIONS.md` to document the project coding conventions

### Core

- Added `BufferManaged`, `BufferNative`, and `BufferUnsafe` along with `AllocatorStrategy`
- Added native and unsafe variants of list, queue, stack, and reference collections (`ListNative`, `QueueNative`, `StackNative`, `ReferenceNative`, and their unsafe counterparts)
- Added shared collection types: `SharedQueue`, `SharedStack`, `SharedQueueNative`, and `SharedStackNative`
- Added read-only and unsafe extension methods for the new and existing collection types
- Added indexer contracts for collection types: `IIndexer<T>`, `IReadOnlyIndexer<T>`, `IRefIndexer<T>`, and `IRefReadOnlyIndexer<T>`
- Added `ArrayUnsafe`, `ReferenceUnsafe`, and `EncosyMemoryExtensions`
- Added extension methods for `ArraySetNative` types
- Added `StringVaultUnsafe` and the `IStringVault` and `IReadOnlyStringVault` interfaces covering both managed and native string vaults
- Added the `ENCOSY_CLEAR_GLOBAL_STRING_VAULT_ON_ENTER_PLAY_MODE` compilation symbol to clear `GlobalStringVault` on entering Play Mode
- Added an internal constructor for `NativeSliceReadOnly`
- Renamed `NativeStringVault` to `StringVaultNative` and its `TryGetString` API to `TryGetUnmanagedString`
- Renamed `StatelessList` to `ListProxy`, reimplemented it with missing functions, and replaced `ProxiedList` and `IListProxy` with it
- Unified buffer implementations: merged the strategy types into the buffers, then renamed `ManagedBuffer` and `NativeBuffer` to `BufferManaged` and `BufferNative`
- Updated validation checks across runtime code to use shared settings via `ThrowHelper`
- Marked overlapping fields and pointer operations with safe and unsafe guidance for future updates
- Moved built-in `JsonArrayMap` support to the persistence sample
- Fixed incorrect extension methods for `SharedList` types
- Fixed the implementation of unsafe exposed types in the Bcl.Extensions plugins
- Removed `FixedArray` and `FixedHashMap`
- Removed the buffer strategy types: `IBufferStrategy`, `ManagedStrategy`, `NativeStrategy`, and `BufferBase`

### SourceGen

- Updated generated code to conform to the "Unsafe Evolution" direction of future C#
- Rebuilt all source generators for `0.1.7-preview.1`
- Fixed compilation symbol defines at the top of generated files
- Fixed a wrong type reference: `HashSetAPI` should be `EncosyHashSetExtenions`

### Entities.Stats

- Updated validation checks to use shared settings and generated overlapping-field guidance for `StatVariant`

### Tests

- Added EditorMode coverage for buffers, collections, extensions, and validation helpers
- Allowed the test assembly to inspect `EncosyTower.Core` internals

### Samples

- Updated samples
- Moved `JsonArrayMap` support to the persistence sample

### Versioning

- `EncosyTower.Formatters` to `0.1.7-preview.1`
- `EncosyTower.SourceGen.*` to `0.1.7-preview.1`
- Package and sample references to `0.1.7-preview.1`

## 0.1.6-preview.10

### Core

- Added parameter for customizing AES iterations
- Added overloads with `count` param to WhenAll tasks
- Fixed incorrect ByteBool conversion
- Fixed incorrect Dispose method for StringVaults
- Fixed incorrect string collisions handling in StringVaults
- Fixed incorrect implementation of Awaitables.Completed.Awaitable
- Fixed incorrect empty separator handling for SpanSplitExtensions.Split overloads
- Fixed missing invocation of _actionOnReturn in SimpleConcurrentPool
- Fixed missing character W in RandomStringGenerator.CHARSET
- Fixed incorrect range checks for Insert and FindIndex
- Fixed incorrect hash calculation for managed string in StringVault
- Fixed incorrect method call syntax in SharedArray & SharedReference
- Fixed leak in NativeStrategy<T>.Dispose
- Fixed incorrect range check for CopyFromSpan & CopyToSpan
- Fixed incorrect implementation of DateTimeId.IsValid
- Fixed incorrect implementation of MessageBroker<T>.PublishAsync
- Fixed incorrect return value for CachedPublisher`2.Validate
- Fixed incorrect implementation for IndexOf and Remove in StatelessList
- Fixed incorrect implementation of EncosyNativeArrayExtensionsUnsafe.MemoryCopyUnsafe
- Fixed incorrect implementation of NB<T>.Clear
- Fixed wrong implementation of equality for Result type
- Prevented possible null exception for Option.Equals<T> when T is reference type
- Removed implicit conversion of decimal to Variant

### Entities.Stats

- Fixed an incorrection that makes Assert always fails for TryUpdateStatAssumeSingleEntity

### Samples

- Updated samples

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.10`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.10`
- Package and sample references to `0.1.6-preview.10`

## 0.1.6-preview.9

### Core

- ByteBool types are now serializable
- ByteBool types now implement IFixedString and IFixedString<T>

### SourceGen

- Entities.Stats: StatDataStore now stores ByteBool types

### Samples

- Updated samples

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.9`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.9`
- Package and sample references to `0.1.6-preview.9`

## 0.1.6-preview.8

### Core

- Added ByteBool variants for bool2, bool2x2,... bool4x4 in Unity Mathematics

### SourceGen

- Entities.Stats: repaced bools with ByteBool variants to fix Burst error BC1063

### Samples

- Updated samples

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.8`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.8`
- Package and sample references to `0.1.6-preview.8`

## 0.1.6-preview.7

### Core

- Variants: Added CanStore<T> method to VariantConverter

### SourceGen

- Variants: Replaced compiled time condition with VariantConverter.CanStore<T>
- PolyEnumStructs: Improved algorithm to calculate struct size

### Samples

- Updated samples

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.7`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.7`
- Package and sample references to `0.1.6-preview.7`

## 0.1.6-preview.6

### Entities.Stats

- Fixed multiple bugs

### SourceGen

- Fixed missing generated API for Entities.Stats
- Fixed wrong calculation of type size. Now corrected with field alignments.

### Samples

- Updated samples

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.6`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.6`
- Package and sample references to `0.1.6-preview.6`

## 0.1.6-preview.5

### Entities.Stats

- Fixed multiple bugs

### SourceGen

- Updated code emission for `StatSystemSpec+WriteCode`

### Samples

- Updated sample for Stats

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.5`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.5`
- Package and sample references to `0.1.6-preview.5`

## 0.1.6-preview.4

### Entities.Stats

- Added method `GetStatComponentTypeSet` to `StatAPI`

### SourceGen

- Emitted additional markers to help navigating generated code

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.4`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.4`
- Package and sample references to `0.1.6-preview.4`

## 0.1.6-preview.3

### Contracts

- Added interfaces `IIsValid` and `IIsInitialized`
- Added the interfaces on types that has properties `bool IsValid` or `bool IsInitialized`
- Modified source generators to include the interfaces

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.3`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.3`
- Package and sample references to `0.1.6-preview.3`

## 0.1.6-preview.2

### Databases.Authoring
- Fixed: `DatabaseRawSheetImporter` now ignores column path that contains `$` character
- Fixed: `SheetUtility` now correctly validates and sanitizes file names

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.2`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.2`
- Package and sample references to `0.1.6-preview.2`

## 0.1.6-preview.1

### General

- Moved the development of this package over here from [Tower of Encosy](https://github.com/laicasaane/tower_of_encosy/)
- Added a "Sign and release" CI to support [UPM Signing](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-export.html)

### Breaking changes

- Rebranded `UserDataVault` to `Persistence`
  - Performed multiple renaming on related APIs

### Versioning

- `EncosyTower.Formatters` to `0.1.6-preview.1`
- `EncosyTower.SourceGen.*` to `0.1.6-preview.1`
- Package and sample references to `0.1.6-preview.1`
