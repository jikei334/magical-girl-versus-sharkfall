using System;
using System.Collections.Generic;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 生成された道路網全体のデータ(道路区間の一覧)。
    /// </summary>
    public sealed class RoadLayout
    {
        /// <summary>道路区間一覧。</summary>
        public IReadOnlyList<RoadSegment> Segments { get; }

        /// <summary>
        /// 引数: segments - 道路区間一覧
        /// 返り値: なし(コンストラクタ)
        /// 例外: segmentsがnullの場合、ArgumentNullExceptionを投げる
        /// </summary>
        public RoadLayout(IReadOnlyList<RoadSegment> segments)
        {
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        }
    }
}
