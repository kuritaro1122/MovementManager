using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Curve;

namespace KuriTaro.MovementManagement {
    public class MovementManager : MonoBehaviour {
        private MovementController movementController = new MovementController();
        private CallBackManager callBackManager = new CallBackManager();
        public bool Active => this.movementController.Active;
        public bool Stop => this.movementController.Stop;

        // Update is called once per frame
        void Update() {
            this.movementController.Update_(Time.deltaTime);
            this.movementController.MovePosition(this.transform);
            this.callBackManager.Call(this.movementController.T);
        }
        void OnDrawGizmos() {
            if (this.movementController != null) this.movementController.DrawGizmos();
        }

        public MovementManager SetMovement(Vector3 toPos, float duration, bool speedBase = false, bool local = false, params (float, System.Action<MovementManager>)[] flags) {
            ICurve curve;
            Vector3 beginPos = local ? this.transform.localPosition : this.movementController.GetMovedPos(this.transform.position);
            curve = new StraightLine(beginPos, toPos);
            SetMovement(curve, duration, speedBase, local, flags);
            return this;
        }
        // 曲線移動
        public MovementManager SetMovement(Vector3[] points, float duration, bool speedBase = false, bool local = false, params (float, System.Action<MovementManager>)[] flags) {
            List<Vector3> _points = new List<Vector3>(points);
            Vector3 beginPos = local ? this.transform.localPosition : this.movementController.GetMovedPos(this.transform.position);
            _points.Insert(0, beginPos);
            ICurve curve = new SplineCurve(_points.ToArray());
            SetMovement(curve, duration, speedBase, local, flags);
            return this;
        }
        public MovementManager SetMovement(ICurve curve, float duration, bool speedBase = false, bool local = false, params (float, System.Action<MovementManager>)[] flags) {
            this.movementController.Set(this.transform, curve, duration, speedBase, local);
            this.callBackManager.ClearFlags();
            if (flags != null) foreach ((float t, System.Action<MovementManager> callBack) in flags) {
                    this.callBackManager.AddFlag(new Flag(t, () => callBack(this)));
                }
            return this;
        }

        public MovementManager SetMovement(ICurve curve, float duration, bool speedBase = false, params (float, System.Action<MovementManager>)[] flags) {
            return SetMovement(curve, duration, speedBase, false, flags);
        }
        public MovementManager SetMovement(ICurve curve, float duration, params (float, System.Action<MovementManager>)[] flags) {
            return SetMovement(curve, duration, false, false, flags);
        }
        public MovementManager SetPathPos(float t, bool normalized) {
            this.movementController.SetPathPos(t, normalized);
            return this;
        }
        public MovementManager StartMovement() {
            this.movementController.StartMovement();
            return this;
        }
        public MovementManager StopMovement() {
            this.movementController.StopMovement();
            return this;
        }
        public Vector3 RemoveMovement() {
            return this.movementController.RemoveMovement();
        }

        public MovementManager SetTransitionCurve(AnimationCurve curve) {
            this.movementController.SetTransitionCurve(curve);
            return this;
        }

        public Vector3 GetCurrentDirection() => this.movementController.GetCurrentDirection();

        public static (float, System.Action<MovementManager>)[] ConvertFlags<T>(T self, params (float, System.Action<T>)[] flags) {
            (float, System.Action<MovementManager>)[] _flags = new (float, System.Action<MovementManager>)[flags.Length];
            for (int i = 0; i < flags.Length; i++) {
                (float t, System.Action<T> action) = flags[i];
                _flags[i] = (t, m => action(self));
            }
            return _flags;
        }

        class MovementController {
            public bool Active { get; private set; } = false;
            public bool Stop { get; private set; } = false;
            //
            private ICurve curve;
            private float duration;
            private bool speedBase;
            private bool local;
            //
            private Vector3 beginPos;
            private float time;
            private float t;
            public float T {
                get {
                    if (!this.Active) return 0f;
                    else if (this.time == 0) return 1f;
                    else return this.transitionCurve.Evaluate(Mathf.Clamp01(this.t / this.time));
                }
            }
            private const float DirectionPrefOffset = 1f;
            private AnimationCurve transitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            private AnimationCurve defaultTransitionCurve => AnimationCurve.Linear(0, 0, 1, 1);
            // parent
            private Transform self;
            private Transform parent;
            private Matrix4x4 parentMatrix4x4; // parent変更時のTransformを保存
            private Vector3 currentVelocity = Vector3.zero;
            public void Set(Transform self, ICurve curve, float duration, bool speedBase = false, bool local = false) {
                this.curve = curve;
                this.duration = duration;
                this.speedBase = speedBase;
                this.local = local;
                this.self = self;
                // 初めて移動開始した時の、ペアレントとの位置関係を保存
                // ペアレントが変わっても保存し直す
                //UpdateParentOffset();
                this.time = this.speedBase ? (this.curve.GetCurveLength() / this.duration) : this.duration;
                this.t = 0;
                this.Active = true;
                this.Stop = true;
            }
            public void SetTransitionCurve(AnimationCurve animationCurve) {
                this.transitionCurve = animationCurve;
            }
            public void SetPathPos(float t, bool normalized) {
                if (this.curve == null) {
                    Debug.LogError("ScrappianOrderController/curve is null.");
                    return;
                }
                this.t = (normalized ? t : (t / curve.GetCurveLength())) * time;
            }
            public Vector3 GetMovedPos(Vector3 pos) {
                return MovedMatrix4x4.inverse.MultiplyPoint(pos);
            }
            public void StopMovement() => this.Stop = true;
            public void StartMovement() => this.Stop = false;
            public Vector3 RemoveMovement() {
                Vector3 velocity = this.currentVelocity;
                this.curve = null;
                this.duration = default;
                this.speedBase = default;
                this.local = default;
                this.beginPos = default;
                this.time = default;
                this.t = 0f;
                this.transitionCurve = defaultTransitionCurve;
                this.self = default;
                this.parent = null;
                this.parentMatrix4x4 = Matrix4x4.identity;
                this.Active = false;
                this.Stop = false;
                this.currentVelocity = Vector3.zero;
                return velocity;
            }
            public void Update_(float deltaTime, Rigidbody rb = null) {
                UpdateParentOffset();
                if (!this.Active) return;
                if (this.t < this.time) {
                    if (!this.Stop) this.t += deltaTime;
                    this.t = Mathf.Clamp(this.t, 0f, this.time);
                } else {
                    this.Active = false;
                    if (rb != null) rb.velocity = Vector3.zero;
                }
            }
            private void UpdateParentOffset() {
                if (this.self == null) return;
                if (this.parent != this.self.parent) {
                    this.parent = this.self.parent;
                    if (this.parent != null) this.parentMatrix4x4 = this.parent.localToWorldMatrix;
                    else this.parentMatrix4x4 = Matrix4x4.identity;
                }
            }
            private Vector3 GetLocalPosition() {
                if (this.curve == null) {
                    Debug.LogError("ScrappianOrderController/curve is null.");
                    return default;
                }
                Vector3 localPos = this.curve.GetPosition(T, true);
                return localPos + beginPos;
            }
            private Vector3 GetPosition() {
                return MovedMatrix4x4.MultiplyPoint(GetLocalPosition());
            }
            private Matrix4x4 MovedMatrix4x4 {
                get {
                    Matrix4x4 matrix4x4;
                    if (this.parent != null && !this.local) {
                        matrix4x4 = this.parent.localToWorldMatrix * this.parentMatrix4x4.inverse;
                        // ローカル補正
                    } else matrix4x4 = Matrix4x4.identity;
                    return matrix4x4;
                }
            }
            
            public void MovePosition(Transform _transform) {
                if (!this.Active || this.Stop) {
                    currentVelocity = Vector3.zero;
                    return;
                }
                float deltaTime = Time.deltaTime;
                if (deltaTime <= 0) return;
                Vector3 pos = GetPosition();
                if (!local) {
                    if (deltaTime > 0) currentVelocity = (pos - _transform.position) / deltaTime;
                    _transform.position = pos;
                } else {
                    if (deltaTime > 0) currentVelocity = (pos - _transform.localPosition) / deltaTime;
                    _transform.localPosition = pos;
                }
            }

            public Vector3 GetCurrentDirection(float offset = DirectionPrefOffset) {
                float _offset = offset / this.curve.GetCurveLength();
                Vector3 pos = this.curve.GetPosition(T, true);
                Vector3 prePos = this.curve.GetPosition(T - _offset, true);
                Vector3 direction = pos - prePos;
                return direction;
            }
            public void DrawGizmos() {
                if (!this.Active) return;
                BaseCurveTester.DrawCurve(curve, parent: MovedMatrix4x4);
                Vector3 pos = GetPosition();
                Vector3 direction = GetCurrentDirection();
                Gizmos.DrawLine(pos, pos + direction.normalized * 10);
                Gizmos.DrawWireSphere(this.curve.GetPosition(T, true), 1f);
                float _offset = DirectionPrefOffset / this.curve.GetCurveLength();
                Gizmos.DrawWireSphere(this.curve.GetPosition(T - _offset, true), 1f);
            }

        }
        class CallBackManager<V> {
            private List<IFlag> flags = new List<IFlag>();
            public void Call(V _t) {
                foreach (var f in flags) f.TryCall(_t);
            }
            public void FlagsDown() {
                foreach (var f in flags) f.FlagDown();
            }
            public void ClearFlags() {
                flags.Clear();
            }
            public void AddFlag(IFlag flag) {
                if (flags != null) this.flags.Add(flag);
            }
            public interface IFlag {
                void TryCall(V _t);
                void FlagDown();
                bool Compare(V _t);
            }
            public abstract class BaseFlag : IFlag {
                protected V t;
                private System.Action callBack = () => { };
                private bool called = false;

                public BaseFlag(V t, System.Action callBack) {
                    this.t = t;
                    this.called = false;
                    if (callBack != null) this.callBack = callBack;
                }
                public void TryCall(V _t) {
                    if (this.called) return;
                    if (Compare(_t)) {
                        this.callBack();
                        this.called = true;
                    }
                }
                public void FlagDown() => this.called = false;
                public abstract bool Compare(V _t);
            }
        }
        class CallBackManager : CallBackManager<float> { }
        class Flag : CallBackManager.BaseFlag {
            public Flag(float t, System.Action callBack) : base(t, callBack) { }
            public override bool Compare(float _t) => _t >= base.t;
        }
    }
}
