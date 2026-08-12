using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace Myrt1eSkill_Remake.Core;

internal static class NetworkUpdateService
{
    public static void ForceFullUpdateToAllChunked(int clientsPerFrame = 2)
    {
        var pending = Utilities.GetPlayers().Where(player => player is { IsValid: true, IsBot: false }).Select(player => player.Index).ToList();
        if (pending.Count > 0) ForceChunk(pending, 0, Math.Max(1, clientsPerFrame));
    }

    private static void ForceChunk(List<uint> pending, int start, int perFrame)
    {
        if (start >= pending.Count) return;
        var server = new NetworkServerService().GetGameServer();
        var end = Math.Min(start + perFrame, pending.Count);
        for (var index = start; index < end; index++)
        {
            var player = Utilities.GetPlayerFromIndex((int)pending[index]);
            if (player is { IsValid: true }) server.GetClientBySlot(player.Slot)?.ForceFullUpdate();
        }
        Server.NextFrame(() => ForceChunk(pending, end, perFrame));
    }

    private sealed class NetworkServerService : NativeObject
    {
        private readonly VirtualFunctionWithReturn<nint, nint> _getGameServer;
        public NetworkServerService() : base(NativeAPI.GetValveInterface(0, "NetworkServerService_001"))
        {
            _getGameServer = new VirtualFunctionWithReturn<nint, nint>(Handle, GameData.GetOffset("INetworkServerService_GetIGameServer"));
        }
        public NetworkGameServer GetGameServer() => new(_getGameServer.Invoke(Handle));
    }

    private unsafe sealed class NetworkGameServer(nint handle) : NativeObject(handle)
    {
        private static readonly int SlotsOffset = GameData.GetOffset("INetworkGameServer_Slots");
        private ref UtlVector<nint> Slots => ref Unsafe.AsRef<UtlVector<nint>>((void*)(Handle + SlotsOffset));
        public ServerSideClient? GetClientBySlot(int slot)
        {
            if (Handle == nint.Zero || slot < 0 || slot >= Slots.Count || Slots.Memory.Pointer == null) return null;
            var pointer = Slots.Memory.Pointer[slot];
            return pointer == nint.Zero ? null : new ServerSideClient(pointer);
        }
    }

    private unsafe struct UtlVector<T> where T : unmanaged { public int Count; public UtlMemory<T> Memory; }
    private unsafe struct UtlMemory<T> where T : unmanaged { public T* Pointer; public int AllocationCount; public int GrowSize; }

    private unsafe sealed class ServerSideClient(nint handle) : NativeObject(handle)
    {
        private static readonly int DeltaTickOffset = GameData.GetOffset("CServerSideClient_m_nDeltaTick");
        public void ForceFullUpdate()
        {
            if (Handle != nint.Zero) Unsafe.AsRef<int>((void*)(Handle + DeltaTickOffset)) = -1;
        }
    }
}
