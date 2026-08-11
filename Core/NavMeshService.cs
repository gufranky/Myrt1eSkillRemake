using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using RayTraceAPI;
using SteamDatabase.ValvePak;
using ValveResourceFormat.NavMesh;
using GameVector = CounterStrikeSharp.API.Modules.Utils.Vector;
using NavVector = System.Numerics.Vector3;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Loads the current map's static navigation mesh without touching CS2 memory.
/// The mesh supplies reachable candidates; RayTraceAPI remains the authority for
/// whether a player's hull can safely occupy a candidate at this moment.
/// </summary>
public sealed class NavMeshService
{
    private const string RayTraceCapability = "raytrace:craytraceinterface";
    private const float PlayerHullRadius = 16.0f;
    private const float PlayerHullHeight = 72.0f;
    private const float MinimumTeleportDistance = 384.0f;
    private const float MinimumPlayerClearance = 64.0f;
    private const int CandidateAttempts = 64;

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly PluginCapability<CRayTraceInterface> _rayTrace = new(RayTraceCapability);
    private NavMeshFile? _mesh;
    private string? _mapName;
    private string? _source;
    private string? _lastError;
    private bool _loaded;
    private bool _missingRayTraceLogged;

    public bool IsReady => _mesh is not null;
    public int AreaCount => _mesh?.Areas.Count ?? 0;
    public string MapName => _mapName ?? string.Empty;
    public string Source => _source ?? string.Empty;
    public string LastError => _lastError ?? string.Empty;

    public NavMeshService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        _loaded = true;

        if (!string.IsNullOrWhiteSpace(Server.MapName))
        {
            LoadMap(Server.MapName);
        }
    }

    public void Unload()
    {
        if (!_loaded)
        {
            return;
        }

        _plugin.RemoveListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
        Clear();
        _loaded = false;
    }

    public bool TryTeleportRandom(CCSPlayerController player, out string failure)
    {
        failure = string.Empty;
        var pawn = player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (!player.IsValid
            || !player.PawnIsAlive
            || pawn is not { IsValid: true }
            || pawn.Handle == IntPtr.Zero
            || pawn.Collision is null
            || origin is null)
        {
            failure = "玩家当前不可传送";
            return false;
        }

        if (!TryFindSafeRandomPosition(player, out var destination, out failure))
        {
            return false;
        }

        var original = new GameVector(origin.X, origin.Y, origin.Z);
        var pawnIndex = pawn.Index;
        pawn.Teleport(destination, pawn.AbsRotation, new GameVector(0.0f, 0.0f, 0.0f));

        // Recheck after the engine has linked the pawn at its new position. If a
        // dynamic obstruction appeared between selection and teleport, fail safe.
        Server.NextFrame(() =>
        {
            var currentPawn = player.PlayerPawn.Value;
            if (!player.IsValid
                || !player.PawnIsAlive
                || currentPawn is not { IsValid: true }
                || currentPawn.Index != pawnIndex)
            {
                return;
            }

            if (!IsHullSpaceFree(player, destination, logMissingCapability: false))
            {
                currentPawn.Teleport(original, currentPawn.AbsRotation, new GameVector(0.0f, 0.0f, 0.0f));
                PluginText.Chat(player, "[随机传送] 新位置出现障碍，已返回原位置。");
            }
        });

        return true;
    }

    public bool TryFindSafeRandomPosition(
        CCSPlayerController player,
        out GameVector destination,
        out string failure)
    {
        destination = new GameVector(0.0f, 0.0f, 0.0f);
        failure = string.Empty;

        var mesh = _mesh;
        var pawn = player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (mesh is null)
        {
            failure = string.IsNullOrWhiteSpace(_lastError)
                ? "当前地图的导航网格尚未加载"
                : _lastError!;
            return false;
        }

        if (!player.IsValid
            || !player.PawnIsAlive
            || pawn is not { IsValid: true }
            || pawn.Collision is null
            || origin is null)
        {
            failure = "玩家当前不可传送";
            return false;
        }

        if (!TryGetRayTrace(out _, logMissing: true))
        {
            failure = "RayTraceAPI 不可用，已取消不安全的传送";
            return false;
        }

        var hullIndex = SelectPlayerHull(mesh);
        var hullAreas = mesh.GetHullAreas(hullIndex);
        if (hullAreas is null || hullAreas.Count == 0)
        {
            failure = $"导航网格中没有玩家 Hull {hullIndex} 的区域";
            return false;
        }

        var start = new NavVector(origin.X, origin.Y, origin.Z);
        var startArea = FindClosestArea(hullAreas, start, 512.0f);
        if (startArea is null)
        {
            failure = "玩家附近没有导航区域";
            return false;
        }

        var reachable = CollectReachableAreas(mesh, hullIndex, startArea);
        if (reachable.Count == 0)
        {
            failure = "没有从当前位置可达的导航区域";
            return false;
        }

        var distant = reachable
            .Where(area => HorizontalDistanceSquared(PolygonCenter(area.Corners), start)
                >= MinimumTeleportDistance * MinimumTeleportDistance)
            .ToArray();
        var candidates = distant.Length > 0 ? distant : reachable.ToArray();

        for (var attempt = 0; attempt < CandidateAttempts; attempt++)
        {
            var area = candidates[Random.Shared.Next(candidates.Length)];
            if (!TrySamplePolygon(area.Corners, out var sampled))
            {
                continue;
            }

            if (!TryResolveSafeGround(player, sampled, out var safePosition))
            {
                continue;
            }

            if (!IsClearOfPlayers(player, safePosition, MinimumPlayerClearance))
            {
                continue;
            }

            destination = safePosition;
            return true;
        }

        failure = $"尝试 {CandidateAttempts} 个导航落点后仍未找到安全位置";
        return false;
    }

    private void OnMapStart(string mapName) => LoadMap(mapName);

    private void OnMapEnd() => Clear();

    private void LoadMap(string mapName)
    {
        Clear();
        _mapName = mapName;

        try
        {
            if (!TryOpenNavStream(mapName, out var stream, out var source, out var error))
            {
                _lastError = error;
                _plugin.Logger.LogWarning(
                    "NavMesh unavailable for map {Map}: {Error}",
                    mapName,
                    error);
                return;
            }

            using (stream)
            {
                var mesh = new NavMeshFile();
                mesh.Read(stream);
                if (mesh.Areas.Count == 0)
                {
                    throw new InvalidDataException("NAV contains no areas");
                }

                _mesh = mesh;
            }

            _source = source;
            _lastError = null;
            _plugin.Logger.LogInformation(
                "Loaded static NavMesh for {Map}: version={Version}, areas={Areas}, source={Source}",
                mapName,
                _mesh.Version,
                _mesh.Areas.Count,
                source);
        }
        catch (Exception exception)
        {
            _mesh = null;
            _source = null;
            _lastError = $"NAV 解析失败：{exception.Message}";
            _plugin.Logger.LogError(exception, "Failed to load NavMesh for map {Map}", mapName);
        }
    }

    private static bool TryOpenNavStream(
        string mapName,
        out Stream stream,
        out string source,
        out string error)
    {
        stream = Stream.Null;
        source = string.Empty;
        error = string.Empty;

        var relativeMap = NormalizeMapName(mapName);
        if (relativeMap.Length == 0)
        {
            error = "地图名称无效";
            return false;
        }

        var mapsRoot = Path.Combine(Server.GameDirectory, "csgo", "maps");
        var leafName = Path.GetFileName(relativeMap);
        foreach (var navPath in EnumerateMapFiles(mapsRoot, relativeMap, leafName, ".nav"))
        {
            try
            {
                stream = new FileStream(navPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                source = navPath;
                return true;
            }
            catch (IOException)
            {
                // Try the next candidate; a workshop download may still be replacing this file.
            }
        }

        foreach (var vpkPath in EnumerateMapFiles(mapsRoot, relativeMap, leafName, ".vpk"))
        {
            try
            {
                using var package = new Package();
                package.Read(vpkPath);
                var navEntries = package.Entries?.GetValueOrDefault("nav");
                var entry = navEntries?.FirstOrDefault(candidate =>
                    string.Equals(candidate.FileName, leafName, StringComparison.OrdinalIgnoreCase))
                    ?? navEntries?.FirstOrDefault();
                if (entry is null)
                {
                    continue;
                }

                package.ReadEntry(entry, out var bytes);
                stream = new MemoryStream(bytes, writable: false);
                source = $"{vpkPath}::{entry.GetFullPath()}";
                return true;
            }
            catch (InvalidDataException)
            {
                // Not every file named .vpk is a package we can read; continue safely.
            }
            catch (IOException)
            {
                // Try another exact/workshop candidate.
            }
        }

        error = $"未找到 {leafName}.nav，也未在地图 VPK 中找到 NAV";
        return false;
    }

    private static IEnumerable<string> EnumerateMapFiles(
        string mapsRoot,
        string relativeMap,
        string leafName,
        string extension)
    {
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var exact = Path.Combine(mapsRoot, relativeMap.Replace('/', Path.DirectorySeparatorChar) + extension);
        if (File.Exists(exact) && yielded.Add(exact))
        {
            yield return exact;
        }

        var rootCandidate = Path.Combine(mapsRoot, leafName + extension);
        if (File.Exists(rootCandidate) && yielded.Add(rootCandidate))
        {
            yield return rootCandidate;
        }

        var workshopRoot = Path.Combine(mapsRoot, "workshop");
        if (!Directory.Exists(workshopRoot))
        {
            yield break;
        }

        foreach (var candidate in Directory.EnumerateFiles(
                     workshopRoot,
                     leafName + extension,
                     SearchOption.AllDirectories))
        {
            if (yielded.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static string NormalizeMapName(string mapName)
    {
        var normalized = mapName.Replace('\\', '/').Trim().TrimStart('/');
        if (normalized.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[5..];
        }

        if (normalized.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".nav", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "." or "..")
            ? string.Empty
            : string.Join('/', segments);
    }

    private static byte SelectPlayerHull(NavMeshFile mesh)
    {
        var generation = mesh.GenerationParams;
        if (generation is null || generation.HullParams.Length == 0)
        {
            return mesh.Areas.Values.Select(area => area.HullIndex).DefaultIfEmpty((byte)0).Min();
        }

        return generation.HullParams
            .Select((hull, index) => new
            {
                Hull = hull,
                Index = index,
                Score = MathF.Abs(hull.Radius - PlayerHullRadius)
                    + MathF.Abs(hull.Height - PlayerHullHeight) * 0.25f
            })
            .Where(item => item.Index <= byte.MaxValue
                && item.Hull.Enabled
                && mesh.GetHullAreas((byte)item.Index) is { Count: > 0 })
            .OrderBy(item => item.Score)
            .Select(item => (byte)item.Index)
            .DefaultIfEmpty(mesh.Areas.Values.Select(area => area.HullIndex).Min())
            .First();
    }

    private static NavMeshArea? FindClosestArea(
        IReadOnlyCollection<NavMeshArea> areas,
        NavVector position,
        float maximumDistance)
    {
        NavMeshArea? closest = null;
        var closestScore = float.MaxValue;
        foreach (var area in areas)
        {
            if (area.Corners.Length < 3)
            {
                continue;
            }

            var center = PolygonCenter(area.Corners);
            var horizontal = PointInPolygon(position, area.Corners)
                ? 0.0f
                : MathF.Sqrt(HorizontalDistanceSquared(center, position));
            var score = horizontal + MathF.Abs(center.Z - position.Z) * 1.5f;
            if (score < closestScore)
            {
                closestScore = score;
                closest = area;
            }
        }

        return closestScore <= maximumDistance ? closest : null;
    }

    private static List<NavMeshArea> CollectReachableAreas(
        NavMeshFile mesh,
        byte hullIndex,
        NavMeshArea start)
    {
        var areas = mesh.GetHullAreas(hullIndex)?
            .ToDictionary(area => area.AreaId)
            ?? [];
        var result = new List<NavMeshArea>(areas.Count);
        var visited = new HashSet<uint>();
        var queue = new Queue<NavMeshArea>();
        visited.Add(start.AreaId);
        queue.Enqueue(start);

        while (queue.TryDequeue(out var current))
        {
            result.Add(current);
            foreach (var connection in current.Connections.SelectMany(edge => edge))
            {
                if (!visited.Add(connection.AreaId)
                    || !areas.TryGetValue(connection.AreaId, out var next))
                {
                    continue;
                }

                queue.Enqueue(next);
            }
        }

        return result;
    }

    private bool TryResolveSafeGround(
        CCSPlayerController player,
        NavVector candidate,
        out GameVector safePosition)
    {
        safePosition = new GameVector(0.0f, 0.0f, 0.0f);
        if (!TryGetRayTrace(out var rayTrace, logMissing: true))
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.Collision is null)
        {
            return false;
        }

        var options = CreatePlayerTraceOptions(pawn);
        var start = new GameVector(candidate.X, candidate.Y, candidate.Z + 96.0f);
        var end = new GameVector(candidate.X, candidate.Y, candidate.Z - 256.0f);
        var pointMins = new GameVector(-0.5f, -0.5f, -0.5f);
        var pointMaxs = new GameVector(0.5f, 0.5f, 0.5f);
        TraceResult groundTrace = default;

        try
        {
            rayTrace.TraceHullShape(start, end, pointMins, pointMaxs, pawn, options, out groundTrace);
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "NavMesh ground trace failed");
            return false;
        }

        if (!groundTrace.DidHit || groundTrace.IsAllSolid || groundTrace.Normal.Z < 0.65f)
        {
            return false;
        }

        safePosition = new GameVector(
            groundTrace.EndPos.X,
            groundTrace.EndPos.Y,
            groundTrace.EndPos.Z + 2.0f);
        return IsHullSpaceFree(player, safePosition, logMissingCapability: true);
    }

    private bool IsHullSpaceFree(
        CCSPlayerController player,
        GameVector position,
        bool logMissingCapability)
    {
        if (!TryGetRayTrace(out var rayTrace, logMissingCapability))
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.Collision is null)
        {
            return false;
        }

        var end = new GameVector(position.X, position.Y, position.Z + 1.0f);
        var mins = new GameVector(-PlayerHullRadius, -PlayerHullRadius, 0.0f);
        var maxs = new GameVector(PlayerHullRadius, PlayerHullRadius, PlayerHullHeight);
        TraceResult occupancyTrace = default;
        try
        {
            rayTrace.TraceHullShape(
                position,
                end,
                mins,
                maxs,
                pawn,
                CreatePlayerTraceOptions(pawn),
                out occupancyTrace);
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "NavMesh occupancy trace failed");
            return false;
        }

        return !occupancyTrace.DidHit && !occupancyTrace.IsAllSolid;
    }

    private static TraceOptions CreatePlayerTraceOptions(CCSPlayerPawn pawn) => new()
    {
        InteractsWith = pawn.Collision!.CollisionAttribute.InteractsWith | (ulong)InteractionLayers.Hitboxes,
        InteractsExclude = 0,
        DrawBeam = 0
    };

    private bool TryGetRayTrace(out CRayTraceInterface rayTrace, bool logMissing)
    {
        rayTrace = null!;
        try
        {
            rayTrace = _rayTrace.Get()!;
        }
        catch (Exception exception)
        {
            if (logMissing)
            {
                LogMissingRayTrace(exception);
            }

            return false;
        }

        if (rayTrace is not null)
        {
            return true;
        }

        if (logMissing)
        {
            LogMissingRayTrace(null);
        }

        return false;
    }

    private static bool IsClearOfPlayers(
        CCSPlayerController subject,
        GameVector position,
        float clearance)
    {
        var clearanceSquared = clearance * clearance;
        foreach (var other in Utilities.GetPlayers())
        {
            if (!other.IsValid || !other.PawnIsAlive || other.Slot == subject.Slot)
            {
                continue;
            }

            var origin = other.PlayerPawn.Value?.AbsOrigin;
            if (origin is null)
            {
                continue;
            }

            var dx = position.X - origin.X;
            var dy = position.Y - origin.Y;
            var dz = position.Z - origin.Z;
            if (dx * dx + dy * dy + dz * dz < clearanceSquared)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TrySamplePolygon(NavVector[] corners, out NavVector point)
    {
        point = default;
        if (corners.Length < 3)
        {
            return false;
        }

        var first = corners[0];
        var triangles = new List<(NavVector B, NavVector C, float Area)>();
        var totalArea = 0.0f;
        for (var i = 1; i < corners.Length - 1; i++)
        {
            var b = corners[i];
            var c = corners[i + 1];
            var area = MathF.Abs((b.X - first.X) * (c.Y - first.Y)
                - (b.Y - first.Y) * (c.X - first.X)) * 0.5f;
            if (area <= 0.01f)
            {
                continue;
            }

            triangles.Add((b, c, area));
            totalArea += area;
        }

        if (triangles.Count == 0 || totalArea <= 0.01f)
        {
            return false;
        }

        var selection = Random.Shared.NextSingle() * totalArea;
        var selected = triangles[^1];
        foreach (var triangle in triangles)
        {
            selection -= triangle.Area;
            if (selection <= 0.0f)
            {
                selected = triangle;
                break;
            }
        }

        var root = MathF.Sqrt(Random.Shared.NextSingle());
        var v = Random.Shared.NextSingle();
        var aWeight = 1.0f - root;
        var bWeight = root * (1.0f - v);
        var cWeight = root * v;
        var sampled = first * aWeight + selected.B * bWeight + selected.C * cWeight;

        // Keep away from area borders where the player hull may overlap a wall.
        point = Vector3.Lerp(PolygonCenter(corners), sampled, 0.80f);
        return true;
    }

    private static NavVector PolygonCenter(IReadOnlyList<NavVector> corners)
    {
        var total = NavVector.Zero;
        foreach (var corner in corners)
        {
            total += corner;
        }

        return corners.Count == 0 ? total : total / corners.Count;
    }

    private static bool PointInPolygon(NavVector point, IReadOnlyList<NavVector> corners)
    {
        var inside = false;
        for (int i = 0, j = corners.Count - 1; i < corners.Count; j = i++)
        {
            var a = corners[i];
            var b = corners[j];
            if ((a.Y > point.Y) != (b.Y > point.Y)
                && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static float HorizontalDistanceSquared(NavVector left, NavVector right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return dx * dx + dy * dy;
    }

    private void LogMissingRayTrace(Exception? exception)
    {
        if (_missingRayTraceLogged)
        {
            return;
        }

        _missingRayTraceLogged = true;
        _plugin.Logger.LogError(
            exception,
            "Ray-Trace capability {Capability} is unavailable; safe NavMesh teleport is disabled",
            RayTraceCapability);
    }

    private void Clear()
    {
        _mesh = null;
        _mapName = null;
        _source = null;
        _lastError = null;
        _missingRayTraceLogged = false;
    }
}
