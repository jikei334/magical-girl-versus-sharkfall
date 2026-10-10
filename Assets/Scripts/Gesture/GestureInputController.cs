using System;
using System.Collections.Generic;
using MagicalGirl.Core.Gesture;
using UnityEngine;

namespace MagicalGirl.Gesture
{
    /// <summary>
    /// Beam Proのタッチパッドへのなぞり操作を検知し、UnistrokeRecognizerでジェスチャーを
    /// 認識するコンポーネント。タッチ開始から終了までの軌跡を蓄積し、終了時に認識を行う。
    ///
    /// 注意: グラス表示中にBeam Pro側のタッチパッドがUnityの標準タッチ入力(Input.touches)として
    /// 取得できるかは実機未検証。取得できない場合、Androidネイティブ入力など別のAPIへの
    /// 切り替えが必要になる可能性がある。
    /// </summary>
    public class GestureInputController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("この値未満のスコアは不一致(詠唱失敗)として扱う(0〜1)")]
        float m_MatchThreshold = 0.7f;

        [SerializeField]
        [Tooltip("長押し(再キャリブレーション等のトリガー)と判定するまでの保持時間(秒)")]
        float m_LongPressHoldDurationSeconds = 1.2f;

        [SerializeField]
        [Tooltip("長押し判定で「動かしていない」とみなす開始位置からの移動量の上限(px)")]
        float m_LongPressMaxMovementPx = 24f;

        UnistrokeRecognizer m_Recognizer;
        LongPressConfig m_LongPressConfig;
        readonly List<GesturePoint> m_CurrentStroke = new();
        bool m_IsTracking;
        bool m_LongPressFired;
        float m_StrokeStartTime;
        GesturePoint m_StrokeStartPoint;
        float m_MaxMovementFromStart;

        /// <summary>タッチ開始時(詠唱中演出のトリガー用)に発火する。</summary>
        public event Action CastingStarted;

        /// <summary>ジェスチャーがテンプレートに一致した(詠唱成功)ときに発火する。</summary>
        public event Action<GestureRecognitionResult> GestureRecognized;

        /// <summary>ジェスチャーがどのテンプレートにも一致しなかった(詠唱失敗)ときに発火する。</summary>
        public event Action GestureFailed;

        /// <summary>タッチパッドをほぼ動かさずに一定時間押し続けた(長押し)ときに発火する。
        /// 再キャリブレーションなど、ジェスチャーとは別系統のトリガー用。</summary>
        public event Action LongPressDetected;

        /// <summary>
        /// 組み込みテンプレートを持つUnistrokeRecognizerと、長押し判定の設定を構築する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            m_Recognizer = new UnistrokeRecognizer(m_MatchThreshold);
            m_LongPressConfig = new LongPressConfig(m_LongPressHoldDurationSeconds, m_LongPressMaxMovementPx);
        }

        /// <summary>
        /// 毎フレーム、タッチ入力の状態(開始・移動・終了)を追跡する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (Input.touchCount == 0)
            {
                if (m_IsTracking)
                {
                    // タッチが急に取得できなくなった場合(実機での取りこぼし等)も、
                    // 中途半端な状態を残さずその時点までの軌跡で判定を打ち切る。
                    FinishStroke();
                }
                return;
            }

            // 複数タッチには対応しない(なぞりは片手想定)ため、常に最初のタッチだけを見る。
            var touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginStroke(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (m_IsTracking)
                        UpdateStroke(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (m_IsTracking)
                        FinishStroke();
                    break;
            }
        }

        /// <summary>
        /// 軌跡に点を追加し、長押し判定(開始位置からの移動量・経過時間)を更新する。
        /// 長押しと判定された場合はLongPressDetectedを発火し、軌跡の追跡を打ち切る
        /// (指を離したときにジェスチャー失敗として重複処理されないようにするため)。
        /// 引数: position - 現在のタッチ位置
        /// 返り値: なし
        /// </summary>
        void UpdateStroke(Vector2 position)
        {
            var point = ToGesturePoint(position);
            m_CurrentStroke.Add(point);

            if (m_LongPressFired)
                return;

            var dx = point.X - m_StrokeStartPoint.X;
            var dy = point.Y - m_StrokeStartPoint.Y;
            var movement = Mathf.Sqrt(dx * dx + dy * dy);
            if (movement > m_MaxMovementFromStart)
                m_MaxMovementFromStart = movement;

            var elapsed = Time.time - m_StrokeStartTime;
            if (LongPressDetector.IsTriggered(m_LongPressConfig, elapsed, m_MaxMovementFromStart))
            {
                m_LongPressFired = true;
                m_IsTracking = false;
                m_CurrentStroke.Clear();
                LongPressDetected?.Invoke();
            }
        }

        /// <summary>
        /// 軌跡の記録を開始する。
        /// 引数: position - タッチ開始位置
        /// 返り値: なし
        /// </summary>
        void BeginStroke(Vector2 position)
        {
            m_CurrentStroke.Clear();
            m_CurrentStroke.Add(ToGesturePoint(position));
            m_IsTracking = true;
            m_LongPressFired = false;
            m_StrokeStartTime = Time.time;
            m_StrokeStartPoint = ToGesturePoint(position);
            m_MaxMovementFromStart = 0f;
            CastingStarted?.Invoke();
        }

        /// <summary>
        /// 軌跡の記録を終え、認識結果に応じてイベントを発火する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void FinishStroke()
        {
            m_IsTracking = false;

            if (m_CurrentStroke.Count < 2)
            {
                GestureFailed?.Invoke();
                return;
            }

            var result = m_Recognizer.Recognize(m_CurrentStroke);
            if (result.IsMatch)
                GestureRecognized?.Invoke(result);
            else
                GestureFailed?.Invoke();
        }

        /// <summary>タッチ座標(px)をGesturePointへ変換する。</summary>
        static GesturePoint ToGesturePoint(Vector2 position) => new GesturePoint(position.x, position.y);
    }
}
