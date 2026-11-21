using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// Which "room" you are in. Rooms are joined by a code typed on the computer.
    ///
    /// Only one room exists, and it runs entirely on this device: BOTS is a room full of computer-controlled players
    /// that behave like real ones (see BotManager). There is no online server, so any other code is "not found".
    /// This is the one place a real networking layer would plug in later: TryJoin would ask a server instead.
    /// </summary>
    public static class RoomService
    {
        public const string Solo = "SOLO";
        public const string BotsCode = "BOTS";

        public enum JoinResult { Joined, AlreadyHere, NotFound }

        public static string CurrentRoom { get; private set; } = Solo;
        public static bool InBotsRoom { get { return CurrentRoom == BotsCode; } }

        /// <summary>Raised with the new room's code whenever you join or leave one.</summary>
        public static event System.Action<string> RoomChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            CurrentRoom = Solo;   // a fresh play session always starts alone
            RoomChanged = null;
        }

        public static JoinResult TryJoin(string code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code != BotsCode) return JoinResult.NotFound;
            if (CurrentRoom == code) return JoinResult.AlreadyHere;

            CurrentRoom = code;
            RoomChanged?.Invoke(CurrentRoom);
            return JoinResult.Joined;
        }

        public static void Leave()
        {
            if (CurrentRoom == Solo) return;
            CurrentRoom = Solo;
            RoomChanged?.Invoke(CurrentRoom);
        }
    }
}
