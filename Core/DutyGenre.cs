namespace AetherRadio.Core;

public static class DutyGenre
{
    // Labels come from the Japanese game sheets, independent of UI/client language.
    public static string Classify(string type, string uiCategory, bool allianceRoulette, int parties)
    {
        if (type == "レイド")
            return uiCategory.Contains("アライアンス") || allianceRoulette ? "アライアンス"
                : parties > 1 ? "特殊コンテンツ" : "ノーマルレイド";
        if (type.Contains("レイド") || type is "禁断の地エウレカ" or "セイブ・ザ・クイーン" or "蜃気楼の島 クレセントアイル" or "特殊ダンジョン探索")
            return "特殊コンテンツ";
        if (type.Contains("討伐") || type.Contains("討滅")) return "討滅";
        if (type.Contains("ダンジョン")) return type.Contains("ディープ") ? "ディープダンジョン" : "ダンジョン";
        return string.IsNullOrWhiteSpace(type) ? "その他" : type == "PvP" ? "PvP" : "その他のコンテンツ";
    }
}
