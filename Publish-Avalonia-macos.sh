#!/bin/sh
set -eu

cd "$(dirname "$0")"
ROOT="$(pwd)"

APP_NAME="NxFileViewer"
HOST_NAME="NxFileViewer.Ava"
PROJECT="src/NxFileViewer.Ava/NxFileViewer.Ava.csproj"
OUT_DIR="Publish"
ICNS_PATH="src/Resources/Icon.icns"
RCODESIGN_VERSION="${RCODESIGN_VERSION:-0.29.0}"
PREFIX="${PREFIX:-/usr/local}"

if [ -z "${VERSION:-}" ]; then
    VERSION="$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$PROJECT" | head -n 1)"
fi
case "$VERSION" in
    v*|V*) VERSION="${VERSION#?}" ;;
esac
if [ -z "$VERSION" ]; then
    VERSION="1.0.0"
fi

echo "${APP_NAME} version: ${VERSION}"

have() {
    command -v "$1" >/dev/null 2>&1
}

install_linux_deps() {
    echo "Installing macOS packaging tools..."
    sudo apt-get update
    sudo apt-get install -y cmake g++ make zlib1g-dev libbz2-dev hfsprogs curl ca-certificates git zip

    arch="$(uname -m)"
    case "$arch" in
        x86_64|amd64) rcodesign_arch="x86_64-unknown-linux-musl" ;;
        aarch64|arm64) rcodesign_arch="aarch64-unknown-linux-musl" ;;
        *)
            echo "Unsupported architecture: $arch"
            exit 1
            ;;
    esac

    workdir="$(mktemp -d)"
    trap 'rm -rf "$workdir"' EXIT
    cd "$workdir"

    curl -fsSL -o rcodesign.tar.gz \
        "https://github.com/indygreg/apple-platform-rs/releases/download/apple-codesign/${RCODESIGN_VERSION}/apple-codesign-${RCODESIGN_VERSION}-${rcodesign_arch}.tar.gz"
    tar -xzf rcodesign.tar.gz
    sudo install -m 755 apple-codesign-*/rcodesign "${PREFIX}/bin/rcodesign"

    git clone --depth 1 https://github.com/mozilla/libdmg-hfsplus.git
    cmake -S libdmg-hfsplus -B libdmg-hfsplus
    make -C libdmg-hfsplus
    sudo install -m 755 libdmg-hfsplus/dmg/dmg "${PREFIX}/bin/dmg"
    sudo install -m 755 libdmg-hfsplus/hfs/hfsplus "${PREFIX}/bin/dmg-hfsplus"

    cd "$ROOT"
    trap - EXIT
    rm -rf "$workdir"

    echo "Installed:"
    command -v rcodesign
    command -v dmg
    command -v dmg-hfsplus
    command -v mkfs.hfsplus
    rcodesign --version
}

ensure_deps() {
   if have rcodesign && have dmg && have dmg-hfsplus && have mkfs.hfsplus; then
        return
    fi

    install_linux_deps
}

sign_bundle() {
    path="$1"
    if have codesign; then
        echo "Ad-hoc signing \"$path\" with codesign."
        codesign --force --deep --sign - "$path"
        return
    fi
    if have rcodesign; then
        echo "Ad-hoc signing \"$path\" with rcodesign."
        rcodesign sign "$path"
        return
    fi
    echo "Skipping code signing (codesign/rcodesign not found)."
}

write_plist() {
    plist="$1"
    version="$2"
    cat > "$plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>dev.lordbubbles.nxfileviewer</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>NX File Viewer</string>
    <key>CFBundleDisplayName</key>
    <string>NX File Viewer</string>
    <key>CFBundleIconFile</key>
    <string>${APP_NAME}.icns</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleShortVersionString</key>
    <string>${version}</string>
    <key>CFBundleVersion</key>
    <string>${version}</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © NX File Viewer</string>
</dict>
</plist>
EOF
}

create_dmg() {
    app_dir="$1"
    dmg_path="$2"
    volume_name="$3"
    stage="${OUT_DIR}/${volume_name}_dmgstage"
    rm -rf "$stage" "$dmg_path"
    mkdir -p "$stage"
    cp -R "$app_dir" "$stage/"

    staged_app="${stage}/${APP_NAME}.app"
    find "$staged_app" -type d -exec chmod 0755 {} +
    find "$staged_app" -type f -exec chmod 0644 {} +
    chmod 0755 "${staged_app}/Contents/MacOS/${APP_NAME}"
    ln -s /Applications "${stage}/Applications"

    if have hdiutil; then
        echo "Creating DMG with hdiutil."
        hdiutil create -volname "$volume_name" -srcfolder "$stage" -ov -format ULFO "$dmg_path"
        rm -rf "$stage"
        sign_bundle "$dmg_path"
        return
    fi

    if ! have mkfs.hfsplus || ! have dmg-hfsplus || ! have dmg; then
        rm -rf "$stage"
        echo "Cannot create DMG: need hdiutil (macOS) or mkfs.hfsplus + dmg-hfsplus + dmg (Linux)."
        exit 1
    fi

    echo "Creating DMG with libdmg-hfsplus."
    staging_size="$(find "$staged_app" -type f -exec stat -c%s {} + | awk '{s+=$1} END {print s}')"
    padding="$(( (staging_size * 15 + 50) / 100 + 5 * 1024 * 1024 ))"
    total_size="$((staging_size + padding))"
    uncompressed="${OUT_DIR}/UNCOMPRESSED_$(basename "$dmg_path")"
    rm -f "$uncompressed"
    dd if=/dev/zero of="$uncompressed" bs=1 count=0 seek="$total_size" status=none
    mkfs.hfsplus -v "$volume_name" "$uncompressed"

    stage_full="$(cd "$stage" && pwd)"
    uncompressed_full="$(cd "$(dirname "$uncompressed")" && pwd)/$(basename "$uncompressed")"
    dir_list="${uncompressed_full}.dirs"
    file_list="${uncompressed_full}.files"
    find "$stage_full" -mindepth 1 -type d | sort > "$dir_list"
    find "$stage_full" -mindepth 1 -type f > "$file_list"

    while IFS= read -r src; do
        rel="${src#"$stage_full"/}"
        dmg-hfsplus "$uncompressed_full" mkdir "/$rel"
    done < "$dir_list"

    added=0
    host_rel="${APP_NAME}.app/Contents/MacOS/${APP_NAME}"
    while IFS= read -r src; do
        rel="${src#"$stage_full"/}"
        dmg-hfsplus "$uncompressed_full" add "$src" "/$rel"
        if [ "$rel" = "$host_rel" ]; then
            dmg-hfsplus "$uncompressed_full" chmod 0755 "/$rel"
        fi
        added=$((added + 1))
    done < "$file_list"

    echo "Added ${added} files to the HFS image."
    rm -f "$dir_list" "$file_list"

    link_stage="${OUT_DIR}/${volume_name}_dmglink"
    rm -rf "$link_stage"
    mkdir -p "$link_stage"
    ln -s /Applications "${link_stage}/Applications"
    dmg-hfsplus "$uncompressed_full" -s clone_link addall "$link_stage" /
    rm -rf "$link_stage"

    dmg dmg -c zlib "$uncompressed_full" "$dmg_path"
    rm -f "$uncompressed_full"
    rm -rf "$stage"
    sign_bundle "$dmg_path"
}

publish_osx() {
    rid="$1"
    release="${APP_NAME}_v${VERSION}_${rid}"
    publish_dir="${OUT_DIR}/${release}_build"
    app_dir="${OUT_DIR}/${APP_NAME}.app"
    dmg_path="${OUT_DIR}/${release}.dmg"

    echo "============================================================================"
    echo "> Publishing ${release}.dmg"
    rm -rf "$publish_dir" "$app_dir" "$dmg_path"

    dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained true \
        -p:Version="$VERSION" \
        -p:SelfContained=true \
        -p:UseAppHost=true \
        -p:PublishSingleFile=true \
        -p:EnableCompressionInSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=false \
        -p:IncludeAllContentForSelfExtract=false \
        -p:DebugType=None \
        -p:DebugSymbols=false \
        -o "$publish_dir"

    host="${publish_dir}/${HOST_NAME}"
    if [ ! -f "$host" ]; then
        echo "macOS apphost missing after publish: $host"
        exit 1
    fi

    mkdir -p "${app_dir}/Contents/MacOS" "${app_dir}/Contents/Resources"
    cp "$host" "${app_dir}/Contents/MacOS/${APP_NAME}"
    chmod 0755 "${app_dir}/Contents/MacOS/${APP_NAME}"

    for lib in "$publish_dir"/*.dylib "$publish_dir"/*.so; do
        [ -f "$lib" ] || continue
        cp "$lib" "${app_dir}/Contents/MacOS/"
    done

    write_plist "${app_dir}/Contents/Info.plist" "$VERSION"
    printf 'APPL????' > "${app_dir}/Contents/PkgInfo"
    if [ ! -f "$ICNS_PATH" ]; then
        echo "macOS icon missing: $ICNS_PATH"
        exit 1
    fi
    cp "$ICNS_PATH" "${app_dir}/Contents/Resources/${APP_NAME}.icns"
    sign_bundle "$app_dir"
    create_dmg "$app_dir" "$dmg_path" "$APP_NAME"
    rm -rf "$app_dir" "$publish_dir"
}

ensure_deps
mkdir -p "$OUT_DIR"
dotnet clean "$PROJECT"
publish_osx osx-x64
publish_osx osx-arm64
echo "Done. macOS packages are in \"${OUT_DIR}\"."
