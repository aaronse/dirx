Below are two copy-paste-ready markdown files.

---

# `MASTER-SPEC.md`

````markdown
# DirX Master Spec

## 1. Project Name

**DirX**  
A dir-like command line tool for enumerating files and directories using familiar classic `dir`-style arguments, with additional practical filtering for date ranges.

---

## 2. Purpose

DirX exists to solve a practical gap between:

- the familiarity of classic Windows `dir`
- the scripting flexibility of PowerShell
- the extensibility and maintainability of a compiled C# CLI tool

The project provides a fast, simple, human-usable command line tool that feels familiar to users who know `dir`, while adding capabilities that are awkward, verbose, or inconsistent to express in raw shell commands.

Its initial primary use case is:

- search a specified path and optional subdirectories
- match a file pattern such as `*.mp4`
- filter results to those whose relevant timestamps fall within an optional inclusive min/max range
- sort results using familiar `dir`-style options
- emit either human-readable listings or bare output suitable for piping into other tools

Example:

```bash
dirx /s /b C:\Users\aaron\Videos\*.mp4 /min 2025-1-1 /max 2025-2-1
````

---

## 3. Problem Statement

Classic `dir` is familiar and convenient, but limited for this specific scenario:

* filtering by date/time ranges is not ergonomic enough for the desired workflow
* behavior is not tuned for this specific “find recent files by pattern” use case
* combining recursion, pattern matching, timestamp selection, sorting, and script-friendly output often becomes clumsy

PowerShell can do this, but:

* commands are longer and less memorable
* ad hoc scripts are less pleasant for repeated everyday use
* user intent is buried in shell plumbing rather than reflected in a dedicated tool interface

DirX aims to provide:

* **familiar syntax**
* **high utility for daily use**
* **simple mental model**
* **easy future extensibility**
* **clean implementation suitable for public sharing and further development**

---

## 4. Goals

## 4.1 Functional Goals

DirX should:

* accept a target path or path-pattern including wildcards
* support optional recursive traversal
* support optional bare output mode
* support optional sorting with common classic `dir` conventions
* support inclusive `/min` and `/max` date filters
* support selecting which file timestamp field is used for filtering, display, and sorting
* support basic attribute-based filtering
* produce deterministic and script-friendly output
* fail clearly when inputs are invalid

## 4.2 UX Goals

DirX should feel:

* familiar to users who know Windows `dir`
* more focused than general-purpose shell pipelines
* easy to remember
* pleasant to use repeatedly
* obvious enough that a user can guess many switches correctly

## 4.3 Engineering Goals

DirX should be:

* implemented in C#
* easy to build and publish
* easy for both humans and LLMs to extend safely
* structured so additional `dir`-like features can be added incrementally
* public-repo friendly
* suitable for future packaging as a standalone executable

---

## 5. Non-Goals

At least in the early phases, DirX is **not** trying to be:

* a perfect byte-for-byte clone of Windows `dir.exe`
* a full shell replacement
* a general file indexing engine
* an always-on search database
* a cross-platform abstraction over all file system quirks
* a GUI application
* a full PowerShell module ecosystem replacement

It is acceptable for DirX to support a **useful subset** of classic `dir` behavior first, as long as that subset is deliberate, documented, and internally consistent.

---

## 6. Primary Users

### 6.1 Direct Human Users

* Windows power users
* developers
* makers
* advanced hobbyists
* users who often search media, logs, exports, build artifacts, or project files
* users who know `dir` but want more useful date filtering

### 6.2 Indirect Users

* scripts and automation
* LLM-assisted workflows
* dev agents generating or consuming file lists
* build and media processing pipelines

---

## 7. Core Use Cases

## 7.1 Find media files within a date range

```bash
dirx /s /b C:\Users\aaron\Videos\*.mp4 /min 2025-1-1 /max 2025-2-1
```

## 7.2 Find newest matching files first

```bash
dirx /s /b /o:-d C:\Exports\*.zip
```

## 7.3 List files sorted by name

```bash
dirx /on C:\Work\*.cs
```

## 7.4 Show directories too

```bash
dirx /a:d C:\Projects\*
```

## 7.5 Use creation time rather than last write time

```bash
dirx /t:c /o:-d C:\Media\*.jpg /min 2025-01-01
```

## 7.6 Produce pipeline-friendly bare paths

```bash
dirx /s /b C:\Temp\*.log
```

---

## 8. Command Line Interface Requirements

## 8.1 General Invocation Shape

```bash
dirx [switches] <path-pattern> [/min <date>] [/max <date>]
```

### Notes

* `<path-pattern>` is required
* the path-pattern may include wildcards such as `*` and `?`
* if a plain directory path is supplied, default search pattern should become `*`
* `/min` and `/max` are inclusive
* invalid arguments should produce clear error output and a non-zero exit code

---

## 9. Supported Arguments

## 9.1 Traversal

### `/s`

Recurse into subdirectories.

### Default

Search only the specified directory.

---

## 9.2 Output Style

### `/b`

Bare output. Emit only the full path of each result, one per line.

### Default

Emit human-readable structured listing including timestamps and other useful metadata.

---

## 9.3 Sorting

DirX should support common `dir`-style sorting conventions.

### `/o:<fields>`

Sort by one or more fields.

Supported fields:

* `n` = name
* `e` = extension
* `s` = size
* `d` = date

### Descending Sort

A leading `-` before a sort key means descending.

Examples:

```bash
/o:n
/o:-d
/o:ne
/o:-d,n
```

### Convenience Forms

Support these aliases:

* `/on`
* `/oe`
* `/os`
* `/od`

### Sort Semantics

* sorting should be stable and deterministic
* when multiple items compare equal on explicit sort keys, a deterministic fallback sort should apply, preferably full path
* sort behavior should be documented and predictable

---

## 9.4 Time Field Selection

### `/t:w`

Use `LastWriteTime`

### `/t:c`

Use `CreationTime`

### Default

`/t:w`

### Semantics

The selected time field should consistently drive:

* date filtering
* date sorting
* displayed timestamp in human-readable output

This keeps behavior simple and unsurprising.

---

## 9.5 Date Range Filters

### `/min <date>`

Inclusive lower bound on selected timestamp.

### `/max <date>`

Inclusive upper bound on selected timestamp.

### Requirements

* both are optional
* if both are supplied, `/min` must be less than or equal to `/max`
* date parsing should support common local culture formats and reasonable invariant forms like:

  * `2025-1-1`
  * `2025-01-01`
  * `2025-01-01 13:30`
* invalid values should produce clear errors

---

## 9.6 Attribute Filtering

### `/a`

Include files, directories, hidden, and system entries

### `/a:<flags>`

Support a useful initial subset of classic attribute filters.

Initial supported flags:

* `h` = hidden
* `s` = system
* `d` = directories

### Initial Semantics

* absent `/a` behavior should default to normal file-oriented listing
* `/a:d` should allow directories to be included
* exact compatibility with every `dir.exe` attribute flag is not required in phase 1
* supported subset must be documented clearly

---

## 10. Path and Pattern Resolution

DirX must support patterns like:

```bash
C:\Users\aaron\Videos\*.mp4
```

### Requirements

* resolve absolute and relative paths
* detect whether the argument is a literal directory path versus a path-pattern containing wildcards
* if a directory is provided without wildcard pattern, use `*`
* if the parent path does not exist, fail clearly
* wildcard matching should be delegated to .NET file system enumeration behavior unless there is a specific reason to replace it

---

## 11. Output Requirements

## 11.1 Bare Output

When `/b` is specified:

* output only one full path per line
* no headers
* no totals
* no extra formatting noise
* suitable for piping into other tools

## 11.2 Human-Readable Output

Default output should include enough information to be useful interactively.

Suggested fields:

* displayed timestamp
* size or directory marker
* basic attributes
* full path

Example shape:

```text
2025-01-15 14:22:08         120,445  A      C:\Users\aaron\Videos\clip1.mp4
2025-01-18 10:01:44        <DIR>    D      C:\Users\aaron\Videos\Archive
```

### Requirements

* formatting should be aligned and readable
* output should remain parseable enough for human inspection
* exact spacing does not need to match `dir.exe`

---

## 12. Error Handling Requirements

DirX should fail clearly and early when possible.

Examples of invalid states:

* missing required path-pattern
* unsupported switch
* missing value after `/min` or `/max`
* invalid date syntax
* nonexistent root path
* impossible date range (`/min > /max`)

### Error UX Requirements

* error text should be brief and specific
* return non-zero exit code
* help text should be available via `/?, /h, /help`

---

## 13. Design Principles

## 13.1 Familiar First

Prefer conventions that resemble classic `dir` where practical.

## 13.2 Practical Over Perfect Compatibility

Useful behavior matters more than complete parity with `dir.exe`.

## 13.3 Script-Friendly

Bare output mode should remain simple and reliable.

## 13.4 Deterministic

Identical inputs should produce predictable ordering and filtering.

## 13.5 Incrementally Extensible

Implementation should make it easy to add more switches later.

## 13.6 Public-Repo Ready

Naming, comments, structure, and docs should be understandable to external contributors.

---

## 14. Implementation Guidance

## 14.1 Language and Platform

* Language: C#
* Project Type: Console application
* Initial target platform: Windows-focused
* Runtime: modern .NET

### Recommendation

Prefer a straightforward console app structure over unnecessary abstractions in the earliest version.

---

## 14.2 Suggested Internal Structure

A good initial structure might include:

* `Program.cs`
* `Options.cs`
* `ArgumentParser.cs`
* `FileEnumerator.cs`
* `Filters.cs`
* `Sorter.cs`
* `OutputFormatter.cs`

This may be split later. A single-file implementation is acceptable initially if readability remains good.

---

## 14.3 Recommended Internal Responsibilities

### Argument Parsing

* parse classic-style switches
* validate arguments
* normalize values into a strongly typed options model

### Pattern Resolution

* determine root directory and search pattern
* normalize paths

### Enumeration

* enumerate files and optionally directories
* handle recursion
* gracefully manage inaccessible directories if desired

### Filtering

* apply attribute filters
* apply selected time-field date range filters

### Sorting

* parse sort specification
* apply deterministic multi-key sorting

### Output

* emit either bare or human-readable output

---

## 15. LLM Guidance

This project should be friendly to both human maintainers and LLM-based dev agents.

### Important LLM Guidance

* do not attempt to re-implement the entire Windows `dir.exe` surface area unless explicitly instructed
* preserve the core design intent: a useful, focused, familiar tool
* avoid introducing overly abstract architecture too early
* prefer small, verifiable, incremental improvements
* do not silently change CLI semantics
* update README and spec when behavior changes materially
* preserve deterministic behavior
* prefer explicit tests for parsing, filtering, and sorting logic

### LLM Priorities

1. correctness
2. clarity
3. familiar CLI behavior
4. maintainability
5. extensibility

---

## 16. Testing Expectations

DirX should eventually have automated tests around:

### Argument Parsing

* switch recognition
* sort parsing
* date parsing
* invalid argument handling

### Pattern Resolution

* directory-only inputs
* wildcard inputs
* relative paths
* invalid paths

### Filtering

* `/min` only
* `/max` only
* both `/min` and `/max`
* `/t:c` versus `/t:w`
* hidden/system/directory inclusion behavior

### Sorting

* name sort
* date sort ascending/descending
* multi-key sort
* deterministic fallback ordering

### Output

* bare output formatting
* human-readable output formatting

---

## 17. Performance Expectations

Performance should be reasonable for everyday directory scanning.

### Early-Phase Guidance

* correctness and clarity matter more than micro-optimization
* avoid obviously wasteful approaches
* accept in-memory sorting for normal usage
* optimize only when real large-tree usage justifies it

Potential future work:

* streaming modes
* lazy pipelines
* faster directory traversal under heavy workloads
* explicit handling of unauthorized directories without aborting entire scans

---

## 18. Security and Safety Considerations

DirX is low-risk, but still should:

* avoid destructive actions
* not modify files or timestamps
* not require elevated permissions for normal operation
* avoid misleading output
* fail safely when access is denied

This project is intentionally read-only.

---

## 19. Public Repository Positioning

This repo should present DirX as:

* a practical utility
* intentionally small and focused
* easy to inspect and build
* open to incremental contribution
* useful as both a personal tool and an example of building a pragmatic CLI in C#

---

## 20. Roadmap

## Phase 1 — Core Useful Tool

* path-pattern support
* `/s`
* `/b`
* `/min`
* `/max`
* `/t:c|w`
* `/o:` sorting
* convenience sort aliases
* basic `/a` and `/a:` support
* clear help text
* public README

## Phase 2 — Hardening

* automated tests
* improved error coverage
* totals/footer
* relative path output option
* configurable output columns
* better handling of inaccessible directories

## Phase 3 — Better `dir` Compatibility

* broader attribute support
* closer compatibility with more classic `dir` switches
* optional headers/footers
* richer date/time formatting options

## Phase 4 — Packaging and Distribution

* single-file publish
* release binaries
* CI build
* versioning and changelog
* winget or other distribution if worthwhile

---

## 21. Key Decisions and Rationale

### Decision: Use C# instead of PowerShell for the main tool

**Why:** better maintainability, easier structured parsing, easier testing, easier future packaging.

### Decision: Support a useful subset of `dir` rather than perfect compatibility first

**Why:** faster path to a usable tool, lower complexity, better clarity, less risk of wasted effort.

### Decision: Keep `/min` and `/max` as first-class features

**Why:** these are the core reason the tool exists.

### Decision: Keep `/b` as a strong first-class mode

**Why:** enables reuse in scripts, pipelines, and LLM workflows.

### Decision: Let selected `/t:` drive filtering, sorting, and display

**Why:** simplest consistent mental model.

---

## 22. Success Criteria

The project is successful when:

* a user can quickly understand what it does
* a user familiar with `dir` can use it without much learning
* the tool reliably solves the target “find matching files in date range” workflow
* output works well both for humans and piping
* codebase is clean enough that humans and LLMs can safely extend it
* the public repo communicates the intent clearly

---

## 23. Suggested Future Questions

These are valid future design questions, but should not block initial progress:

* should default human-readable output include only file name instead of full path when non-recursive?
* should there be a relative-path mode?
* should min/max support file age shorthand like `-7d`, `-24h`, `today`, `yesterday`?
* should there be explicit include/exclude path filters?
* should date filtering eventually support “effective newest of creation/write” mode?
* how close should compatibility get to `dir.exe` over time?

---

## 24. Summary

DirX is a focused, practical command line utility that brings together:

* familiar `dir`-style interaction
* useful date filtering
* script-friendly output
* clean C# implementation
* room for future growth

Its job is not to be everything. Its job is to be the tool users reach for when they want a familiar way to quickly find files matching patterns and time windows, with sorting and output modes that work well for both people and automation.


