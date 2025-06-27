using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Proiect
{
    public partial class Store : Form
    {
        //private static readonly string redisConnectionString = "redis-10874.c335.europe-west2-1.gce.redns.redis-cloud.com:10874,password=jURlxVjs4aBFrDtnHflQESldtNyXoAys";
        private static readonly string redisConnectionString = "redis-16658.crce198.eu-central-1-3.ec2.redns.redis-cloud.com:16658,password=2kgm9DrslqbJ0DrbZHYyo4cKP83fmCss";
        private ConnectionMultiplexer redis;
        private IDatabase db;

        public Store()
        {
            InitializeComponent();
            InitializeRedis();
            db.StringSet("Pret_Cos_Cumparaturi", "0");
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
            // Creează instanța noului formular (de exemplu, `Form2`)
            Form1 form2 = new Form1();

            // Afișează noul formular
            form2.Show();

            // Ascunde (sau închide) formularul curent (Form1)
            this.Hide(); // `this.Close();` dacă vrei să-l închizi complet
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        public class CircularButton : Button
        {
            protected override void OnPaint(PaintEventArgs pevent)
            {
                Graphics g = pevent.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Desenează cercul
                using (Brush brush = new SolidBrush(this.BackColor))
                {
                    g.FillEllipse(brush, 0, 0, this.Width - 1, this.Height - 1);
                }

                // Desenează conturul cercului
                using (Pen pen = new Pen(this.ForeColor, 2))
                {
                    g.DrawEllipse(pen, 0, 0, this.Width - 1, this.Height - 1);
                }

                // Desenează textul în centru
                TextRenderer.DrawText(g, this.Text, this.Font,
                    new Rectangle(0, 0, this.Width, this.Height),
                    this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                // Menține forma circulară ajustând lățimea și înălțimea
                this.Width = this.Height;
            }
        }

        string OLD_pretTotal = null;
        string OLD_Cantitate = null;
        string OLD_Produse = null;
        int reducere = 0; // Variabila pentru reducere

        private void button2_Click(object sender, EventArgs e)
        {
            int pretTotal = 0;
            

            try
            {
                // Obține prețul total din label1
                string labelText = label1.Text.Replace("Preț:", "").Replace("lei", "").Trim();
                OLD_pretTotal = labelText;

                // Încercăm să convertim valoarea textului la int
                if (int.TryParse(labelText, out pretTotal) && pretTotal > 0)
                {
                    // Aplicăm reducerea dacă prețul total depășește 50 RON
                    if (pretTotal > 50)
                    {
                        reducere = 10; // Reducere de 10%
                        pretTotal = pretTotal - (pretTotal * reducere / 100); // Calculăm noul preț total
                        MessageBox.Show($"Felicitări! Ați beneficiat de o reducere de {reducere}% pentru achiziționarea produselor în valoare de peste 50 RON.");
                    }

                    // Trimite prețul total către Redis
                    db.StringSet("Pret_Cos_Cumparaturi", pretTotal.ToString());
                    MessageBox.Show("Prețul total a fost trimis către Redis!");
                }
                else
                {
                    MessageBox.Show("Prețul total este 0 sau invalid!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la trimiterea prețului: " + ex.Message);
                Console.WriteLine(ex.Message);
            }

            OLD_Cantitate = Cantitate.Text;
            OLD_Produse = label3.Text;

            label3.Text = "Ați selectat:";
            Cantitate.Text = "Cantitate:";
            label1.Text = "Preț:";

            // Resetăm variabilele de cantitate pentru fiecare produs
            cantitate = 0;       // Resetăm cantitatea de Ardei iute
            cantitateRosii = 0;  // Resetăm cantitatea de Roșii
            cantitateVinete = 0; // Resetăm cantitatea de Vinete
            cantitateLamai = 0;  // Resetăm cantitatea de Lămâi
            cantitatePortocale = 0; // Resetăm cantitatea de Portocale
            cantitateArdeiVerde = 0; // Resetăm cantitatea de Ardei verde
            cantitateArdeiRosu = 0;  // Resetăm cantitatea de Ardei roșu
            cantitateMere = 0;  // Resetăm cantitatea de Mere

            pretTotal = 0; // Resetăm și prețul total
        }


        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {
            //label3.BackColor = this.BackColor;
        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }
        // Declarația variabilei pentru cantitate
        int cantitate = 0;
        bool mesajAfișat = false;
        int cantitateRosii = 0;
        bool mesajAfișatRosii = false;

        // Variabile globale pentru prețurile per kg
        private const int pretArdei = 12; // lei/kg
        private const int pretRosii = 9; // lei/kg

        private void button6_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Ardei iute" nu este deja în textul label3
            if (!label3.Text.Contains("Ardei iute"))
            {
                // Dacă lista de produse e goală, adaugă "Ardei iute" fără virgulă
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Ardei iute";  // Adaugă fără virgulă
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Ardei iute";  // Adaugă cu virgulă doar dacă mai există produse
                }
            }

            // Crește cantitatea pentru "Ardei iute"
            cantitate++;
            Cantitate.Text = ActualizeazaCantitateText();  // Actualizează labelul cu cantitatea în kg
            ActualizeazaPretTotal(); // Actualizează prețul total
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (cantitate > 0)
            {
                // Scade cantitatea
                cantitate--;

                // Actualizează labelul cu cantitatea în kg
                Cantitate.Text = ActualizeazaCantitateText();

                // Dacă cantitatea ajunge la 0, elimină "Ardei iute" din label3
                if (cantitate == 0)
                {
                    if (label3.Text.Contains("Ardei iute"))
                    {
                        label3.Text = label3.Text.Replace(", Ardei iute", "").Replace("Ardei iute, ", "").Replace("Ardei iute", "").Trim();

                        if (label3.Text == "Ați selectat:" || label3.Text == "")
                        {
                            label3.Text = "Ați selectat:";
                        }
                    }
                }
                ActualizeazaPretTotal(); // Actualizează prețul total
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Roșii" nu este deja în textul label3
            if (!label3.Text.Contains("Roșii"))
            {
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Roșii";
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Roșii";
                }
            }

            // Crește cantitatea pentru "Roșii"
            cantitateRosii++;
            Cantitate.Text = ActualizeazaCantitateText();
            ActualizeazaPretTotal(); // Actualizează prețul total
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (cantitateRosii > 0)
            {
                cantitateRosii--;

                Cantitate.Text = ActualizeazaCantitateText();

                if (cantitateRosii == 0)
                {
                    if (label3.Text.Contains("Roșii"))
                    {
                        label3.Text = label3.Text.Replace(", Roșii", "").Replace("Roșii, ", "").Replace("Roșii", "").Trim();

                        if (label3.Text == "Ați selectat:" || label3.Text == "")
                        {
                            label3.Text = "Ați selectat:";
                        }
                    }
                }
                ActualizeazaPretTotal(); // Actualizează prețul total
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizeazaPretTotal()
        {
            int pretTotal = (cantitate * pretArdei)
                  + (cantitateRosii * pretRosii)
                  + (cantitateVinete * pretVinete)
                  + (cantitateLamai * pretLamai)
                  + (cantitatePortocale * pretPortocale)
                  + (cantitateArdeiVerde * pretArdeiVerde)
                  + (cantitateArdeiRosu * pretArdeiRosu)
                  + (cantitateMere * pretMere); // Adaugă prețul pentru Mere

            label1.Text = "Preț: " + pretTotal.ToString() + " lei";
        }

        private string ActualizeazaCantitateText()
        {
            string cantitateText = "Cantitate: ";

            if (cantitate > 0)
            {
                cantitateText += cantitate.ToString() + "kg Ardei iute";
            }

            if (cantitateRosii > 0)
            {
                if (cantitate > 0) cantitateText += ", ";
                cantitateText += cantitateRosii.ToString() + "kg Roșii";
            }

            if (cantitateVinete > 0)
            {
                if (cantitate > 0 || cantitateRosii > 0) cantitateText += ", ";
                cantitateText += cantitateVinete.ToString() + "kg Vinete";
            }

            if (cantitateLamai > 0)
            {
                if (cantitate > 0 || cantitateRosii > 0 || cantitateVinete > 0) cantitateText += ", ";
                cantitateText += cantitateLamai.ToString() + "kg Lămâi";
            }

            if (cantitatePortocale > 0)
            {
                if (cantitate > 0 || cantitateRosii > 0 || cantitateVinete > 0 || cantitateLamai > 0) cantitateText += ", ";
                cantitateText += cantitatePortocale.ToString() + "kg Portocale";
            }

            if (cantitateArdeiVerde > 0)
            {
                if (cantitate > 0 || cantitateRosii > 0 || cantitateVinete > 0 || cantitateLamai > 0 || cantitatePortocale > 0)
                    cantitateText += ", ";
                cantitateText += cantitateArdeiVerde.ToString() + "kg Ardei Verde";
            }

            if (cantitateArdeiRosu > 0)
            {
                if (cantitate > 0 || cantitateRosii > 0 || cantitateVinete > 0 || cantitateLamai > 0 || cantitatePortocale > 0 || cantitateArdeiVerde > 0)
                    cantitateText += ", ";
                cantitateText += cantitateArdeiRosu.ToString() + "kg Ardei Roșu";
            }

            // Adaugă cantitatea pentru Mere (dacă există)
            if (cantitateMere > 0)
            {
                if (cantitate > 0 || cantitateRosii > 0 || cantitateVinete > 0 || cantitateLamai > 0 || cantitatePortocale > 0 || cantitateArdeiVerde > 0 || cantitateArdeiRosu > 0)
                    cantitateText += ", ";
                cantitateText += cantitateMere.ToString() + "kg Mere";
            }

            return cantitateText;
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Store_Load(object sender, EventArgs e)
        {

        }

        int cantitateVinete = 0;
        const int pretVinete = 10;

        private void button9_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Vinete" nu este deja în textul label3
            if (!label3.Text.Contains("Vinete"))
            {
                // Dacă lista de produse e goală, adaugă "Vinete" fără virgulă
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Vinete";  // Adaugă fără virgulă
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Vinete";  // Adaugă cu virgulă doar dacă mai există produse
                }
            }

            // Crește cantitatea pentru "Vinete"
            cantitateVinete++;
            Cantitate.Text = ActualizeazaCantitateText();  // Actualizează labelul cu cantitatea în kg
            ActualizeazaPretTotal();  // Actualizează prețul total
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (cantitateVinete > 0)
            {
                // Scade cantitatea
                cantitateVinete--;

                // Actualizează labelul cu cantitatea în kg
                Cantitate.Text = ActualizeazaCantitateText();

                // Dacă cantitatea ajunge la 0, elimină "Vinete" din label3
                if (cantitateVinete == 0)
                {
                    // Elimină "Vinete" din textul label3 și virgula de dinaintea lui
                    label3.Text = label3.Text.Replace("Vinete, ", "").Replace(", Vinete", "").Replace("Vinete", "").Trim();

                    // Verifică dacă mai sunt alte produse și actualizează mesajul de afișare
                    if (label3.Text == "Ați selectat:" || label3.Text == "")
                    {
                        label3.Text = "Ați selectat:"; // Dacă niciun produs nu a rămas, resetează la mesajul inițial
                    }
                }

                // Actualizează prețul total
                ActualizeazaPretTotal();
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int cantitateLamai = 0;
        const int pretLamai = 8;

        private void button11_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Lămâi" nu este deja în textul label3
            if (!label3.Text.Contains("Lămâi"))
            {
                // Dacă lista de produse e goală, adaugă "Lămâi" fără virgulă
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Lămâi";  // Adaugă fără virgulă
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Lămâi";  // Adaugă cu virgulă doar dacă mai există produse
                }
            }

            // Crește cantitatea pentru "Lămâi"
            cantitateLamai++;
            Cantitate.Text = ActualizeazaCantitateText();  // Actualizează labelul cu cantitatea în kg
            ActualizeazaPretTotal();  // Actualizează prețul total
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (cantitateLamai > 0)
            {
                // Scade cantitatea
                cantitateLamai--;

                // Actualizează labelul cu cantitatea în kg
                Cantitate.Text = ActualizeazaCantitateText();

                // Dacă cantitatea ajunge la 0, elimină "Lămâi" din label3
                if (cantitateLamai == 0)
                {
                    // Elimină "Lămâi" din textul label3 și virgula de dinaintea lor
                    label3.Text = label3.Text.Replace("Lămâi, ", "").Replace(", Lămâi", "").Replace("Lămâi", "").Trim();

                    // Verifică dacă mai sunt alte produse și actualizează mesajul de afișare
                    if (label3.Text == "Ați selectat:" || label3.Text == "")
                    {
                        label3.Text = "Ați selectat:"; // Dacă niciun produs nu a rămas, resetează la mesajul inițial
                    }
                }

                // Actualizează prețul total
                ActualizeazaPretTotal();
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Variabile globale pentru Portocale
        int cantitatePortocale = 0;
        const int pretPortocale = 7;

        private void button13_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Portocale" nu este deja în textul label3
            if (!label3.Text.Contains("Portocale"))
            {
                // Dacă lista de produse e goală, adaugă "Portocale" fără virgulă
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Portocale";  // Adaugă fără virgulă
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Portocale";  // Adaugă cu virgulă doar dacă mai există produse
                }
            }

            // Crește cantitatea pentru "Portocale"
            cantitatePortocale++;
            Cantitate.Text = ActualizeazaCantitateText();  // Actualizează labelul cu cantitatea în kg
            ActualizeazaPretTotal();  // Actualizează prețul total
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (cantitatePortocale > 0)
            {
                // Scade cantitatea
                cantitatePortocale--;

                // Actualizează labelul cu cantitatea în kg
                Cantitate.Text = ActualizeazaCantitateText();

                // Dacă cantitatea ajunge la 0, elimină "Portocale" din label3
                if (cantitatePortocale == 0)
                {
                    // Elimină "Portocale" din textul label3 și virgula de dinaintea lor
                    label3.Text = label3.Text.Replace("Portocale, ", "").Replace(", Portocale", "").Replace("Portocale", "").Trim();

                    // Verifică dacă mai sunt alte produse și actualizează mesajul de afișare
                    if (label3.Text == "Ați selectat:" || label3.Text == "")
                    {
                        label3.Text = "Ați selectat:"; // Dacă niciun produs nu a rămas, resetează la mesajul inițial
                    }
                }

                // Actualizează prețul total
                ActualizeazaPretTotal();
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int cantitateArdeiVerde = 0;
        const int pretArdeiVerde = 15;

        private void button15_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Ardei Verde" nu este deja în textul label3
            if (!label3.Text.Contains("Ardei Verde"))
            {
                // Dacă lista de produse e goală, adaugă "Ardei Verde" fără virgulă
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Ardei Verde";  // Adaugă fără virgulă
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Ardei Verde";  // Adaugă cu virgulă doar dacă mai există produse
                }
            }

            // Crește cantitatea pentru "Ardei Verde"
            cantitateArdeiVerde++;
            Cantitate.Text = ActualizeazaCantitateText();  // Actualizează labelul cu cantitatea în kg
            ActualizeazaPretTotal();  // Actualizează prețul total
        }


        private void button16_Click(object sender, EventArgs e)
        {
            if (cantitateArdeiVerde > 0)
            {
                // Scade cantitatea
                cantitateArdeiVerde--;

                // Actualizează labelul cu cantitatea în kg
                Cantitate.Text = ActualizeazaCantitateText();

                // Dacă cantitatea ajunge la 0, elimină "Ardei Verde" din label3
                if (cantitateArdeiVerde == 0)
                {
                    // Elimină "Ardei Verde" din textul label3 și virgula de dinaintea lui
                    label3.Text = label3.Text.Replace("Ardei Verde, ", "").Replace(", Ardei Verde", "").Replace("Ardei Verde", "").Trim();

                    // Verifică dacă mai sunt alte produse și actualizează mesajul de afișare
                    if (label3.Text == "Ați selectat:" || label3.Text == "")
                    {
                        label3.Text = "Ați selectat:"; // Dacă niciun produs nu a rămas, resetează la mesajul inițial
                    }
                }

                // Actualizează prețul total
                ActualizeazaPretTotal();
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int cantitateArdeiRosu = 0;
        const int pretArdeiRosu = 13;

        private void button17_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Ardei Roșu" nu este deja în textul label3
            if (!label3.Text.Contains("Ardei Roșu"))
            {
                // Dacă lista de produse e goală, adaugă "Ardei Roșu" fără virgulă
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Ardei Roșu";  // Adaugă fără virgulă
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Ardei Roșu";  // Adaugă cu virgulă doar dacă mai există produse
                }
            }

            // Crește cantitatea pentru "Ardei Roșu"
            cantitateArdeiRosu++;
            Cantitate.Text = ActualizeazaCantitateText();  // Actualizează labelul cu cantitatea în kg
            ActualizeazaPretTotal();  // Actualizează prețul total
        }


        private void button18_Click(object sender, EventArgs e)
        {
            if (cantitateArdeiRosu > 0)
            {
                // Scade cantitatea
                cantitateArdeiRosu--;

                // Actualizează labelul cu cantitatea în kg
                Cantitate.Text = ActualizeazaCantitateText();

                // Dacă cantitatea ajunge la 0, elimină "Ardei Roșu" din label3
                if (cantitateArdeiRosu == 0)
                {
                    // Elimină "Ardei Roșu" din textul label3 și virgula de dinaintea lui
                    label3.Text = label3.Text.Replace("Ardei Roșu, ", "").Replace(", Ardei Roșu", "").Replace("Ardei Roșu", "").Trim();

                    // Verifică dacă mai sunt alte produse și actualizează mesajul de afișare
                    if (label3.Text == "Ați selectat:" || label3.Text == "")
                    {
                        label3.Text = "Ați selectat:"; // Dacă niciun produs nu a rămas, resetează la mesajul inițial
                    }
                }

                // Actualizează prețul total
                ActualizeazaPretTotal();
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int cantitateMere = 0;
        const int pretMere = 4;

        private void button19_Click(object sender, EventArgs e)
        {
            // Verificăm dacă "Mere" nu este deja în textul label3
            if (!label3.Text.Contains("Mere"))
            {
                // Dacă lista de produse e goală, adaugă "Mere" fără virgulă
                if (label3.Text == "Ați selectat:")
                {
                    label3.Text += " Mere";  // Adaugă fără virgulă
                }
                else
                {
                    label3.Text = label3.Text.TrimEnd(',') + ", Mere";  // Adaugă cu virgulă doar dacă mai există produse
                }
            }

            // Crește cantitatea pentru "Mere"
            cantitateMere++;
            Cantitate.Text = ActualizeazaCantitateText();  // Actualizează labelul cu cantitatea în kg
            ActualizeazaPretTotal();  // Actualizează prețul total
        }


        private void button20_Click(object sender, EventArgs e)
        {
            if (cantitateMere > 0)
            {
                // Scade cantitatea
                cantitateMere--;

                // Actualizează labelul cu cantitatea în kg
                Cantitate.Text = ActualizeazaCantitateText();

                // Dacă cantitatea ajunge la 0, elimină "Mere" din label3
                if (cantitateMere == 0)
                {
                    // Elimină "Mere" din textul label3 și virgula de dinaintea lui
                    label3.Text = label3.Text.Replace("Mere, ", "").Replace(", Mere", "").Replace("Mere", "").Trim();

                    // Verifică dacă mai sunt alte produse și actualizează mesajul de afișare
                    if (label3.Text == "Ați selectat:" || label3.Text == "")
                    {
                        label3.Text = "Ați selectat:"; // Dacă niciun produs nu a rămas, resetează la mesajul inițial
                    }
                }

                // Actualizează prețul total
                ActualizeazaPretTotal();
            }
            else
            {
                MessageBox.Show("Cantitatea nu poate fi mai mică de 0!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void label2_Click_2(object sender, EventArgs e)
        {

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OLD_Cantitate))
            {
                MessageBox.Show("Cosul de cumpărături este gol! Achiziționați produse înainte de a genera bonul.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Ieșim din funcție pentru a preveni continuarea
            }

            string cantitateCurata = OLD_Cantitate.Replace("Cantitate: ", "");
            string produseCurate = OLD_Produse.Replace("Ati selectat: ", "");

            string successData = db.StringGet("Cos_Cumparaturi_success");

            if (!string.IsNullOrEmpty(successData))
            {
                string[] parts = successData.Split('|');
                string successStatus = parts[0];
                string uid = parts[1];

                if (successStatus == "1")
                {
                    int pretTotal = int.Parse(OLD_pretTotal);
                    int reducere = 0;

                    // Calculăm reducerea dacă prețul total depășește 50 RON
                    if (pretTotal > 50)
                    {
                        reducere = 10; // Reducere de 10%
                        pretTotal = pretTotal - (pretTotal * reducere / 100); // Aplicăm reducerea
                    }

                    // Generăm bonul
                    string bonContent = $"--------------------------------------------------------------\n" +
                                        $"                          BON FISCAL\n" +
                                        $"--------------------------------------------------------------\n" +
                                        $"Nume magazin: Piață Complex\n" +
                                        $"Adresa: Aleea Studentilor, Camin 14C\n" +
                                        $"Telefon: +40 774 461 861\n" +
                                        $"Email: tatarmateiccc@gmail.com\n\n" +
                                        $"Data: {DateTime.Now.ToString("dd.MM.yyyy")}\n" +
                                        $"Ora: {DateTime.Now.ToString("HH:mm:ss")}\n" +
                                        $"--------------------------------------------------------------\n" +
                                        $"Produse:\n" +
                                        $"--------------------------------------------------------------\n";

                    string[] produse = cantitateCurata.Split(',');

                    foreach (var produs in produse)
                    {
                        string produsCurent = produs.Trim();
                        bonContent += $"| {produsCurent.PadRight(59)}|\n";
                    }

                    bonContent += $"--------------------------------------------------------------\n" +
                                  $"Subtotal: {OLD_pretTotal} RON\n" +
                                  $"Reducere: {reducere}%\n" +
                                  $"--------------------------------------------------------------\n" +
                                  $"Total: {pretTotal} RON\n" +
                                  $"\nModalitate plată: Card\n" +
                                  $"--------------------------------------------------------------\n" +
                                  $"    Vă mulțumim că ați ales magazinul nostru!\n" +
                                  $"          Vă așteptăm cu drag!\n" +
                                  $"--------------------------------------------------------------\n";

                    string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Bonuri Magazin");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string filePath = Path.Combine(folderPath, $"bon_{uid}.txt");

                    File.WriteAllText(filePath, bonContent);
                    MessageBox.Show("Bonul a fost salvat pe Desktop!", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Achiziția nu a fost finalizată cu succes!", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nu există date pentru a tipări bonul.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            db.StringSet("Cos_Cumparaturi_success", "");
        }


    }

}