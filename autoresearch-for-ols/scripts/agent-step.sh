#!/usr/bin/env bash
set -euo pipefail

echo "agent iteration: ${AR_ITERATION:-0}"

uv run train.py > run.log 2>&1
# Optional: switch agentMode to "command" and use this script.
# Example: amp -p "improve benchmark metric"
