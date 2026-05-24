using System.Text.RegularExpressions;
using MallangTwins.Data;
using MallangTwins.Save;
namespace MallangTwins.Agents {
    public static class SafeTextFilter {
        static readonly string[] BannedWords = { "죽", "살해", "혐오", "욕설", "바보", "멍청", "꺼져" };
        public static string CleanName(string input, string fallback) {
            if (string.IsNullOrWhiteSpace(input)) return fallback;
            string t = Regex.Replace(input.Trim(), @"[^\p{L}\p{N}가-힣\s]", "");
            if (t.Length > 8) t = t.Substring(0, 8);
            return ContainsBannedWord(t) ? fallback : t;
        }
        public static string CleanStoryLine(string input, string fallback) {
            if (string.IsNullOrWhiteSpace(input)) return fallback;
            string t = Regex.Replace(input.Trim(), @"\s+", " ");
            if (t.Length > 80) t = t.Substring(0, 80);
            return ContainsBannedWord(t) ? fallback : t;
        }
        public static bool ContainsBannedWord(string text) {
            if (string.IsNullOrWhiteSpace(text)) return false;
            foreach (var w in BannedWords) if (text.Contains(w)) return true;
            return false;
        }
    }
    public class DifficultyAgent {
        public int GetBonusMoves(int failCount) => failCount >= 5 ? 5 : failCount >= 3 ? 3 : failCount >= 2 ? 2 : 0;
        public string GetEncouragement(int failCount) => failCount >= 3 ? "괜찮아. 이번엔 조금 더 쉽게 도와줄게!" : "아쉬워도 괜찮아. 다시 해보면 돼!";
    }
    public class LearningCoachAgent {
        public string GetShortTip(LearningTheme t) {
            switch(t) {
                case LearningTheme.Animals: return "동물마다 생김새와 사는 곳이 달라요.";
                case LearningTheme.Numbers: return "숫자는 순서대로 이어지면 길이 돼요.";
                case LearningTheme.Words: return "글자는 모이면 뜻이 있는 단어가 돼요.";
                case LearningTheme.Nature: return "물, 햇빛, 바람은 자연을 움직이게 해요.";
                case LearningTheme.Emotions: return "감정은 없애는 게 아니라 알아봐 주는 거예요.";
                default: return "천천히 보면 답이 보여요.";
            }
        }
    }
    public class StoryAgent {
        public string BuildIntro(StageDefinition s) {
            var p = SaveManager.Instance != null ? SaveManager.Instance.Data.playerPreset : new PlayerPreset();
            string n = SafeTextFilter.CleanName(p.firstTwinName, "나별");
            string d = SafeTextFilter.CleanName(p.secondTwinName, "다별");
            string line = s != null ? SafeTextFilter.CleanStoryLine(s.introLine, "같은 블록을 세 개 모아 반짝 조각을 되찾아보자!") : "반짝 조각을 찾아보자!";
            return $"{n}: \"규칙을 찾아보자.\"\n{d}: \"좋아, 바로 해보자!\"\n\n{line}";
        }
        public string BuildClearLine(StageDefinition s) {
            string line = s != null ? SafeTextFilter.CleanStoryLine(s.clearLine, "반짝 조각을 되찾았어!") : "반짝 조각을 되찾았어!";
            return $"해냈어!\n{line}";
        }
    }
}
