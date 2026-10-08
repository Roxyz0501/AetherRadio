using System.Diagnostics;
using System.Numerics;
using System.Text;
using AetherRadio.Core;
using Dalamud.Bindings.ImGui;

namespace AetherRadio;

public sealed class RadioUi(Configuration config, Catalog catalog, Player player, Action save)
{
    private readonly Localization text = new(() => config.Language);
    public Action? LanguageChanged { get; set; }
    public Func<bool>? FontFailed { get; set; }
    private bool revealSettings;
    public void OpenSettings() { Open(); revealSettings = true; }
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
            ImGui.SameLine(); ImGui.TextColored(Muted, text["Subtitle"]);
            ImGui.Separator();
            if (ImGui.BeginTabBar("tabs"))
            {
                if (ImGui.BeginTabItem(text.Label("Library"))) { DrawLibrary(); ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(text.Label("Settings"), revealSettings ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None)) { revealSettings = false; DrawSettings(); ImGui.EndTabItem(); }
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.81f, 0.38f, 1));
                ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.27f, 0.17f, 0.06f, 1));
                ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.43f, 0.28f, 0.09f, 1));
                ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.35f, 0.22f, 0.06f, 1));
                var support = ImGui.BeginTabItem(text.Label("Support"));
                ImGui.PopStyleColor(4);
                if (support)
                {
                    ImGui.Spacing(); ImGui.TextColored(Accent, "BGMPlayer · Roxyz0501");
                    ImGui.TextWrapped(text["SupportInfo"]);
                    ImGui.Spacing(); ImGui.TextUnformatted(text["SupportRecipient"]);
                    if (ImGui.Button(text.Label("SupportButton")))
                    {
                        try { Process.Start(new ProcessStartInfo("https://ko-fi.com/roxyz0501") { UseShellExecute = true }); }
                        catch { player.Error = "BrowserError"; }
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
            ImGui.TextWrapped(player.Error ?? text["Loading"]);
            return;
        }
        DrawFilters();
        ImGui.TextColored(Muted, text["SearchHint"]);
        ImGui.SetNextItemWidth(-1);
        if (TextInput("##search", ref search, 256)) resetScroll = true;
        var height = Math.Max(180, ImGui.GetContentRegionAvail().Y - 145);
        if (ImGui.BeginChild("nav", new Vector2(Math.Min(230, Math.Max(185, ImGui.GetWindowWidth() * 0.24f)), height), true))
        {
            ImGui.TextColored(Muted, text["Library"]);
            Nav(text["AllTracks"], 0);
            Nav(text["Favorites"], 1);
            ImGui.Separator();
            ImGui.TextColored(Muted, text["Playlists"]);
            foreach (var list in config.Playlists)
            {
                if (ImGui.Selectable($"{list.Name}###{list.Id}", library == 2 && playlist == list.Id))
                { library = 2; playlist = list.Id; rename = list.Name; resetScroll = true; }
            }
            ImGui.SetNextItemWidth(-1);
            ImGui.TextColored(Muted, text["NewListName"]);
            ImGui.SetNextItemWidth(-1);
            TextInput("##newlist", ref newList, 64);
            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(newList));
            if (ImGui.Button(text.Label("Create"), new Vector2(-1, 0)))
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
            ImGui.TextColored(Muted, text.Format("TrackCount", visible.Count));
            ImGui.SameLine(); ImGui.BeginDisabled(visible.Count == 0 || !player.Available);
            if (ImGui.SmallButton(text.Label("PlayList"))) player.Start(visible[0], visible.Select(t => t.Id));
            ImGui.SameLine();
            if (ImGui.SmallButton(text.Label("Shuffle")))
            {
                config.Shuffle = true; player.Queue.Shuffle = true; Changed();
                player.Start(visible[Random.Shared.Next(visible.Count)], visible.Select(t => t.Id));
            }
            ImGui.EndDisabled();
            if (ImGui.BeginChild("song-scroll", new Vector2(0, 0), false))
            {
                if (visible.Count == 0) ImGui.TextWrapped(text["NoTracks"]);
                if (resetScroll) { ImGui.SetScrollY(0); resetScroll = false; }
                DrawTrackList(list);
            }
            ImGui.EndChild();
        }
        ImGui.EndChild();
        ImGui.Separator();
        NowPlaying();
    }

    private void Nav(string label, int mode)
    {
        if (ImGui.Selectable($"{label}###nav-{mode}", library == mode)) { library = mode; resetScroll = true; }
    }
    private void DrawFilters()
    {
        var locations = allLocations ??= catalog.Tracks.Values.SelectMany(t => t.Locations).Distinct().ToArray();
        if (ImGui.BeginTabBar("expansions", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            ExpansionTab(text["All"], null);
            foreach (var group in locations.GroupBy(x => x.ExpansionId).OrderBy(x => x.Key))
                ExpansionTab(text.Expansion(group.First()), group.Key);
            ImGui.EndTabBar();
        }
        var scoped = locations.Where(x => expansion == null || x.ExpansionId == expansion).ToArray();
        if (ImGui.BeginTabBar("genres", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            GenreTab(text["All"], null);
            foreach (var name in scoped.Select(x => x.Category).Distinct().OrderBy(GenreOrder).ThenBy(x => x)) GenreTab(text.Genre(name), name);
            ImGui.EndTabBar();
        }
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##area", area == null ? text["AllAreas"] : area == "未分類のBGM" ? text["Unclassified"] : area))
        {
            if (ImGui.Selectable(text["All"] + "###all-areas", area == null)) { area = null; resetScroll = true; }
            foreach (var name in scoped.Where(x => genre == null || x.Category == genre).Select(x => x.Name).Distinct().OrderBy(x => x))
                if (ImGui.Selectable((name == "未分類のBGM" ? text["Unclassified"] : name) + "###area-" + name, area == name)) { area = name; resetScroll = true; }
            ImGui.EndCombo();
        }
    }
    private void ExpansionTab(string label, uint? id)
    {
        if (!ImGui.BeginTabItem($"{label}###expansion-{id}")) return;
        if (expansion != id) { expansion = id; genre = null; area = null; resetScroll = true; }
        ImGui.EndTabItem();
    }
    private void GenreTab(string label, string? value)
    {
        // Separate tab state for each expansion avoids retaining a hidden genre.
        ImGui.PushID(expansion?.ToString() ?? "all");
        if (ImGui.BeginTabItem($"{label}###genre-{value}"))
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
        var key = $"{search}|{expansion}|{genre}|{area}|{library}|{playlist}|{revision}|{text.Language}";
        if (cacheKey == key) return;
        cacheKey = key;
        IEnumerable<Track> source = library switch
        {
            1 => catalog.Tracks.Values.Where(t => config.Favorites.Contains(t.Id)).OrderBy(t => t.Title),
            2 => (list?.Tracks ?? []).Where(catalog.Tracks.ContainsKey).Select(id => catalog.Tracks[id]),
            _ => catalog.Tracks.Values.OrderBy(t => t.Title),
        };
        visible = source.Where(t => (string.IsNullOrWhiteSpace(search) || (t.SearchText.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) || text.Title(t).Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) || t.Locations.Any(l => text.Genre(l.Category).Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)))) &&
            t.Locations.Any(l => (expansion == null || l.ExpansionId == expansion) && (genre == null || l.Category == genre) && (area == null || l.Name == area))).ToList();
    }
    private void DrawTrack(Track track, Playlist? list)
    {
        ImGui.PushID(track.Id);
        if (FavoriteButton(config.Favorites.Contains(track.Id)))
        { if (!config.Favorites.Add(track.Id)) config.Favorites.Remove(track.Id); Changed(); }
        ImGui.SameLine();
        if (ImGui.SmallButton("＋")) ImGui.OpenPopup("add");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text["AddToPlaylist"]);
        if (ImGui.BeginPopup("add"))
        {
            ImGui.TextUnformatted(text["AddToPlaylist"]);
            if (config.Playlists.Count == 0) ImGui.TextUnformatted(text["CreatePlaylistFirst"]);
            foreach (var target in config.Playlists)
            {
                ImGui.BeginDisabled(target.Tracks.Contains(track.Id));
                if (ImGui.Selectable($"{target.Name}###{target.Id}")) { target.Tracks.Add(track.Id); Changed(); }
                ImGui.EndDisabled();
            }
            ImGui.EndPopup();
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(!player.Available);
        if (ImGui.Selectable($"{text.Title(track)}###play", player.IsPlaying && player.Current?.Id == track.Id, ImGuiSelectableFlags.None, new Vector2(Math.Max(60, ImGui.GetContentRegionAvail().X - (library == 2 ? 112 : 5)), 0)))
            player.Start(track, visible.Select(t => t.Id));
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip(); ImGui.TextUnformatted(text.Title(track)); ImGui.TextUnformatted($"BGM ID: {track.Id}");
            foreach (var l in track.Locations) ImGui.TextUnformatted($"{text.Expansion(l)} / {text.Genre(l.Category)} / {text.Area(l)}{(l.Inferred ? text["Inferred"] : "")}");
            ImGui.TextColored(Muted, track.Path); ImGui.EndTooltip();
        }
        if (library == 2 && list != null)
        {
            var index = list.Tracks.IndexOf(track.Id);
            ImGui.SameLine();
            if (ImGui.SmallButton("↑") && index > 0) { (list.Tracks[index - 1], list.Tracks[index]) = (list.Tracks[index], list.Tracks[index - 1]); Changed(); }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(text["MoveUp"]);
            ImGui.SameLine();
            if (ImGui.SmallButton("↓") && index < list.Tracks.Count - 1) { (list.Tracks[index + 1], list.Tracks[index]) = (list.Tracks[index], list.Tracks[index + 1]); Changed(); }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(text["MoveDown"]);
            ImGui.SameLine(); if (ImGui.SmallButton("×")) { list.Tracks.Remove(track.Id); Changed(); }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(text["RemoveTrack"]);
        }
        ImGui.PopID();
    }
    private void DrawListEditor(Playlist list)
    {
        ImGui.SetNextItemWidth(-1); TextInput("##rename", ref rename, 64);
        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(rename));
        if (ImGui.SmallButton(text.Label("Rename"))) { list.Name = rename.Trim(); Changed(); }
        ImGui.EndDisabled(); ImGui.SameLine();
        if (ImGui.SmallButton(text.Label("DeleteList"))) ImGui.OpenPopup("delete-list");
        if (ImGui.BeginPopup("delete-list"))
        {
            ImGui.TextWrapped(text.Format("DeletePrompt", list.Name));
            if (ImGui.Button(text.Label("Delete"))) { config.Playlists.Remove(list); library = 0; playlist = null; Changed(); ImGui.CloseCurrentPopup(); }
            ImGui.SameLine(); if (ImGui.Button(text.Label("Cancel"))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
        var missing = list.Tracks.Count(id => !catalog.Tracks.ContainsKey(id));
        if (missing > 0) ImGui.TextWrapped(text.Format("MissingTracks", missing));
    }

    private void DrawMini()
    {
        var viewport = ImGui.GetMainViewport();
        var size = new Vector2(420, Math.Max(218, ImGui.GetTextLineHeightWithSpacing() * 9.5f));
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
            var buttonsWidth = ImGui.CalcTextSize(config.PinMini ? text["Unpin"] : text["Pin"]).X + ImGui.CalcTextSize(text["Open"]).X + ImGui.CalcTextSize("×").X + ImGui.GetStyle().FramePadding.X * 6 + ImGui.GetStyle().ItemSpacing.X * 3;
            ImGui.InvisibleButton("##move-mini", new Vector2(Math.Max(80, ImGui.GetContentRegionAvail().X - buttonsWidth), 22));
            ImGui.GetWindowDrawList().AddText(header + new Vector2(0, 3), ImGui.ColorConvertFloat4ToU32(Accent), "BGMPlayer");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(config.PinMini ? text["PinnedHint"] : text["DragHint"]);
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
            if (ImGui.SmallButton((config.PinMini ? text["Unpin"] : text["Pin"]) + "###mini-pin")) { config.PinMini = !config.PinMini; Changed(); }
            ImGui.SameLine();
            if (ImGui.SmallButton(text.Label("Open"))) Open();
            ImGui.SameLine();
            if (ImGui.SmallButton("×")) { config.ShowMini = false; Changed(); }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(text["HideMini"]);
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
        var title = player.Current is { } current ? text.Title(current) : text["ChooseTrack"];
        if (ImGui.BeginChild("mini-title", new Vector2(ImGui.GetContentRegionAvail().X, 24), false, ImGuiWindowFlags.NoScrollbar)) ImGui.TextUnformatted(title);
        ImGui.EndChild();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(title);
        ImGui.TextColored(Muted, player.Current?.Locations.FirstOrDefault() is { } location ? text.Expansion(location) : "BGMPlayer");
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
        if (TransportButton("shuffle", text["Shuffle"], config.Shuffle)) { config.Shuffle = !config.Shuffle; player.Queue.Shuffle = config.Shuffle; Changed(); }
        ImGui.SameLine(); if (TransportButton("previous", text["Previous"])) player.Previous();
        ImGui.SameLine(); if (TransportButton(player.IsPlaying ? "stop" : "play", player.IsPlaying ? text["StopTooltip"] : text["Play"], true)) { if (player.IsPlaying) player.Stop(); else player.Resume(); }
        ImGui.SameLine(); if (TransportButton("next", text["Next"])) player.Next();
        ImGui.EndDisabled();
        ImGui.SameLine(); DrawRepeatButton();
        ImGui.SameLine(); ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 157);
        DrawVolume("##mini-volume", 135);
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(Muted, player.Error != null ? text["CheckStatus"] : !player.IsPlaying ? text["Stopped"] : config.Repeat == RepeatMode.One ? text["RepeatOne"] : config.AutoAdvance ? text.Format("NextIn", Math.Max(0, player.AdvanceSeconds - (int)player.Elapsed)) : config.LockBgm ? text["BgmLocked"] : text["Playing"]);
        ImGui.PopTextWrapPos();
    }
    private void DrawRepeatButton()
    {
        var label = config.Repeat switch { RepeatMode.One => text["RepeatOne"], RepeatMode.All => text["RepeatAll"], _ => text["RepeatOff"] };
        if (TransportButton("repeat", text.Format("RepeatHint", label), config.Repeat != RepeatMode.Off, config.Repeat == RepeatMode.One))
        {
            config.Repeat = config.Repeat switch { RepeatMode.All => RepeatMode.One, RepeatMode.One => RepeatMode.Off, _ => RepeatMode.All };
            player.Queue.Repeat = config.Repeat;
            Changed();
        }
    }
    private static bool TransportButton(string icon, string tooltip, bool accent = false, bool repeatOne = false)
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
        else if (icon == "repeat")
        {
            d.AddLine(c + new Vector2(-8, 2), c + new Vector2(-8, -6), color, 1.4f);
            d.AddLine(c + new Vector2(-8, -6), c + new Vector2(8, -6), color, 1.4f);
            d.AddLine(c + new Vector2(8, -6), c + new Vector2(4, -10), color, 1.4f);
            d.AddLine(c + new Vector2(8, -6), c + new Vector2(4, -2), color, 1.4f);
            d.AddLine(c + new Vector2(8, -2), c + new Vector2(8, 6), color, 1.4f);
            d.AddLine(c + new Vector2(8, 6), c + new Vector2(-8, 6), color, 1.4f);
            d.AddLine(c + new Vector2(-8, 6), c + new Vector2(-4, 2), color, 1.4f);
            d.AddLine(c + new Vector2(-8, 6), c + new Vector2(-4, 10), color, 1.4f);
            if (repeatOne)
            {
                d.AddLine(c + new Vector2(-2, -2), c + new Vector2(0, -4), color, 1.6f);
                d.AddLine(c + new Vector2(0, -4), c + new Vector2(0, 4), color, 1.6f);
            }
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
    private void NowPlaying()
    {
        ImGui.TextColored(Muted, player.IsPlaying ? text["Playing"] : text["Stopped"]);
        var title = player.Current is { } current ? text.Title(current) : text["NoSelection"];
        // A clipped single-line title keeps long localized names inside the mini player.
        if (ImGui.BeginChild("title", new Vector2(0, ImGui.GetTextLineHeight() + 2), false, ImGuiWindowFlags.NoScrollbar)) ImGui.TextUnformatted(title);
        ImGui.EndChild();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(title);
        ImGui.BeginDisabled(!CatalogReady || !player.Available);
        if (TransportButton("previous", text["Previous"])) player.Previous();
        ImGui.SameLine();
        if (TransportButton(player.IsPlaying ? "stop" : "play", player.IsPlaying ? text["StopTooltip"] : text["Play"], true)) { if (player.IsPlaying) player.Stop(); else player.Resume(); }
        ImGui.SameLine(); if (TransportButton("next", text["Next"])) player.Next();
        ImGui.SameLine();
        if (TransportButton("shuffle", text["Shuffle"], config.Shuffle)) { config.Shuffle = !config.Shuffle; player.Queue.Shuffle = config.Shuffle; Changed(); }
        ImGui.EndDisabled();
        ImGui.SameLine(); DrawRepeatButton();
        ImGui.SameLine(); ImGui.SetCursorPosX(250);
        DrawVolume(text.Label("Volume"), 220);
        if (player.Error != null) ImGui.TextWrapped(player.Error);
    }

    private void DrawSettings()
    {
        ImGui.TextUnformatted("言語 / Language");
        var language = Array.IndexOf(Languages.Codes, text.Language);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##language", ref language, string.Join('\0', Languages.Names) + "\0"))
        {
            config.Language = Languages.Codes[language]; Changed(); LanguageChanged?.Invoke();
        }
        if (FontFailed?.Invoke() == true) ImGui.TextWrapped(text["FontError"]);
        ImGui.TextWrapped(text["GameDataLanguage"]);
        ImGui.Spacing(); ImGui.TextColored(Accent, text["Playback"]);
        ImGui.TextWrapped(text["VolumeInfo"]);
        var locked = config.LockBgm;
        if (ImGui.Checkbox(text.Label("LockBgm"), ref locked)) { config.LockBgm = locked; Changed(); }
        ImGui.TextWrapped(text["LockInfo"]);
        var repeat = (int)config.Repeat;
        ImGui.TextUnformatted(text["Repeat"]); ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##repeat-setting", ref repeat, string.Join('\0', new[] { text["RepeatOff"], text["RepeatAll"], text["RepeatOne"] }) + "\0")) { config.Repeat = (RepeatMode)repeat; player.Queue.Repeat = config.Repeat; Changed(); }
        var advance = config.AutoAdvance;
        if (ImGui.Checkbox(text.Label("AutoAdvance"), ref advance)) { config.AutoAdvance = advance; Changed(); }
        var seconds = config.TrackSeconds;
        var useDuration = config.UseTrackDuration;
        if (ImGui.Checkbox(text.Label("UseDuration"), ref useDuration)) { config.UseTrackDuration = useDuration; Changed(); }
        ImGui.TextUnformatted(text["AdvanceSeconds"]); ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderInt("##advance-seconds", ref seconds, 15, 1800)) { config.TrackSeconds = seconds; Changed(); }
        ImGui.TextWrapped(text["AdvanceInfo"]);
        ImGui.Spacing(); ImGui.Separator(); ImGui.TextColored(Accent, text["MiniPlayer"]);
        var show = config.ShowMini; if (ImGui.Checkbox(text.Label("ShowMini"), ref show)) { config.ShowMini = show; Changed(); }
        var corner = config.Corner;
        ImGui.TextUnformatted(text["Position"]); ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##position", ref corner, string.Join('\0', new[] { text["FreePosition"], text["TopLeft"], text["TopRight"], text["BottomLeft"], text["BottomRight"] }) + "\0")) { config.Corner = corner; Changed(); }
        var pin = config.PinMini; if (ImGui.Checkbox(text.Label("PinPosition"), ref pin)) { config.PinMini = pin; Changed(); }
        ImGui.TextWrapped(text["MoveInfo"]);
        ImGui.TextUnformatted(text["Opacity"]); ImGui.SetNextItemWidth(-1);
        var opacity = config.Opacity; if (ImGui.SliderFloat("##opacity", ref opacity, 0.5f, 1, "%.2f")) { config.Opacity = opacity; Changed(); }
        ImGui.Spacing(); ImGui.Separator(); ImGui.TextColored(Accent, text["LibraryInfo"]);
        if (CatalogReady)
        {
            ImGui.TextUnformatted(text.Format("CatalogCount", catalog.Tracks.Count, catalog.MissingFiles));
            ImGui.TextWrapped(text["CatalogInfo"]);
            if (catalog.Warnings.Count > 0) ImGui.TextWrapped(text["CatalogWarning"]);
        }
        ImGui.TextWrapped(text["CommandHelp"]);
        if (ImGui.Button(text.Label("RestoreBgm"))) player.Stop();
        if (player.Error != null) ImGui.TextWrapped(player.Error);
    }
    private void Changed() { revision++; save(); }

    private void DrawVolume(string label, float width)
    {
        var percent = config.VolumePercent;
        ImGui.SetNextItemWidth(width);
        if (ImGui.SliderInt(label, ref percent, 0, BgmVolumeSession.MaxPercent, "%d%%", ImGuiSliderFlags.AlwaysClamp)) config.VolumePercent = percent;
        if (ImGui.IsItemDeactivatedAfterEdit()) save();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text["VolumeHint"]);
    }

    private static Vector2 ClampMiniPosition(Vector2 position, Vector2 viewport, Vector2 size) =>
        Vector2.Clamp(position, Vector2.Zero, Vector2.Max(Vector2.Zero, viewport - size));

    private bool FavoriteButton(bool selected)
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
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(selected ? text["RemoveFavorite"] : text["AddFavorite"]);
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
