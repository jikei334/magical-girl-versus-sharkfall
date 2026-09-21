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

4. Beam Proにインストールして起動する。

   ```bash
   adb install -r Builds/MagicalGirl.apk
   adb shell am start -n com.jikei334.magicalgirlversussharkfall/com.unity3d.player.UnityPlayerActivity
   ```

   Manifest構成が変わる変更をした後は、一度アンインストールしてから入れ直すこと。
   adb接続の詳細は別Issue(#3)で整備する。

## 動作確認

グラス側にデバイス種別とトラッキングモードが表示される。
`Device: XREAL_DEVICE_TYPE_AIR2_PRO`、`Tracking: MODE_3DOF` になれば成功。
`INVALID` の場合はグラスとBeam ProのUSB-C接続を疑う(PCとBeam ProのADB接続とは別物)。

## 原因不明のコンパイルエラーが出たとき

`Library/` と `Temp/` を削除してクリーンビルドする。
