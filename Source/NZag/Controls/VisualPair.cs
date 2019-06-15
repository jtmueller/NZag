using System.Windows.Media;

namespace NZag.Controls
{
    internal class VisualPair
    {
        public readonly Visual Background;
        public readonly Visual Character;

        public VisualPair(Visual background, Visual character)
        {
            Background = background;
            Character = character;
        }
    }
}