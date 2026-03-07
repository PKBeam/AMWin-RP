# AMWin-RP 
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) &nbsp; ([English](README.md) | [한국어](README-KO.md))

Apple MusicのネイティブWindowsアプリ向けのDiscord Rich Presenceクライアントです。
Last.FMおよびListenBrainzでのスクロブル（再生履歴の保存）もサポートしています。

<image width=450 src="https://github.com/user-attachments/assets/df5d6a83-4630-4384-b521-bc80c286a499" />
&nbsp; &nbsp; 
<image src=https://github.com/user-attachments/assets/ea63ddf1-d822-4ffd-be9d-24e13701fce9 width=300 />

## インストール
AMWin-RPは **Windows 11 24H2以降** を必要とします。

ビルド済みファイルは[こちら](https://github.com/PKBeam/AMWin-RP/releases)で見つけることができます。

### どのリリースを使えばいいですか？
お使いのPCのプロセッサに合わせて **x64** または **ARM64** を選択してください。
その後、標準のリリースか、`NoRuntime` とマークされたリリースのどちらかを選択します。

迷った場合は、ラベルのない標準リリース（`NoRuntime` ではない方）を使用してください。
このバージョンは、実行に必要な .NET のコンポーネントが同梱されているためファイルサイズは大きくなりますが、そのまま動作します。

`NoRuntime` リリースはサイズが非常に小さいですが、[.NET 10 デスクトップランタイム](https://dotnet.microsoft.com/ja-jp/download/dotnet/10.0)がインストールされている必要があります。
ランタイムが未インストールの状態でアプリを起動すると、インストールを促すダイアログが表示されます。

## 使用方法
AMWin-RPを使用するには、[Microsoft Store版](https://apps.microsoft.com/detail/9PFHDD62MXS1)のApple Musicが必要です。

- `.exe` を開いてアプリを起動します。
- AMWin-RPはバックグラウンドで実行され、システムトレイに最小化されます。
- トレイアイコンをダブルクリックすると、設定画面が表示されます。
    - ここから、Windows起動時の自動実行、スクロブル、曲検出などの設定を行えます。
- アプリを終了するには、トレイアイコンを右クリックして「Exit」を選択します。
- デフォルトでは、Rich Presenceを表示するために、Apple Musicアプリが開いており、音楽が再生中である（一時停止していない）必要があります。

**注意**: 仮想デスクトップを使用している場合、AMWin-RPとApple Musicは**同じデスクトップ**に配置されている必要があります。これは、Apple Musicクライアントから情報を取得するために使用しているUI Automationライブラリの技術的な制限によるものです。

## スクロブル
このスクロブラーの実装はオフラインでのスクロブルをサポートしていません。インターネットに接続されていない状態で聴いた曲の履歴は保存されませんのでご注意ください。

### Last.FM
Last.FMから独自のAPI KeyとAPI Secretを取得する必要があります。
[https://www.last.fm/api](https://www.last.fm/api) にアクセスし、「Get an API account」から生成してください。
取得した情報を、Last.FMのユーザー名とパスワードとともに設定メニューで入力してください。

Last.FMのパスワードは、ローカルWindowsアカウントの[Windows資格情報マネージャー](https://support.microsoft.com/ja-jp/windows/%E8%B3%87%E6%A0%BC%E6%83%85%E5%A0%B1%E3%83%9E%E3%83%8D%E3%83%BC%E3%82%B8%E3%83%A3%E3%83%BC%E3%81%AB%E3%82%A2%E3%82%AF%E3%82%BB%E3%82%B9%E3%81%99%E3%82%8B-1b5c916a-6a16-889f-8581-fc16e8165ac0)に保存されます。

### ListenBrainz
設定でユーザートークンを追加することで、ListenBrainzへのスクロブルが可能です。

## バグ報告
新しくIssueを作成する前に、同様の問題が既に報告されていないか確認してください。
問題を報告する際は、関連するログファイル（`%localappdata%\AMWin-RichPresence` にあります）を添付してください。

投稿前に、以下の項目を再確認してください：
- 同様の問題が、オープンまたはクローズされたIssueに存在しないか。
- Discordの設定でRich Presenceの表示が有効になっているか（設定 > アクティビティ設定 > アクティビティのプライバシー > アクティビティを共有）。