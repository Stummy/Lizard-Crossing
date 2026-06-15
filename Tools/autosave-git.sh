#!/usr/bin/env bash
# Lizard Crossing auto-save: stage everything, commit if there are changes,
# and push to GitHub if a remote is configured. Safe to run anytime — it's a
# no-op when nothing changed, and never errors the caller.
cd "C:/Users/Family/New Game/LizardCrossing" || exit 0

git add -A >/dev/null 2>&1

# Only commit when there is something staged.
if git diff --cached --quiet; then
  exit 0
fi

stamp="$(date '+%Y-%m-%d %H:%M:%S')"
git commit -q -m "autosave: $stamp" >/dev/null 2>&1

# Push only if an 'origin' remote exists (so this works before GitHub is linked).
if git remote get-url origin >/dev/null 2>&1; then
  git push -q origin HEAD >/dev/null 2>&1 || true
fi
exit 0
