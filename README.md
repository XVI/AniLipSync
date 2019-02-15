# AniLipSync: AniCast LipSync Library
OVRLipSyncをベースに、リミテッドアニメっぽいリップシンクを実現するためのライブラリです。

# 動作検証済みの環境
- Windows 10 Version 1803 Build 17134.471
- OVRLipSync Version 1.30.0
- Unity 2018.1.0f2

# サンプル
`Assets/AniLipSync/Examples/Scenes/AniLipSync.unity` にサンプルシーンがあります。マイクでしゃべると唇のモデルがリップシンクで動きます。

実行前に[OVRLipSync](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/)のインポートが必要です。

# 使い方
1. [OVRLipSync](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/)をインポート
2. [AniLipSync.unitypackage](https://github.com/XVI/AniLipSync/releases) をインポート
3. `Assets/Oculus/LipSync/Prefabs/LipSyncInterface` プレハブをシーンに配置
4. `Assets/AniLipSync/Prefabs/AniLipSync` プレハブをシーンに配置
5. `AniLipSync` GameObjectの `AnimMorphTarget` の各プロパティをインスペクタで編集（とくに`Skinned Mesh Renderer`と`Viseme To Blend Shape`は変更が必要です）

# AnimMorphTargetの各プロパティの説明
## Transition Curves
aa, E, ih, oh, ou のそれぞれの音素へ遷移する際に、BlendShapeの重みを時間をかけて変化させるためのカーブです。

例えば、黙っている状態から aa の音素を検知した場合、Element 0 のカーブに従って時々刻々とBlendShapeの重みを変化させます。aa の状態で ih の音素を検知した場合、Element 2 のカーブに従います。

徐々に重みを増やすことで、ゆるやかに口の形が変化するような表現が可能です。

## Curve Amplifier
Transition Curveの縦軸の値に倍率をかけます。

Transition Curveの縦軸を 0.0 ～ 1.0 の範囲にしておいて 100 の倍率をかけることで、カーブ編集時の煩雑な操作を省略できます。

## Weight Threashold
最大の重みを持つ音素が閾値以下の重みの場合は、前フレームの口の形を維持します。

小さなノイズ等で口が開いてしまう現象を回避できます。また、口の形が頻繁に切り替わるのを防ぐことができます。

## Frame Rate
1秒間にBlendShapeの重みを更新する頻度の設定です。

リミテッドアニメ風の効果を得ることができます。

## Skinned Mesh Renderer
唇のBlendShapeを持ったSkinnedMeshRendererを指定してください。

## Viseme To Blend Shape
aa, E, ih, oh, ouの順でBlendShapeのIndexを指定してください。

## Rms Threshold
無音を検知しているときにこのRMS値（音量）より大きい場合は、前フレームの口の形を維持します。

長音を発声しているときに口が閉じてしまうことを防ぎます。

# 制限事項
- 声を入力してから口が動くまでに遅延があります。配信ソフトや動画編集ソフトを使用して音声を250msほど遅延させるとタイミングが合うでしょう。
- `LowLatencyLipSyncContext`はOVRLipSyncより後に実行する必要があります。AniLipSync.unitypackageのインポートで自動的に設定されますが、スクリプトのみをコピーする際は`Script Execution Order`による設定が必要です。

# ライセンス
本リポジトリに上がっている部分はMIT Licenseです。LICENSEファイルを参照してください。クレジットを記載することで、営利・非営利を問わず利用いただけます。

OVRLipSyncのライセンスについては、Oculus社のサイトを参照してください。
