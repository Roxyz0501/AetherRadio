using System.Diagnostics;
using System.Numerics;
using System.Text;
using AetherRadio.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;

namespace AetherRadio;

public sealed class RadioUi(Configuration config, Catalog catalog, Player player, IGameConfig gameConfig, Action save)
{
    private static readonly Vector4 Mint = new(0.45f, 0.91f, 0.78f, 1);
    private static readonly Vector4 Muted = new(0.62f, 0.69f, 0.77f, 1);
    private bool open;
    private string search = "";
    private uint? expansion;
    private string? area;
    private int library;
    private Guid? playlist;
    private string newList = "";
    private string rename = "";
    private int page;
    private int revision;
    private string cacheKey = "";
    private List<Track> visible = [];
    private Location[]? allLocations;
    public bool CatalogReady { get; set; }
    public void Open() => open = true;

    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 14);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 7);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 10);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(18, 16));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 9));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.035f, 0.06f, 0.10f, 1));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.055f, 0.09f, 0.14f, 1));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.10f, 0.24f, 0.27f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.15f, 0.38f, 0.38f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.18f, 0.46f, 0.42f, 1));
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.10f, 0.30f, 0.30f, 1));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.14f, 0.36f, 0.36f, 1));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.17f, 0.42f, 0.39f, 1));
        ImGui.PushStyleColor(ImGuiCol.CheckMark, Mint);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, Mint);
        try
        {
            if (open) DrawMain();
            if (config.ShowMini) DrawMini();
        }
        finally { ImGui.PopStyleColor(10); ImGui.PopStyleVar(5); }
    }

    private void DrawMain()
    {
        ImGui.SetNextWindowSize(new Vector2(1000, 690), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(850, 540), new Vector2(1800, 1200));
        if (ImGui.Begin("Aether Radio###AetherRadio.Main", ref open, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextColored(Mint, "AETHER RADIO");
            ImGui.SameLine(); ImGui.TextColored(Muted, "いつもの景色に、好きな音楽を。");
            ImGui.Separator();
            if (ImGui.BeginTabBar("tabs"))
            {
                if (ImGui.BeginTabItem("ライブラリ")) { DrawLibrary(); ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem("設定")) { DrawSettings(); ImGui.EndTabItem(); }
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.81f, 0.38f, 1));
                ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.27f, 0.17f, 0.06f, 1));
                ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.43f, 0.28f, 0.09f, 1));
                ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.35f, 0.22f, 0.06f, 1));
                var support = ImGui.BeginTabItem("支援");
                ImGui.PopStyleColor(4);
                if (support)
                {
                    ImGui.Spacing(); ImGui.TextColored(Mint, "Aether Radio by Roxyz0501");
                    ImGui.TextWrapped("気に入っていただけたら、任意で開発を支援できます。支援の有無による機能の違いはありません。");
                    ImGui.Spacing(); ImGui.TextUnformatted("支援先: Roxyz0501");
                    if (ImGui.Button("Ko-fiで支援する"))
                    {
                        try { Process.Start(new ProcessStartInfo("https://ko-fi.com/roxyz0501") { UseShellExecute = true }); }
                        catch { player.Error = "ブラウザーを開けませんでした: https://ko-fi.com/roxyz0501"; }
                    }
                    ImGui.TextColored(Muted, "https://ko-fi.com/roxyz0501");
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
    }

    private void DrawLibrary()
    {
        if (!CatalogReady)
        {
            ImGui.TextWrapped(player.Error ?? "ゲーム内の曲一覧を読み込んでいます…");
            return;
        }
        ImGui.TextColored(Muted, "曲名・コンテンツ名・BGM IDで検索");
        ImGui.SetNextItemWidth(-1);
        if (TextInput("##search", ref search, 256)) page = 0;
        var height = Math.Max(180, ImGui.GetContentRegionAvail().Y - 145);
        if (ImGui.BeginChild("nav", new Vector2(185, height), true))
        {
            ImGui.TextColored(Muted, "YOUR LIBRARY");
            Nav("すべての曲", 0);
            Nav("お気に入り", 1);
            ImGui.Separator();
            ImGui.TextColored(Muted, "マイリスト");
            foreach (var list in config.Playlists)
            {
                if (ImGui.Selectable($"{list.Name}##{list.Id}", library == 2 && playlist == list.Id))
                { library = 2; playlist = list.Id; rename = list.Name; page = 0; }
            }
            ImGui.SetNextItemWidth(-1);
            ImGui.TextColored(Muted, "新しいリスト名");
            ImGui.SetNextItemWidth(-1);
            TextInput("##newlist", ref newList, 64);
            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(newList));
            if (ImGui.Button("＋ 作成", new Vector2(-1, 0)))
            {
                var list = new Playlist { Name = newList.Trim() };
                config.Playlists.Add(list); library = 2; playlist = list.Id; rename = list.Name; newList = ""; Changed();
            }
            ImGui.EndDisabled();
        }
        ImGui.EndChild(); ImGui.SameLine();
        if (ImGui.BeginChild("tracks", new Vector2(0, height), true))
        {
            DrawFilters();
            var list = config.Playlists.FirstOrDefault(x => x.Id == playlist);
            if (library == 2 && list != null) DrawListEditor(list);
            RefreshVisible(list);
            ImGui.TextColored(Muted, $"{visible.Count:N0} 曲");
            ImGui.SameLine(); ImGui.BeginDisabled(visible.Count == 0 || !player.Available);
            if (ImGui.SmallButton("この一覧を再生")) player.Start(visible[0], visible.Select(t => t.Id));
            ImGui.SameLine();
            if (ImGui.SmallButton("ランダム再生"))
            {
                config.Shuffle = true; player.Queue.Shuffle = true; Changed();
                player.Start(visible[Random.Shared.Next(visible.Count)], visible.Select(t => t.Id));
            }
            ImGui.EndDisabled();
            var pages = Math.Max(1, (visible.Count + 74) / 75);
            page = Math.Clamp(page, 0, pages - 1);
            if (pages > 1)
            {
                if (ImGui.SmallButton("前ページ")) page = Math.Max(0, page - 1);
                ImGui.SameLine(); ImGui.TextUnformatted($"{page + 1} / {pages}"); ImGui.SameLine();
                if (ImGui.SmallButton("次ページ")) page = Math.Min(pages - 1, page + 1);
            }
            if (ImGui.BeginChild("song-scroll", new Vector2(0, 0), false))
            {
                if (visible.Count == 0) ImGui.TextWrapped("該当する曲がありません。検索条件を変えるか、曲の「＋」からマイリストに追加してください。");
                foreach (var track in visible.Skip(page * 75).Take(75)) DrawTrack(track, list);
            }
            ImGui.EndChild();
        }
        ImGui.EndChild();
        ImGui.Separator();
        NowPlaying(false);
    }

    private void Nav(string label, int mode)
    {
        if (ImGui.Selectable(label, library == mode)) { library = mode; page = 0; }
    }
    private void DrawFilters()
    {
        ImGui.SetNextItemWidth(220);
        var locations = allLocations ??= catalog.Tracks.Values.SelectMany(t => t.Locations).Distinct().ToArray();
        var label = expansion == null ? "すべての拡張" : locations.FirstOrDefault(x => x.ExpansionId == expansion)?.Expansion ?? "その他";
        if (ImGui.BeginCombo("##expansion", label))
        {
            if (ImGui.Selectable("すべての拡張", expansion == null)) { expansion = null; area = null; page = 0; }
            foreach (var group in locations.GroupBy(x => x.ExpansionId).OrderBy(x => x.Key))
                if (ImGui.Selectable(group.First().Expansion, expansion == group.Key)) { expansion = group.Key; area = null; page = 0; }
            ImGui.EndCombo();
        }
        ImGui.SameLine(); ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##area", area ?? "すべてのコンテンツ／フィールド"))
        {
            if (ImGui.Selectable("すべて", area == null)) { area = null; page = 0; }
            foreach (var loc in locations.Where(x => expansion == null || x.ExpansionId == expansion).OrderBy(x => x.Category).ThenBy(x => x.Name).DistinctBy(x => x.Category + ": " + x.Name))
            {
                var key = loc.Category + ": " + loc.Name;
                if (ImGui.Selectable(key, area == key)) { area = key; page = 0; }
            }
            ImGui.EndCombo();
        }
        if (ImGui.SmallButton("絞り込みを解除")) { expansion = null; area = null; search = ""; page = 0; }
    }
    private void RefreshVisible(Playlist? list)
    {
        var key = $"{search}|{expansion}|{area}|{library}|{playlist}|{revision}";
        if (cacheKey == key) return;
        cacheKey = key;
        IEnumerable<Track> source = library switch
        {
            1 => catalog.Tracks.Values.Where(t => config.Favorites.Contains(t.Id)).OrderBy(t => t.Title),
            2 => (list?.Tracks ?? []).Where(catalog.Tracks.ContainsKey).Select(id => catalog.Tracks[id]),
            _ => catalog.Tracks.Values.OrderBy(t => t.Title),
        };
        visible = source.Where(t => (string.IsNullOrWhiteSpace(search) || t.SearchText.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)) &&
            t.Locations.Any(l => (expansion == null || l.ExpansionId == expansion) && (area == null || l.Category + ": " + l.Name == area))).ToList();
    }
    private void DrawTrack(Track track, Playlist? list)
    {
        ImGui.PushID(track.Id);
        if (FavoriteButton(config.Favorites.Contains(track.Id)))
        { if (!config.Favorites.Add(track.Id)) config.Favorites.Remove(track.Id); Changed(); }
        ImGui.SameLine();
        if (ImGui.SmallButton("＋")) ImGui.OpenPopup("add");
        if (ImGui.BeginPopup("add"))
        {
            ImGui.TextUnformatted("マイリストへ追加");
            if (config.Playlists.Count == 0) ImGui.TextUnformatted("左側でマイリストを作成してください。");
            foreach (var target in config.Playlists)
            {
                ImGui.BeginDisabled(target.Tracks.Contains(track.Id));
                if (ImGui.Selectable($"{target.Name}##{target.Id}")) { target.Tracks.Add(track.Id); Changed(); }
                ImGui.EndDisabled();
            }
            ImGui.EndPopup();
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(!player.Available);
        if (ImGui.Selectable($"{track.Title}##play", player.IsPlaying && player.Current?.Id == track.Id, ImGuiSelectableFlags.None, new Vector2(Math.Max(60, ImGui.GetContentRegionAvail().X - (library == 2 ? 112 : 5)), 0)))
            player.Start(track, visible.Select(t => t.Id));
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip(); ImGui.TextUnformatted(track.Title); ImGui.TextUnformatted($"BGM ID: {track.Id}");
            foreach (var l in track.Locations) ImGui.TextUnformatted($"{l.Expansion} / {l.Category} / {l.Name}{(l.Inferred ? "（曲名・場所から補助分類）" : "")}");
            ImGui.TextColored(Muted, track.Path); ImGui.EndTooltip();
        }
        if (library == 2 && list != null)
        {
            var index = list.Tracks.IndexOf(track.Id);
            ImGui.SameLine();
            if (ImGui.SmallButton("↑") && index > 0) { (list.Tracks[index - 1], list.Tracks[index]) = (list.Tracks[index], list.Tracks[index - 1]); Changed(); }
            ImGui.SameLine();
            if (ImGui.SmallButton("↓") && index < list.Tracks.Count - 1) { (list.Tracks[index + 1], list.Tracks[index]) = (list.Tracks[index], list.Tracks[index + 1]); Changed(); }
            ImGui.SameLine(); if (ImGui.SmallButton("×")) { list.Tracks.Remove(track.Id); Changed(); }
        }
        ImGui.PopID();
    }
    private void DrawListEditor(Playlist list)
    {
        ImGui.SetNextItemWidth(180); TextInput("##rename", ref rename, 64);
        ImGui.SameLine(); ImGui.BeginDisabled(string.IsNullOrWhiteSpace(rename));
        if (ImGui.SmallButton("名前変更")) { list.Name = rename.Trim(); Changed(); }
        ImGui.EndDisabled(); ImGui.SameLine();
        if (ImGui.SmallButton("リスト削除")) ImGui.OpenPopup("delete-list");
        if (ImGui.BeginPopup("delete-list"))
        {
            ImGui.TextUnformatted($"「{list.Name}」を削除しますか？");
            if (ImGui.Button("削除する")) { config.Playlists.Remove(list); library = 0; playlist = null; Changed(); ImGui.CloseCurrentPopup(); }
            ImGui.SameLine(); if (ImGui.Button("キャンセル")) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
        var missing = list.Tracks.Count(id => !catalog.Tracks.ContainsKey(id));
        if (missing > 0) ImGui.TextWrapped($"現在のゲームデータにない曲 {missing} 件は保持しています。");
    }

    private void DrawMini()
    {
        var viewport = ImGui.GetMainViewport();
        var size = new Vector2(370, 212);
        if (config.Corner != 0)
        {
            var right = config.Corner is 2 or 4;
            var bottom = config.Corner is 3 or 4;
            ImGui.SetNextWindowPos(viewport.WorkPos + new Vector2(right ? viewport.WorkSize.X - size.X - 18 : 18, bottom ? viewport.WorkSize.Y - size.Y - 18 : 18), ImGuiCond.Always);
        }
        else ImGui.SetNextWindowPos(viewport.WorkPos + new Vector2(18, 80), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(size);
        ImGui.SetNextWindowBgAlpha(config.Opacity);
        var flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
        if (config.PinMini || config.Corner != 0) flags |= ImGuiWindowFlags.NoMove;
        if (ImGui.Begin("##AetherRadio.Mini", flags))
        {
            ImGui.TextColored(Mint, "AETHER RADIO");
            ImGui.SameLine(ImGui.GetWindowWidth() - 116);
            if (ImGui.SmallButton("開く")) open = true;
            ImGui.SameLine();
            if (ImGui.SmallButton("×")) { config.ShowMini = false; Changed(); }
            NowPlaying(true);
        }
        ImGui.End();
    }
    private void NowPlaying(bool mini)
    {
        ImGui.TextColored(Muted, player.IsPlaying ? "NOW PLAYING" : "好きな曲を選んで再生");
        var title = player.Current?.Title ?? "Aether Radioへようこそ";
        // A clipped single-line title keeps long localized names inside the mini player.
        if (ImGui.BeginChild("title", new Vector2(0, ImGui.GetTextLineHeight() + 2), false, ImGuiWindowFlags.NoScrollbar)) ImGui.TextUnformatted(title);
        ImGui.EndChild();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(title);
        ImGui.BeginDisabled(!CatalogReady || !player.Available);
        if (ImGui.Button("|<")) player.Previous();
        ImGui.SameLine();
        if (ImGui.Button(player.IsPlaying ? "停止" : "再生")) { if (player.IsPlaying) player.Stop(); else player.Resume(); }
        ImGui.SameLine(); if (ImGui.Button(">|")) player.Next();
        ImGui.SameLine();
        var shuffle = config.Shuffle;
        if (ImGui.Checkbox("ランダム", ref shuffle)) { config.Shuffle = shuffle; player.Queue.Shuffle = shuffle; Changed(); }
        ImGui.EndDisabled();
        if (gameConfig.TryGet(SystemConfigOption.SoundBgm, out uint volume))
        {
            var v = (int)volume; ImGui.SetNextItemWidth(mini ? 170 : 220);
            if (ImGui.SliderInt("BGM音量", ref v, 0, 100, "%d%%")) gameConfig.Set(SystemConfigOption.SoundBgm, (uint)v);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("ゲーム設定のBGM音量を変更します。");
        }
        if (mini)
        {
            if (player.Error != null) { ImGui.TextColored(new Vector4(1, 0.66f, 0.45f, 1), "再生状況を確認するには「開く」"); }
            else ImGui.TextColored(Muted, config.Repeat == RepeatMode.One ? "1曲リピート" : $"{(config.LockBgm ? "BGM固定" : "ゲーム優先")}  ·  {(config.AutoAdvance ? Math.Max(0, player.AdvanceSeconds - (int)player.Elapsed) + "秒で次の曲（目安）" : "手動で曲送り")}");
        }
        else if (player.Error != null) ImGui.TextWrapped(player.Error);
    }
    private void DrawSettings()
    {
        ImGui.Spacing(); ImGui.TextColored(Mint, "再生");
        var locked = config.LockBgm;
        if (ImGui.Checkbox("再生中はコンテンツ・戦闘・フィールドのBGM変更を無視", ref locked)) { config.LockBgm = locked; Changed(); }
        ImGui.TextWrapped("オフの場合、ゲームから次のBGM変更要求が来た時点で通常のBGMへ戻ります。停止・ログアウト・プラグイン終了時にも固定を解除します。");
        var repeat = (int)config.Repeat;
        if (ImGui.Combo("リピート", ref repeat, "オフ\0リスト全体\01曲\0")) { config.Repeat = (RepeatMode)repeat; player.Queue.Repeat = config.Repeat; Changed(); }
        var advance = config.AutoAdvance;
        if (ImGui.Checkbox("自動で次の曲へ", ref advance)) { config.AutoAdvance = advance; Changed(); }
        var seconds = config.TrackSeconds;
        var useDuration = config.UseTrackDuration;
        if (ImGui.Checkbox("曲名データの収録時間を曲送りの目安に使う", ref useDuration)) { config.UseTrackDuration = useDuration; Changed(); }
        if (ImGui.SliderInt("曲送り間隔（秒）", ref seconds, 15, 1800)) { config.TrackSeconds = seconds; Changed(); }
        ImGui.TextWrapped("自動曲送りは収録時間の目安（不明な曲は指定秒数）で切り替えます。曲の終了検知ではありません。1曲リピートではゲーム本来の再生・ループを維持します。");
        ImGui.Spacing(); ImGui.Separator(); ImGui.TextColored(Mint, "ミニプレイヤー");
        var show = config.ShowMini; if (ImGui.Checkbox("ミニプレイヤーを表示", ref show)) { config.ShowMini = show; Changed(); }
        var corner = config.Corner;
        if (ImGui.Combo("配置", ref corner, "自由に移動\0左上\0右上\0左下\0右下\0")) { config.Corner = corner; Changed(); }
        var pin = config.PinMini; if (ImGui.Checkbox("自由配置の位置を固定", ref pin)) { config.PinMini = pin; Changed(); }
        var opacity = config.Opacity; if (ImGui.SliderFloat("背景の不透明度", ref opacity, 0.5f, 1, "%.2f")) { config.Opacity = opacity; Changed(); }
        ImGui.Spacing(); ImGui.Separator(); ImGui.TextColored(Mint, "ライブラリ情報");
        if (CatalogReady)
        {
            ImGui.TextUnformatted($"{catalog.Tracks.Count:N0} 曲 / ファイル未収録 {catalog.MissingFiles:N0} 件");
            ImGui.TextWrapped("インストール済みゲームデータのBGM表を読み取ります。正式曲名を取得できない曲はIDとファイル名で表示します。分類できない曲は「その他」にあります。");
            foreach (var warning in catalog.Warnings) ImGui.TextWrapped(warning);
        }
        ImGui.TextWrapped("/aetherradio で開く · /aetherradio stop でゲームBGMへ戻す");
        if (ImGui.Button("ゲームBGMへ戻す")) player.Stop();
        if (player.Error != null) ImGui.TextWrapped(player.Error);
    }
    private void Changed() { revision++; save(); }

    private static bool FavoriteButton(bool selected)
    {
        var clicked = ImGui.Button("##favorite", new Vector2(22, 22));
        var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;
        var color = ImGui.ColorConvertFloat4ToU32(selected ? Mint : Muted);
        var draw = ImGui.GetWindowDrawList();
        for (var i = 0; i < 10; i++)
        {
            Vector2 Point(int n) => center + new Vector2(MathF.Cos(n * MathF.PI / 5 - MathF.PI / 2), MathF.Sin(n * MathF.PI / 5 - MathF.PI / 2)) * (n % 2 == 0 ? 7 : 3);
            draw.AddLine(Point(i), Point((i + 1) % 10), color, selected ? 2 : 1);
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(selected ? "お気に入りから解除" : "お気に入りに追加");
        return clicked;
    }

    private static unsafe bool TextInput(string label, ref string value, int capacity)
    {
        // Call the public native InputText API with a bounded UTF-8 buffer.
        // This avoids the internal InputTextEx path used by the string wrapper.
        Span<byte> buffer = stackalloc byte[capacity * 4 + 1];
        Span<byte> labelBuffer = stackalloc byte[Encoding.UTF8.GetByteCount(label) + 1];
        buffer.Clear(); labelBuffer.Clear();
        Encoding.UTF8.GetBytes(value.AsSpan(0, Math.Min(value.Length, capacity)), buffer);
        Encoding.UTF8.GetBytes(label, labelBuffer);
        fixed (byte* text = buffer)
        fixed (byte* id = labelBuffer)
        {
            if (ImGuiNative.InputText(id, text, (nuint)buffer.Length, ImGuiInputTextFlags.None, default, null) == 0) return false;
        }
        var length = buffer.IndexOf((byte)0);
        var decoded = Encoding.UTF8.GetString(buffer[..(length < 0 ? buffer.Length : length)]);
        value = decoded.Length <= capacity ? decoded : decoded[..capacity];
        return true;
    }
}
