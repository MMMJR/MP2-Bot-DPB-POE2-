using DreamPoeBot.Loki.Game;
using System.Windows;


namespace MP2
{
    /// <summary>
    /// Interaction logic for EmptyProjectGui.xaml
    /// </summary>
    public partial class Mp2AutoLoginGui
    {
        public Mp2AutoLoginGui()
        {
            InitializeComponent();
        }

        private void SetCharNameButton_Click(object sender, RoutedEventArgs e)
        {
            if (LokiPoe.IsInGame)
            {
                var charName = LokiPoe.Me.Name;
                Mp2AutoLoginSettings.Instance.Character = string.IsNullOrEmpty(charName) ? "error" : charName;
            }
            else
            {
                MessageBoxes.Error("You must be in the game.");
            }
        }
    }
}
