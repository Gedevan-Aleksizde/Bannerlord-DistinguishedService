
v1.2.12向けに作り直した Distinguished Service です. JacobsThierry のフォークを元に作成しました. https://github.com/JacobsThierry/Bannerlord-DistinguishedService


## Differences from the original (and other versions)

1. Using MCM: You can change the options by the MCM screen instead of editing settings.xml
2. Full Localizability: You can translate almost all of the text. The only exceptions are internal error messages.
3. Name Pool Extensionability: You can add name suffixes (or,  generally, also prefixes). See the following section
4. Extra minor tweaks: some options are changed for more convenience, and the other less important options are abolished due to compatibility. E. g.:

* You can't change the location of external_namelist.txt. This mod reads only `DistinguishedServiceRedux/external_namelist.txt`
* Rework or Remove options that actually don't work while are shown on the previous Settings.xml

### How to add NPC name formats

You can add extra name formats like `"blah blah <of the foo bar>"`.

create `DistinguishedServiceRedux/ModuleData/module_strings/namelist-suctomized.xml` and add entries as in namelist.xml.

If you want to add a troop-type-specific suffix, you should write it ID like this:

`ServDist_nameformat_<TROOPTYPE>.<NAME>`

`<TROOPTYPE>` must be one of `infantry`, `cavalry`,  or `ranged`.  `<NAME>` is up to you, if only the ID is distinct from any other one.

If you want to add a culture-specific suffix, you should write it ID like this:

`ServDist_nameformat_regional_<CULTURETYPE>.<NAME>`

`<CUTLURETYPE>` must be any culture ID. You can see the IDS from the other XML file. `<NAME>` is up to you, if only the ID is distinct from any other one.

### How to add extra random NPC names 

You can add extra random names only for picked troops by Distinguishd Service.

Open `DistinguishedServiceRedux/external_namelist.txt` and add any name per a row, and save it in UTF-8 encoding.

Then, the picked soldier's name is chosen randomly from this file, and the name will be **deleted** from the file once selected.

HINT: You can add genreral random NPC names by some mods like my [Historical Name Expansion](https://www.nexusmods.com/mountandblade2bannerlord/mods/6129).


### How to add NPC first names.

## To Translators 

You can translate all the text except for some internal error messages. Copy the English language folder and edit the files as usual.

even in the external_namelist.txt, you can assign the localization IDs.

To English users: My English writing is not so sophisticated. To refine the English text, you can edit XML files in `DistinguishedServiceRedux/ModuleData/module_strings` folder. However, the text in the MCM is not included in this folder.

## Compatibility

I tested this mod only with the latest (v1.2.12) Bannerlord, and it does not ensure compatibility with the previous versions.

This mod is potentially incompatible with some mods. But I can't test it with all of the mods. At least, incompatible with the other variants of Distinguished Service.

## When Encountering Bugs

Submit the Butterlib bug report if you have. Tell me the details if Butterlib doesn't run. I can't solve your problem with only posts like "crashed when I'm playing." Remember that I always played earlier than you.

------------

## インストール方法

Harmony, ButterLib, UIExtenderEx, Mod Configuration Menu のインストールが必要です.


## オリジナルとの違い

1. MCMの使用, オプションはsettings.xmlの編集ではなくMCMから変更します
2. 完全に翻訳可能, オリジナルや他のフォークバージョンはローカライゼーションを考慮していませんが, このバージョンは内部エラー以外の全てのテキストを翻訳可能です
3. ランダム名前リストの拡張性: 抜擢されるNPC名の接頭語の種類も変更できます. 方法は後述します.
4. その他細かな変更, 利便性のためModの細かい挙動が変更されたり, 互換性のため重要度の低い一部のオプションが廃止されたりしています.

* NameListファイルの場所が変更不能になりました. 常に DistinguishedServiceRedux/external_namelist.txt のみ読み取り対象となります. また, ファイルは書き換えられません.
* Settings.xmlに項目が存在したものの, 実際には効果のないオプションの削除または実装


### NPCのフルネームの追加・編集方法

NPC名ではなく,「XXのYY」のような, 二つ名や通り名の種類を増やす方法です.

ファイル `DistinguishedServiceRedux/ModuleData/module_strings/namelist.xml` を開いて, 既に書かれているものを参考に `<string>` タグを追加してください. 兵種固有の名称を追加したい場合は, IDは以下の形式で書いてください.

`ServDist_nameformat_<TROOPTYPE>.<NAME>`

`<TROOPTYPE>` は `infantry`, `cavalry`, `archer` のいずれかにしてください (archer は実際にはクロスボウ兵や投擲兵も含まれます). `<NAME>` は, IDが他のものと重複しない限り, 自由にできます.

文化固有の名称を追加したい場合は, IDは以下の形式で書いてください.

`ServDist_nameformat_regional_<CULTURETYPE>.<NAME>`

`<CUTLURETYPE>` はいずれかの文化IDにしてください. 文化IDはどこかのXMLファイルで, `<Culture id="<ID>" ...>` のように書かれています. `<NAME>` は, IDが他のものと重複しない限り, 自由にできます.


### ランダムNPC名の追加

Distinguished Service で生成されるヒーローは, 他のランダム生成NPCと同様に名前がランダムで決まります. ユーザーが設定した名前の中からランダムで選ぶことも可能です.

`DistinguishedServiceRedux/external_namelist.txt` を開いて, 1行ごとに設定したい名前を書き込んでください. ファイルはUTF-8エンコードを想定しています. この名前からランダムに選ばれるたびに, **選ばれた名前がファイルから削除される**ため, ここで設定した名前が重複することはありません. このファイルに何も記載がない場合は, 通常通りあらかじめ設定されたNPC名の一覧からランダムに選ばれます.

ヒント: ランダム生成NPC全般の名称は, Historical Name Expansion などで種類を増やせます. こちらはDS以外の全てのランダムNPCの適用対象で, なおかつ一度選ばれても削除されることがありません.


## インストール方法

Harmony, ButterLib, UIExtenderEx, Mod Configuration Menu のインストールが必要です.