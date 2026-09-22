#!/usr/bin/env bash
# XREAL SDK(com.xreal.xr)を Packages/ 配下に埋め込みパッケージとして展開し、
# asmdef の不具合(UnityEngine.UI 参照の欠落)をパッチする。
#
# 使い方: tools/setup-xreal-sdk.sh <com.xreal.xr.tar.gz へのパス>
#
# SDKは再配布条件が未確認のためリポジトリにコミットしない(.gitignore で除外)。
# XREAL Developer Portal からtar.gzを手動でダウンロードして、このスクリプトに渡す。
set -euo pipefail

# リポジトリのルート(このスクリプトの1つ上)
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_DIR="${ROOT_DIR}/Packages/com.xreal.xr"
ASMDEF="${PACKAGE_DIR}/Runtime/Unity.XR.XREAL.asmdef"

# エラーメッセージを標準エラーに出して終了する
# 引数: $1 = エラーメッセージ
die() {
  echo "エラー: $1" >&2
  exit 1
}

[[ $# -eq 1 ]] || die "使い方: $0 <com.xreal.xr.tar.gz へのパス>"
TARBALL="$1"
[[ -f "${TARBALL}" ]] || die "tar.gz が見つかりません: ${TARBALL}"

if [[ -e "${PACKAGE_DIR}" ]]; then
  die "既に ${PACKAGE_DIR} が存在します。差し替える場合は先に削除してください。"
fi

# tar.gz の中身は package/ フォルダなので、一時ディレクトリに展開してから移動する
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "${TMP_DIR}"' EXIT
tar -xzf "${TARBALL}" -C "${TMP_DIR}"
[[ -d "${TMP_DIR}/package" ]] || die "tar.gz の中に package/ フォルダがありません"
mkdir -p "${ROOT_DIR}/Packages"
mv "${TMP_DIR}/package" "${PACKAGE_DIR}"

[[ -f "${ASMDEF}" ]] || die "asmdef が見つかりません: ${ASMDEF}"

# SDKの不具合対応: references の先頭に UnityEngine.UI を追加する(既にあれば何もしない)
if grep -q '"UnityEngine.UI"' "${ASMDEF}"; then
  echo "asmdef には既に UnityEngine.UI が含まれています。パッチは不要です。"
else
  sed -i 's/"references": \[/"references": [\n        "UnityEngine.UI",/' "${ASMDEF}"
  grep -q '"UnityEngine.UI"' "${ASMDEF}" || die "asmdef のパッチに失敗しました"
  echo "asmdef に UnityEngine.UI 参照を追加しました。"
fi

echo "完了: ${PACKAGE_DIR}"
