#!/usr/bin/env sh
# VBWR B
#
# Project: HelpMe
# Repository: https://github.com/umbertotechnopreneur/PromptMeUp
# Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
#
# VibeWare: Human intent, AI execution, and plenty of tokens
# Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
#
# Modified with AI: OpenAI Codex; added this header on 2026-10-10.
# Human guidance: Umberto Giacobbi; requested VibeWare branding.
#
# Copyright (c) 2026 Umberto Giacobbi
# License: MIT - see LICENSE
#
# VBWR E

# SPDX-License-Identifier: MIT

set -eu

action="${1:-status}"
if [ "$#" -gt 0 ]; then shift; fi
case "$action" in
  install|remove|status) ;;
  *) echo "Usage: sh hm-path.sh [install|remove|status] [--executable /path/to/hm] [--yes]" >&2; exit 2 ;;
esac

executable=""
confirmed=false
while [ "$#" -gt 0 ]; do
  case "$1" in
    --executable)
      if [ "$#" -lt 2 ] || [ -n "$executable" ] || [ -z "$2" ]; then
        echo "Supply --executable once, followed by the built hm path." >&2; exit 2
      fi
      executable=$2; shift 2 ;;
    --yes) confirmed=true; shift ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_dir/../../.." && pwd)
if [ -n "$executable" ]; then
  case "$executable" in
    /*) ;;
    *) executable="$PWD/$executable" ;;
  esac
elif [ -x "$script_dir/hm" ]; then
  executable="$script_dir/hm"
elif [ -f "$repository_root/HelpMe/HelpMe.csproj" ] && [ -x "$repository_root/artifacts/bin/HelpMe/debug/hm" ]; then
  executable="$repository_root/artifacts/bin/HelpMe/debug/hm"
elif command -v hm >/dev/null 2>&1; then
  executable=$(command -v hm)
else
  echo "hm was not found. Supply --executable /path/to/built/hm." >&2
  exit 1
fi

if [ ! -f "$executable" ] || [ ! -x "$executable" ]; then
  echo "The selected hm executable is missing or not executable." >&2; exit 1
fi

if [ "$confirmed" = true ]; then
  exec "$executable" "--path=$action" --yes
fi

exec "$executable" "--path=$action"
