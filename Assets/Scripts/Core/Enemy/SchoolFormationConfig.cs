using System;

namespace MagicalGirl.Core.Enemy
{
    /// <summary>
    /// SchoolFormationの生成パラメータ。全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct SchoolFormationConfig
    {
        /// <summary>編隊パターン。</summary>
        public FormationPattern Pattern { get; }

        /// <summary>編隊のメンバー数(1以上)。</summary>
        public int MemberCount { get; }

        /// <summary>メンバー間の間隔(0より大きい値)。</summary>
        public float Spacing { get; }

        /// <summary>
        /// 引数: pattern, memberCount, spacing - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// 例外: memberCountが1未満、またはspacingが0以下の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public SchoolFormationConfig(FormationPattern pattern, int memberCount, float spacing)
        {
            if (memberCount < 1)
                throw new ArgumentOutOfRangeException(nameof(memberCount), memberCount, "memberCountは1以上である必要があります。");
            if (spacing <= 0f)
                throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "spacingは0より大きい必要があります。");

            Pattern = pattern;
            MemberCount = memberCount;
            Spacing = spacing;
        }
    }
}
