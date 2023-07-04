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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public Storyboard FlashGridButtonAnimation =>
            (Storyboard)this.FindResource("FlashGridButton");

        public Storyboard FlashGridSuccessAnimation =>
            (Storyboard)this.FindResource("FlashGridSuccess");

        public Storyboard FlashGridFailureAnimation =>
            (Storyboard)this.FindResource("FlashGridFailed");

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
