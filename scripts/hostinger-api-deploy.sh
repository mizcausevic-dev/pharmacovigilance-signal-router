#!/usr/bin/env bash
#
# Deploy a static site to a Hostinger-hosted subdomain via Hostinger's own
# HTTPS Developer API (developers.hostinger.com), bearer-token authenticated.
#
# WHY THIS EXISTS: the FTP (port 21) and SSH/SFTP (port 65002) deploy paths
# both go through Hostinger's edge WAF, which blocks GitHub Actions runner
# IPs on an IP-reputation basis (verified 2026-09-12: nc connects to port 21,
# the actual login stalls; port 65002 sees the same treatment on a
# bad-reputation runner IP). This path never touches FTP or SSH at all: it's
# the same HTTPS API (api version /api/hosting/v1) that Hostinger's own MCP
# integration and hPanel file manager use, authenticated with a bearer API
# token instead of an IP-trusted connection. Confirmed reachable and correct
# by direct read (list/browse) via the hostinger-hosting MCP tools before
# this script was written; the write flow below is sourced verbatim from
# @hostinger/sdk's generated API docs (HostingFilesApi.md, HostingWebsitesApi.md),
# not reverse-engineered.
#
# Usage:
#   HOSTINGER_API_TOKEN=... scripts/hostinger-api-deploy.sh <local-dir> <domain> [username]
#
# Exit codes: 0 success, 1 bad args, 2 missing token, 3 upload-url request
# failed, 4 TUS upload failed, 5 deploy trigger failed.
set -euo pipefail

API_BASE="https://developers.hostinger.com/api/hosting/v1"
LOCAL_DIR="${1:?usage: $0 <local-dir> <domain> [username]}"
DOMAIN="${2:?usage: $0 <local-dir> <domain> [username]}"
USERNAME="${3:-u815783393}"
ARCHIVE_NAME="deploy-$(date +%Y%m%d-%H%M%S).zip"

[ -n "${HOSTINGER_API_TOKEN:-}" ] || { echo "FAIL: HOSTINGER_API_TOKEN not set"; exit 2; }
[ -d "$LOCAL_DIR" ] || { echo "FAIL: $LOCAL_DIR is not a directory"; exit 1; }

WORKDIR="$(mktemp -d)"
trap 'rm -rf "$WORKDIR"' EXIT
ARCHIVE="$WORKDIR/$ARCHIVE_NAME"

echo "==> Zipping $LOCAL_DIR -> $ARCHIVE_NAME"
(cd "$LOCAL_DIR" && zip -qr "$ARCHIVE" .)

echo "==> Requesting TUS upload URL for $USERNAME / $DOMAIN"
UPLOAD_RESP="$(curl -sf -X POST "$API_BASE/files/upload-urls" \
  -H "Authorization: Bearer $HOSTINGER_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"domain\":\"$DOMAIN\"}")" || {
    echo "FAIL: could not obtain upload URL"; exit 3; }

UPLOAD_URL="$(echo "$UPLOAD_RESP" | python3 -c 'import sys,json;print(json.load(sys.stdin)["url"])')"
AUTH_KEY="$(echo "$UPLOAD_RESP" | python3 -c 'import sys,json;print(json.load(sys.stdin)["auth_key"])')"
REST_AUTH_KEY="$(echo "$UPLOAD_RESP" | python3 -c 'import sys,json;print(json.load(sys.stdin)["rest_auth_key"])')"

SIZE=$(stat -c%s "$ARCHIVE" 2>/dev/null || stat -f%z "$ARCHIVE")
echo "==> TUS create ($SIZE bytes)"
curl -sf -X POST "$UPLOAD_URL/$ARCHIVE_NAME?override=true" \
  -H "X-Auth: $AUTH_KEY" -H "X-Auth-Rest: $REST_AUTH_KEY" \
  -H "Tus-Resumable: 1.0.0" -H "Upload-Length: $SIZE" -H "Upload-Offset: 0" \
  -o /dev/null || { echo "FAIL: TUS create failed"; exit 4; }

echo "==> TUS upload"
curl -sf -X PATCH "$UPLOAD_URL/$ARCHIVE_NAME?override=true" \
  -H "X-Auth: $AUTH_KEY" -H "X-Auth-Rest: $REST_AUTH_KEY" \
  -H "Tus-Resumable: 1.0.0" -H "Content-Type: application/offset+octet-stream" \
  -H "Upload-Offset: 0" --data-binary "@$ARCHIVE" \
  -o /dev/null || { echo "FAIL: TUS upload failed"; exit 4; }

echo "==> Triggering deploy (extract $ARCHIVE_NAME into $DOMAIN's public_html)"
curl -sf -X POST "$API_BASE/accounts/$USERNAME/websites/$DOMAIN/deploy" \
  -H "Authorization: Bearer $HOSTINGER_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"archive_path\":\"$ARCHIVE_NAME\"}" \
  -o /dev/null || { echo "FAIL: deploy trigger failed"; exit 5; }

echo "OK: deployed $LOCAL_DIR to $DOMAIN"
