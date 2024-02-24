# Traincrew_depMelody

TraincrewでJRの発車メロディボタンを押すと、その駅の発車メロディが流れるアプリです。

## いれかた

リンクはこちら

https://github.com/Kesigomon/Traincrew_depMelody/releases


standaloneがついていない場合、.Netデスクトップランタイムが必要です。

こちらからインストールしてください。

https://dotnet.microsoft.com/ja-jp/download/dotnet/thank-you/runtime-desktop-8.0.2-windows-x64-installer 


## つかいかた
### 音源用意
以下3つの音源を用意しましょう。exeと同じパスに置けばOKです。
- `./sound/館浜_1.wav`
 - 館浜1番乗り場で流れるメロディです。
 - 同じ要領で、駅名_n.wav とすれば、その駅のn番線で流れる音源になります。
- `./sound/default.wav`
  - 発車音声が設定されていない場合はこちらが発車メロディとして流れます
- `./sound/doorClosing_1.wav`
  - 1番線、ドアが閉まりますの音源です。発車メロディを止めたら流れます

### あとは
アプリを起動して、 メロディーを流すときはオン、止めるときはオフにすればOKです。

n番線、ドアが閉まりますも自動で流れます。