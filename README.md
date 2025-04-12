# game-ai-behaviour
![image](https://user-images.githubusercontent.com/6957962/212475238-3cf6ddfe-6304-4c3b-9b17-27be8459fcc9.png)
## 概要
#### 特徴
* MonoBehaviourに依存しないBehaviourTreeツール
* データ構造と実行処理を分離しやすくして、依存関係を切り離しやすいクラス設計
* シンプルな機能や拡張設計
#### 背景
既存のBehaviourTreeは、AIを取り巻く環境としてヘビーな物が多く、BehaviourTreeをワンポイントで使いたいなと思って作成したツールです。
## セットアップ
#### インストール
1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 以下を入力してインストール
   * https://github.com/DaitokuAmy/game-ai-behaviour.git?path=/Packages/com.daitokuamy.gameaibehaviour
   ![image](https://user-images.githubusercontent.com/6957962/209446846-c9b35922-d8cb-4ba3-961b-52a81515c808.png)

あるいはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記します。
```json
{
    "dependencies": {
        "com.daitokuamy.gameaibehaviour": "https://github.com/DaitokuAmy/game-ai-behaviour.git?path=/Packages/com.daitokuamy.gameaibehaviour"
    }
}
```
バージョンを指定したい場合には以下のように記述します。  
https://github.com/DaitokuAmy/game-ai-behaviour.git?path=/Packages/com.daitokuamy.gameaibehaviour#1.0.0
## 使い方
#### アセット作成
1. ProjectViewの右クリックメニューから、BehaviourTreeのアセットを作成する  
![image](https://user-images.githubusercontent.com/6957962/212539690-4eef8c1a-9738-4661-977f-23440ab55f3c.png)
2. (Blackboard用のAssetがない場合)同じように、Blackboard用のアセットを作成する  
![image](https://user-images.githubusercontent.com/6957962/212539727-c7a3d3a3-2af1-4966-825f-d226a3403fdf.png)
#### BehaviourTreeのエディタでの編集
1. BehaviourTree用のアセットをダブルクリックで開き、Blackboard用のアセットの参照を設定する
![image](https://user-images.githubusercontent.com/6957962/212539880-ac460da8-7997-4439-bc9e-4747147472c5.png)
2. 右クリックメニューから、必要なノードを作成してパスの接続やパラメータの編集を行う  
![image](https://user-images.githubusercontent.com/6957962/212539969-ed96e7d6-6f18-4295-91e3-a3e4f7bd04c4.png)  
![image](https://user-images.githubusercontent.com/6957962/212540090-19ac57b0-865a-4cb0-9b87-b27f3a9d15ae.png)
#### コードからの実行
* 初期化
```C#
_controller = new BehaviourTreeController();
_controller.TickInterval = 1.0f; // 思考頻度
_controller.Setup(_behaviourTree); // BehaviourTreeアセットの参照
```
* 更新
```C#
_controller.Update(Time.deltaTime);
```
* 終了
```C#
_controller.Cleanup();
```

## 拡張方法
#### 純粋なActionNodeを増やす方法(シーン依存度が低い物)
* ActionNodeを継承したクラスを作成する
```C#
public sealed class HogeNode : ActionNode {
  private class Logic : Logic<HogeNode> {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    public Logic(IBehaviourTreeRunner runner, HogeNode node) : base(runner, node) {
    }
    
    /// <summary>
    /// 実行ルーチン
    /// </summary>
    protected override IEnumerator ExecuteRoutineInternal() {
      /* 実行処理 */
      SetState(State.Success);
      yield break;
    }
  }

  /* シリアライズしたい情報 */
  [Tooltip("出力内容のログ")]
  public string hoge = "";

  /// <summary>
  /// ロジックの生成
  /// </summary>
  public override ILogic CreateLogic(IBehaviourTreeRunner runner) {
    return new Logic(runner, this);
  }
}
```
#### HandleableActionNodeを増やす方法(シーン依存度が高い物)
* HandleableActionNodeを継承したクラスを作成する
```C#
public sealed class FooNode : HandleableActionNode {
  [Tooltip("サンプルから文字を取得するためのキー")]
  public string sampleKey;
}
```
* Controllerに処理をBindする
```C#
_controller.BindActionNodeHandler<FooNode>(node => {
  /* ここに実際の更新処理 */
  Debug.Log(GetSampleText(node.sampleKey));

  // Successを返すと処理が完了、Failureなら失敗、Runningなら継続
  return IActionNodeHandler.State.Success;
});
```
* ControllerにHandlerクラスをBindする(別のBind方法)
```C#
public class FooNodeHandler : ActionNodeHandler<FooNode> {
  private Sample _sample;
  
  public void Setup(Sample sample) {
    _sample = sample;
  }
  
  protected override bool OnEnterInternal(FooNode node) {
    /* 開始処理(falseを返すと実行失敗) */
    return true;
  }

  protected override IActionNodeHandler.State OnUpdateInternal(FooNode node) {
    /* 更新処理 */
    Debug.Log(_sample.GetSampleText(node.sampleKey));
    return IActionNodeHandler.State.Success;
  }

  protected override void OnExitInternal(FooNode node) {
    /* 終了処理 */
  }
}
```
```C#
_controller.BindActionNodeHandler<FooNode, FooNodeHandler>(onInit: handler => {
  handler.Setup(this);
});
```

## 機能
#### Root
* Root Node  
![root_node](https://user-images.githubusercontent.com/6957962/212528550-d828bee6-d619-4493-93d1-00a64157b9d0.jpg)
  * BehaviourTreeの起点となるノード

#### Composite
* Selector Node  
![selector_node](https://user-images.githubusercontent.com/6957962/212528596-fff170a3-109d-4cb5-b7a9-587e9b7a37c3.jpg)
  * 左から成功するまで順次実行するノード
  * 子ノードが成功した場合、自身も成功となる
  * 全ての子ノードの実行が失敗した場合、自身も失敗となる
  * Force SuccessをONにした場合は、失敗した時も自身が成功となる

* Sequencer Node  
![sequencer_node](https://user-images.githubusercontent.com/6957962/212528605-244271cd-c36a-4d17-a810-b73203eee5d8.jpg)
  * 左から失敗するまで順次実行するノード
  * 子ノードが失敗した場合、自身も失敗となる
  * 全ての子ノードの実行が成功した場合、自身も成功となる
  * Force SuccessをONにした場合は、失敗した時も自身が成功となる

* Random Selector Node  
![random_selector](https://github.com/DaitokuAmy/game-ai-behaviour/assets/6957962/54b58db9-ecf4-43b8-bfb0-d4fe6630fcf8)
  * 割り当てられたWeightに合わせて確率で子ノードを実行するノード
  * 抽選された子ノードが失敗した場合、次の子ノードを抽選し続ける
  * 子ノードが成功した場合、自身も成功となる
  * 全ての子ノードの実行が失敗した場合、自身も失敗となる
  * Force SuccessをONにした場合は、失敗した時も自身が成功となる

* Random Sequencer Node  
![random_sequencer](https://github.com/DaitokuAmy/game-ai-behaviour/assets/6957962/0de40cd8-7218-499b-95d5-b9e30e4a4ca9)
  * 割り当てられたWeightに合わせて確率で子ノードを実行するノード
  * 抽選された子ノードが失敗した場合、自身も失敗となる
  * 子ノードが成功した場合、次の子ノードを抽選し続ける
  * 全ての子ノードの実行が成功した場合、自身も成功となる
  * Force SuccessをONにした場合は、失敗した時も自身が成功となる

#### Decorator
* If Node  
![if_node](https://user-images.githubusercontent.com/6957962/212528619-c481ef71-2c6e-4e1f-98f5-e49ecfd52ef2.jpg)
  * 指定された条件全てが通った場合、子ノードを実行するノード
  * 条件を失敗した or 子ノードが失敗した場合、自身も失敗となる
  * 子ノードが成功した場合、自身も成功となる

* While Node  
![while_node](https://user-images.githubusercontent.com/6957962/212528622-a559864e-dbd1-4eee-a1c4-34834c03b45c.jpg)
  * 指定された条件全てが通っている間、子ノードを実行し続けるノード
  * 条件を失敗した場合、自身は成功となる
  * 子ノードが失敗した場合、自身も失敗となる

* Repeat Node  
![repeat_node](https://user-images.githubusercontent.com/6957962/212528629-f5bca380-0584-4140-98bd-945bd059e4c5.jpg)
  * 指定された回数、子ノードを実行し続けるノード
  * 指定された回数分、子ノードの実行が完了した場合、自身も成功となる
  * 子ノードが失敗した場合、自身も失敗となる
 
* Retry Node  
![retry](https://github.com/DaitokuAmy/game-ai-behaviour/assets/6957962/0e6e6786-b26f-4478-be31-18644b141e05)
  * 子ノードが失敗した場合、指定された回数リトライし続けるノード
  * 指定された回数分、子ノードの実行が失敗した場合、自身も失敗となる
  * 子ノードが成功した場合、自身も成功となる

* Through Node  
![through_node](https://user-images.githubusercontent.com/6957962/212528639-834a5d1a-5026-41bd-a83e-dfbad9f04da6.jpg)
  * 子ノードを実行するだけのノード
  * 子ノードが成功した場合、自身も成功となる
  * 子ノードが失敗した場合、自身も失敗となる

* Set Integer/Float/String/Boolean  
![set_integer_node](https://github.com/user-attachments/assets/65ba9820-4bb5-4c19-8a7b-608dc3f26859)
![set_float_node](https://github.com/user-attachments/assets/da6d708b-f4bb-4fb8-a533-8164bd441324)
![set_string_node](https://github.com/user-attachments/assets/9a401564-5a80-4fdc-9027-690fe49f434f)
![set_boolean_node](https://github.com/user-attachments/assets/e90fc589-2771-4cbe-ba22-132edf9dfec0)
  * 通る際にPropertyの値を変更するノード
  * 子ノードが成功した場合、自身も成功となる
  * 子ノードが失敗した場合、自身も失敗となる

#### Action
* Log Node  
![log_node](https://user-images.githubusercontent.com/6957962/212528664-e759e37d-cda5-4243-9147-8827c328e508.jpg)
  * ログを出力するだけのノード

* Reset Node  
![reset_node](https://user-images.githubusercontent.com/6957962/212528674-aea1b816-b201-4f33-b8ec-90e93ee27004.jpg)
  * 思考をリセットし、改めてRootNodeから再実行を促すためのノード

* Wait Node  
![wait_node](https://user-images.githubusercontent.com/6957962/212528678-de3b5dfe-fcde-4d49-9247-541388259c3c.jpg)
  * 指定時間経過するまで実行を一時停止するためのノード

#### Function Root
* Function Root Node  
![function_root_node](https://user-images.githubusercontent.com/6957962/212528686-29b21c48-ef4a-46fd-aa0a-ce1c34cd6098.jpg)
  * 関数の実行ルートとなるノード

#### Link
* Function Node  
![function_node](https://user-images.githubusercontent.com/6957962/212528700-c6faabfd-e505-4a69-a256-1750a12cd788.jpg)
  * 関数の実行を行うためのノード

* Sub Tree Node  
![sub_tree_node](https://user-images.githubusercontent.com/6957962/212528696-cd976a75-7dc6-4d3a-adbd-276336e44a11.jpg)
  * 他のBehaviourTreeアセットのRootNodeを実行するためのノード

#### Blackboard
![image](https://user-images.githubusercontent.com/6957962/212540180-02c1f40a-641c-4b0a-b095-a44e02a5889a.png)  
プロパティを定義し、実行中プログラムからこの値を書き換える事で、Treeの分岐などに動的な影響を与えるために使用します  
また、ここで定義したプロパティは「If, While」といった条件指定を行うノードにて使用可能になっています
* Property
  * Integer
    * 整数型のプロパティ
  * Float
    * 小数点型のプロパティ
  * String
    * 文字列型のプロパティ
  * Boolean
    * 論理型のプロパティ
