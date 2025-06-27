using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Proiect
{
    public partial class Introducere_Pin : Form
    {
        //private static readonly string redisConnectionString = "redis-10874.c335.europe-west2-1.gce.redns.redis-cloud.com:10874, password=jURlxVjs4aBFrDtnHflQESldtNyXoAys";
        private static readonly string redisConnectionString = "redis-16658.crce198.eu-central-1-3.ec2.redns.redis-cloud.com:16658,password=2kgm9DrslqbJ0DrbZHYyo4cKP83fmCss";
        private ConnectionMultiplexer redis;
        private IDatabase db;

        public Introducere_Pin()
        {
            InitializeComponent();
            InitializeRedis();
            //db.StringSet("pin_status", "INVALID");
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pin = textBox1.Text.Trim();

            // Validare: PIN-ul trebuie să fie format din exact 4 cifre
            if (string.IsNullOrEmpty(pin) || pin.Length != 4 || !pin.All(char.IsDigit))
            {
                MessageBox.Show("PIN-ul trebuie să fie format din exact 4 cifre!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Trimiterea PIN-ului către Redis
                db.StringSet("pin_introdus", pin);
                db.StringSet("command", "CHECK_PIN");
                MessageBox.Show("PIN-ul a fost trimis pentru validare.");

                // Închidem formularul curent
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la trimiterea PIN-ului: {ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void Introducere_Pin_Load(object sender, EventArgs e)
        {

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
