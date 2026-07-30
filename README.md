# unity1week-shiro

Unity1week「しろ」向けに制作するゲームです。

## ゲーム概要

雪原の中で、姿の見えない敵から逃げ続けるサバイバルゲームです。

敵そのものは見えず、雪の上に残る足跡を頼りに位置を把握します。

ただし、同じ場所を何度も通ると雪がなくなり、足跡が残らなくなります。
時間が経つほど敵の位置を把握しづらくなり、自然に難易度が上昇します。

一定時間生き残ると雪が復活し、次のWaveでは敵の数が増えます。

## 開発環境

- Unity 6000.3.18f1
- Visual Studio Code
- Git / GitHub

## 開発ルール

原則として `main` ブランチを直接編集せず、機能ごとにブランチを作成します。

例：

- `feature/player-movement`
- `feature/enemy`
- `feature/footprints`
- `feature/snow-system`
- `feature/wave-system`
- `feature/ui`

## 起動方法

1. このリポジトリをcloneする
2. Unity Hubを開く
3. `Add project from disk` を選択
4. cloneしたフォルダを指定
5. Unity 6000.3.18f1で開く

## テーマ

unity1week お題：「しろ」