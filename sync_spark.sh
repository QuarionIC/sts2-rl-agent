#!/usr/bin/env bash
# Sync code to the Spark WITH a git-rev stamp, so remote results carry
# provenance instead of "code version : unknown".
set -euo pipefail
cd "$(dirname "$0")"
REV=$(git rev-parse --short HEAD)
DIRTY=$([ -n "$(git status --porcelain)" ] && echo dirty || echo clean)
echo "$REV $DIRTY" > GIT_REV
tar czf /tmp/sts2_sync.tgz --exclude='__pycache__' --exclude='*.pyc' \
    sts2_env scripts docs pyproject.toml GIT_REV
scp -q /tmp/sts2_sync.tgz spark:~/sts2/
ssh spark 'cd ~/sts2 && tar xzf sts2_sync.tgz && rm sts2_sync.tgz && echo "synced rev $(cat GIT_REV)"'
