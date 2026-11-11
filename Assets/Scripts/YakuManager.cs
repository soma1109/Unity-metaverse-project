using System.Collections.Generic;
using System.Linq;
using Unity.Collections.Tests.CoreCLR.TestJobs;
using UnityEngine;

public class YakuInfo
{
    public string id;       // 内部識別用（"TsukimiZake", "HanamiZake" など）
    public string name;     // 表示する名前（"月見で一杯"）
    public int point;       // 点数
    public bool isEnabled;  // ルールとして有効かどうかのフラグ

    // コンストラクタ（初期化しやすくする仕組み）
    public YakuInfo(string id, string name, int point, bool isEnabled = true)
    {
        this.id = id;
        this.name = name;
        this.point = point;
        this.isEnabled = isEnabled;
    }
}

// 結果を持ち帰るための専用の入れ物（段ボール箱）
public class YakuResult
{
    public List<string> yakuNames = new List<string>(); // 成立した役の名前のリスト
    public int totalScore = 0;                          // 合計点数
}

public class YakuManager : MonoBehaviour
{
    // あらかじめゲームに存在するすべての役のデータをリスト（あるいは辞書）で持っておく！
    private Dictionary<string, YakuInfo> yakuDictionary = new Dictionary<string, YakuInfo>()
    {
        { "Nomi",        new YakuInfo("Nomi",        "のみ",       10) },
        { "Tsukimizake", new YakuInfo("TsukimiZake", "月見で一杯",  5) },
        { "Hanamizake",  new YakuInfo("HanamiZake",  "花見で一杯",  5) },
        { "Gokou",       new YakuInfo("Gokou",       "五光",       10) },
        { "Shikou",      new YakuInfo("Shikou",      "四光",        8) },
        { "Ameshikou",   new YakuInfo("Ameshikou",   "雨四光",      7) },
        { "Sankou",      new YakuInfo("Sankou",      "三光",        5) },
        { "Inoshikatyo", new YakuInfo("Inoshikatyo", "猪鹿蝶",      5) },
        { "Akatan",      new YakuInfo("Akatan",      "赤短",        5) },
        { "Aotan",       new YakuInfo("Aotan",       "青短",        5) },
        { "Tane",        new YakuInfo("Tane",        "たね",        1) },
        { "Tan",         new YakuInfo("Tan",         "たん",        1) },
        { "Kasu",        new YakuInfo("Kasu",        "かす",        1) },
        // ここにどんどん役を追加していける！
    };

    // プレイヤーの獲得札（PlayerCollection）を渡して、すべての役を判定・計算するメイン関数
    public YakuResult EvaluateYaku(PlayerCollection collection)
    {
        YakuResult result = new YakuResult();
        
        // 役の判定を上から順に全部実行して、1つの箱（result）に結果を貯めていく！
        EvaluateBrightYaku(collection, result);   // 光札系（五光、四光など）
        EvaluateAnimalYaku(collection, result);  // タネ札系（猪鹿蝶など）
        EvaluateAlcoholYaku(collection, result); // 盃系（月見酒・花見酒）
        EvaluateRibbonYaku(collection, result);  // 短冊系（赤短・青短など）
        EvaluateChaffYaku(collection, result);   // カス系

        return result; // 全部詰まった箱を返す！
    }

    // --- ① 光札の判定 ---
    private void EvaluateBrightYaku(PlayerCollection collection, YakuResult result)
    {
        List<GameObject> brightCards = collection.GetList(CardType.Bright);
        int count = brightCards.Count;

        //五光
        YakuInfo gokou = yakuDictionary["Gokou"];
        YakuInfo shikou = yakuDictionary["Shikou"];
        YakuInfo ame = yakuDictionary["Ameshikou"];
        YakuInfo sankou = yakuDictionary["Sankou"];
        if (count == 5)
        {
            result.yakuNames.Add(gokou.name);
            result.totalScore += gokou.point;
        }
        else if (count == 4)
        {
            // 11月の雨札（小野道風など）が含まれているかで「雨四光」か「四光」かを分ける一般的なルール
            bool hasRainMan = HasSpecificCard(brightCards, CardType.Bright, 11);
            if (hasRainMan)
            {
                result.yakuNames.Add(ame.name);
                result.totalScore += ame.point;
            }
            else
            {
                result.yakuNames.Add(shikou.name);
                result.totalScore += shikou.point;
            }
        }
        else if (count == 3)
        {
            bool hasRainMan =  HasSpecificCard(brightCards, CardType.Bright, 11);
            if (!hasRainMan)
            {
                result.yakuNames.Add(sankou.name);
                result.totalScore += sankou.point;
            }
        }
    }

    private void EvaluateAlcoholYaku(PlayerCollection collection, YakuResult result)
    {
        List<GameObject> AnimalCards = collection.GetList(CardType.Animal);
        //盃の有無
        bool hasSakazuki = HasSpecificCard(AnimalCards, CardType.Animal, 9);
        if (!hasSakazuki) return;

        List<GameObject> BrightCards = collection.GetList(CardType.Bright);
        //花見で一杯
        YakuInfo hanami = yakuDictionary["Hanamizake"];
        if (HasSpecificCard(BrightCards, CardType.Bright, 3))
        {
            result.yakuNames.Add(hanami.name);
            result.totalScore += hanami.point;
        }
        //月見で一杯
        YakuInfo tsukimi = yakuDictionary["Tsukimizake"];
        if (HasSpecificCard(BrightCards, CardType.Bright, 8))
        {
            result.yakuNames.Add(tsukimi.name);
            result.totalScore += tsukimi.point;
        }
        //のみ
        YakuInfo nomi = yakuDictionary["Nomi"];
        if (HasSpecificCard(BrightCards, CardType.Bright, 3) &&
            HasSpecificCard(BrightCards, CardType.Bright, 8))
        {
            result.yakuNames.Add(nomi.name);
            result.totalScore += nomi.point;
        }
    }

    // --- ② タネ札の判定 ---
    private void EvaluateAnimalYaku(PlayerCollection collection, YakuResult result)
    {
        List<GameObject> animalCards = collection.GetList(CardType.Animal);

        // 猪鹿蝶の判定
        YakuInfo inoshikatyo = yakuDictionary["Inoshikatyo"];
        if (HasSpecificCard(animalCards, CardType.Animal, 7) && 
            HasSpecificCard(animalCards, CardType.Animal, 10) && 
            HasSpecificCard(animalCards, CardType.Animal, 6))
        {
            result.yakuNames.Add(inoshikatyo.name);
            result.totalScore += inoshikatyo.point;
        }

        // タネ札の枚数役（5枚以上で1点、1枚増えるごとに+1点）
        YakuInfo tane = yakuDictionary["Tane"];
        if (animalCards.Count >= 5)
        {
            int points = tane.point * animalCards.Count - 4;
            result.yakuNames.Add($"{tane.name} ({animalCards.Count}枚)");
            result.totalScore += points;
        }
    }

    // --- ③ 短冊札の判定 ---
    private void EvaluateRibbonYaku(PlayerCollection collection, YakuResult result)
    {
        List<GameObject> ribbonCards = collection.GetList(CardType.Ribbon);

        // 赤短
        YakuInfo akatan = yakuDictionary["Akatan"];
        if (HasSpecificCard(ribbonCards, CardType.Ribbon, 1) && 
            HasSpecificCard(ribbonCards, CardType.Ribbon, 2) && 
            HasSpecificCard(ribbonCards, CardType.Ribbon, 3))
        {
            result.yakuNames.Add(akatan.name);
            result.totalScore += akatan.point;
        }
        
        // 青短
        YakuInfo aotan = yakuDictionary["Aotan"];
        if (HasSpecificCard(ribbonCards, CardType.Ribbon, 6) && 
            HasSpecificCard(ribbonCards, CardType.Ribbon, 9) && 
            HasSpecificCard(ribbonCards, CardType.Ribbon, 10))
        {
            result.yakuNames.Add(aotan.name);
            result.totalScore += aotan.point;
        }

        // 短冊の枚数役（5枚以上で1点、1枚増えるごとに+1点）
        YakuInfo tan = yakuDictionary["Tan"];
        if (ribbonCards.Count >= 5)
        {
            int points = tan.point * ribbonCards.Count - 4;
            result.yakuNames.Add($"{tan.name} ({ribbonCards.Count}枚)");
            result.totalScore += points;
        }
    }

    // --- ④ カス札の判定 ---
    private void EvaluateChaffYaku(PlayerCollection collection, YakuResult result)
    {
        List<GameObject> chaffCards = collection.GetList(CardType.Chaff);

        // カス札は10枚で1点、以降1枚につき+1点
        YakuInfo kasu = yakuDictionary["Kasu"];
        if (chaffCards.Count >= 10)
        {
            int points = kasu.point * chaffCards.Count - 9;
            result.yakuNames.Add($"{kasu.name} ({chaffCards.Count}枚)");
            result.totalScore += points;
        }
    }

    // --- 🛠️ 便利なヘルパー関数（CardDataの仕様に合わせて中身を調整する部分） ---
    // 特定のカードが含まれているかチェックする
    private bool HasSpecificCard(List<GameObject> cards, CardType type, int month)
    {
        return cards.Any(card => {
            CardData data = card.GetComponent<CardData>();
            if (data == null) return false;

            return data.cardType == type && data.month ==month;
        });
    }
}