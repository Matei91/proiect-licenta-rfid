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
    public partial class GetCashForm : Form
    {
        //private static readonly string redisConnectionString = "redis-10874.c335.europe-west2-1.gce.redns.redis-cloud.com:10874, password=jURlxVjs4aBFrDtnHflQESldtNyXoAys";
        private static readonly string redisConnectionString = "redis-16658.crce198.eu-central-1-3.ec2.redns.redis-cloud.com:16658,password=2kgm9DrslqbJ0DrbZHYyo4cKP83fmCss";
        private ConnectionMultiplexer redis;
        private IDatabase db;

        public GetCashForm()
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

        private void GetCashForm_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Verifică statusul PIN-ului înainte de a continua
                var pinStatus = db.StringGet("pin_status"); // Sau cheia corectă din Redis
                if (pinStatus != "VALID")
                {
                    MessageBox.Show("PIN-ul introdus este invalid. Vă rugăm să îl reintroduceți.");
                    return;
                }

                // Verifică dacă valoarea introdusă este un număr întreg valid
                int sumaExtrasa;
                if (int.TryParse(textBox1.Text, out sumaExtrasa))
                {
                    // Validăm suma introdusă pentru a fi mai mare decât 0
                    if (sumaExtrasa <= 0)
                    {
                        MessageBox.Show("Suma introdusă trebuie să fie mai mare decât 0.");
                    }
                    else if (sumaExtrasa % 10 != 0 && sumaExtrasa % 10 != 5)
                    {
                        MessageBox.Show("Suma introdusă trebuie să fie multiplu de 10 sau de 5.");
                    }
                    else
                    {
                        // Trimite suma pentru retragere pe Redis
                        db.StringSet("suma_extragere", sumaExtrasa.ToString()); // Suma este trimisă în Redis ca string
                        db.StringSet("command", "EXTRAGERE_NUMERAR"); // Setează comanda de extragere numerar

                        // Afișează un mesaj de succes
                        MessageBox.Show($"Suma de {sumaExtrasa} de lei a fost trimisă pentru extragere.");

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


        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
