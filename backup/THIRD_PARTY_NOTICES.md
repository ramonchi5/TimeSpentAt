# Third-Party Notices

## LiveSplit

TimeSpentAt is a custom component for LiveSplit and references LiveSplit APIs.

LiveSplit is licensed under the MIT License.

Copyright (c) 2013 Christopher Serr and Sergey Papushin.

Official repository: https://github.com/LiveSplit/LiveSplit
License: https://github.com/LiveSplit/LiveSplit/blob/master/LICENSE

## FancyText

TimeSpentAt includes optional rendering compatibility with FancyText by reading the public `FancyTextRuntime` effect state when FancyText is loaded in the same LiveSplit process. The text-effect drawing code was adapted from FancyText patterns so TimeSpentAt can render consistent shadows, outlines, and gradients without requiring FancyText at compile time.

FancyText is licensed under the MIT License.

Copyright (c) 2026 ramonchi5.

Repository: https://github.com/ramonchi5/FancyText

## SplitDetail

TimeSpentAt's segment-time summing follows the same cumulative-split-minus-previous-split approach used by SplitDetail for completed segments, active segments, and comparison times.

SplitDetail is licensed under the MIT License.

Copyright (c) 2026 SplitDetail contributors.

Repository: https://github.com/ramonchi5/SplitDetail

## LiveSplit.Core.dll

This project references `LiveSplit.Core.dll` during development/building.

The copy in `packages/` is used as a compile-time reference only. It is not required in the release package because LiveSplit provides it at runtime.

## UpdateManager.dll

This project references `UpdateManager.dll` during development/building.

The copy in `packages/` is used as a compile-time reference only. It is not required in the release package because LiveSplit provides it at runtime.

## Microsoft .NET Framework Reference Assemblies

This project uses Microsoft .NET Framework Reference Assemblies for local builds.

The checked-in NuGet package metadata lists Microsoft as the author and links its license at https://github.com/Microsoft/dotnet/blob/master/LICENSE. These assemblies are build-time references and are not part of the TimeSpentAt release package.
