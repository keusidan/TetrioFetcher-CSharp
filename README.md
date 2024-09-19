# TetrioFetcher-CSharp
> はじめに

このリポジトリは[TETR.IO](https://tetr.io)のAPI([TETRA CHANNEL](https://ch.tetr.io))やリプレイデータ(.ttrmファイル)の詳細statsをC#で取得できるようにしたものです  
**TETR.IOの関係者の皆さんとは全く関連がありません、絶対にTETR.IO関係者の皆さんにこのライブラリの問題等を報告したりしないようにしてください**  
また現在このリポジトリは開発途中であり、大きな変更を加える可能性があります  
開発者はこのプログラムで起きた問題、副作用に関して一切責任を負いません  
外部に公開する初めてのgithubプロジェクトのため、色々足らないなどがあるかもしれません、ご承知ください
Issuesに関してはどんどん受け付けております

> 使い方

[Tetrio.User.Accountクラス](TetrioFetcher-CSharp/TetrioUser.cs)からAPIのデータを取得できます
取得する際に1秒のインターバルが発生します

(Tetrio.RecordData)[TetrioFetcher-CSharp/RecordData.cs)からリプレイデータの詳細を一部取得できます(盤面等は取得できません)
例えば、apmなどのstats、ユーザーの各ラウンドの詳細プレイ時間等です
