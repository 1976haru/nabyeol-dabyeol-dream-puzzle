using UnityEngine;
using MallangTwins.Save;
namespace MallangTwins.Core {
    public class Bootstrapper : MonoBehaviour {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureSave(){ if(SaveManager.Instance==null)new GameObject("SaveManager").AddComponent<SaveManager>(); }
    }
}
