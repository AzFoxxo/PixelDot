#!/usr/bin/env sh
cd "$(dirname "$0")"

# Logical width 120, logical height 80 with scale 8 creates a 960x640 window
if [ $# -eq 0 ]; then
    dotnet run -- 120 80 8 ConwaysGameOfLife
else
    dotnet run -- "$@"
fi
