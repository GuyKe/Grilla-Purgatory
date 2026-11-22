using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// The loading screen: the character waves at you while the game loads in the background, a bar fills, and a new tip
    /// appears every few seconds. It stays up long enough to read a tip or two, then starts the game.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public string nextScene = "Game";
        public float minimumSeconds = 7f;
        public float secondsPerTip = 4.5f;
        public Text tipText, statusText;
        public Image barFill;           // its width grows from the left
        public float barWidth = 1100f;   // the width of the bar when it is full

        AsyncOperation load;
        float startTime, nextTipTime;
        int tipIndex;
        int[] order;

        void Start()
        {
            startTime = Time.time;

            order = new int[GameTips.All.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)   // a different order each time
            {
                int j = Random.Range(0, i + 1);
                int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
            }
            ShowNextTip();

            if (!Application.CanStreamedLevelBeLoaded(nextScene))
            {
                statusText.text = "Scene \"" + nextScene + "\" is not in Build Settings";
                enabled = false;
                return;
            }
            load = SceneManager.LoadSceneAsync(nextScene);
            load.allowSceneActivation = false;   // wait until we have shown a tip or two
        }

        void Update()
        {
            float real = Mathf.Clamp01(load.progress / 0.9f);
            float time = Mathf.Clamp01((Time.time - startTime) / minimumSeconds);
            float shown = Mathf.Min(real, time);

            barFill.rectTransform.sizeDelta = new Vector2(barWidth * shown, barFill.rectTransform.sizeDelta.y);
            statusText.text = shown >= 1f ? "STARTING..." : "LOADING   " + Mathf.RoundToInt(shown * 100f) + "%";

            if (Time.time >= nextTipTime) ShowNextTip();
            if (shown >= 1f) load.allowSceneActivation = true;
        }

        void ShowNextTip()
        {
            tipText.text = GameTips.All[order[tipIndex % order.Length]];
            tipIndex++;
            nextTipTime = Time.time + secondsPerTip;
        }
    }
}
