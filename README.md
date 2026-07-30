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

## 開発環境

- Unity 6000.3.18f1
- Visual Studio Code
- Git / GitHub

## 環境構築・起動方法

### 1. リポジトリをclone

作業したい場所でターミナルを開き、以下を実行します。

git clone https://github.com/rhirooka/unity1week-shiro.git

cloneしたフォルダへ移動します。

cd unity1week-shiro

### 2. Unityのバージョンを確認

本プロジェクトでは以下のUnity Editorを使用します。

Unity 6000.3.18f1

Unity Hubの `Installs` から、同じバージョンがインストールされていることを確認してください。

### 3. Unity Hubにプロジェクトを追加

Unity Hubを開き、

Add
→ Add project from disk

を選択します。

その後、cloneした `unity1week-shiro` フォルダを指定してください。

### 4. Unityプロジェクトを起動

Unity HubのProjectsに追加された `unity1week-shiro` を開きます。

初回起動時は `Library` などのファイルが自動生成されるため、起動に時間がかかる場合があります。

## 開発を始めるとき

`main` ブランチを直接編集せず、作業ごとにブランチを作成します。

まず `main` を最新の状態にします。

git switch main
git pull origin main

その後、作業用ブランチを作成します。

例：

git switch -c feature/player-movement

作業内容に応じて、以下のようなブランチ名を使用します。

- `feature/player-movement`
- `feature/enemy`
- `feature/footprints`
- `feature/snow-system`
- `feature/wave-system`
- `feature/ui`
（上記のは一例なので変更可能性あり）

実装が完了したらcommit・pushし、GitHubでPull Requestを作成して `main` にマージします。

## テーマ

unity1week お題：「しろ」