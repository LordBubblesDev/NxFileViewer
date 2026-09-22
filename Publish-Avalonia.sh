#!/bin/sh
set -eu

cd "$(dirname "$0")"

APP_NAME="NxFileViewer"
HOST_NAME="NxFileViewer.Ava"
PROJECT="src/NxFileViewer.Ava/NxFileViewer.Ava.csproj"
OUT_DIR="Publish"

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
mkdir -p "$OUT_DIR"
dotnet clean "$PROJECT"

publish_common() {
    rid="$1"
    publish_dir="$2"
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
}

zip_dir() {
    source_dir="$1"
    zip_path="$2"
    entry_root="$3"
    tmp="${OUT_DIR}/.ziptmp"
    rm -rf "$tmp" "$zip_path"
    mkdir -p "${tmp}/${entry_root}"
    cp -a "${source_dir}/." "${tmp}/${entry_root}/"
    (cd "$tmp" && zip -r "$zip_path" "$entry_root")
    rm -rf "$tmp"
}

publish_win() {
    rid="$1"
    release="${APP_NAME}_v${VERSION}_${rid}"
    publish_dir="${OUT_DIR}/${release}_build"
    zip_path="$(pwd)/${OUT_DIR}/${release}.zip"

    echo "============================================================================"
    echo "> Publishing ${release}.zip"
    rm -rf "$publish_dir" "$zip_path"

    publish_common "$rid" "$publish_dir"
    if [ -f "${publish_dir}/${HOST_NAME}.exe" ]; then
        mv "${publish_dir}/${HOST_NAME}.exe" "${publish_dir}/${APP_NAME}.exe"
    fi
    (cd "$publish_dir" && zip -r "$zip_path" .)
    rm -rf "$publish_dir"
}

publish_linux() {
    rid="$1"
    release="${APP_NAME}_v${VERSION}_${rid}"
    publish_dir="${OUT_DIR}/${release}_build"
    zip_path="$(pwd)/${OUT_DIR}/${release}.zip"

    echo "============================================================================"
    echo "> Publishing ${release}.zip"
    rm -rf "$publish_dir" "$zip_path"

    publish_common "$rid" "$publish_dir"
    if [ -f "${publish_dir}/${HOST_NAME}" ]; then
        mv "${publish_dir}/${HOST_NAME}" "${publish_dir}/${APP_NAME}"
    fi
    chmod +x "${publish_dir}/${APP_NAME}" 2>/dev/null || true
    find "$publish_dir" -name '*.so' -exec chmod +x {} +
    zip_dir "$publish_dir" "$zip_path" "$release"
    rm -rf "$publish_dir"
}

publish_win win-x64
publish_win win-arm64
publish_linux linux-x64
publish_linux linux-arm64
echo "Done. Windows and Linux packages are in \"${OUT_DIR}\"."
