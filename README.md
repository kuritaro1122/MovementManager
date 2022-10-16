# MovementManager
GameObjectの移動を制御する。指定座標へ移動したり、パス上を移動することができる。

# Requirement
* System
* System.Collections.Generic
* UnityEngine
* [Curve](https://github.com/kuritaro1122/Curve)

# Usage
① 任意のGameObjectにMovementManagerをコンポーネント\
② Set関数で目標の回転を設定\
③ StartMovement関数で移動開始
```cs
MovementManager manager;
float time = 5f;

manager.SetMovement(new Vector3(0, 0, 0), time, speedBase: false)
    .StartMovement();
```

# Public Function
```cs
// 移動を設定
MovementManager SetMovement(Vector3 toPos, float duration, bool speedBase = false, bool local = false, params (float, System.Action<MovementManager>)[] flags)
MovementManager SetMovement(Vector3[] points, float duration, bool speedBase = false, bool local = false, params (float, System.Action<MovementManager>)[] flags)
MovementManager SetMovement(ICurve curve, float duration, bool speedBase = false, bool local = false, params (float, System.Action<MovementManager>)[] flags)
MovementManager SetMovement(ICurve curve, float duration, bool speedBase = false, params (float, System.Action<MovementManager>)[] flags)
MovementManager SetMovement(ICurve curve, float duration, params (float, System.Action<MovementManager>)[] flags)
// 現在の位置を変更（移動経路上）
MovementManager SetPathPos(float t, bool normalized)
// 移動を開始/再開
MovementManager StartMovement()
// 移動を一時停止
MovementManager StopMovement()
// 移動を初期化
Vector3 RemoveMovement()
// 移動時間の分布を変更
MovementManager SetTransitionCurve(AnimationCurve curve)
// 現在の移動方向を取得
Vector3 GetCurrentDirection()
```

# Static Function
```cs
static (float, System.Action<MovementManager>)[] ConvertFlags<T>(T self, params (float, System.Action<T>)[] flags)
```

# Note
flagが正しく動作しない可能性があります.
パスの設定には[ICurve](https://github.com/kuritaro1122/Curve)インターフェースを実装したクラスを引数として渡す必要があります.

### 関連
* [ObjectOrderController](https://github.com/kuritaro1122/ObjectOrderController)
* [RotateManager](https://github.com/kuritaro1122/RotateManager)

# License
"MovementManager" is under [MIT license](https://en.wikipedia.org/wiki/MIT_License).
