namespace GrillaPurgatory.Game
{
    /// <summary>The tips shown on loading screens.</summary>
    public static class GameTips
    {
        public static readonly string[] All =
        {
            "Push off the ground with your hands to move. Swing both arms and let go to launch yourself.",
            "Hold both grips with your arms out wide for a moment to calibrate your arm span.",
            "The box is a safe spot. Nothing can get you in there, so take your time.",
            "Press the red button by the conveyor belt for a flashlight. Squeeze a grip to pick it up.",
            "The maze is huge and very dark. Point your flashlight ahead, but a light can be seen from far away.",
            "You can hear the Lurker before you see it. The louder it gets, the closer it is.",
            "The security monitor in the box shows cameras around the maze. Check where it is before you go.",
            "Slapping the ground hard makes noise. Move gently if you want to stay hidden.",
            "Your mirror is on the wall behind where you wake up. Take a look at yourself.",
            "If the Lurker catches you, you wake up back in the box. Take a breath and try again.",
            "The corridors are wide, so the Lurker can spot you from a long way off. Use the corners.",
            "Is it running slow? Change the quality on the computer in the box.",
            "Only the box is safe. Reach the exit at the far end of the maze to come back.",
        };

        public static string Random()
        {
            return All[UnityEngine.Random.Range(0, All.Length)];
        }
    }
}
