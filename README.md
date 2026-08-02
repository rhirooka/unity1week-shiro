# unity1week-shiro

Unity1weekのお題 **「しろ」** をテーマに制作するゲームです。

---

# ゲーム概要

「Snow Escape」は、雪原で姿の見えない鬼から逃げ続け、生存時間を競うタイムアタック型サバイバルゲームです。

通常の鬼は姿が見えないため、雪の上に残る**足跡**から接近方向を推測します。プレイヤー自身の足跡は鬼の行動に影響しません。

雪の上に刻まれた足跡は消えずに残り続けます。時間の経過とともに足跡が増えて進行経路を判別しづらくなり、難易度が上昇します。

プレイヤーのライフは3つです。鬼との接触でライフを1つ失い、その後2秒間は無敵になります。すべてのライフを失うとゲーム終了となり、その時点の生存時間が記録されます。

ゲーム開始時には4体の鬼が出現し、その後は10秒ごとに1体ずつ追加されます。また、60秒ごとにその場から動かない鬼が追加され、プレイヤーが3メートル以内へ近づくと姿が見えるようになります。

雪原のリセットやWave制はありません。

---

# 操作方法

`C`キーで俯瞰視点と一人称視点を切り替えられます。

## 俯瞰視点

| 操作 | キー |
|------|------|
| 移動 | 矢印キー |
| ダッシュ | Space |
| 一人称視点へ切り替え | C |

## 一人称視点

| 操作 | キー・入力 |
|------|-----------|
| 前後移動 | W / S |
| 左右移動 | A / D |
| 視点操作 | マウス |
| ダッシュ | Space |
| 俯瞰視点へ切り替え | C |

一人称視点ではマウスカーソルが画面中央に固定されます。Spaceを押しながら移動・マウス操作を行うことで、視点を動かしながらダッシュできます。

ダッシュ中はスタミナを消費し、ダッシュしていない間に回復します。

---

# ゲームの実行方法

1. Unity Hubから`unity1week-shiro`をUnity 6000.3.18f1で開きます。
2. `Assets/Scenes/SampleScene.unity`を開きます。
3. Unity Editor上部のPlayボタンを押します。
4. タイトル画面の「スタート」を押します。

ゲーム空間やUIは実行時に自動生成されるため、シーンへ追加のPrefabを配置する必要はありません。

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
feature/ui
feature/first-person-camera
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

ゲームの主要ファイルは次のとおりです。

```text
Assets/
├─ Scenes/
│  └─ SampleScene.unity
└─ SnowEscape/
   ├─ SnowEscapeGame.cs      # ゲーム進行・敵・足跡・UI・フィールド
   ├─ SnowEscapePlayer.cs    # プレイヤーの移動・スタミナ・見た目
   ├─ SnowEscapeCamera.cs    # 俯瞰・一人称カメラ
   ├─ Meterials/             # ペンギン用マテリアル
   ├─ Prefabs/               # Prefab格納先
   └─ Editor/                # ペンギンPrefab作成用Editorスクリプト
```

次のフォルダはGitで管理します。

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

# Web版公開

`reference/web-demo/index.html`に、ゲーム設計の参考となったWebデモがあります。Unity版とは一部の実装・操作が異なります。

# メンバー

| 名前 | 担当 |
|------|------|
| （未記入） | （未記入） |
| （未記入） | （未記入） |
| （未記入） | （未記入） |
