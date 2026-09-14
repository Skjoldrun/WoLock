#!/usr/bin/env bash
# Aktiviert den System-Standy (Suspend) wieder.
# Für den Server im Schrank (HDMI-Dummy + KRDP + SSH).
#
# Usage:  ./sleep-again.sh
#
set -euo pipefail

SLEEP_TARGETS="sleep.target suspend.target hibernate.target hybrid-sleep.target freeze.target"

echo ">> Aktiviere System-Standy wieder ..."
sudo systemctl unmask ${SLEEP_TARGETS}

echo ">> Prüfe den aktuellen Zustand (\"disabled\"/\"enabled\" = wieder aktiv):"
for t in ${SLEEP_TARGETS}; do
  printf "%-24s " "$t"
  systemctl is-enabled "$t" 2>/dev/null || true
  echo
done

echo ">> Fertig. Der Standby funktioniert wieder wie gewohnt."
