using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SIPSample
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        private string TransferNumber = string.Empty;

        public Window2()
        {
            InitializeComponent();
        }


        public string GetTransferNumber()
        {
            return TransferNumber;
        }

        public int GetReplaceLineNum()
        {
            if (TextBoxLineNum.Text.Length <= 0)
            {
                return 0;
            }

            return int.Parse(TextBoxLineNum.Text);
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxTranferNumber.Text.Length > 0)
            {
                TransferNumber = TextBoxTranferNumber.Text;
                TextBoxTranferNumber.Text = "";
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please enter the transfer number.");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TransferNumber = "";
            TextBoxTranferNumber.Text = "";

            TextBoxLineNum.Text = "";
        }
    }
}
