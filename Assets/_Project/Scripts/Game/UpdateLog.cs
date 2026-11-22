namespace GrillaPurgatory.Game
{
    /// <summary>
    /// The text shown on the update log screen in the box. Newest first. To add a release, put a new Entry at
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
                Version = "v1.0", Title = "The Box and the Maze",
                Notes = new[]
                {
                    "A new character, Pip, waves at you on the loading screen and in the mirror.",
                    "You wake in a safe box: a desk and computer, a mirror, this update log and a security monitor.",
                    "Press the red button for a flashlight from the conveyor. Squeeze a grip to pick it up.",
                    "The monitor shows cameras in the maze, in the style of an old VHS tape.",
                    "Walk out the door into a massive, dark maze. Something is lurking in it.",
                },
            },
            new Entry
            {
                Version = "v0.9", Title = "A fresh start",
                Notes = new[]
                {
                    "Removed the old maps, monsters, bots and rooms to start again.",
                    "Kept: the gorilla movement, arm-span calibration, the mirror and the computer.",
                },
            },
            new Entry
            {
                Version = "v0.3", Title = "Movement",
                Notes = new[]
                {
                    "Arm-span calibration: hold both grips in a T-pose.",
                    "Surfaces can grip or slide.",
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
