using StackExchange.Redis;
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
    public partial class Activare_Pin_NumePersoana : Form
    {

        //private static readonly string redisConnectionString = "redis-10874.c335.europe-west2-1.gce.redns.redis-cloud.com:10874, password=jURlxVjs4aBFrDtnHflQESldtNyXoAys";
        private static readonly string redisConnectionString = "redis-16658.crce198.eu-central-1-3.ec2.redns.redis-cloud.com" +
            ":16658,password=2kgm9DrslqbJ0DrbZHYyo4cKP83fmCss";
        private ConnectionMultiplexer redis;
        private IDatabase db;

        public Activare_Pin_NumePersoana()
        {
            InitializeComponent();
            InitializeRedis();
        }

        private void InitializeRedis()
        {
            try
            {
                redis = ConnectionMultiplexer.Connect(redisConnectionString);
                db = redis.GetDatabase();
                Console.WriteLine("Conectat la Redis!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la conectarea Redis: " + ex.Message);
            }
        }

        private void Activare_Pin_NumePersoana_Load(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Preluăm datele din câmpurile text
            string numePrenume = textBox2.Text.Trim();
            string pin = textBox1.Text.Trim();

            // Validare: câmpuri completate
            if (string.IsNullOrEmpty(numePrenume) || string.IsNullOrEmpty(pin))
            {
                MessageBox.Show("Te rog să completezi toate câmpurile!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validare: nume și prenume
            if (!numePrenume.Contains(" ") || numePrenume.Split(' ').Length != 2)
            {
                MessageBox.Show("Numele complet trebuie să conțină nume și prenume separate de un spațiu!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validare: PIN-ul trebuie să fie format din exact 4 cifre
            if (pin.Length != 4 || !pin.All(char.IsDigit))
            {
                MessageBox.Show("PIN-ul trebuie să fie format din 4 cifre!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Salvăm datele în Redis
                db.StringSet("Nume_Prenume", numePrenume);
                db.StringSet("PIN_Card", pin);

                // Setează comanda pentru activarea cardului
                db.StringSet("command", "ACTIVARE_CARD");

                MessageBox.Show("Cardul a fost activat și comanda a fost trimisă cu succes!", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Închidem form-ul după activare
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A apărut o eroare: {ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            db.StringSet("pin_status", "Exit");
            // Creează o instanță a formularului TOP_UP
            //TOP_UP topUpForm = new TOP_UP();

            // Deschide formularul TOP_UP
            //topUpForm.Show();

            // Închide formularul curent
            this.Close();
        }
    }
}
