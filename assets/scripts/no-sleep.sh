#!/usr/bin/env bash
# Deaktiviert den System-Standy (Suspend) für die laufende Sitzung.
# Für den Server im Schrank (HDMI-Dummy + KRDP + SSH).
#
# Usage:  ./no-sleep.sh
#
set -euo pipefail

SLEEP_TARGETS="sleep.target suspend.target hibernate.target hybrid-sleep.target freeze.target"

echo ">> Deaktiviere System-Standy ..."
sudo systemctl mask ${SLEEP_TARGETS}

echo ">> Prüfe den aktuellen Zustand (\"masked\" = deaktiviert):"
for t in ${SLEEP_TARGETS}; do
  printf "%-24s " "$t"
  systemctl is-enabled "$t" 2>/dev/null || true
  echo
done

echo ">> Fertig. Der Rechner fährt jetzt nicht mehr von selbst in den Standby."
echo "   (Bildschirm-Sperre ggf. zusätzlich in den KDE-Einstellungen ausschalten.)"
