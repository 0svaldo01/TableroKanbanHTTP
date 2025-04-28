using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TableroKanbanHTTP.Services;
using TableroKanbanHTTP.ViewModels;

namespace TableroKanbanHTTP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TableroKanbanServer server;
        private readonly DatosTableroViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            server = new TableroKanbanServer();
            viewModel =new DatosTableroViewModel(server);
            DataContext = viewModel;

        }
    }
}