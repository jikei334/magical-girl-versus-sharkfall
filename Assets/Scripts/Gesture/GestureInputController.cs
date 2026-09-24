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

        UnistrokeRecognizer m_Recognizer;
        readonly List<GesturePoint> m_CurrentStroke = new();
        bool m_IsTracking;

        /// <summary>タッチ開始時(詠唱中演出のトリガー用)に発火する。</summary>
        public event Action CastingStarted;

        /// <summary>ジェスチャーがテンプレートに一致した(詠唱成功)ときに発火する。</summary>
        public event Action<GestureRecognitionResult> GestureRecognized;

        /// <summary>ジェスチャーがどのテンプレートにも一致しなかった(詠唱失敗)ときに発火する。</summary>
        public event Action GestureFailed;

        /// <summary>
        /// 組み込みテンプレートを持つUnistrokeRecognizerを構築する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            m_Recognizer = new UnistrokeRecognizer(m_MatchThreshold);
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
                        m_CurrentStroke.Add(ToGesturePoint(touch.position));
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (m_IsTracking)
                        FinishStroke();
                    break;
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
