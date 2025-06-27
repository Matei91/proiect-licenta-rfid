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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Creează instanța noului formular (de exemplu, `Form2`)
            //Top UP form2 = new Form2();
            TOP_UP form2 = new TOP_UP();
            // Afișează noul formular
            form2.Show();

            // Ascunde (sau închide) formularul curent (Form1)
            this.Hide(); // `this.Close();` dacă vrei să-l închizi complet
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Creează instanța noului formular (de exemplu, `Form2`)
            //Top UP form2 = new Form2();
            Store form2 = new Store();
            // Afișează noul formular
            form2.Show();

            // Ascunde (sau închide) formularul curent (Form1)
            this.Hide(); // `this.Close();` dacă vrei să-l închizi complet
        }
    }
}
