# セットアップ手順

XREAL Air 2 Pro + Beam Pro 向けのUnityプロジェクトを、クローン直後の状態からビルドできるようにする手順。
実機でハマりやすい点は `XREAL_DEVELOPMENT_NOTES.md`(ローカル資料)にまとめている。

## 前提

- Unity Hub でサインイン済みで、Personalライセンスを有効化済みであること
  (バッチモードは未認証だと `No valid Unity Editor license found` で失敗する)
- Unity **6000.0.32f1** に Android Build Support(SDK/NDK/OpenJDK含む)をインストール済みであること
- XREAL Developer Portal から `com.xreal.xr.tar.gz`(動作確認済み: 3.0.0-pre.4)をダウンロード済みであること

## 手順

1. XREAL SDKを導入する(SDKはコミットしないため、クローンごとに必要)。

   ```bash
   tools/setup-xreal-sdk.sh <com.xreal.xr.tar.gz へのパス>
   ```

   `Packages/com.xreal.xr` に展開し、asmdef の `UnityEngine.UI` 参照欠落(SDKの不具合)をパッチする。
   SDKを別バージョンに差し替えたときは、`Packages/com.xreal.xr` を削除して再実行する。

2. プロジェクトをセットアップする(Player Settings、XRローダー登録、`XREALSettings` 生成、確認用シーン生成)。
   Unityエディタのメニュー `XREAL > Setup Project` でも実行できる。

   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\6000.0.32f1\Editor\Unity.exe" -batchmode -nographics `
     -projectPath <リポジトリのパス> -buildTarget Android `
     -executeMethod MagicalGirl.EditorTools.ProjectSetup.Setup -quit -logFile Logs/setup.log
   ```

3. APKをビルドする(メニュー `XREAL > Build APK` でも可)。成果物は `Builds/MagicalGirl.apk`。

   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\6000.0.32f1\Editor\Unity.exe" -batchmode -nographics `
     -projectPath <リポジトリのパス> -buildTarget Android `
     -executeMethod MagicalGirl.EditorTools.ProjectSetup.Build -quit -logFile Logs/build.log
   ```

   ログは `error CS|Exception|Build result` でgrepして確認する。失敗時は終了コード1になる。

4. Beam Proにインストールして起動する。方法は次の2通り。
   Beam ProのUSBデバッグ接続は不安定で `adb devices` に出なくなることがあるため、その場合はHTTP経由に切り替える。

   ### 方法A: adb

   ```bash
   adb install -r Builds/MagicalGirl.apk
   adb shell am start -n com.jikei334.magicalgirlversussharkfall/com.unity3d.player.UnityPlayerActivity
   ```

   adb接続の詳細(adb over Wi-Fi等)は別Issue(#3)で整備する。

   ### 方法B: HTTP経由(adb不要)

   PC上のビルド成果物をローカルWebサーバーで公開し、Beam Proのブラウザからダウンロードしてインストールする。
   PCとBeam Proは同じLAN(Wi-Fi)に接続しておくこと。

   1. PCでAPKのあるフォルダをHTTPサーバーとして公開する(PythonまたはNode.jsが必要)。

      ```bash
      cd Builds
      python -m http.server 8123 --bind 0.0.0.0
      ```

      Windowsのファイアウォールで、Pythonの受信(プライベートネットワーク)を許可するか聞かれたら許可する。
      PCのLAN内IPアドレスは `ipconfig`(Windows)または `ip addr`(Linux)で確認する。

   2. Beam Proのブラウザで `http://<PCのLAN IP>:8123/MagicalGirl.apk` を開き、APKをダウンロードする。

   3. ダウンロードしたAPKを開いてインストールする。初回は「提供元不明のアプリ」のインストール許可
      (そのブラウザやファイルアプリに対する許可)を求められるので許可する。

   4. Beam Proのアプリ一覧から「Magical Girl versus Sharkfall」を起動する。

   5. インストールが終わったら、PC側のサーバーは `Ctrl+C` で停止する。

   注意:
   - 公開されるのは `Builds/` フォルダの中身だけ。`--bind 0.0.0.0` は同じネットワークの全端末から見えるため、
     信頼できるネットワークでのみ使い、使い終わったら止めること。
   - 別のフォルダ(プロジェクトのルートなど)で実行しないこと。ソースや設定が公開されてしまう。

   共通の注意: Manifest構成が変わる変更(例: `SupportMultiResume` の切り替え)をした後は、
   どちらの方法でも、一度アンインストールしてから入れ直すこと。
   同じ `applicationId` への上書きインストールでは変更が反映されないことがあった。

## 動作確認

グラス側にデバイス種別とトラッキングモードが表示される。
`Device: XREAL_DEVICE_TYPE_AIR2_PRO`、`Tracking: MODE_3DOF` になれば成功。
`INVALID` の場合はグラスとBeam ProのUSB-C接続を疑う(PCとBeam ProのADB接続とは別物)。

## 原因不明のコンパイルエラーが出たとき

`Library/` と `Temp/` を削除してクリーンビルドする。
