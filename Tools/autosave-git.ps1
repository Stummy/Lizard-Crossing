# Lizard Crossing auto-save (Windows/PowerShell).
# Stages all changes, commits if anything changed, and pushes when a GitHub
# remote is configured. No-op when nothing changed; never throws to the caller.
$ErrorActionPreference = 'SilentlyContinue'
Set-Location 'C:\Users\Family\New Game\LizardCrossing'

git add -A 2>$null
$staged = git diff --cached --name-only 2>$null
if (-not $staged) { exit 0 }

$stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
git commit -q -m "autosave: $stamp" 2>$null

$remotes = git remote 2>$null
if ($remotes -match 'origin') {
    git push -q origin HEAD 2>$null
}
exit 0
