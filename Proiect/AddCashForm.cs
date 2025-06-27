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
    public partial class AddCashForm : Form
    {
        //private static readonly string redisConnectionString = "redis-10874.c335.europe-west2-1.gce.redns.redis-cloud.com:10874, password=jURlxVjs4aBFrDtnHflQESldtNyXoAys";
        private static readonly string redisConnectionString = "redis-16658.crce198.eu-central-1-3.ec2.redns.redis-cloud.com:16658,password=2kgm9DrslqbJ0DrbZHYyo4cKP83fmCss";
        private ConnectionMultiplexer redis;
        private IDatabase db;

        public AddCashForm()
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

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
        private void buttonOk_Click(object sender, EventArgs e)
        {
            
        }


        private void AddCashForm_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void AddCashForm_Load_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            decimal soldCurent = Convert.ToDecimal(db.StringGet("balance"));

            try
            {
                // Verifică dacă valoarea introdusă este un număr întreg valid
                int sumaIntroducere;
                if (int.TryParse(textBox1.Text, out sumaIntroducere))
                {
                    // Validăm suma introdusă pentru a fi mai mare decât 0 și mai mică sau egală cu 500
                    if (sumaIntroducere <= 0)
                    {
                        MessageBox.Show("Suma introdusă trebuie să fie mai mare decât 0.");
                    }
                    else if (sumaIntroducere % 10 != 0 && sumaIntroducere % 10 != 5)
                    {
                        MessageBox.Show("Suma introdusă trebuie să fie multiplu de 10 sau de 5.");
                    }
                    else if (sumaIntroducere > 500)
                    {
                        MessageBox.Show("Suma introdusă nu poate depăși 500 de lei.");
                    }
                    else if (soldCurent + sumaIntroducere > 500)
                    {
                        MessageBox.Show("Suma totală de pe card nu poate depăși 500 de lei. Introduceți o sumă mai mică.");
                    }
                    else
                    {
                        // Trimite suma pe Redis pentru Arduino
                        db.StringSet("suma_adaugare", sumaIntroducere.ToString()); // Suma este trimisă în Redis ca string

                        // Trimite comanda de adăugare numerar
                        db.StringSet("command", "ADAUGARE_NUMERAR");
                        db.StringSet("suma_adaugare", sumaIntroducere.ToString()); // Suma introdusă se trimite pe Redis

                        // Afișează un mesaj de succes
                        MessageBox.Show($"Suma de {sumaIntroducere} de lei a fost trimisă pentru adăugare.");

                        // Închide fereastra curentă
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Vă rugăm să introduceți o sumă validă.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A apărut o eroare: {ex.Message}");
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }

}
