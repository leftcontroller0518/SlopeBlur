# Slope Blur for YMM4

YukkuriMovieMaker4 (YMM4) 用の映像エフェクトプラグインです。
画像の輝度（またはアルファ/RGB）の**勾配（スロープ）**に沿ってサンプルを辿り、平均化することで、
エッジや模様の流れに沿った滑らかなブラーを作ります。処理は Direct2D カスタムエフェクト + HLSL（GPU）で行います。

## パラメータ

| 名前 | 説明 |
|---|---|
| 強さ | 勾配に沿って辿る最大距離（片側, px）。アニメーション可 |
| 方向 | 勾配ベクトルの回転角。0°=勾配に沿う / 90°=等高線に沿って流れる。アニメーション可 |
| 参照 | 勾配の元にする値（輝度 / アルファ / 赤 / 緑 / 青） |
| 勾配の範囲 | 勾配を測る距離（px）。大きいほど緩やかな傾斜にも反応する。アニメーション可 |
| 品質（反復回数） | 片側のサンプル数（1〜32）。大きいほど滑らか・重い |

エフェクトは「ぼかし」カテゴリの **Slope Blur** から追加できます。

## ビルド方法

必要なもの:

- Visual Studio 2022 以降（.NET デスクトップ開発ワークロード）と **.NET 10 SDK**
- Windows 10/11 SDK（`fxc.exe` を使って HLSL をコンパイルします）
- YMM4 本体（.NET 10 ベースのバージョン）

手順:

1. `Directory.Build.props` の `YMM4DirPath` を、YMM4 の `YukkuriMovieMaker4.exe` があるフォルダに書き換える（**末尾の `\` 必須**）。
   - 例: `<YMM4DirPath Condition="'$(YMM4DirPath)' == ''">D:\YMM4\</YMM4DirPath>`
   - コマンドラインから指定する場合: `dotnet build -p:YMM4DirPath="D:\YMM4\"`
2. `SlopeBlur.sln` を Visual Studio で開き、ビルド（Debug / Release）。
3. ビルド後、`SlopeBlur.dll` が `<YMM4DirPath>user\plugin\SlopeBlur\` に自動コピーされる。
4. YMM4 を再起動する。

`fxc.exe` が自動検出されない場合は `Directory.Build.props` に `FxcPath` を指定してください。

## 構成

```
SlopeBlur.sln
Directory.Build.props        YMM4 / fxc のパス設定
SlopeBlur/
  SlopeBlur.csproj
  SlopeBlurEffect.cs           エフェクト定義（パラメータ）
  SlopeBlurEffectProcessor.cs  フレームごとの値の反映
  SlopeBlurCustomEffect.cs     Direct2D カスタムエフェクト
  ShaderResourceUri.cs
  Shaders/SlopeBlur.hlsl       ピクセルシェーダー
```

## アルゴリズム概要

各ピクセルから、参照値の勾配 ∇h を中心差分（範囲＝勾配の範囲）で求め、
方向で回転させた勾配ベクトルに `強さ / 反復回数` を掛けた分だけ位置をずらしながら色をサンプルします。
これを勾配の正・負の両方向へ反復回数ぶん行い、全サンプルを平均します。
勾配が急な場所ほど大きく動き、平坦な場所ではほとんど動きません。
