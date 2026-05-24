using System.Collections;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;
namespace NabyeolDabyeolDreamPuzzle.Agents
{
    public class HintHighlighter:MonoBehaviour
    {
        [SerializeField,Min(1f)] private float highlightScaleMultiplier=1.25f; [SerializeField,Min(.01f)] private float pulseDuration=.18f; [SerializeField,Min(1)] private int pulseCount=2; private Coroutine routine;
        public void Highlight(Block a,Block b){ if(a==null||b==null) return; ClearHighlight(); routine=StartCoroutine(Run(a,b)); }
        public void ClearHighlight(){ if(routine!=null){ StopCoroutine(routine); routine=null; }}
        private IEnumerator Run(Block a,Block b){ var ascale=a.transform.localScale; var bscale=b.transform.localScale; for(int i=0;i<pulseCount;i++){ if(a==null||b==null) break; a.transform.localScale=ascale*highlightScaleMultiplier; b.transform.localScale=bscale*highlightScaleMultiplier; yield return new WaitForSeconds(pulseDuration); if(a==null||b==null) break; a.transform.localScale=ascale; b.transform.localScale=bscale; yield return new WaitForSeconds(pulseDuration);} if(a!=null) a.transform.localScale=ascale; if(b!=null) b.transform.localScale=bscale; routine=null; }
    }
}
