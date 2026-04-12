#!/usr/bin/env bash
set -euo pipefail

# Demo metric: random score for quick smoke testing.
SCORE=$(grep "^rmse:" run.log | head -n 1 | sed 's/.*rmse:[[:space:]]*//')
echo "score=$SCORE"
