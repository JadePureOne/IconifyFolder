using IconifyFolder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IconifyFolder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitData();
        }

        private void InitData()
        {
            this.DataContext = App.Current.Services.GetService<MainViewModel>();
        }
    }
}