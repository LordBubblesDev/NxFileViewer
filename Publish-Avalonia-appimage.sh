#!/bin/sh
set -eu

cd "$(dirname "$0")"
ROOT="$(pwd)"

APP_NAME="NxFileViewer"
HOST_NAME="NxFileViewer.Ava"
PROJECT="src/NxFileViewer.Ava/NxFileViewer.Ava.csproj"
OUT_DIR="Publish"
ICON_PNG="src/Resources/Icon.png"
APPIMAGETOOL_VERSION="${APPIMAGETOOL_VERSION:-continuous}"

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
    echo "Installing AppImage packaging tools..."
    sudo apt-get update
    sudo apt-get install -y squashfs-tools wget ca-certificates
}

ensure_deps() {
    if ! have mksquashfs; then
        if have apt-get; then
            install_linux_deps
        else
            echo "mksquashfs is required. On Debian/Ubuntu: sudo apt-get install -y squashfs-tools"
            exit 1
        fi
    fi
    if ! have wget && ! have curl; then
        echo "wget or curl is required to download appimagetool."
        exit 1
    fi
    if [ ! -f "$ICON_PNG" ]; then
        echo "AppImage icon missing: $ICON_PNG"
        exit 1
    fi
}

download() {
    url="$1"
    dest="$2"
    if have wget; then
        wget -q -O "$dest" "$url"
    else
        curl -fsSL -o "$dest" "$url"
    fi
}

ensure_appimagetool() {
    host_arch="$(uname -m)"
    case "$host_arch" in
        x86_64|amd64) tool_arch="x86_64" ;;
        aarch64|arm64) tool_arch="aarch64" ;;
        *)
            echo "Unsupported host architecture for appimagetool: $host_arch"
            exit 1
            ;;
    esac

    tool="${ROOT}/${OUT_DIR}/.appimagetool"
    if [ ! -x "$tool" ]; then
        mkdir -p "$OUT_DIR"
        echo "Downloading appimagetool (${tool_arch})..."
        download \
            "https://github.com/AppImage/appimagetool/releases/download/${APPIMAGETOOL_VERSION}/appimagetool-${tool_arch}.AppImage" \
            "$tool"
        chmod 0755 "$tool"
    fi
    APPIMAGETOOL="$tool"
}

write_desktop() {
    dest="$1"
    cat > "$dest" <<EOF
[Desktop Entry]
Type=Application
Name=NX File Viewer
Comment=Nintendo Switch NSP/XCI file viewer
Exec=${APP_NAME}
Icon=nxfileviewer
Categories=Utility;
Terminal=false
StartupNotify=true
EOF
}

write_apprun() {
    dest="$1"
    cat > "$dest" <<EOF
#!/bin/sh
HERE="\$(dirname "\$(readlink -f "\$0")")"
exec "\$HERE/usr/bin/${APP_NAME}" "\$@"
EOF
    chmod 0755 "$dest"
}

publish_linux() {
    rid="$1"
    publish_dir="$2"
    rm -rf "$publish_dir"
    dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained true \
        -p:Version="$VERSION" \
        -p:SelfContained=true \
        -p:UseAppHost=true \
        -p:PublishSingleFile=true \
        -p:EnableCompressionInSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:IncludeAllContentForSelfExtract=true \
        -p:DebugType=None \
        -p:DebugSymbols=false \
        -o "$publish_dir"

    if [ -f "${publish_dir}/${HOST_NAME}" ]; then
        mv "${publish_dir}/${HOST_NAME}" "${publish_dir}/${APP_NAME}"
    fi
    if [ ! -f "${publish_dir}/${APP_NAME}" ]; then
        echo "Linux apphost missing after publish: ${publish_dir}/${APP_NAME}"
        exit 1
    fi
    chmod 0755 "${publish_dir}/${APP_NAME}"
    find "$publish_dir" -name '*.so' -exec chmod 0755 {} +
}

pack_appimage() {
    rid="$1"
    case "$rid" in
        linux-x64) export ARCH="x86_64" ;;
        linux-arm64) export ARCH="aarch64" ;;
        *)
            echo "Unsupported runtime: $rid"
            exit 1
            ;;
    esac

    release="${APP_NAME}_v${VERSION}_${rid}"
    publish_dir="${OUT_DIR}/${release}_appimage_build"
    appimage_path="$(pwd)/${OUT_DIR}/${release}.AppImage"
    stage="${OUT_DIR}/${release}_appdir"

    echo "============================================================================"
    echo "> Publishing ${release}.AppImage"
    rm -rf "$publish_dir" "$stage" "$appimage_path"

    publish_linux "$rid" "$publish_dir"

    mkdir -p \
        "${stage}/usr/bin" \
        "${stage}/usr/share/applications" \
        "${stage}/usr/share/icons/hicolor/256x256/apps"

    cp -a "${publish_dir}/." "${stage}/usr/bin/"
    write_desktop "${stage}/usr/share/applications/nxfileviewer.desktop"
    ln -s usr/share/applications/nxfileviewer.desktop "${stage}/nxfileviewer.desktop"
    write_apprun "${stage}/AppRun"
    cp "$ICON_PNG" "${stage}/nxfileviewer.png"
    cp "$ICON_PNG" "${stage}/usr/share/icons/hicolor/256x256/apps/nxfileviewer.png"
    chmod 0755 "${stage}/AppRun" "${stage}/usr/bin/${APP_NAME}"

    export APPIMAGE_EXTRACT_AND_RUN=1
    "$APPIMAGETOOL" "$stage" "$appimage_path"
    chmod 0755 "$appimage_path"

    size="$(wc -c < "$appimage_path")"
    if [ "$size" -lt 1048576 ]; then
        echo "AppImage is too small (${size} bytes): $appimage_path"
        exit 1
    fi

    rm -rf "$publish_dir" "$stage"
}

ensure_deps
ensure_appimagetool
mkdir -p "$OUT_DIR"
dotnet clean "$PROJECT"
pack_appimage linux-x64
pack_appimage linux-arm64
echo "Done. Linux AppImages are in \"${OUT_DIR}\"."
