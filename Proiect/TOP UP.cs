using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using StackExchange.Redis;

namespace Proiect
{
    public partial class TOP_UP : Form
    {
        // Configurare Redis
        //private static readonly string redisConnectionString = "redis-10874.c335.europe-west2-1.gce.redns.redis-cloud.com:10874,password=jURlxVjs4aBFrDtnHflQESldtNyXoAys";
        private static readonly string redisConnectionString = "redis-16658.crce198.eu-central-1-3.ec2.redns.redis-cloud.com:16658,password=2kgm9DrslqbJ0DrbZHYyo4cKP83fmCss";
        private ConnectionMultiplexer redis;
        private IDatabase db;

        public TOP_UP()
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
            try
            {
                // Instanțiem noul form pentru activarea cardului
                Activare_Pin_NumePersoana activareForm = new Activare_Pin_NumePersoana();

                // Afișăm noul form ca fereastră modală
                activareForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la deschiderea formularului: " + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void TOP_UP_Load(object sender, EventArgs e)
        {

        }
        private void button1_Paint(object sender, PaintEventArgs e)
        {
            Button btn = (Button)sender;
            Rectangle rect = btn.ClientRectangle;
            rect.Inflate(-1, -1);  // Adjust thickness here
            Color borderColor = Color.Blue;  // Choose your border color

            ControlPaint.DrawBorder(e.Graphics, rect, borderColor, ButtonBorderStyle.Solid);
        }

        private void label1_Click_1(object sender, EventArgs e)
        {
            //label1.Text = "Instructions:\n1. Click the button to start.\n2. Enter your details.\n3. Press 'Submit' to finish.";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // În `Form1`

            // Creează instanța noului formular (de exemplu, `Form2`)
            Instructiuni_pentru_folosire_ATM form3 = new Instructiuni_pentru_folosire_ATM();

            // Afișează noul formular
            form3.Show();

            // Ascunde (sau închide) formularul curent (Form1)
            this.Hide(); // `this.Close();` dacă vrei să-l închizi complet


        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Creează instanța noului formular (de exemplu, `Form2`)
            Form1 form2 = new Form1();

            // Afișează noul formular
            form2.Show();

            // Ascunde (sau închide) formularul curent (Form1)
            this.Hide(); // `this.Close();` dacă vrei să-l închizi complet

        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                // Conectare la Redis
                //ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"); // Asigură-te că Redis este conectat la acest server
                //IDatabase db = redis.GetDatabase();

                // Setează comanda de anulare în Redis
                db.StringSet("command", "ANULARE_COMANDA");

                // Afișează mesajul în interfața utilizatorului
                MessageBox.Show("Comanda a fost anulată.");
            }
            catch (Exception ex)
            {
                // Afișează eroarea dacă apare una
                MessageBox.Show("Eroare la trimiterea comenzii: " + ex.Message);
            }
        }
        private bool isPinFormClosed = false;

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Introducere_Pin pinForm = new Introducere_Pin();
                pinForm.ShowDialog();

                // Verificăm statusul PIN-ului cu un timeout de 10 secunde
                string pinStatus = "";
                int attempts = 20; // 20 * 500ms = 10 secunde
                while (attempts > 0 && string.IsNullOrEmpty(pinStatus))
                {
                    pinStatus = db.StringGet("pin_status");
                    Thread.Sleep(500);
                    attempts--;
                }

                if (pinStatus == "VALID")
                {
                    MessageBox.Show("PIN-ul este corect! Deschidem formularul pentru adăugarea numerarului.");
                    AddCashForm addCashForm = new AddCashForm();
                    addCashForm.ShowDialog();
                }
                else
                {
                    MessageBox.Show("PIN-ul introdus este greșit sau timpul a expirat!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Resetăm statusul PIN-ului în Redis
                db.StringSet("pin_status", "");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare: {ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                // Deschide formularul pentru introducerea PIN-ului
                Introducere_Pin pinForm = new Introducere_Pin();
                pinForm.ShowDialog();

                // Verificăm statusul PIN-ului din Redis
                string pinStatus = "";
                while (pinStatus == "")  // Așteptăm până primim răspunsul
                {
                    pinStatus = db.StringGet("pin_status");
                    Thread.Sleep(500); // Așteptăm 500ms înainte de a verifica din nou
                }

                if (pinStatus == "VALID")
                {
                    MessageBox.Show("PIN-ul este corect! Începem interogarea soldului.");

                    // Trimitem comanda de interogare sold către Redis
                    db.StringSet("command", "INTEROGARE_SOLD");

                    // Verificăm răspunsul din Redis
                    string soldValue = "";
                    while (string.IsNullOrEmpty(soldValue)) // Așteptăm răspunsul
                    {
                        soldValue = db.StringGet("sold_value");
                        Thread.Sleep(500); // Așteptăm 500ms înainte de a verifica din nou
                    }

                    // Afișăm soldul curent
                    MessageBox.Show($"Soldul curent este: {soldValue} lei", "Interogare Sold", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Resetăm valoarea soldului în Redis
                    db.StringSet("sold_value", "");
                }
                else
                {
                    MessageBox.Show("PIN-ul introdus este greșit! Interogarea soldului a fost anulată.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Resetăm statusul PIN-ului în Redis
                db.StringSet("pin_status", "");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare: {ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Introducere_Pin pinForm = new Introducere_Pin();
                pinForm.ShowDialog();

                // Verificăm statusul PIN-ului cu un timeout de 10 secunde
                string pinStatus = "";
                int attempts = 20; // 20 * 500ms = 10 secunde
                while (attempts > 0 && string.IsNullOrEmpty(pinStatus))
                {
                    pinStatus = db.StringGet("pin_status");
                    Thread.Sleep(500);
                    attempts--;
                }

                if (pinStatus == "VALID")
                {
                    MessageBox.Show("PIN-ul este corect! Deschidem formularul pentru extragerea numerarului.");
                    GetCashForm getCashForm = new GetCashForm();
                    getCashForm.ShowDialog();
                }
                else
                {
                    MessageBox.Show("PIN-ul introdus este greșit sau timpul a expirat!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Resetăm statusul PIN-ului în Redis
                db.StringSet("pin_status", "");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare: {ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // Deschide formularul pentru introducerea PIN-ului
                Introducere_Pin pinForm = new Introducere_Pin();
                pinForm.ShowDialog(); // Așteptăm închiderea formularului PIN

                // Verifică statusul PIN-ului din Redis
                string pinStatus = "";
                while (pinStatus == "") // Așteptăm până se actualizează statusul PIN-ului
                {
                    pinStatus = db.StringGet("pin_status");
                    Thread.Sleep(500); // Așteaptă 500ms înainte de a verifica din nou
                }

                // În loc de a verifica doar dacă este "VALID", așteptăm și confirmarea din Arduino
                int maxAttempts = 10;
                while (pinStatus != "VALID" && maxAttempts > 0)
                {
                    pinStatus = db.StringGet("pin_status");  // Verificăm statusul PIN-ului
                    Thread.Sleep(500);  // Verificăm din nou după 500ms
                    maxAttempts--;
                }

                if (pinStatus == "VALID")
                {
                    // Dacă PIN-ul este valid, trimitem comanda pentru afișarea tranzacțiilor
                    db.StringSet("command", "AFISARE_TRANZACTII");
                    MessageBox.Show("Comanda de Afisare Tranzactii ale cardului a fost trimisă!");
                }
                else
                {
                    // Dacă PIN-ul nu este valid, afișăm un mesaj de eroare
                    MessageBox.Show("PIN-ul introdus este greșit! Încearcă din nou.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Resetăm statusul PIN-ului în Redis
                db.StringSet("pin_status", "");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la trimiterea comenzii: " + ex.Message);
                Console.WriteLine(ex.Message);
            }
        }



        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                // Setează comanda pentru activarea cardului
                db.StringSet("command", "DEZACTIVARE_CARD");
                MessageBox.Show("Comanda de dezactivare card a fost trimisă!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la trimiterea comenzii: " + ex.Message);
                Console.WriteLine(ex.Message);
            }
        }

        private void Informatii_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
