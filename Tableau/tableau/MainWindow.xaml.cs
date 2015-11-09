using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace tableau
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Initializes the main window that will hold all the views of the program within a stackpanel
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainMenu mm = new MainMenu();
            WindowPanel.Children.Add(mm);
        }
    }
}
