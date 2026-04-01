# DirX

DirX is a small C# command line tool that feels a lot like classic Windows `dir`, but is focused on a very practical use case:

- find files matching a pattern
- optionally recurse through subdirectories
- filter by date range
- sort results
- output either human-readable listings or bare paths for piping into other tools

Example:

```bash
dirx /s /b C:\Users\aza\Videos\*.mp4 /min 2025-1-1 /max 2025-2-1
````

That command searches for `.mp4` files under `C:\Users\aza\Videos`, including subdirectories, and prints only files whose selected timestamp falls between January 1 and February 1, 2025.

---

## Why this project exists

Classic `dir` is familiar and quick, but it is not especially pleasant for this “find files in a date window” workflow.

PowerShell can absolutely do it, but the command gets longer, noisier, and less memorable than a purpose-built tool.

DirX exists to provide:

* a familiar `dir`-style command experience
* simpler date filtering
* useful sorting
* bare output for scripts and pipelines
* a clean C# codebase that can grow over time

This project is intentionally small and practical.

---

## Current goals

DirX currently aims to support a useful subset of `dir` behavior, not a perfect clone.

Core features include:

* wildcard path patterns like `C:\Temp\*.log`
* `/s` recursive search
* `/b` bare output
* `/o:` sorting
* `/od`, `/on`, `/oe`, `/os` convenience sort switches
* `/t:c` and `/t:w` time field selection
* `/min` and `/max` inclusive date filtering
* basic `/a` and `/a:` attribute filtering

---

## Example usage

### Find files in a date range

```bash
dirx /s /b C:\Users\aza\Videos\*.mp4 /min 2025-1-1 /max 2025-2-1
```

### Show newest files first

```bash
dirx /s /o:-d C:\Exports\*.zip
```

### Sort by name

```bash
dirx /on C:\Work\*.cs
```

### Use creation time instead of last write time

```bash
dirx /t:c /o:-d C:\Photos\*.jpg /min 2025-01-01
```

---

## Design philosophy

DirX tries to be:

* familiar
* focused
* script-friendly
* deterministic
* easy to extend

It does **not** try to replace a shell or fully clone every detail of `dir.exe`.

---

## Building

```bash
dotnet build -c Release
```

---

## Status

Early-stage utility. The main priority is getting a clean, useful core feature set in place first, then hardening it with tests and broader compatibility over time.

---

## Roadmap

There's NO planned improvements.  This is a FDD project, Frustration Driven Development...  Features will be added if/when friction exceeds my frustration threshold.  Maybe, will include:

* automated tests
* better `dir` compatibility
* richer attribute support
* totals/footer output
* publish-ready single-file executable builds

---

## Contributing

Contributions are cautiously welcome, bot PRs will be rejected.  There's room for improvement, especially around:

* argument parsing hardening
* compatibility improvements
* tests
* output polish
* docs

Please keep changes aligned with the core intent of the project: a practical, familiar, date-filterable `dir`-like tool that stays simple and useful.

---

## License

TBD

