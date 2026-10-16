#!/usr/bin/env bash
# Collect evidence for intermittent WorldBox startup failures without changing app data.
set -Eeuo pipefail
PACKAGE="${1:?Usage: bash modding/diagnose_startup.sh <android.package.id> [attempts] [wait_seconds]}"
ATTEMPTS="${2:-30}"
WAIT="${3:-15}"
[[ "$ATTEMPTS" =~ ^[0-9]+$ ]] && (( ATTEMPTS > 0 )) || { echo "attempts must be positive integer" >&2; exit 2; }
[[ "$WAIT" =~ ^[0-9]+$ ]] && (( WAIT > 0 )) || { echo "wait_seconds must be positive integer" >&2; exit 2; }
command -v adb >/dev/null || { echo "adb is required" >&2; exit 2; }
adb get-state | grep -qx device || { echo "Exactly one authorized adb device is required" >&2; exit 2; }
adb shell pm path "$PACKAGE" | grep -q '^package:' || { echo "Package not installed: $PACKAGE" >&2; exit 2; }
OUT="startup-diagnostics-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$OUT"
adb shell getprop ro.build.version.release > "$OUT/android-version.txt"
adb shell dumpsys package "$PACKAGE" > "$OUT/package.txt"
echo "attempt,result,log" > "$OUT/results.csv"
for ((i=1;i<=ATTEMPTS;i++)); do
  adb shell am force-stop "$PACKAGE" >/dev/null
  adb logcat -c
  adb shell monkey -p "$PACKAGE" -c android.intent.category.LAUNCHER 1 > "$OUT/launch-$i.txt" 2>&1 || true
  sleep "$WAIT"
  adb logcat -d -v threadtime > "$OUT/logcat-$i.txt"
  # Capture native tombstone/crash reports, Java crashes and Mono/Unity failures.
  if grep -Eiq 'FATAL EXCEPTION|Fatal signal|backtrace:|AndroidRuntime.*FATAL|SIGSEGV|SIGABRT|mono.*(abort|fatal)|Unable to load assembly|DllNotFoundException|TypeLoadException|MissingMethodException' "$OUT/logcat-$i.txt"; then
    result="possible-crash"
  else
    result="no-crash-signature"
  fi
  echo "$i,$result,logcat-$i.txt" >> "$OUT/results.csv"
  echo "Attempt $i/$ATTEMPTS: $result"
done
tar -czf "$OUT.tar.gz" "$OUT"
echo "Saved $OUT.tar.gz. Review each log: signatures are evidence, not proof of app-specific failure."
