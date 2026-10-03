using System.Diagnostics;
using System.Numerics;
using System.Text;
using AetherRadio.Core;
using Dalamud.Bindings.ImGui;

namespace AetherRadio;

public sealed class RadioUi(Configuration config, Catalog catalog, Player player, Action save)
{
    private static readonly Vector4 Accent = new(0.60f, 0.81f, 1f, 1);
    private static readonly Vector4 Muted = new(0.64f, 0.69f, 0.71f, 1);
    private bool open;
    private bool revealMain;
    private string search = "";
    private uint? expansion;
    private string? area;
    private string? genre;
    private bool resetScroll;
    private int library;
    private Guid? playlist;
    private string newList = "";
    private string rename = "";
    private int revision;
    private string cacheKey = "";
    private List<Track> visible = [];
    private Location[]? allLocations;
    private bool miniDragged;
    public bool CatalogReady { get; set; }
    public void Open() { open = true; revealMain = true; }

    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 9);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 7);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 10);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(18, 16));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 9));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.063f, 0.086f, 0.098f, 1));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.078f, 0.102f, 0.114f, 1));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.11f, 0.14f, 0.155f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.19f, 0.25f, 0.28f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.24f, 0.32f, 0.36f, 1));
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.14f, 0.19f, 0.21f, 1));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.19f, 0.25f, 0.28f, 1));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.24f, 0.32f, 0.36f, 1));
        ImGui.PushStyleColor(ImGuiCol.CheckMark, Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, Accent);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.91f, 0.93f, 0.92f, 1));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.18f, 0.22f, 0.24f, 1));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.11f, 0.14f, 0.155f, 1));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.19f, 0.25f, 0.28f, 1));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.24f, 0.32f, 0.36f, 1));
        ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.09f, 0.12f, 0.135f, 1));
        ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.22f, 0.29f, 0.33f, 1));
        ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.18f, 0.24f, 0.28f, 1));
        ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.05f, 0.07f, 0.08f, 1));
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.05f, 0.07f, 0.08f, 1));
        try
        {
            if (open) DrawMain();
            if (config.ShowMini) DrawMini();
        }
        finally { ImGui.PopStyleColor(20); ImGui.PopStyleVar(5); }
    }

    private void DrawMain()
    {
        var viewport = ImGui.GetMainViewport();
        var maxSize = Vector2.Max(new Vector2(320, 260), viewport.WorkSize - new Vector2(32));
        ImGui.SetNextWindowSize(new Vector2(1000, 690), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(Vector2.Min(new Vector2(850, 540), maxSize), maxSize);
        if (revealMain)
        {
            var size = Vector2.Min(new Vector2(1000, 690), maxSize);
            ImGui.SetNextWindowSize(size, ImGuiCond.Always);
            ImGui.SetNextWindowPos(viewport.WorkPos + (viewport.WorkSize - size) / 2, ImGuiCond.Always);
            ImGui.SetNextWindowFocus();
            revealMain = false;
        }
        if (ImGui.Begin("BGMPlayer###AetherRadio.Main", ref open, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextColored(Accent, "BGMPlayer");
            ImGui.SameLine(); ImGui.TextColored(Muted, "BGMプレイヤー");
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
                    ImGui.Spacing(); ImGui.TextColored(Accent, "BGMPlayer by Roxyz0501");
                    ImGui.TextWrapped("Ko-fiから開発を支援できます（任意）。");
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
        DrawFilters();
        ImGui.TextColored(Muted, "曲名・コンテンツ名・BGM IDで検索");
        ImGui.SetNextItemWidth(-1);
        if (TextInput("##search", ref search, 256)) resetScroll = true;
        var height = Math.Max(180, ImGui.GetContentRegionAvail().Y - 145);
        if (ImGui.BeginChild("nav", new Vector2(185, height), true))
        {
            ImGui.TextColored(Muted, "ライブラリ");
            Nav("すべての曲", 0);
            Nav("お気に入り", 1);
            ImGui.Separator();
            ImGui.TextColored(Muted, "マイリスト");
            foreach (var list in config.Playlists)
            {
                if (ImGui.Selectable($"{list.Name}##{list.Id}", library == 2 && playlist == list.Id))
                { library = 2; playlist = list.Id; rename = list.Name; resetScroll = true; }
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
            if (ImGui.BeginChild("song-scroll", new Vector2(0, 0), false))
            {
                if (visible.Count == 0) ImGui.TextWrapped("該当する曲がありません。検索条件を変えるか、曲の「＋」からマイリストに追加してください。");
                if (resetScroll) { ImGui.SetScrollY(0); resetScroll = false; }
                DrawTrackList(list);
            }
            ImGui.EndChild();
        }
        ImGui.EndChild();
        ImGui.Separator();
        NowPlaying(false);
    }

    private void Nav(string label, int mode)
    {
        if (ImGui.Selectable(label, library == mode)) { library = mode; resetScroll = true; }
    }
    private void DrawFilters()
    {
        var locations = allLocations ??= catalog.Tracks.Values.SelectMany(t => t.Locations).Distinct().ToArray();
        if (ImGui.BeginTabBar("expansions", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            ExpansionTab("すべて", null);
            foreach (var group in locations.GroupBy(x => x.ExpansionId).OrderBy(x => x.Key))
                ExpansionTab(group.First().Expansion, group.Key);
            ImGui.EndTabBar();
        }
        var scoped = locations.Where(x => expansion == null || x.ExpansionId == expansion).ToArray();
        if (ImGui.BeginTabBar("genres", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            GenreTab("すべて", null);
            foreach (var name in scoped.Select(x => x.Category).Distinct().OrderBy(GenreOrder).ThenBy(x => x)) GenreTab(name, name);
            ImGui.EndTabBar();
        }
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##area", area ?? "すべてのコンテンツ／フィールド"))
        {
            if (ImGui.Selectable("すべて", area == null)) { area = null; resetScroll = true; }
            foreach (var name in scoped.Where(x => genre == null || x.Category == genre).Select(x => x.Name).Distinct().OrderBy(x => x))
                if (ImGui.Selectable(name, area == name)) { area = name; resetScroll = true; }
            ImGui.EndCombo();
        }
    }
    private void ExpansionTab(string label, uint? id)
    {
        if (!ImGui.BeginTabItem(label)) return;
        if (expansion != id) { expansion = id; genre = null; area = null; resetScroll = true; }
        ImGui.EndTabItem();
    }
    private void GenreTab(string label, string? value)
    {
        // Separate tab state for each expansion avoids retaining a hidden genre.
        ImGui.PushID(expansion?.ToString() ?? "all");
        if (ImGui.BeginTabItem(label))
        {
            if (genre != value) { genre = value; area = null; resetScroll = true; }
            ImGui.EndTabItem();
        }
        ImGui.PopID();
    }
    private static int GenreOrder(string name) => name switch
    {
        "討滅" => 0, "レイド" => 1, "ダンジョン" => 2, "フィールド" => 3, "その他" => 99, _ => 4,
    };
    private void DrawTrackList(Playlist? list)
    {
        // Only submit visible rows while preserving a single continuous scroll range.
        var clipper = new ImGuiListClipper();
        clipper.Begin(visible.Count);
        while (clipper.Step())
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++) DrawTrack(visible[i], list);
        clipper.End();
    }
    private void RefreshVisible(Playlist? list)
    {
        var key = $"{search}|{expansion}|{genre}|{area}|{library}|{playlist}|{revision}";
        if (cacheKey == key) return;
        cacheKey = key;
        IEnumerable<Track> source = library switch
        {
            1 => catalog.Tracks.Values.Where(t => config.Favorites.Contains(t.Id)).OrderBy(t => t.Title),
            2 => (list?.Tracks ?? []).Where(catalog.Tracks.ContainsKey).Select(id => catalog.Tracks[id]),
            _ => catalog.Tracks.Values.OrderBy(t => t.Title),
        };
        visible = source.Where(t => (string.IsNullOrWhiteSpace(search) || t.SearchText.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)) &&
            t.Locations.Any(l => (expansion == null || l.ExpansionId == expansion) && (genre == null || l.Category == genre) && (area == null || l.Name == area))).ToList();
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
        var size = new Vector2(420, 218);
        if (config.Corner != 0)
        {
            var right = config.Corner is 2 or 4;
            var bottom = config.Corner is 3 or 4;
            ImGui.SetNextWindowPos(viewport.WorkPos + new Vector2(right ? viewport.WorkSize.X - size.X - 18 : 18, bottom ? viewport.WorkSize.Y - size.Y - 18 : 18), ImGuiCond.Always);
        }
        else if (config.MiniPosition is { } saved)
            ImGui.SetNextWindowPos(viewport.WorkPos + ClampMiniPosition(saved, viewport.WorkSize, size), ImGuiCond.Always);
        else ImGui.SetNextWindowPos(viewport.WorkPos + new Vector2(18, 80), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(size);
        ImGui.SetNextWindowBgAlpha(config.Opacity);
        var flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove;
        if (ImGui.Begin("##AetherRadio.Mini", flags))
        {
            var header = ImGui.GetCursorScreenPos();
            ImGui.InvisibleButton("##move-mini", new Vector2(Math.Max(80, ImGui.GetContentRegionAvail().X - 140), 22));
            ImGui.GetWindowDrawList().AddText(header + new Vector2(0, 3), ImGui.ColorConvertFloat4ToU32(Accent), "BGMPlayer");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(config.PinMini ? "位置を固定しています。「解除」で移動できます。" : "ここをドラッグして移動");
            if (!config.PinMini && ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var delta = ImGui.GetIO().MouseDelta;
                if (delta != Vector2.Zero)
                {
                    config.Corner = 0;
                    config.MiniPosition = ClampMiniPosition(ImGui.GetWindowPos() + delta - viewport.WorkPos, viewport.WorkSize, size);
                    ImGui.SetWindowPos(viewport.WorkPos + config.MiniPosition.Value);
                    miniDragged = true;
                }
            }
            if (miniDragged && !ImGui.IsMouseDown(ImGuiMouseButton.Left)) { miniDragged = false; Changed(); }
            ImGui.SameLine();
            if (ImGui.SmallButton(config.PinMini ? "解除" : "固定")) { config.PinMini = !config.PinMini; Changed(); }
            ImGui.SameLine();
            if (ImGui.SmallButton("開く")) Open();
            ImGui.SameLine();
            if (ImGui.SmallButton("×")) { config.ShowMini = false; Changed(); }
            DrawMiniPlayback();
        }
        ImGui.End();
    }
    private void DrawMiniPlayback()
    {
        var origin = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        var tile = ImGui.ColorConvertFloat4ToU32(new Vector4(0.14f, 0.19f, 0.20f, 1));
        draw.AddRectFilled(origin, origin + new Vector2(48), tile, 5);
        for (var i = 0; i < 4; i++) draw.AddCircle(origin + new Vector2(24), 10 + i * 3, 0xff45534a, 32, 1);
        draw.AddCircleFilled(origin + new Vector2(24), 5, 0xffb0c69b);
        draw.AddCircleFilled(origin + new Vector2(24), 1.5f, tile);
        ImGui.Dummy(new Vector2(48)); ImGui.SameLine();
        ImGui.BeginGroup();
        var title = player.Current?.Title ?? "曲を選んでください";
        if (ImGui.BeginChild("mini-title", new Vector2(ImGui.GetContentRegionAvail().X, 24), false, ImGuiWindowFlags.NoScrollbar)) ImGui.TextUnformatted(title);
        ImGui.EndChild();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(title);
        ImGui.TextColored(Muted, player.Current?.Locations.FirstOrDefault()?.Expansion ?? "BGMPlayer");
        ImGui.EndGroup();
        ImGui.Spacing();
        var timeline = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        draw.AddLine(timeline, timeline + new Vector2(width, 0), 0xff3d4549, 3);
        // This is an advance timer, not an audio seek bar.
        if (player.IsPlaying && config.AutoAdvance && config.Repeat != RepeatMode.One)
            draw.AddLine(timeline, timeline + new Vector2(width * Math.Clamp((float)player.Elapsed / player.AdvanceSeconds, 0, 1), 0), ImGui.ColorConvertFloat4ToU32(Accent), 3);
        ImGui.Dummy(new Vector2(width, 2));
        ImGui.BeginDisabled(!CatalogReady || !player.Available);
        if (TransportButton("shuffle", "ランダム再生", config.Shuffle)) { config.Shuffle = !config.Shuffle; player.Queue.Shuffle = config.Shuffle; Changed(); }
        ImGui.SameLine(); if (TransportButton("previous", "前の曲")) player.Previous();
        ImGui.SameLine(); if (TransportButton(player.IsPlaying ? "stop" : "play", player.IsPlaying ? "停止してゲームBGMへ戻す" : "再生", true)) { if (player.IsPlaying) player.Stop(); else player.Resume(); }
        ImGui.SameLine(); if (TransportButton("next", "次の曲")) player.Next();
        ImGui.EndDisabled();
        ImGui.SameLine(); ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 157);
        DrawVolume("##mini-volume", 135);
        ImGui.TextColored(Muted, player.Error != null ? "「開く」で再生状況を確認" : !player.IsPlaying ? "停止中" : config.AutoAdvance && config.Repeat != RepeatMode.One ? $"約{Math.Max(0, player.AdvanceSeconds - (int)player.Elapsed)}秒で次の曲" : config.LockBgm ? "BGM固定中" : "再生中");
    }
    private static bool TransportButton(string icon, string tooltip, bool accent = false)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, accent ? Accent : new Vector4(0, 0, 0, 0));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, accent ? new Vector4(0.72f, 0.86f, 1, 1) : new Vector4(0.19f, 0.25f, 0.28f, 1));
        var clicked = ImGui.Button("##" + icon, new Vector2(34));
        ImGui.PopStyleColor(2);
        var c = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;
        var color = ImGui.ColorConvertFloat4ToU32((accent ? new Vector4(0.06f, 0.09f, 0.11f, 1) : new Vector4(0.8f, 0.85f, 0.86f, 1)) with { W = ImGui.GetStyle().Alpha });
        var d = ImGui.GetWindowDrawList();
        if (icon == "stop") d.AddRect(c - new Vector2(5), c + new Vector2(5), color, 0, ImDrawFlags.None, 1.4f);
        else if (icon == "shuffle")
        {
            d.AddLine(c + new Vector2(-7, -5), c + new Vector2(7, 5), color, 1.4f);
            d.AddLine(c + new Vector2(-7, 5), c + new Vector2(7, -5), color, 1.4f);
            d.AddLine(c + new Vector2(3, -5), c + new Vector2(7, -5), color, 1.4f);
            d.AddLine(c + new Vector2(7, -5), c + new Vector2(7, -1), color, 1.4f);
            d.AddLine(c + new Vector2(3, 5), c + new Vector2(7, 5), color, 1.4f);
        }
        else
        {
            var direction = icon == "previous" ? -1 : 1;
            d.AddTriangle(c + new Vector2(-4 * direction, -7), c + new Vector2(-4 * direction, 7), c + new Vector2(6 * direction, 0), color, 1.4f);
            if (icon != "play") d.AddLine(c + new Vector2(8 * direction, -7), c + new Vector2(8 * direction, 7), color, 1.4f);
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        return clicked;
    }
    private void NowPlaying(bool mini)
    {
        ImGui.TextColored(Muted, player.IsPlaying ? "再生中" : "停止中");
        var title = player.Current?.Title ?? "曲が選択されていません";
        // A clipped single-line title keeps long localized names inside the mini player.
        if (ImGui.BeginChild("title", new Vector2(0, ImGui.GetTextLineHeight() + 2), false, ImGuiWindowFlags.NoScrollbar)) ImGui.TextUnformatted(title);
        ImGui.EndChild();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(title);
        ImGui.BeginDisabled(!CatalogReady || !player.Available);
        if (TransportButton("previous", "前の曲")) player.Previous();
        ImGui.SameLine();
        if (TransportButton(player.IsPlaying ? "stop" : "play", player.IsPlaying ? "停止してゲームBGMへ戻す" : "再生", true)) { if (player.IsPlaying) player.Stop(); else player.Resume(); }
        ImGui.SameLine(); if (TransportButton("next", "次の曲")) player.Next();
        ImGui.SameLine();
        if (TransportButton("shuffle", "ランダム再生", config.Shuffle)) { config.Shuffle = !config.Shuffle; player.Queue.Shuffle = config.Shuffle; Changed(); }
        ImGui.EndDisabled();
        ImGui.SameLine(); ImGui.SetCursorPosX(250);
        DrawVolume("プレイヤー音量", mini ? 170 : 220);
        if (mini)
        {
            if (player.Error != null) { ImGui.TextColored(new Vector4(1, 0.66f, 0.45f, 1), "再生状況を確認するには「開く」"); }
            else ImGui.TextColored(Muted, config.Repeat == RepeatMode.One ? "1曲リピート" : $"{(config.LockBgm ? "BGM固定" : "ゲーム優先")}  ·  {(config.AutoAdvance ? Math.Max(0, player.AdvanceSeconds - (int)player.Elapsed) + "秒で次の曲（目安）" : "手動で曲送り")}");
        }
        else if (player.Error != null) ImGui.TextWrapped(player.Error);
    }
    private void DrawSettings()
    {
        ImGui.Spacing(); ImGui.TextColored(Accent, "再生");
        ImGui.TextWrapped("音量は再生中のBGMだけに反映します。ゲームの音量設定は変更しません。100%はゲーム側で設定したBGM音量です。");
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
        ImGui.Spacing(); ImGui.Separator(); ImGui.TextColored(Accent, "ミニプレイヤー");
        var show = config.ShowMini; if (ImGui.Checkbox("ミニプレイヤーを表示", ref show)) { config.ShowMini = show; Changed(); }
        var corner = config.Corner;
        if (ImGui.Combo("配置", ref corner, "自由に移動\0左上\0右上\0左下\0右下\0")) { config.Corner = corner; Changed(); }
        var pin = config.PinMini; if (ImGui.Checkbox("位置を固定", ref pin)) { config.PinMini = pin; Changed(); }
        ImGui.TextWrapped("ミニプレイヤー上部の「BGMPlayer」をドラッグすると移動できます。");
        var opacity = config.Opacity; if (ImGui.SliderFloat("背景の不透明度", ref opacity, 0.5f, 1, "%.2f")) { config.Opacity = opacity; Changed(); }
        ImGui.Spacing(); ImGui.Separator(); ImGui.TextColored(Accent, "ライブラリ情報");
        if (CatalogReady)
        {
            ImGui.TextUnformatted($"{catalog.Tracks.Count:N0} 曲 / ファイル未収録 {catalog.MissingFiles:N0} 件");
            ImGui.TextWrapped("インストール済みゲームデータのBGM表を読み取ります。曲名が不明な曲は「曲名未登録」と表示します。場所が分かる場合は場所名を添えています。分類できない曲は「その他」にあります。");
            foreach (var warning in catalog.Warnings) ImGui.TextWrapped(warning);
        }
        ImGui.TextWrapped("/bgmplayer で開く · /bgmplayer stop でゲームBGMへ戻す");
        if (ImGui.Button("ゲームBGMへ戻す")) player.Stop();
        if (player.Error != null) ImGui.TextWrapped(player.Error);
    }
    private void Changed() { revision++; save(); }

    private void DrawVolume(string label, float width)
    {
        var percent = config.VolumePercent;
        ImGui.SetNextItemWidth(width);
        if (ImGui.SliderInt(label, ref percent, 0, 100, "%d%%")) config.VolumePercent = percent;
        if (ImGui.IsItemDeactivatedAfterEdit()) save();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("プレイヤー音量。100%はゲーム設定のBGM音量。SE・ボイス・環境音の音量は変更しません。");
    }

    private static Vector2 ClampMiniPosition(Vector2 position, Vector2 viewport, Vector2 size) =>
        Vector2.Clamp(position, Vector2.Zero, Vector2.Max(Vector2.Zero, viewport - size));

    private static bool FavoriteButton(bool selected)
    {
        var clicked = ImGui.Button("##favorite", new Vector2(22, 22));
        var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;
        var color = ImGui.ColorConvertFloat4ToU32(selected ? Accent : Muted);
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
