# unity1week-shiro

Unity1weekのお題 **「しろ」** をテーマに制作するゲームです。

---

# ゲーム概要

雪原の中で、姿の見えない敵から逃げ続けるサバイバルゲームです。

敵そのものは見えず、雪の上に残る**足跡**を頼りに敵の位置を推測します。

しかし、同じ場所を何度も歩くと雪が踏み固められ、その場所には足跡が残らなくなります。

時間が経つにつれて敵の位置を把握しづらくなり、自然と難易度が上昇します。

一定時間生き残ると雪原がリセットされ、敵が増えた次のWaveが始まります。

---

# 開発環境

| 項目 | バージョン |
|------|-----------|
| Unity | 6000.3.18f1 |
| エディタ | Visual Studio Code |
| バージョン管理 | Git / GitHub |

---

# 初めて参加する人へ（環境構築）

## ① このリポジトリをコピー（clone）する

作業したい場所でターミナルを開き、以下を実行してください。

```bash
git clone https://github.com/rhirooka/unity1week-shiro.git
```

cloneが終わったらプロジェクトフォルダへ移動します。

```bash
cd unity1week-shiro
```

---

## ② Unityのバージョンを確認する

このプロジェクトは

**Unity 6000.3.18f1**

で開発しています。

Unity Hubの

```
Installs
```

から同じバージョンがインストールされていることを確認してください。

※違うバージョンで開くとエラーになる可能性があります。

---

## ③ Unity Hubへプロジェクトを追加する

Unity Hubを開き、

```
Add
↓
Add project from disk
```

を選択します。

cloneした

```
unity1week-shiro
```

フォルダを指定してください。

---

## ④ プロジェクトを開く

Unity Hubから

```
unity1week-shiro
```

を開きます。

初回起動時は

- Library
- Temp

などのフォルダをUnityが自動生成するため、少し時間がかかります。

これは正常です。

---

# 開発を始める前に

このプロジェクトでは、

**mainブランチを直接編集しません。**

必ず作業用ブランチを作成してから開発してください。

---

## ① mainを最新にする

まずmainブランチへ移動し、最新状態を取得します。

```bash
git switch main
git pull origin main
```

---

## ② 作業用ブランチを作る

自分専用のブランチを作ります。

例

```bash
git switch -c feature/player-movement
```

ブランチ名は担当する機能に合わせて作成してください。

例

```
feature/player-movement
feature/enemy
feature/footprints
feature/snow-system
feature/wave-system
feature/ui
```

※上記は一例です。機能名に合わせて変更して構いません。

---

# 開発中の流れ

ゲームを作るときは毎回Web Buildする必要はありません。

基本的には

```
コードを書く
↓
UnityでPlay
↓
動作確認
↓
修正
```

を繰り返します。

機能が完成したら

```
commit
↓
push
↓
Pull Request
```

を作成してください。

レビュー後、mainへマージします。

---

# コミット・Pushする方法

変更が終わったら

```bash
git add .
git commit -m "コミット内容"
git push origin 作業ブランチ名
```

またはVS Codeのソース管理から

```
コミット
↓
同期（Push）
```

でも大丈夫です。

---

# Pull Request

GitHub上で

```
Pull Request
```

を作成し、

```
作業ブランチ
↓
main
```

へマージします。

**mainへ直接Pushしないようにしてください。**

---

# フォルダ構成

```
Assets/
Packages/
ProjectSettings/
```

はGitで管理しています。

以下のフォルダはUnityが自動生成するためGit管理しません。

```
Library/
Temp/
Logs/
Build/
UserSettings/
```

---

# テーマ

Unity1week

**お題：「しろ」**

# web版公開
（未記入）

# メンバー

| 名前 | 担当 |
|------|------|
| （未記入） | （未記入） |
| （未記入） | （未記入） |
| （未記入） | （未記入） |