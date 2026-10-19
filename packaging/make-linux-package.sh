#!/usr/bin/env bash
# Builds <out>/Dropit-<version>-linux-x64.tar.xz from a published app folder and fills in the AUR PKGBUILD.
#   packaging/make-linux-package.sh <version> <publish-dir> [out-dir] [source-url-or-file]
# The last argument overrides where the PKGBUILD downloads the tarball from (CI uses the local file to test it).
set -euo pipefail
version="$1"
publish="$2"
out="${3:-dist}"
root="$(cd "$(dirname "$0")/.." && pwd)"
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

mkdir -p "$stage/dropit/app" "$stage/dropit/share" "$out"
cp -a "$publish/." "$stage/dropit/app/"
"$publish/dropit" integrate --export "$stage/dropit/share" > /dev/null
cp "$root/src/Dropit.Linux/Assets/dropit.png" "$stage/dropit/share/dropit.png"

cp "$root/LICENSE" "$root/THIRD-PARTY-NOTICES.md" "$stage/dropit/share/"

tarball="Dropit-$version-linux-x64.tar.xz"
XZ_OPT="-9e -T0" tar -C "$stage" --owner=0 --group=0 -cJf "$out/$tarball" dropit
sha="$(sha256sum "$out/$tarball" | cut -d' ' -f1)"
source="${4:-$tarball::https://github.com/BrO-ken/Dropit/releases/download/v$version/$tarball}"

license="LicenseRef-AllRightsReserved"
if [ -f "$root/LICENSE" ] && grep -q "MIT License" "$root/LICENSE"; then license="MIT"; fi

sed -e "s|@VERSION@|$version|" \
    -e "s|@SOURCE@|$source|" \
    -e "s|@SHA256@|$sha|" \
    -e "s|@LICENSE@|$license|" \
    "$root/packaging/aur/PKGBUILD.in" > "$out/PKGBUILD"
echo "$out/$tarball"
echo "$out/PKGBUILD"
