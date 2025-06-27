using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Proiect
{
    public partial class Instructiuni_pentru_folosire_ATM : Form
    {
        public Instructiuni_pentru_folosire_ATM()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // În `Form1`
            
                // Creează instanța noului formular (de exemplu, `Form2`)
                TOP_UP form2 = new TOP_UP();

                // Afișează noul formular
                form2.Show();

                // Ascunde (sau închide) formularul curent (Form1)
                this.Hide(); // `this.Close();` dacă vrei să-l închizi complet
            

        }

        private void Instructiuni_pentru_folosire_ATM_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
