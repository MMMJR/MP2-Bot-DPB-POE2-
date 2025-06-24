using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MP2.EXtensions;
using MP2.EXtensions.Mapper;
using MP2;
using Binding = System.Windows.Data.Binding;
using CheckBox = System.Windows.Controls.CheckBox;
using ToolTip = System.Windows.Controls.ToolTip;


namespace MP2
{
	/// <summary>
	/// </summary>
	public partial class Mp2Gui
	{
        private readonly ToolTip _toolTip = new ToolTip();
        private readonly ICollectionView _mapCollectionView;
        private readonly ICollectionView _affixCollectionView;

        public Mp2Gui()
		{
			InitializeComponent();
            _mapCollectionView = CollectionViewSource.GetDefaultView(Mp2Settings.Instance.MapList);
            _affixCollectionView = CollectionViewSource.GetDefaultView(Mp2Settings.Instance.AffixList);
        }

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			Mp2Settings.Instance.SettingNumber++;
		}

        private bool AffixFilter(object obj)
        {
            var text = AffixSearchTextBox.Text;
            var affix = (AffixData)obj;
            return affix.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase) ||
                   affix.Description.ContainsIgnorecase(text);
        }

        private void AffixSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(AffixSearchTextBox.Text))
            {
                _affixCollectionView.Filter = null;
            }
            _affixCollectionView.Filter = AffixFilter;
        }

        private async void MapGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var header = e.Column.Header as string;
            if (header == null || header != "E%")
                return;

            var type = ((MapData)e.Row.DataContext).Type;
            if (type == MapType.Regular || type == MapType.Bossroom)
                return;

            e.Cancel = true;
            await ShowTooltip("Only for Regular and Bossroom maps");
        }

        private async void OnIbCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            var mapData = (MapData)((CheckBox)sender).DataContext;

            if (mapData.Type == MapType.Bossroom)
                return;

            await ShowTooltip("Only for Bossroom maps");
        }

        private async void OnFtCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            var mapData = (MapData)((CheckBox)sender).DataContext;
            var type = mapData.Type;

            if (type == MapType.Multilevel || type == MapType.Complex)
                return;

            await ShowTooltip("Only for Multilevel and Complex maps");
        }

        private async Task ShowTooltip(string msg)
        {
            if (_toolTip.IsOpen)
                return;

            _toolTip.Content = msg;
            _toolTip.IsOpen = true;
            await Task.Delay(2000);
            _toolTip.IsOpen = false;
        }

        // Last selected row no longer stays focused forever 
        private void DataGridUnselectAll(object sender, SelectionChangedEventArgs e)
        {
            ((DataGrid)sender).UnselectAll();
        }

        private async void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            if(await VerifySuApiClient.VerifyUserKey())
            {
                var DaysLeft = await VerifySuApiClient.VerifySu.GetKeyExpirationDate(Mp2Settings.Instance.UserKey);
                if (DaysLeft == null) DaysLeft = "1";
                Mp2Settings.Instance.DaysLeft = DaysLeft;
            }
        }
    }

    public class EnumToBoolConverter : IValueConverter
    {
        public static readonly EnumToBoolConverter Instance = new EnumToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}
