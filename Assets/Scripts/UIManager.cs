using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("テキストUIの参照")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI scoreText;

    // ターン表示を更新する窓口
    public void UpdateTurnText(bool isMyTurn)
    {
        if (turnText == null) return;

        turnText.text = isMyTurn ? "MY TURN" : "ENEMY'S TURN";
    }

    // 点数表示を更新する窓口
    // 点数と役を一緒に受け取って、綺麗に画面に表示する
    public void UpdateScoreText(int myScore, int enemyScore, List<string> myYakuNames, List<string> enemyYakuNames)
    {
        if (scoreText == null) return;

        // 役がない場合は「なし」と表示する
        string myYakuStr = (myYakuNames != null && myYakuNames.Count > 0) ? string.Join(", ", myYakuNames) : "なし";
        string enemyYakuStr = (enemyYakuNames != null && enemyYakuNames.Count > 0) ? string.Join(", ", enemyYakuNames) : "なし";

        // \n を使って改行を入れ、見やすくレイアウトする
        scoreText.text = 
            $"<< YOU >>  score: {myScore}\n" +
            $"           [{myYakuStr}]\n" +
            $"--------------------\n" +
            $"<< CPU >>  score: {enemyScore}\n" +
            $"           [{enemyYakuStr}]";
    }
}