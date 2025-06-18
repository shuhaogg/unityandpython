using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static int score;

    Text text;

    void Awake ()
    {
        this.text = GetComponent <Text> ();
        score = 0;
    }

    void Update ()
    {
        this.text.text = "Score: " + score;
    }
}
