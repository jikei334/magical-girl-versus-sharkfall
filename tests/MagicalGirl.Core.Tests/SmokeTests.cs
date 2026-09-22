using Xunit;

namespace MagicalGirl.Core.Tests
{
    /// <summary>
    /// CI(GitHub Actions)からdotnet testが正しく実行されることを確認するための
    /// 最小限のスモークテスト。Unity非依存ロジックの本格的なテストは、各Issueで
    /// Assets/Scripts/Core配下にコードが追加され次第このプロジェクトに追加していく。
    /// </summary>
    public class SmokeTests
    {
        /// <summary>
        /// テストランナー自体が実行されることだけを確認する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        [Fact]
        public void TestRunner_IsWired()
        {
            Assert.True(true);
        }
    }
}
