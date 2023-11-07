# AMWin-RP
Apple MusicのネイティブWindowsアプリ向けのDiscord Rich Presenceクライアントです。また、Last.FMのscrobblingもサポートしています！

![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) 

![image](https://user-images.githubusercontent.com/18737124/236110561-e11eabf5-d2c4-4fb3-a743-3152a1aef916.png)

![image](https://user-images.githubusercontent.com/18737124/213862194-e02ec9e7-07ab-481f-9dc5-451b9159c903.png)

## インストール

リリースは[こちら](https://github.com/PKBeam/AMWin-RP/releases)で見つけることができます。

もし、[.NET7.0ランタイム](https://dotnet.microsoft.com/ja-jp/download/dotnet/7.0)を持っているか、インストール可能であれば、NoRuntime リリースをダウンロードできます。

それ以外の場合は、NoRuntimeとラベル付されていない別のリリースをダウンロードしてください。
このリリースは、アプリケーションの実行に必要な.NET 7.0のコンポーネントをバンドルしているため、ファイルサイズが大きくなっています。

## 使用方法
Apple Musicの[Microsoft Storeバージョン](https://apps.microsoft.com/store/detail/apple-music-preview/9PFHDD62MXS1) のみがサポートされています。
iTunes、Apple Music via WSA、またはサードパーティのプレイヤーには対応していません。

このアプリはバックグラウンドで実行され、システムトレイに最小化されます。アプリを閉じるには、トレイアイコンを右クリックして「Exit」を選択します。
リッチプレゼンスを表示するためには、Apple Musicアプリが開いており、音楽が再生中である必要があります（一時停止していない状態）。

お好みでアプリをWindows起動時に自動的に実行する設定を行うこともできます。これはトレイアイコンをダブルクリックして設定を変更することで行えます。

<hr/>

## Last.FMでのScrobbling
Last.FMへのScrobblingがサポートされています。Last.FMのAPI KeyとSecret Keyを取得する必要があります。これを生成するには、[https://www.last.fm/api](https://www.last.fm/api) にアクセスし、「Get an API account」を選択してください。これらの情報とLast.FMのユーザー名とパスワードを使用してください：

![AMWin-RP-Scrobbler_Settings](https://user-images.githubusercontent.com/317772/215867741-2999591c-35eb-442a-a349-b8e9046634fb.png)

Last.FMのパスワードは、ローカルWindowsアカウントの[Windows資格情報マネージャ](https://support.microsoft.com/ja-jp/windows/%E8%B3%87%E6%A0%BC%E6%83%85%E5%A0%B1%E3%83%9E%E3%83%8D%E3%83%BC%E3%82%B8%E3%83%A3%E3%83%BC%E3%81%AB%E3%82%A2%E3%82%AF%E3%82%BB%E3%82%B9%E3%81%99%E3%82%8B-1b5c916a-6a16-889f-8581-fc16e8165ac0)に保存されています。

このScrobbling実装ではオフラインScrobblingをサポートしていないため、インターネットに接続していない状態で聴いた曲は失われます。


<hr/>


##　どのように動作しているのでしょうか？

**(技術的な詳細が続きます)**

この最大の課題は、Apple Musicアプリから曲の情報を抽出できるようにすることです。

これは.NETの[UI オートメーション](https://learn.microsoft.com/ja-jp/dotnet/framework/ui-automation/ui-automation-overview)を使用して実現されます。これにより、ユーザーのデスクトップ上の任意のウィンドウのUI要素にアクセスできます。

一般的なプロセスは次のとおりです：
- デスクトップ上でApple Musicウィンドウを探します。
- 次に、我々は望んでいる情報を保持する既知のUIコントロールに移動します（たとえば、曲名など）。
- この情報を抽出し、Discord RPCを処理するプログラムの一部に送信します。

もう1つの問題は、曲のカバーアートを取得することです。

あまり文書化されていませんが、Discord RPCではassets keyの代わりに画像の URL を送信することで、任意の画像を指定できるようになりました。）

私の知る限り、UI オートメーションを使用してウィンドウに表示されている画像を取得することはできません。代わりに、Apple MusicのウェブサイトにHTTPリクエストを送信し、そこから曲を検索してカバー画像のURLを取得しようとします。理想的ではありませんが、ほとんどの場合、我々が求めているものを提供してくれます。

