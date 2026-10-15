#!/usr/bin/env bash
# Redirect every tool cache into the git-ignored scratch directory.
#
# Why: where `$HOME` is not writable - the agent sandbox on this box, or any locked-down
# environment - the first `dotnet restore` fails with:
#
#   error : Read-only file system : '/home/<user>/.nuget/packages'
#
# and nothing can build. This points the caches somewhere writable.
#
# Usage - it exports variables, so source it rather than executing it:
#
#   source local_dev/scripts/sandbox-env.sh
#   ./local_dev/scripts/build-all-linux.sh --test
#
# Harmless on a normal machine: it only changes where caches are written, and the target
# directory is git-ignored either way. The paths are derived from this script's own location,
# so nothing here hard-codes a machine path.

_scratch="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/local_user_files"

export NUGET_PACKAGES="$_scratch/nuget/packages"
export NUGET_HTTP_CACHE_PATH="$_scratch/nuget/http-cache"
export DOTNET_CLI_HOME="$_scratch/dotnet-home"
export XDG_DATA_HOME="$_scratch/xdg/data"
export XDG_CACHE_HOME="$_scratch/xdg/cache"
export XDG_CONFIG_HOME="$_scratch/xdg/config"

mkdir -p "$NUGET_PACKAGES" "$NUGET_HTTP_CACHE_PATH" "$DOTNET_CLI_HOME" \
         "$XDG_DATA_HOME" "$XDG_CACHE_HOME" "$XDG_CONFIG_HOME"

unset _scratch
