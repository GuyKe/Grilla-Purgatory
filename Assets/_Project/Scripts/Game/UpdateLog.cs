namespace GrillaPurgatory.Game
{
    /// <summary>
    /// The text shown on the update log screen in the dome. Newest first. To add a release, put a new Entry at
    /// the top of the list (keep each note to about one line so it fits on the screen).
    /// </summary>
    public static class UpdateLog
    {
        public struct Entry
        {
            public string Version;
            public string Title;
            public string[] Notes;
        }

        public static readonly Entry[] Entries =
        {
            new Entry
            {
                Version = "v0.10", Title = "Rooms",
                Notes = new[]
                {
                    "New ROOM tab on the computer: type a join code with the keyboard and press JOIN.",
                    "Code BOTS: a room full of other players who act like real ones. Check the sign for who it's for.",
                    "Bots chat, wander, tiptoe, panic and run for the dome, and the monster hunts them too.",
                    "Only big movements make noise now. Tiptoe slowly and the monster won't hear you.",
                },
            },
            new Entry
            {
                Version = "v0.9", Title = "The Dome",
                Notes = new[]
                {
                    "New map: a huge wooden dome that is a safe spot, with a wall-mounted computer and this update log.",
                    "Outside is a big open field with a few hills and trees far apart.",
                    "New monster: it has no eyes and hunts by sound only. It is very fast.",
                    "Move your hands and body slowly to stay hidden. Fast swings and running are loud.",
                },
            },
            new Entry
            {
                Version = "v0.8", Title = "The Ape",
                Notes = new[]
                {
                    "You now have a body: a furry ape with a head, torso, arms and fists. No legs.",
                    "A working mirror so you can see yourself. Quality is set on the computer.",
                },
            },
            new Entry
            {
                Version = "v0.7", Title = "Fear",
                Notes = new[]
                {
                    "A quiet heartbeat that speeds up as the monster gets close, and the edges of your view darken.",
                    "The monster now starts and respawns closer to you.",
                    "Dying is now a black screen and the sound of being eaten.",
                },
            },
            new Entry
            {
                Version = "v0.6", Title = "Safe Spot",
                Notes = new[]
                {
                    "A safe zone around the spawn and the graphics computer: the monster can't enter.",
                    "The graphics computer: touch the screen to change quality, resolution and more.",
                    "Fixed being hard to move after dying.",
                },
            },
            new Entry
            {
                Version = "v0.5", Title = "The Hollow",
                Notes = new[]
                {
                    "The first monster: it wanders, hears your hands, chases on sight and catches you.",
                    "It can't jump, so high ground keeps you out of reach.",
                    "A death cutscene when it catches you.",
                },
            },
            new Entry
            {
                Version = "v0.4", Title = "The Field",
                Notes = new[]
                {
                    "A hilly grass field with grass, dirt and rock, and a forest of climbable trees.",
                    "Fixed sliding everywhere; only ice is slippery now.",
                },
            },
            new Entry
            {
                Version = "v0.3", Title = "Movement",
                Notes = new[]
                {
                    "Arm-span calibration: hold both grips in a T-pose.",
                    "Surfaces grip differently: bark sticks, ice slides.",
                    "A test course for running, wall running, pinch climbing and branching.",
                },
            },
            new Entry
            {
                Version = "v0.2", Title = "Arms",
                Notes = new[]
                {
                    "Gorilla-style arm locomotion: push off the ground and surfaces with your hands.",
                    "You stand on the ground at a lowered height so your hands reach it.",
                },
            },
            new Entry
            {
                Version = "v0.1", Title = "Hello VR",
                Notes = new[]
                {
                    "Project set up for Quest and PC VR with head and hand tracking.",
                },
            },
        };
    }
}
