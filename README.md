# LiveSplit.TimeSpentAt

TimeSpentAt is a LiveSplit component that adds up the segment times for every split or subsplit whose name matches configured search text.

## Features

- Search by a word or phrase typed in the settings panel.
- Whole-word matching by default, so searching for `cut` does not count `cutscene`.
- Optional "match inside longer words" mode for substring matching.
- Live sum that includes completed matching segments plus the current matching segment while it is running.
- Label column with optional counter, editable label text, bold, and custom color.
- Optional comparison column, disabled by default, with one or two summed comparison lines such as `PB:` and `Best:`.
- Configurable comparison choices, including LiveSplit's current comparison.
- Sum column with configurable bold, custom color, and time accuracy.
- Instance name support for layouts that use multiple TimeSpentAt components.
- Optional compatibility with FancyText runtime effects when FancyText is loaded in the same LiveSplit layout.

## How It Works

The component scans the active run on every update. For each matching segment, it calculates segment time the same way LiveSplit and SplitDetail do: the segment's cumulative split time minus the previous split time. If the current running segment matches the search text, the component uses LiveSplit's current timer value minus the previous split time and adds that live value into the total.

The optional comparison column performs the same per-segment subtraction against selected comparison times, then sums all matching segments for each comparison.

For rendering, TimeSpentAt uses LiveSplit's layout fonts/colors by default. If FancyText is present, TimeSpentAt reads FancyText's public runtime effects through reflection so text shadows, outlines, and gradients can stay visually consistent without requiring FancyText as a hard dependency.

## Matching

By default, matching is case-insensitive and requires word boundaries around the search text. For example:

| Search | Segment name | Counted by default? |
|---|---|---|
| `cut` | `cut` | Yes |
| `cut` | `Cut Scene` | Yes |
| `cut` | `cutscene` | No |
| `cutscene` | `Final Cutscene` | Yes |

Enable `Match inside longer words` to make `cut` count `cutscene`.

## Build

Build the solution in Release mode:

```powershell
dotnet build .\TimeSpentAt.sln -c Release
```

The component DLL is produced at:

```text
src\TimeSpentAt\bin\Release\net481\LiveSplit.TimeSpentAt.dll
```

When this repository is cloned next to a `LiveSplit` repository, the build also copies the DLL and PDB to:

```text
..\LiveSplit\artifacts\bin\TimeSpentAt\release\
```

Set `CopyToLiveSplitArtifacts=false` to disable that copy.

## Install

Copy `LiveSplit.TimeSpentAt.dll` into LiveSplit's `Components` folder, then add `Time Spent At` from LiveSplit's layout editor.

## Release Package

For a normal GitHub release, upload `LiveSplit.TimeSpentAt.dll`. The `packages` folder is only for compile-time references and should not be included in the release package. The PDB is only useful for debugging.

## Development Notes

Important files:

| File | Purpose |
|---|---|
| `src/TimeSpentAt/UI/Components/TimeSpentAt.cs` | Component entry point, matching, value calculation, layout, and drawing. |
| `src/TimeSpentAt/UI/Components/TimeSpentAtSettings.cs` | Settings UI, comparison dropdown refresh, XML persistence, and display options. |
| `src/TimeSpentAt/UI/Components/TimeSpentAtFactory.cs` | LiveSplit component registration. |
| `packages/` | Compile-time references only. |

Before publishing a build, test an empty search, a whole-word match, an inside-word match, a run with subsplits, a run with missing comparison data, comparison column on/off, one and two comparison lines, and all accuracy options.

## License

TimeSpentAt is licensed under the MIT License. See [LICENSE](LICENSE).

LiveSplit, FancyText, SplitDetail, and related build-time assemblies are licensed separately. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
