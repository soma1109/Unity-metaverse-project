using UnityEngine;

public class CardData : MonoBehaviour
{
    [Header("カードの基本情報")]
    public int month;        // 何月か (1〜12)
    public CardType cardType; // 札の種類 (光、タネ、短冊、カス)

    // 点数は「種類」や「特定の月」によって自動で決まるプロパティにする
    public int CardPoint
    {
        get
        {
            // ここで種類ごとに点数を自動計算（またはゲームのルールに合わせて調整）
            switch (cardType)
            {
                case CardType.Bright: return 20; // 光札は基本的に高得点
                case CardType.Animal: return 10; // タネ札
                case CardType.Ribbon: return 5;  // 短冊札
                case CardType.Chaff:  return 1;  // カス札
                default: return 0;
            }
        }
    }
}

public enum CardType
{
    Bright, // 光札
    Animal, // タネ札
    Ribbon, // 短冊札
    Chaff   // カス札
}