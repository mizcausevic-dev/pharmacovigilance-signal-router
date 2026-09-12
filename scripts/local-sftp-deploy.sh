#!/usr/bin/env bash
#
# Local SFTP deploy escape hatch — when GH Actions runners get blocked by
# Hostinger's IP-reputation firewall, we can push directly from a local
# machine with a clean residential IP.
#
# This script connects via SSH key auth to Hostinger and uploads a directory
# tree to a target server-side path using `tar | ssh` for speed and atomicity.
# Same pattern the suite-hub workflow uses, but runs locally — bypasses the
# GH Actions IP-roulette problem entirely.
#
# Prerequisites (one-time):
#   1. Hostinger SSH enabled in hPanel → Advanced → SSH Access
#   2. SSH keypair at ~/.ssh/kineticgain_ed25519 (private) +
#      kineticgain_ed25519.pub registered in hPanel → SSH Keys
#   3. ssh-keyscan -p 65002 -H 82.25.89.47 >> ~/.ssh/known_hosts (one-time)
#
# Usage:
#   scripts/local-sftp-deploy.sh <local-dir> <remote-subdir-under-public_html>
#
#   # Example: push the apex repo's staging-root to /
#   bash scripts/local-sftp-deploy.sh ./staging-root ''
#
#   # Example: push a different repo's content to /suite/
#   bash scripts/local-sftp-deploy.sh /path/to/kinetic-gain-suite-landing suite
#
# Exit codes:
#   0  success (DEPLOY_EXTRACTED echoed by remote)
#   1  bad args
#   2  SSH key missing
#   3  upload failed
#
set -euo pipefail

SSH_KEY="${HOME}/.ssh/kineticgain_ed25519"
SSH_USER="${HOSTINGER_FTP_USER:-u815783393}"
SSH_HOST="${HOSTINGER_FTP_HOST:-82.25.89.47}"
SSH_PORT="${HOSTINGER_FTP_PORT:-65002}"
REMOTE_BASE="${HOSTINGER_REMOTE_BASE:-domains/kineticgain.com/public_html}"

usage() {
  cat <<EOF
Usage: $0 <local-dir> <remote-subdir>
  local-dir      Local directory whose CONTENTS will be uploaded
  remote-subdir  Subdirectory under \$HOME/$REMOTE_BASE/ (use '' for root)

Env overrides:
  HOSTINGER_FTP_USER     (default: $SSH_USER)
  HOSTINGER_FTP_HOST     (default: $SSH_HOST)
  HOSTINGER_FTP_PORT     (default: $SSH_PORT)
  HOSTINGER_REMOTE_BASE  (default: $REMOTE_BASE)

Example:
  $0 ./staging-root ''
  $0 ../kinetic-gain-suite-landing suite
EOF
  exit 1
}

[ $# -eq 2 ] || usage
LOCAL_DIR="$1"
REMOTE_SUBDIR="$2"

[ -d "$LOCAL_DIR" ] || { echo "FAIL: $LOCAL_DIR is not a directory"; exit 1; }
[ -f "$SSH_KEY" ]  || { echo "FAIL: SSH key not found at $SSH_KEY"; exit 2; }

REMOTE_PATH="$REMOTE_BASE"
[ -n "$REMOTE_SUBDIR" ] && REMOTE_PATH="$REMOTE_BASE/$REMOTE_SUBDIR"

# IMPORTANT: <local-dir>'s CONTENTS map 1:1 to <remote-subdir>'s CONTENTS.
# Don't nest: if you want /constellation/index.html live, source must have
# index.html at ROOT — NOT source/constellation/index.html (that lands at
# /constellation/constellation/index.html on the server).
echo "=========================================================="
echo "Local SFTP deploy"
echo "  src : $LOCAL_DIR ($(du -sh "$LOCAL_DIR" | cut -f1))"
echo "  dest: $SSH_USER@$SSH_HOST:$REMOTE_PATH/"
echo "  port: $SSH_PORT (SSH key auth)"
echo "=========================================================="

# ── GUARD 1 of 2: nothing but web-servable files leaves this machine ──
# On 2026-08-21 the deploy scripts, the CI workflow and generate.py were all
# answering 200 in production. The tar excludes below are a DENYLIST: they
# name .git, .github, node_modules, README.md, CHANGELOG.md, LICENSE, docs
# and staging-root. Someone thought hard about that list. `scripts/` is not
# on it, and neither is `*.py`. A denylist only blocks what its author
# imagined, and what gets published is always the thing nobody named.
#
# deploy_guard.py --preflight inverts that: an ALLOWLIST of extensions a web
# server has a reason to hand a visitor. Anything else stops the deploy here,
# before a single byte is uploaded. The excludes below stay as a second layer.
#
# This runs INSIDE the deploy script on purpose. A guard that sits beside the
# deploy path is a guard that gets forgotten on the one rushed evening it
# mattered. Skipping this one means editing this file, which is a visible act
# that shows up in a diff.
# ABSOLUTE path, resolved before the `cd "$LOCAL_DIR"` below. The first
# version of this used $(dirname "$0") directly, which is relative; after the
# cd it pointed inside the staging directory, [ -f "$GUARD" ] went false, and
# the post-deploy verify SILENTLY SKIPPED. A guard that quietly does nothing
# is worse than no guard, because the log still looks clean.
GUARD="$(cd "$(dirname "$0")" && pwd)/deploy_guard.py"
if [ -f "$GUARD" ]; then
  python "$GUARD" --preflight "$LOCAL_DIR" || {
    echo ""
    echo "DEPLOY ABORTED. Nothing was uploaded."
    exit 4
  }
else
  echo "FAIL: $GUARD is missing. Refusing to deploy unguarded."
  echo "      Restore scripts/deploy_guard.py or deploy deliberately by hand."
  exit 4
fi

cd "$LOCAL_DIR"

tar --exclude='.git' --exclude='.github' --exclude='node_modules' \
    --exclude='CHANGELOG.md' --exclude='LICENSE' --exclude='README.md' \
    --exclude='docs' --exclude='.DS_Store' --exclude='staging-root' \
    --exclude='scripts' --exclude='*.py' --exclude='*.sh' --exclude='*.yml' \
    --exclude='generated' --exclude='.env' \
    -czf - . | \
ssh -i "$SSH_KEY" \
    -o StrictHostKeyChecking=accept-new \
    -o ConnectTimeout=30 \
    -o ServerAliveInterval=15 \
    -p "$SSH_PORT" \
    "$SSH_USER@$SSH_HOST" \
    "cd $REMOTE_PATH/ && tar -xzf - && echo DEPLOY_EXTRACTED && ls -la index.html 2>&1 | head -1"

# ── GUARD 2 of 2: prove the live host is not serving source ──
# The preflight above can only see what THIS deploy stages. It cannot see what
# some earlier deploy left behind, and because these deploys are additive by
# design (files not in the archive are never touched, which is what protects
# the subdomain directories) nothing ever self-heals. The six exposed paths
# had been sitting there since a full-tree upload that no longer exists as a
# code path.
#
# So after every upload, ask the live host directly. This is the question no
# other check in the estate could ever have asked: the mobile audit, the link
# scanner and the CI verification all confirm that the right things WORK.
# None of them requests a URL that nothing links to, so none of them could
# discover that a wrong thing is reachable.
#
# Non-fatal on purpose: the upload already succeeded, so failing hard here
# would leave a half-reported deploy. It prints loudly and sets the exit code
# instead.
GUARD_BASE="${DEPLOY_VERIFY_BASE:-https://kineticgain.com}"
if [ ! -f "$GUARD" ]; then
  echo "FAIL: $GUARD vanished between preflight and verify. Not reporting success."
  exit 5
fi
echo ""
python "$GUARD" --verify "$GUARD_BASE" || {
  echo ""
  echo "!!! The upload succeeded but the host is serving files it should not."
  echo "!!! Fix .htaccess and re-run: python $GUARD --verify $GUARD_BASE"
  exit 5
}

echo "OK: deploy complete"
