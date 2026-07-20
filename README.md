# Fireworks Simulator

Unity 6.3 LTSで動く3D花火シミュレーターです。

## 条件への対応

- スタンドアローン実行ファイルを出力できます。
- `TitleScene` と `GameScene` の2シーン構成です。
- タイトル画面とゲーム画面に `Quit` ボタンがあります。強制終了ではなく `Application.Quit()` で終了します。

## 操作

- タイトル画面: `Start` でゲーム開始、`Quit` で終了
- ゲーム画面: クリックで花火を1発発射
- ドラッグ: カメラ回転
- マウスホイール: ズーム
- `Title`: タイトル画面へ戻る
- `Quit`: 終了

## Unity Editorでのビルド

1. Unity 6.3 LTS、6000.3.0f1 以降でこのフォルダを開きます。
2. メニューから `Build > Build Standalone Linux`、`Build Standalone Windows`、または `Build Standalone macOS` を実行します。
3. `Builds/` フォルダに実行ファイルが出力されます。

## コマンドラインビルド例

Linux向け:

```bash
Unity -batchmode -quit -projectPath . -executeMethod BuildStandalone.BuildLinuxBatch
```

出力先:

```text
Builds/FireworksSimulator/FireworksSimulator.x86_64
```
