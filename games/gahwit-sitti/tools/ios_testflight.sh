#!/bin/zsh
# Archives the iOS build on a Mac, reports its size, and sends it to TestFlight (missing/23).
#
#   tools/ios_testflight.sh            archive and export a test .ipa, then print its size
#   tools/ios_testflight.sh upload     archive and upload to App Store Connect (TestFlight)
#
# First make the Xcode project: Unity menu Gahwit Sitti > Build iOS, or BuildTools.BuildIos with -executeMethod.
# Uploading needs an App Store Connect API key in the environment, never in the repo (CLAUDE.md):
#   ASC_KEY_PATH (the .p8 file), ASC_KEY_ID, ASC_ISSUER_ID
# DEVELOPMENT_TEAM defaults to the team in Player Settings.
set -euo pipefail
here=${0:A:h}
unity=$here/../unity
project=$unity/Builds/iOS/Unity-iPhone.xcodeproj
out=$unity/Builds/iOS-archive
team=${DEVELOPMENT_TEAM:-QJ9MPD82U5}
mode=${1:-export}

[[ -d $project ]] || { echo "No Xcode project at $project. Build iOS from Unity first."; exit 1; }
rm -rf $out && mkdir -p $out

auth=()
if [[ $mode == upload ]]; then
  : ${ASC_KEY_PATH:?set ASC_KEY_PATH} ${ASC_KEY_ID:?set ASC_KEY_ID} ${ASC_ISSUER_ID:?set ASC_ISSUER_ID}
  auth=(-authenticationKeyPath $ASC_KEY_PATH -authenticationKeyID $ASC_KEY_ID -authenticationKeyIssuerID $ASC_ISSUER_ID)
fi

xcodebuild -project $project -scheme Unity-iPhone -configuration Release -destination generic/platform=iOS \
  -archivePath $out/GahwitSitti.xcarchive -allowProvisioningUpdates $auth \
  DEVELOPMENT_TEAM=$team CODE_SIGN_STYLE=Automatic archive > $out/archive.log 2>&1 \
  || { grep -v "^ENV" $out/archive.log | grep "error:" | sort -u | head; exit 1; }

# app-store-connect + upload sends it straight to TestFlight; debugging exports a .ipa for this team's phones.
# No thinning by default: Xcode's thinning step crashes on some Macs, and the build is arm64 only, so the .ipa is
# already close to what a phone downloads. THIN=1 asks for a thinned .ipa per phone model and a size report.
thinning='&lt;none&gt;'
[[ ${THIN:-0} == 1 ]] && thinning='&lt;thin-for-all-variants&gt;'
if [[ $mode == upload ]]; then method=app-store-connect; destination=upload; else method=debugging; destination=export; fi
cat > $out/ExportOptions.plist <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>method</key><string>$method</string>
  <key>destination</key><string>$destination</string>
  <key>teamID</key><string>$team</string>
  <key>signingStyle</key><string>automatic</string>
  <key>thinning</key><string>$thinning</string>
</dict></plist>
PLIST
xcodebuild -exportArchive -archivePath $out/GahwitSitti.xcarchive -exportPath $out/export \
  -exportOptionsPlist $out/ExportOptions.plist -allowProvisioningUpdates $auth > $out/export.log 2>&1 \
  || { grep -v "^ENV\|^ARGV" $out/export.log | grep -iE "error|failed" | sort -u | head; exit 1; }

echo "App in the archive: $(du -sh $out/GahwitSitti.xcarchive/Products/Applications/*.app | cut -f1)"
if [[ -f $out/export/app-thinning.plist || -d $out/export ]]; then
  ls $out/export/*.ipa 2>/dev/null | while read ipa; do echo ".ipa (compressed, about the download): $(du -h $ipa | cut -f1)  $(basename $ipa)"; done
  [[ -f "$out/export/App Thinning Size Report.txt" ]] && grep -m3 -E "compressed|uncompressed" "$out/export/App Thinning Size Report.txt"
fi
[[ $mode == upload ]] && echo "Uploaded to App Store Connect. It shows in TestFlight after Apple's processing."
