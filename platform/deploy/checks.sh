#!/usr/bin/env bash
# Run by release.sh before the image build (deploy.conf CHECKS). Every platform test must pass.
# The tests need PostgreSQL on localhost, or PLATFORM_TEST_DB (README "Tests").
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet test Platform.slnx
