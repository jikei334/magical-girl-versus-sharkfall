# 使用OSS・ライブラリ一覧

| OSS名 | バージョン | ライセンス | 用途 |
| --- | --- | --- | --- |
| XR Plugin Management (`com.unity.xr.management`) | 4.4.1 | Unity Companion License | XRプロバイダー(XREALローダー)の管理 |
| XR Core Utilities (`com.unity.xr.core-utils`) | 2.2.0 | Unity Companion License | XR SDKの共通ユーティリティ(XREAL SDKの依存) |
| Unity UI (`com.unity.ugui`) | 1.0.0(Unity 6組み込み) | Unity Companion License | UI(XREAL SDKの依存) |
| xUnit.net v3 (`xunit.v3`) | 4.0.1 | Apache-2.0 | Unity非依存ロジックの単体テスト(`tests/MagicalGirl.Core.Tests`) |
| XR Legacy Input Helpers (`com.unity.xr.legacyinputhelpers`) | 2.1.11(xr.managementの依存として自動解決) | Unity Companion License | `TrackedPoseDriver`による頭部トラッキング(3DoF)のカメラ回転 |

## OSS以外の依存(リポジトリには含めない)

| 名称 | バージョン | ライセンス | 用途 |
| --- | --- | --- | --- |
| Unity Editor | 6000.0.32f1 | Unity Personal(Unity利用規約) | ゲームエンジン |
| .NET SDK | 10.0(最新LTS) | MIT | Unity非依存ロジックのビルド・テスト実行(ローカル/GitHub Actions) |
| XREAL XR Plugin (`com.xreal.xr`) | 3.0.0-pre.4 | 要確認(パッケージにライセンスファイルなし。XREAL Developer Portalの利用規約に従う) | XREAL Air 2 Pro / Beam Proの頭部トラッキングと表示。再配布条件が未確認のためコミットしない |
