using UnityEngine; using UnityEngine.UI;
namespace MallangTwins.UI {
    public class GameHud : MonoBehaviour {
        [SerializeField] Text stageText, scoreText, movesText, goalText;
        public void SetStage(int n,string title){ if(stageText!=null)stageText.text=$"Stage {n}. {title}"; }
        public void SetScore(int score,int target){ if(scoreText!=null)scoreText.text=target>0?$"점수 {score}/{target}":$"점수 {score}"; }
        public void SetMoves(int moves){ if(movesText!=null)movesText.text=$"남은 이동 {moves}"; }
        public void SetGoal(string name,int cur,int target){ if(goalText!=null)goalText.text=$"목표 {name}: {cur}/{target}"; }
    }
    public class StoryPanel : MonoBehaviour {
        [SerializeField] GameObject root; [SerializeField] Text bodyText;
        void Awake(){ if(root!=null)root.SetActive(false); }
        public void Show(string msg){ if(bodyText!=null)bodyText.text=msg; if(root!=null)root.SetActive(true); }
        public void Hide(){ if(root!=null)root.SetActive(false); }
    }
}
